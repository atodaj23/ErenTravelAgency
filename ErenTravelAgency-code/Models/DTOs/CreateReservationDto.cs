using System.ComponentModel.DataAnnotations;

namespace ErenTravel3API.Models.DTOs
{
    public class CreateReservationDto
    {
        [Required(ErrorMessage = "Emri është i detyrueshëm")]
        [StringLength(100)]
        public required string Emri { get; set; }

        [Required(ErrorMessage = "Email është i detyrueshëm")]
        [EmailAddress(ErrorMessage = "Email i pavlefshëm")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Paketa është e detyrueshme")]
        [StringLength(200)]
        public required string Paketa { get; set; }

        public int? PackageId { get; set; }

        [Required(ErrorMessage = "Data e nisjes është e detyrueshme")]
        public DateTime DataNisjes { get; set; }

        [Required(ErrorMessage = "Data e kthimit është e detyrueshme")]
        public DateTime DataKthimit { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Numri i personave duhet të jetë të paktën 1")]
        public int NrPersonave { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Numri i dhomave duhet të jetë të paktën 1")]
        public int NrDhomave { get; set; }

        public string? Role { get; set; }
    }
}