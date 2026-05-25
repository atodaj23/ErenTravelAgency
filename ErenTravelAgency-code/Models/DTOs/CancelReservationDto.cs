using System.ComponentModel.DataAnnotations;

namespace ErenTravel3API.Models.DTOs
{
    public class CancelReservationDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }
}
