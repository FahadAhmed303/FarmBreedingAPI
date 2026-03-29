namespace FarmBreedingAPI.Models
{
    public class GrowthRecord
    {
        public string ATCode { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal Height { get; set; }
        public decimal Width { get; set; }
        public DateTime RecordDate { get; set; }
    }
}