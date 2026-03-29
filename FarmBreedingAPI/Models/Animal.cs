namespace FarmBreedingAPI.Models
{
    public class Animal
    {
        public string ATCode { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string ATCategoryCode { get; set; } = string.Empty;
        public DateTime? DOB { get; set; }
    }
}