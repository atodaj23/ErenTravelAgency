namespace ErenTravel3API.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public string? AgencyName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
