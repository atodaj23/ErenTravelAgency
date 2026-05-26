namespace ErenTravel3API.Models.DTOs
{
    public class PackageDto
    {
        public int Id { get; set; }
        public required string Paketa { get; set; }
        public int Kohezgjatja { get; set; }
        public decimal Cmimi { get; set; }
        public DateTime? TravelDate { get; set; }
        public decimal? CmimiOrigjinal { get; set; }
        public decimal? CmimiB2B { get; set; }
        public bool KaZbritjeB2B { get; set; }
        public string? Pershkrimi { get; set; }
        public string? AgencyName { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int MaxPersons { get; set; }
        public int ReservedPersons { get; set; }
        public int RemainingPersons { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class CreatePackageDto
    {
        public required string Paketa { get; set; }
        public int Kohezgjatja { get; set; }
        public decimal Cmimi { get; set; }
        public DateTime? TravelDate { get; set; }
        public string? Pershkrimi { get; set; }
        public string? AgencyName { get; set; }
        public string? ImageUrl { get; set; }
        public int MaxPersons { get; set; } = 150;
    }

    public class UpdatePackageCapacityDto
    {
        public int MaxPersons { get; set; } = 150;
    }
}
