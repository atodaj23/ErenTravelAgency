using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErenTravel3API.Models
{
    [Table("AgencyFollows")]
    public class AgencyFollow
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public required string UserEmail { get; set; }

        [Required]
        [StringLength(150)]
        public required string AgencyName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
