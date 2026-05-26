using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErenTravel3API.Models
{
    [Table("Packages")]
    public class Package
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Column("Paketa")]
        public required string Paketa { get; set; }

        [Column("Kohezgjatja")]
        public int Kohezgjatja { get; set; }

        [Required]
        [Column("Cmimi")]
        public decimal Cmimi { get; set; }

        [Column("TravelDate")]
        public DateTime? TravelDate { get; set; }

        [StringLength(500)]
        [Column("Pershkrimi")]
        public string? Pershkrimi { get; set; }

        [StringLength(150)]
        [Column("AgencyName")]
        public string? AgencyName { get; set; }

        [StringLength(500)]
        [Column("ImageUrl")]
        public string? ImageUrl { get; set; }

        [Column("MaxPersons")]
        public int MaxPersons { get; set; } = 150;

        [Column("IsAvailable")]
        public bool IsAvailable { get; set; } = true;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}