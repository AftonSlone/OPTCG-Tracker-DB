using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OPTCG.Tracker.Data.Data;
using OPTCG.Tracker.Data.Models;

namespace OPTCG.Tracker.Importer.Services;

public class ImageDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly TrackerDbContext _db;
    private readonly ILogger<ImageDownloadService> _logger;
    private readonly string _outputDir;

    // Throttle like OptcgApiClient to be a good citizen
    private readonly TimeSpan _requestDelay = TimeSpan.FromMilliseconds(750);
    private const int MaxRetries = 3;

    public ImageDownloadService(
        HttpClient httpClient,
        TrackerDbContext db,
        ILogger<ImageDownloadService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _logger = logger;

        var outputDir = Environment.GetEnvironmentVariable("IMAGE_OUTPUT_DIR") ?? "/card-images";
        _outputDir = outputDir.TrimEnd('/');
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // Only process cards where CardImageUrl is still a remote URL (not yet migrated)
        var cardsToMigrate = await _db.Cards
            .Where(c => !string.IsNullOrEmpty(c.CardImageUrl) && c.CardImageUrl.StartsWith("http"))
            .ToListAsync(cancellationToken);

        if (cardsToMigrate.Count == 0)
        {
            _logger.LogInformation("No cards with remote image URLs found to migrate.");
            return;
        }

        _logger.LogInformation("Found {Count} cards with remote image URLs to migrate.", cardsToMigrate.Count);

        var updated = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var card in cardsToMigrate)
        {
            if (string.IsNullOrEmpty(card.SetId))
            {
                _logger.LogWarning("Skipping card {CardImageId}: SetId is null, cannot determine subfolder.", card.CardImageId);
                failed++;
                continue;
            }

            try
            {
                var success = await DownloadAndUpdateCardAsync(card, cancellationToken);
                if (success)
                {
                    updated++;
                }
                else
                {
                    skipped++;
                }

                // Throttle between downloads
                await Task.Delay(_requestDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download image for card {CardImageId}", card.CardImageId);
                failed++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Image migration complete. Updated: {Updated}, Skipped (already exists): {Skipped}, Failed: {Failed}.",
            updated, skipped, failed);
    }

    private async Task<bool> DownloadAndUpdateCardAsync(Card card, CancellationToken cancellationToken)
    {
        var sourceUrl = card.CardImageUrl!;
        var extension = GetExtensionFromUrl(sourceUrl);
        var relativePath = $"card-images/{card.SetId}/{card.CardImageId}.{extension}";
        var localPath = $"{_outputDir}/{card.SetId}/{card.CardImageId}.{extension}";

        // Check if file already exists
        if (File.Exists(localPath))
        {
            _logger.LogDebug("Image already exists for {CardImageId}: {Path}", card.CardImageId, relativePath);
            card.CardImageUrl = relativePath;
            return false; // Skipped (already exists)
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Download with retries
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Downloading image for {CardImageId} (attempt {Attempt}/{Max}): {Url}",
                    card.CardImageId, attempt, MaxRetries, sourceUrl);

                using var response = await _httpClient.GetAsync(sourceUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fileStream, cancellationToken);

                _logger.LogInformation("Downloaded image for {CardImageId}: {Path}", card.CardImageId, relativePath);
                card.CardImageUrl = relativePath;
                return true; // Updated
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                var wait = TimeSpan.FromSeconds(2 * attempt);
                _logger.LogWarning(ex, "Error downloading image for {CardImageId}; retrying in {Wait}s.",
                    card.CardImageId, wait.TotalSeconds);
                await Task.Delay(wait, cancellationToken);
            }
        }

        _logger.LogError("Failed to download image for {CardImageId} after {Max} attempts.", card.CardImageId, MaxRetries);
        return false;
    }

    private static string GetExtensionFromUrl(string url)
    {
        var uri = new Uri(url);
        var path = uri.AbsolutePath;
        var lastDot = path.LastIndexOf('.');
        return lastDot > 0 ? path[(lastDot + 1)..] : "png"; // Default to png if no extension
    }
}
