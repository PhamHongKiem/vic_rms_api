using Newtonsoft.Json;

namespace vic_rms_api.Models
{
    public class AvailabilityResponse
    {
        [JsonProperty("categories")]
        public List<Category> Categories { get; set; } = new List<Category>();
    }

    public class Category
    {
        [JsonProperty("categoryId")]
        public int CategoryId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("rates")]
        public List<Rate> Rates { get; set; } = new List<Rate>();
    }

    public class Rate
    {
        [JsonProperty("rateId")]
        public int RateId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("personBase")]
        public int PersonBase { get; set; }

        [JsonProperty("dayBreakdown")]
        public List<DayBreakdown>? DayBreakdown { get; set; }
    }

    public class DayBreakdown
    {
        [JsonProperty("availableAreas")]
        public int AvailableAreas { get; set; }

        [JsonProperty("closedOnArrival")]
        public bool ClosedOnArrival { get; set; }

        [JsonProperty("closedOnDeparture")]
        public bool ClosedOnDeparture { get; set; }

        [JsonProperty("dailyRate")]
        public decimal DailyRate { get; set; }

        [JsonProperty("theDate")]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonDateTimeNullableConverter))]
        public DateTime? TheDate { get; set; }

        [JsonProperty("minStay")]
        public int? MinStay { get; set; }

        [JsonProperty("minStayOnArrival")]
        public int? MinStayOnArrival { get; set; }

        [JsonProperty("maxStay")]
        public int? MaxStay { get; set; }

        [JsonProperty("stopSell")]
        public bool StopSell { get; set; }
    }

}
