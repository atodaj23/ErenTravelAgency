using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErenTravel3API.Models
{
    [Table("IssueReports")]
    public class IssueReport
    {
        public int Id { get; set; }

        [StringLength(150)]
        public string? ReporterEmail { get; set; }

        [Required]
        [StringLength(50)]
        public required string TargetType { get; set; }

        public int? TargetId { get; set; }

        [StringLength(150)]
        public string? AgencyName { get; set; }

        [Required]
        [StringLength(700)]
        public required string Reason { get; set; }

        [StringLength(40)]
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
