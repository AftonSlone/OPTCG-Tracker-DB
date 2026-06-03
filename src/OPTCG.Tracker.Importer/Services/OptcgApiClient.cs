using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OPTCG.Tracker.Importer.Models;

namespace OPTCG.Tracker.Importer.Services;

public class OptcgApiClient
{
    // Bulk "all" endpoints used to populate the database. Don!! cards are intentionally excluded.
    public static readonly string[] CardEndpoints =
    {
        "api/allSetCards/",
        "api/allSTCards/",
        "api/allPromos/"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<OptcgApiClient> _logger;

    // Be a good citizen: the API runs on a personal VPS with daily limits.
    private readonly TimeSpan _requestDelay = TimeSpan.FromMilliseconds(750);
    private const int MaxRetries = 3;

    public OptcgApiClient(HttpClient httpClient, ILogger<OptcgApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CardDto>> GetCardsAsync(string endpoint, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Fetching {Endpoint} (attempt {Attempt}/{Max})", endpoint, attempt, MaxRetries);

                using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    var wait = TimeSpan.FromSeconds(5 * attempt);
                    _logger.LogWarning("Rate limited on {Endpoint}; waiting {Wait}s before retry.", endpoint, wait.TotalSeconds);
                    await Task.Delay(wait, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var cards = await JsonSerializer.DeserializeAsync<List<CardDto>>(stream, JsonOptions, cancellationToken)
                            ?? new List<CardDto>();

                _logger.LogInformation("Fetched {Count} cards from {Endpoint}", cards.Count, endpoint);

                // Throttle between successful calls.
                await Task.Delay(_requestDelay, cancellationToken);
                return cards;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException && attempt < MaxRetries)
            {
                var wait = TimeSpan.FromSeconds(2 * attempt);
                _logger.LogWarning(ex, "Error fetching {Endpoint}; retrying in {Wait}s.", endpoint, wait.TotalSeconds);
                await Task.Delay(wait, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Failed to fetch {endpoint} after {MaxRetries} attempts.");
    }
}
