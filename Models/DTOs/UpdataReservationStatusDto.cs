using System.ComponentModel.DataAnnotations;

namespace ErenTravel3API.Models.DTOs
{
    public class UpdateReservationStatusDto
    {
        [Required]
        public bool IsConfirmed { get; set; }
    }
}
