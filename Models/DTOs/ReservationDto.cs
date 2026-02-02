namespace ErenTravel3API.Models.DTOs
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public required string Emri { get; set; }
        public required string Email { get; set; }
        public required string Paketa { get; set; }
        public DateTime DataNisjes { get; set; }
        public DateTime DataKthimit { get; set; }
        public int NrPersonave { get; set; }
        public int NrDhomave { get; set; }
        public decimal Cmimi { get; set; }
        public bool Confirmed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}