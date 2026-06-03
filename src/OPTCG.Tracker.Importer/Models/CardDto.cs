using System.Text.Json.Serialization;

namespace OPTCG.Tracker.Importer.Models;

/// <summary>
/// Mirrors the optcgapi.com JSON shape. Numeric fields arrive as JSON strings
/// (e.g. "5000") or null, so they are converted via <see cref="StringToNullableIntConverter"/>.
/// Price fields from the API are intentionally ignored.
/// </summary>
public class CardDto
{
    [JsonPropertyName("card_name")]
    public string? CardName { get; set; }

    [JsonPropertyName("set_name")]
    public string? SetName { get; set; }

    [JsonPropertyName("card_text")]
    public string? CardText { get; set; }

    [JsonPropertyName("set_id")]
    public string? SetId { get; set; }

    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("card_set_id")]
    public string? CardSetId { get; set; }

    [JsonPropertyName("card_color")]
    public string? CardColor { get; set; }

    [JsonPropertyName("card_type")]
    public string? CardType { get; set; }

    [JsonPropertyName("life")]
    [JsonConverter(typeof(StringToNullableIntConverter))]
    public int? Life { get; set; }

    [JsonPropertyName("card_cost")]
    [JsonConverter(typeof(StringToNullableIntConverter))]
    public int? CardCost { get; set; }

    [JsonPropertyName("card_power")]
    [JsonConverter(typeof(StringToNullableIntConverter))]
    public int? CardPower { get; set; }

    [JsonPropertyName("sub_types")]
    public string? SubTypes { get; set; }

    [JsonPropertyName("counter_amount")]
    [JsonConverter(typeof(StringToNullableIntConverter))]
    public int? CounterAmount { get; set; }

    [JsonPropertyName("attribute")]
    public string? Attribute { get; set; }

    [JsonPropertyName("card_image_id")]
    public string? CardImageId { get; set; }

    [JsonPropertyName("card_image")]
    public string? CardImage { get; set; }
}
