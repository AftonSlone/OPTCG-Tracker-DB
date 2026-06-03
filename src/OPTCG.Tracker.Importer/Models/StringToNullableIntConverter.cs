using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OPTCG.Tracker.Importer.Models;

/// <summary>
/// The API returns numeric card fields as JSON strings ("5000") or null,
/// and occasionally as raw numbers. This converter tolerates all three.
/// </summary>
public class StringToNullableIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetInt32();
            case JsonTokenType.String:
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                {
                    return null;
                }
                return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : null;
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing nullable int.");
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
