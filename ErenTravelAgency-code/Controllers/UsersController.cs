using ErenTravel3API.Data;
using ErenTravel3API.Models;
using ErenTravel3API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErenTravel3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly TravelDbContext _context;

        public UsersController(TravelDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.AppUsers
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    AgencyName = u.AgencyName,
                    TaxId = u.TaxId,
                    Address = u.Address,
                    Phone = u.Phone,
                    IsApproved = u.IsApproved,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetPendingUsers()
        {
            var users = await _context.AppUsers
                .Where(u => !u.IsApproved)
                .OrderBy(u => u.CreatedAt)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    AgencyName = u.AgencyName,
                    TaxId = u.TaxId,
                    Address = u.Address,
                    Phone = u.Phone,
                    IsApproved = u.IsApproved,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User nuk u gjet" });

            user.IsApproved = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "User u aprovua me sukses" });
        }

        private static string NormalizeAgency(string? value)
        {
            return (value ?? string.Empty).Trim().ToLower();
        }

        private async Task<(int packageCount, int reservationCount)> DeleteTravelAgentDataAsync(AppUser user)
        {
            if (!string.Equals(user.Role, "TravelAgent", StringComparison.OrdinalIgnoreCase))
                return (0, 0);

            // Match possible agency names used in older data.
            var agencyKeys = new[] { user.AgencyName, user.FullName }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeAgency)
                .Distinct()
                .ToList();

            if (!agencyKeys.Any())
                return (0, 0);

            var packages = await _context.Packages
                .Where(p => p.AgencyName != null && agencyKeys.Contains(p.AgencyName.Trim().ToLower()))
                .ToListAsync();

            var packageIds = packages.Select(p => p.Id).ToList();

            var reservations = packageIds.Any()
                ? await _context.Reservations
                    .Include(r => r.Package)
                    .Where(r => packageIds.Contains(r.PackageId) ||
                                (r.Package.AgencyName != null && agencyKeys.Contains(r.Package.AgencyName.Trim().ToLower())))
                    .ToListAsync()
                : new List<Reservation>();

            // Delete reservations before packages because of foreign keys.
            if (reservations.Any())
                _context.Reservations.RemoveRange(reservations);

            var follows = await _context.AgencyFollows
                .Where(f => agencyKeys.Contains(f.AgencyName.Trim().ToLower()))
                .ToListAsync();

            var reviews = await _context.Reviews
                .Where(r => agencyKeys.Contains(r.AgencyName.Trim().ToLower()))
                .ToListAsync();

            var reports = await _context.IssueReports
                .Where(r => r.AgencyName != null && agencyKeys.Contains(r.AgencyName.Trim().ToLower()))
                .ToListAsync();

            if (follows.Any())
                _context.AgencyFollows.RemoveRange(follows);

            if (reviews.Any())
                _context.Reviews.RemoveRange(reviews);

            if (reports.Any())
                _context.IssueReports.RemoveRange(reports);

            if (packages.Any())
                _context.Packages.RemoveRange(packages);

            return (packages.Count, reservations.Count);
        }

        [HttpDelete("cleanup-orphan-agency-data")]
        public async Task<IActionResult> CleanupOrphanAgencyData()
        {
            // Cleans data left from deleted agencies.
            var activeAgentAgencies = await _context.AppUsers
                .Where(u => u.Role == "TravelAgent")
                .Select(u => u.AgencyName ?? u.FullName)
                .Where(x => x != null && x != "")
                .ToListAsync();

            var activeKeys = activeAgentAgencies.Select(NormalizeAgency).ToHashSet();

            var orphanPackages = await _context.Packages
                .Where(p => p.AgencyName != null
                    && p.AgencyName != "Eren Travel"
                    && !activeKeys.Contains(p.AgencyName.Trim().ToLower()))
                .ToListAsync();

            var orphanPackageIds = orphanPackages.Select(p => p.Id).ToList();
            var orphanReservations = orphanPackageIds.Any()
                ? await _context.Reservations.Where(r => orphanPackageIds.Contains(r.PackageId)).ToListAsync()
                : new List<Reservation>();

            if (orphanReservations.Any())
                _context.Reservations.RemoveRange(orphanReservations);

            if (orphanPackages.Any())
                _context.Packages.RemoveRange(orphanPackages);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"U pastruan {orphanPackages.Count} paketa dhe {orphanReservations.Count} rezervime të agjencive pa Travel Agent aktiv."
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User nuk u gjet" });

            if (user.Email.ToLower() == "admin@grupierenit.com" || user.Role == "Administrator")
                return BadRequest(new { message = "Admini kryesor nuk mund të fshihet" });

            var deletedData = await DeleteTravelAgentDataAsync(user);

            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();

            if (deletedData.packageCount > 0 || deletedData.reservationCount > 0)
            {
                return Ok(new
                {
                    message = $"Travel Agent u fshi me sukses. U fshinë edhe {deletedData.packageCount} paketa dhe {deletedData.reservationCount} rezervime të kësaj agjencie."
                });
            }

            return Ok(new { message = "User u fshi me sukses" });
        }

        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllUsers()
        {
            var users = await _context.AppUsers
                .Where(u => u.Email.ToLower() != "admin@grupierenit.com" && u.Role != "Administrator")
                .ToListAsync();

            var deletedPackages = 0;
            var deletedReservations = 0;

            foreach (var user in users.Where(u => string.Equals(u.Role, "TravelAgent", StringComparison.OrdinalIgnoreCase)))
            {
                var deletedData = await DeleteTravelAgentDataAsync(user);
                deletedPackages += deletedData.packageCount;
                deletedReservations += deletedData.reservationCount;
            }

            _context.AppUsers.RemoveRange(users);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"U fshinë {users.Count} usera, {deletedPackages} paketa dhe {deletedReservations} rezervime të agjencive." });
        }
    }
}
