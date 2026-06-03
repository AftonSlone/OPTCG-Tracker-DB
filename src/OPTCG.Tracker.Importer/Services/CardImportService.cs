using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OPTCG.Tracker.Data.Data;
using OPTCG.Tracker.Data.Models;
using OPTCG.Tracker.Importer.Models;

namespace OPTCG.Tracker.Importer.Services;

public class CardImportService
{
    private readonly OptcgApiClient _apiClient;
    private readonly TrackerDbContext _db;
    private readonly ILogger<CardImportService> _logger;

    public CardImportService(OptcgApiClient apiClient, TrackerDbContext db, ILogger<CardImportService> logger)
    {
        _apiClient = apiClient;
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying database migrations...");
        await _db.Database.MigrateAsync(cancellationToken);

        // 1. Fetch from all endpoints. A single failing endpoint should not abort the
        // whole import, but if every endpoint fails we surface an error.
        var fetched = new List<CardDto>();
        var succeededEndpoints = 0;
        foreach (var endpoint in OptcgApiClient.CardEndpoints)
        {
            try
            {
                var cards = await _apiClient.GetCardsAsync(endpoint, cancellationToken);
                fetched.AddRange(cards);
                succeededEndpoints++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skipping endpoint {Endpoint} after repeated failures.", endpoint);
            }
        }

        if (succeededEndpoints == 0)
        {
            throw new InvalidOperationException("All card endpoints failed; aborting import.");
        }

        // 2. Dedupe across endpoints on the true natural key (CardImageId).
        var deduped = fetched
            .Where(c => !string.IsNullOrWhiteSpace(c.CardImageId))
            .GroupBy(c => c.CardImageId!)
            .Select(g => g.Last())
            .ToList();

        _logger.LogInformation("Fetched {Total} rows, {Unique} unique by CardImageId.", fetched.Count, deduped.Count);

        // 3. Load existing cards keyed by CardImageId for upsert.
        var existing = await _db.Cards.ToDictionaryAsync(c => c.CardImageId, cancellationToken);

        var now = DateTime.UtcNow;
        var inserted = 0;
        var updated = 0;

        foreach (var dto in deduped)
        {
            if (existing.TryGetValue(dto.CardImageId!, out var card))
            {
                ApplyDto(card, dto);
                card.UpdatedAt = now;
                updated++;
            }
            else
            {
                card = new Card
                {
                    CardImageId = dto.CardImageId!,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                ApplyDto(card, dto);
                _db.Cards.Add(card);
                inserted++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var total = await _db.Cards.CountAsync(cancellationToken);
        _logger.LogInformation("Import complete. Inserted {Inserted}, updated {Updated}. Total cards: {Total}.",
            inserted, updated, total);
    }

    private static void ApplyDto(Card card, CardDto dto)
    {
        card.CardSetId = dto.CardSetId ?? string.Empty;
        card.CardName = dto.CardName ?? string.Empty;
        card.SetName = dto.SetName;
        card.SetId = dto.SetId;
        card.CardType = dto.CardType;
        card.CardColor = dto.CardColor;
        card.Rarity = dto.Rarity;
        card.Life = dto.Life;
        card.CardCost = dto.CardCost;
        card.CardPower = dto.CardPower;
        card.CounterAmount = dto.CounterAmount;
        card.Attribute = dto.Attribute;
        card.SubTypes = dto.SubTypes;
        card.CardText = dto.CardText;
        card.CardImageUrl = dto.CardImage;
    }
}
