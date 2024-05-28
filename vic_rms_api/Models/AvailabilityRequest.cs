using System.Globalization;
using System.Text.Json;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace vic_rms_api.Models
{

    public class AvailabilityRequest
    {
        [JsonPropertyName("adults")]
        public int? Adults { get; set; }

        [JsonPropertyName("agentId")]
        public int? AgentId { get; set; }

        [JsonPropertyName("categoryIds")]
        //public List<int>? CategoryIds { get; set; }
        public int[]? CategoryIds { get; set; }

        [JsonPropertyName("children")]
        public int? Children { get; set; }

        [JsonPropertyName("dateFrom")]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonDateTimeNullableConverter))]
        public DateTime? DateFrom { get; set; }

        [JsonPropertyName("dateTo")]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonDateTimeNullableConverter))]
        public DateTime? DateTo { get; set; }

        [JsonPropertyName("infants")]
        public int? Infants { get; set; }

        [JsonPropertyName("propertyId")]
        public int? PropertyId { get; set; }

        [JsonPropertyName("rateIds")]
        //public List<int>? RateIds { get; set; }
        public int[]? RateIds { get; set; }
    }

    public class JsonDateTimeNullableConverter : System.Text.Json.Serialization.JsonConverter<DateTime?>
    {
        private const string _formatWithTime = "yyyy-MM-dd HH:mm:ss";
        private const string _formatDateOnly = "yyyy-MM-dd";

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            string dateString = reader.GetString();
            if (DateTime.TryParseExact(dateString, _formatWithTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime;
            }
            else if (DateTime.TryParseExact(dateString, _formatDateOnly, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateOnly))
            {
                return dateOnly;
            }
            else
            {
                throw new System.Text.Json.JsonException($"Unable to parse the date string '{dateString}' to DateTime.");
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString(value.Value.TimeOfDay == TimeSpan.Zero ? _formatDateOnly : _formatWithTime));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
