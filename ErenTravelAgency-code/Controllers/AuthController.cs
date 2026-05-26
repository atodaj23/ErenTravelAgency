using ErenTravel3API.Data;
using ErenTravel3API.Models;
using ErenTravel3API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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
            {
                await SaveActivityLogAsync("Register", "Failed", dto.Email, dto.Role, dto.AgencyName, "Tentativë regjistrimi me rol të pavlefshëm", "Invalid role");
                return BadRequest(new { message = "Roli i zgjedhur nuk është i vlefshëm" });
            }

            if (string.IsNullOrWhiteSpace(dto.SecurityQuestion) || string.IsNullOrWhiteSpace(dto.SecurityAnswer))
            {
                await SaveActivityLogAsync("Register", "Failed", dto.Email, role, dto.AgencyName, "Tentativë regjistrimi pa pyetje/përgjigje sigurie", "Missing security question");
                return BadRequest(new { message = "Zgjidh pyetjen e sigurisë dhe vendos përgjigjen. Kjo kërkohet për forgot password." });
            }

            if (await _context.AppUsers.AnyAsync(u => u.Email == dto.Email))
            {
                await SaveActivityLogAsync("Register", "Failed", dto.Email, role, dto.AgencyName, "Tentativë regjistrimi me email ekzistues", "Duplicate email");
                return BadRequest(new { message = "Ky email është regjistruar më parë" });
            }

            if ((role == "B2B" || role == "TravelAgent") &&
                (string.IsNullOrWhiteSpace(dto.AgencyName) || string.IsNullOrWhiteSpace(dto.TaxId)))
            {
                await SaveActivityLogAsync("Register", "Failed", dto.Email, role, dto.AgencyName, "Tentativë regjistrimi pa të dhënat e kërkuara të agjencisë", "Missing agency/tax data");
                return BadRequest(new { message = "Për B2B dhe Travel Agent kërkohet emri i agjencisë dhe Tax ID" });
            }

            var user = new AppUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Password = HashPassword(dto.Password),
                Role = role,
                AgencyName = dto.AgencyName,
                TaxId = dto.TaxId,
                Address = dto.Address,
                Phone = dto.Phone,
                SecurityQuestion = dto.SecurityQuestion.Trim(),
                SecurityAnswerHash = HashPassword(NormalizeSecurityAnswer(dto.SecurityAnswer)),
                IsApproved = false
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            await SaveActivityLogAsync("Register", "Success", user.Email, user.Role, user.AgencyName, "User i ri u regjistrua dhe pret aprovim nga admini");

            return Ok(ToDto(user));
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
        {
            var role = NormalizeRole(dto.Role);
            if (role == null)
            {
                await SaveActivityLogAsync("Login", "Failed", dto.Email, dto.Role, null, "Tentativë login me rol të pavlefshëm", "Invalid role");
                return BadRequest(new { message = "Roli nuk është i vlefshëm" });
            }

            if (role == "Administrator")
            {
                if (dto.Email == AdminEmail && dto.Password == AdminPassword)
                {
                    await SaveActivityLogAsync("Login", "Success", AdminEmail, "Administrator", null, "Admin u logua me sukses");
                    return Ok(new LoginResponseDto
                    {
                        Message = "Login i suksesshëm si Administrator",
                        Role = "Administrator",
                        Email = AdminEmail,
                        FullName = "Administrator"
                    });
                }

                await SaveActivityLogAsync("Login", "Failed", dto.Email, "Administrator", null, "Tentativë login admin e pasuksesshme", "Wrong admin credentials");
                return Unauthorized(new { message = "Kredencialet e administratorit janë gabim" });
            }

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Role == role);

            if (user == null || !VerifyPassword(dto.Password, user.Password))
            {
                await SaveActivityLogAsync("Login", "Failed", dto.Email, role, null, "Tentativë login e pasuksesshme", "Wrong email/password/role");
                return Unauthorized(new { message = "Email, password ose rol i pasaktë" });
            }

            if (!user.IsApproved)
            {
                await SaveActivityLogAsync("Login", "Failed", user.Email, user.Role, user.AgencyName, "Tentativë login nga user i paaprovuar", "Account pending approval");
                return Unauthorized(new { message = "Llogaria juaj është në pritje. Duhet të aprovohet nga administratori." });
            }

            await SaveActivityLogAsync("Login", "Success", user.Email, user.Role, user.AgencyName, $"Login i suksesshëm si {user.Role}");

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



        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var role = NormalizeRole(dto.Role);
            if (role == null || role == "Administrator")
            {
                await SaveActivityLogAsync("PasswordReset", "Failed", dto.Email, dto.Role, null, "Tentativë reset password për admin ose rol të pavlefshëm", "Invalid/admin role");
                return BadRequest(new { message = "Forgot password lejohet vetëm për Client, B2B dhe Travel Agent. Admin nuk mund ta ndryshojë password-in nga kjo formë." });
            }

            if (string.IsNullOrWhiteSpace(dto.SecurityQuestion) || string.IsNullOrWhiteSpace(dto.SecurityAnswer))
            {
                await SaveActivityLogAsync("PasswordReset", "Failed", dto.Email, role, null, "Tentativë reset password pa pyetje ose përgjigje sigurie", "Missing security question/answer");
                return BadRequest(new { message = "Zgjidh pyetjen e sigurisë dhe vendos përgjigjen." });
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                await SaveActivityLogAsync("PasswordReset", "Failed", dto.Email, role, null, "Tentativë reset password me password të dobët", "Weak password");
                return BadRequest(new { message = "Password i ri duhet të ketë të paktën 6 karaktere." });
            }

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == dto.Email && u.Role == role);
            if (user == null)
            {
                await SaveActivityLogAsync("PasswordReset", "Failed", dto.Email, role, null, "Tentativë reset password për user që nuk ekziston", "User not found");
                return NotFound(new { message = "Nuk u gjet user me këtë email dhe rol." });
            }

            if (string.IsNullOrWhiteSpace(user.SecurityQuestion) || string.IsNullOrWhiteSpace(user.SecurityAnswerHash))
            {
                await SaveActivityLogAsync("PasswordReset", "Failed", user.Email, user.Role, user.AgencyName, "Tentativë reset password për llogari pa pyetje sigurie", "No security question configured");
                return BadRequest(new { message = "Kjo llogari nuk ka pyetje sigurie të regjistruar. Kontakto administratorin." });
            }

            if (!string.Equals(user.SecurityQuestion.Trim(), dto.SecurityQuestion.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                await SaveActivityLogAsync("PasswordReset", "Failed", user.Email, user.Role, user.AgencyName, "Pyetja e sigurisë nuk përputhet gjatë forgot password", "Wrong security question");
                return Unauthorized(new { message = "Pyetja e sigurisë nuk përputhet me atë që zgjodhe gjatë regjistrimit." });
            }

            if (!VerifyPassword(NormalizeSecurityAnswer(dto.SecurityAnswer), user.SecurityAnswerHash))
            {
                await SaveActivityLogAsync("PasswordReset", "Failed", user.Email, user.Role, user.AgencyName, "Përgjigje sigurie e gabuar gjatë forgot password", "Wrong security answer");
                return Unauthorized(new { message = "Përgjigjja e pyetjes së sigurisë është gabim. Password nuk mund të ndryshohet." });
            }

            user.Password = HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            await SaveActivityLogAsync("PasswordReset", "Success", user.Email, user.Role, user.AgencyName, "Password u ndryshua me sukses përmes pyetjes së sigurisë");

            return Ok(new { message = "Password u ndryshua me sukses. Tani mund të bësh login me password-in e ri." });
        }

        private static string NormalizeSecurityAnswer(string answer)
        {
            return answer.Trim().ToLowerInvariant();
        }


        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        private static bool VerifyPassword(string inputPassword, string storedPassword)
        {
            // Supports older plain-text demo users.
            return storedPassword == HashPassword(inputPassword) || storedPassword == inputPassword;
        }

        private async Task SaveActivityLogAsync(string actionType, string status, string? email, string? role, string? agencyName, string description, string? errorMessage = null)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                ActionType = actionType,
                Status = status,
                UserEmail = email,
                UserRole = role,
                AgencyName = agencyName,
                Description = description,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
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
            SecurityQuestion = user.SecurityQuestion,
            IsApproved = user.IsApproved,
            CreatedAt = user.CreatedAt
        };
    }
}
