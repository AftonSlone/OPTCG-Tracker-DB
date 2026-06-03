namespace OPTCG.Tracker.Data.Models;

public class Card
{
    public Guid Id { get; set; }

    public string CardSetId { get; set; } = string.Empty;

    public string CardName { get; set; } = string.Empty;

    public string? SetName { get; set; }

    public string? SetId { get; set; }

    public string? CardType { get; set; }

    public string? CardColor { get; set; }

    public string? Rarity { get; set; }

    public int? Life { get; set; }

    public int? CardCost { get; set; }

    public int? CardPower { get; set; }

    public int? CounterAmount { get; set; }

    public string? Attribute { get; set; }

    public string? SubTypes { get; set; }

    public string? CardText { get; set; }

    public string? CardImageUrl { get; set; }

    public string CardImageId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
