using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErenTravel3API.Models
{
    [Table("Reservations")]
    public class Reservation
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public int PackageId { get; set; }

        [Required]
        [Column("DataNisjes")]
        public DateTime DataNisjes { get; set; }

        [Required]
        [Column("DataKthimit")]
        public DateTime DataKthimit { get; set; }

        [Required]
        [Range(1, 100)]
        [Column("NrPersonave")]
        public int NrPersonave { get; set; }

        [Required]
        [Range(1, 50)]
        [Column("NrDhomave")]
        public int NrDhomave { get; set; }

        [Required]
        [Column("Cmimi")]
        public decimal Cmimi { get; set; }

        [Column("IsConfirmed")]
        public bool IsConfirmed { get; set; } = false;

        [Column("IsCancelled")]
        public bool IsCancelled { get; set; } = false;

        [Column("CancelledAt")]
        public DateTime? CancelledAt { get; set; }

        [Column("CancellationReason")]
        [StringLength(500)]
        public string? CancellationReason { get; set; }

        [Column("CancellationFee")]
        public decimal CancellationFee { get; set; } = 0;

        [Column("RefundAmount")]
        public decimal RefundAmount { get; set; } = 0;

        [Column("RefundStatus")]
        [StringLength(50)]
        public string? RefundStatus { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public required Customer Customer { get; set; }
        public required Package Package { get; set; }
    }
}