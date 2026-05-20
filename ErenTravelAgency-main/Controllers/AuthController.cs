using ErenTravel3API.Data;
using ErenTravel3API.Models;
using ErenTravel3API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErenTravel3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TravelDbContext _context;
        private const string AdminEmail = "admin@grupierenit.com";
        private const string AdminPassword = "Grupi123";

        public AuthController(TravelDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterUserDto dto)
        {
            var role = NormalizeRole(dto.Role);
            if (role == null || role == "Administrator")
                return BadRequest(new { message = "Roli i zgjedhur nuk është i vlefshëm" });

            if (await _context.AppUsers.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Ky email është regjistruar më parë" });

            if ((role == "B2B" || role == "TravelAgent") &&
                (string.IsNullOrWhiteSpace(dto.AgencyName) || string.IsNullOrWhiteSpace(dto.TaxId)))
            {
                return BadRequest(new { message = "Për B2B dhe Travel Agent kërkohet emri i agjencisë dhe Tax ID" });
            }

            var user = new AppUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Password = dto.Password,
                Role = role,
                AgencyName = dto.AgencyName,
                TaxId = dto.TaxId,
                Address = dto.Address,
                Phone = dto.Phone,
                IsApproved = false
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            return Ok(ToDto(user));
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
        {
            var role = NormalizeRole(dto.Role);
            if (role == null)
                return BadRequest(new { message = "Roli nuk është i vlefshëm" });

            if (role == "Administrator")
            {
                if (dto.Email == AdminEmail && dto.Password == AdminPassword)
                {
                    return Ok(new LoginResponseDto
                    {
                        Message = "Login i suksesshëm si Administrator",
                        Role = "Administrator",
                        Email = AdminEmail,
                        FullName = "Administrator"
                    });
                }

                return Unauthorized(new { message = "Kredencialet e administratorit janë gabim" });
            }

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Password == dto.Password && u.Role == role);

            if (user == null)
                return Unauthorized(new { message = "Email, password ose rol i pasaktë" });

            if (!user.IsApproved)
                return Unauthorized(new { message = "Llogaria juaj është në pritje. Duhet të aprovohet nga administratori." });

            return Ok(new LoginResponseDto
            {
                Message = $"Login i suksesshëm si {user.Role}",
                Role = user.Role,
                Email = user.Email,
                FullName = user.FullName,
                UserId = user.Id,
                AgencyName = user.AgencyName
            });
        }

        private static string? NormalizeRole(string role)
        {
            var value = role.Trim().ToLower();
            return value switch
            {
                "client" or "traveler" or "klient" => "Client",
                "travelagent" or "travel agent" or "agency" or "agjenci" => "TravelAgent",
                "administrator" or "admin" => "Administrator",
                "b2b" or "b2b user" or "business" => "B2B",
                _ => null
            };
        }

        private static UserDto ToDto(AppUser user) => new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AgencyName = user.AgencyName,
            TaxId = user.TaxId,
            Address = user.Address,
            Phone = user.Phone,
            IsApproved = user.IsApproved,
            CreatedAt = user.CreatedAt
        };
    }
}
