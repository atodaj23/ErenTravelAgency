namespace ErenTravel3API.Models.DTOs
{
    public class PackageDto
    {
        public int Id { get; set; }
        public required string Paketa { get; set; }
        public int Kohezgjatja { get; set; }
        public decimal Cmimi { get; set; }
        public string? Pershkrimi { get; set; }
        public bool IsAvailable { get; set; }
    }
}