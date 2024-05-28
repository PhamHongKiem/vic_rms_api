using System.ComponentModel.DataAnnotations;

namespace vic_rms_api.Models
{
    public class wp_rates
    {
        [Key]
        public int RMS_rateID { get; set; }
        public int? RMS_roomtypeID { get; set; }
        public int? RMS_categoryID { get; set; }
        public int? RMS_propertyID { get; set; }
        public string? Rate_Name { get; set; }
    }
    public class wp_rates_grid
    {
        [Key]
        public int RateID { get; set; }
        public int? RMS_rateID { get; set; }
        public int? RMS_roomtypeID { get; set; }
        public int? RMS_propertyID { get; set; }
        public string? Rate_Descriptions { get; set; }
        public DateTime? Datesell { get; set; }
        public int? DailyRate { get; set; }
        public int? Baserate { get; set; }
        public int? Active_Status { get; set; } = 1;
        public int? RoomAvailable { get; set; } = 1;
        // Optionally, track when records are created or updated
        public DateTime? Created_Date { get; set; }
        public DateTime? Updated_Date { get; set; }
    }
    public class wp_hotels
    {
        [Key]
        public int hotelID { get; set; }
        public string? hotel_code { get; set; }
        public string? hotel_name { get; set; }
        public string? descriptions { get; set; }
        public int? active_status { get; set; }
        public string? RMS_propertyID { get; set; }
        public string? RMS_clientID { get; set; }
    }
}
