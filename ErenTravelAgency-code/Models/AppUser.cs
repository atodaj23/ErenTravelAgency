using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErenTravel3API.Models
{
    [Table("AppUsers")]
    public class AppUser
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public required string Email { get; set; }

        [Required]
        [StringLength(100)]
        public required string Password { get; set; }

        [Required]
        [StringLength(30)]
        public required string Role { get; set; } // Client, TravelAgent, B2B

        [StringLength(150)]
        public string? AgencyName { get; set; }

        [StringLength(80)]
        public string? TaxId { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(250)]
        public string? SecurityQuestion { get; set; }

        [StringLength(100)]
        public string? SecurityAnswerHash { get; set; }

        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
