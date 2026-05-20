using System.ComponentModel.DataAnnotations;

namespace ErenTravel3API.Models.DTOs
{
    public class RegisterUserDto
    {
        [Required(ErrorMessage = "Emri është i detyrueshëm")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Email është i detyrueshëm")]
        [EmailAddress(ErrorMessage = "Email i pavlefshëm")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password është i detyrueshëm")]
        [MinLength(6, ErrorMessage = "Password duhet të ketë të paktën 6 karaktere")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Roli është i detyrueshëm")]
        public required string Role { get; set; }

        public string? AgencyName { get; set; }
        public string? TaxId { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }

        [Required]
        public required string Role { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public string? AgencyName { get; set; }
        public string? TaxId { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LoginResponseDto
    {
        public required string Message { get; set; }
        public required string Role { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public int? UserId { get; set; }
        public string? AgencyName { get; set; }
    }
}
