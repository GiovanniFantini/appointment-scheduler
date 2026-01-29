using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppointmentScheduler.API.Converters;

/// <summary>
/// JSON converter per TimeSpan che supporta i formati:
/// - "HH:mm:ss" (es: "09:00:00")
/// - "HH:mm" (es: "09:00")
/// - Format TimeSpan standard
/// </summary>
public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrEmpty(value))
            return TimeSpan.Zero;

        // Try parsing as TimeSpan (handles "HH:mm:ss" format)
        if (TimeSpan.TryParse(value, out var result))
            return result;

        // Try parsing with custom formats
        if (TimeSpan.TryParseExact(value, @"hh\:mm\:ss", null, out result))
            return result;

        if (TimeSpan.TryParseExact(value, @"hh\:mm", null, out result))
            return result;

        throw new JsonException($"Unable to parse TimeSpan from value: {value}");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        // Write in "HH:mm:ss" format for consistency
        writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
    }
}
