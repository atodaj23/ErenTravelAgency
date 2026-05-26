using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErenTravel3API.Models
{
    [Table("Reviews")]
    public class Review
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public required string UserEmail { get; set; }

        [Required]
        [StringLength(100)]
        public required string UserName { get; set; }

        [Required]
        [StringLength(150)]
        public required string AgencyName { get; set; }

        public int? PackageId { get; set; }

        [Range(0, 5)]
        public int Rating { get; set; }

        [StringLength(700)]
        public string? Comment { get; set; }

        public bool IsReported { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
