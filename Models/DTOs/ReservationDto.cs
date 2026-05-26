namespace ErenTravel3API.Models.DTOs
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public int PackageId { get; set; }
        public required string Emri { get; set; }
        public required string Email { get; set; }
        public required string Paketa { get; set; }
        public string? AgencyName { get; set; }
        public DateTime DataNisjes { get; set; }
        public DateTime DataKthimit { get; set; }
        public int NrPersonave { get; set; }
        public int NrDhomave { get; set; }
        public decimal Cmimi { get; set; }
        public bool Confirmed { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public decimal CancellationFee { get; set; }
        public decimal RefundAmount { get; set; }
        public string? RefundStatus { get; set; }
        public bool CanCancel { get; set; }
        public string? CancellationPolicyMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
