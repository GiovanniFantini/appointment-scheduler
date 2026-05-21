using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppointmentScheduler.API.Converters;

/// <summary>
/// JSON converter per <see cref="TimeOnly"/>? che tratta la stringa vuota e il
/// JSON null come <c>null</c>.
///
/// Senza questo converter una stringa vuota ("") inviata per un campo
/// TimeOnly? — caso che si verifica quando un input &lt;input type="time"&gt;
/// del frontend viene svuotato — provoca una JsonException e un 400 con un
/// messaggio tecnico incomprensibile. Qui viene normalizzata a null, coerente
/// con la convenzione di dominio "orario assente = tutto il giorno".
///
/// Accetta i formati "HH:mm" e "HH:mm:ss". In scrittura emette "HH:mm:ss".
/// </summary>
public sealed class NullableTimeOnlyJsonConverter : JsonConverter<TimeOnly?>
{
    private static readonly string[] AcceptedFormats = { "HH:mm:ss", "HH:mm" };

    public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (TimeOnly.TryParseExact(value, AcceptedFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
            return result;

        // Fallback sul parsing libero (gestisce eventuali varianti di cultura).
        if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            return result;

        throw new JsonException($"Valore orario non valido: '{value}'.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        else
            writer.WriteNullValue();
    }
}
