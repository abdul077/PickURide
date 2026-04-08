using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PickURide.API.JsonConverters
{
    /// <summary>
    /// Allows TimeOnly to be read from common formats (e.g. "HH:mm", "HH:mm:ss", "hh:mm tt")
    /// and written in a friendly 12-hour format ("hh:mm tt").
    /// </summary>
    public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
    {
        private static readonly string[] ReadFormats =
        {
            "HH:mm",
            "HH:mm:ss",
            "H:mm",
            "H:mm:ss",
            "hh:mm tt",
            "h:mm tt",
            "hh:mm:ss tt",
            "h:mm:ss tt"
        };

        private readonly string _writeFormat;

        public TimeOnlyJsonConverter(string writeFormat = "hh:mm tt")
        {
            _writeFormat = writeFormat;
        }

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string for TimeOnly but got {reader.TokenType}.");
            }

            var raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return default;
            }

            raw = raw.Trim();

            // Fast path: TimeOnly.Parse can handle some inputs, but not reliably with AM/PM across cultures.
            if (TimeOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }

            if (TimeOnly.TryParseExact(raw, ReadFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed;
            }

            throw new JsonException($"Invalid TimeOnly value '{raw}'. Expected formats like HH:mm or hh:mm AM/PM.");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_writeFormat, CultureInfo.InvariantCulture));
        }
    }
}

