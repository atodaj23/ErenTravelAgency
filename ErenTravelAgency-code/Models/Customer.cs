using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErenTravel3API.Models
{
    [Table("Customers")]
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Column("Emri")]
        public required string Emri { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Column("Email")]
        public required string Email { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}