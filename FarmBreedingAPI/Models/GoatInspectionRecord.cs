using System;

namespace FarmBreedingAPI.Models
{
    public class GoatInspectionRecord
    {
        public int AnimalNo { get; set; }

        public decimal? Weight { get; set; }

        public int? Teeth { get; set; }

        public bool? Nails { get; set; }

        public bool? Hair { get; set; }

        public DateTime RecordDate { get; set; }
    }
}
