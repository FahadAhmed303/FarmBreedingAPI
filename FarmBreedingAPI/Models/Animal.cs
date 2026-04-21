namespace FarmBreedingAPI.Models
{
    public class Animal
    {
        public string ATCode { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string ATCategoryCode { get; set; } = string.Empty;
        public DateTime? DOB { get; set; }

        // 🔥 OPTIONAL FIELDS (FIXED)
        public string? SourceType { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal? Price { get; set; }
        public string? AgentName { get; set; }
        public string? MotherCode { get; set; }
    }
}