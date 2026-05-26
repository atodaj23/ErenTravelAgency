using ErenTravel3API.Data;
using ErenTravel3API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ErenTravel3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityLogsController : ControllerBase
    {
        private readonly TravelDbContext _context;

        public ActivityLogsController(TravelDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActivityLog>>> GetLogs([FromQuery] string? actionType = null, [FromQuery] string? agencyName = null)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(actionType))
                query = query.Where(x => x.ActionType == actionType);

            if (!string.IsNullOrWhiteSpace(agencyName))
                query = query.Where(x => x.AgencyName == agencyName);

            var logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(300)
                .ToListAsync();

            return Ok(logs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLog(ActivityLog log)
        {
            if (string.IsNullOrWhiteSpace(log.ActionType))
                return BadRequest(new { message = "ActionType është i detyrueshëm" });

            if (string.IsNullOrWhiteSpace(log.Status))
                log.Status = "Info";

            log.CreatedAt = DateTime.UtcNow;
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Log u ruajt" });
        }

        [HttpGet("agency-report")]
        public async Task<IActionResult> DownloadAgencyReport([FromQuery] string agencyName)
        {
            if (string.IsNullOrWhiteSpace(agencyName))
                return BadRequest(new { message = "Emri i agjencisë është i detyrueshëm" });

            var packages = await _context.Packages.Where(p => p.AgencyName == agencyName).ToListAsync();
            var packageIds = packages.Select(p => p.Id).ToList();
            var reservations = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Package)
                .Where(r => packageIds.Contains(r.PackageId))
                .ToListAsync();
            var reviews = await _context.Reviews.Where(r => r.AgencyName == agencyName).ToListAsync();
            var logs = await _context.ActivityLogs.Where(l => l.AgencyName == agencyName).OrderByDescending(l => l.CreatedAt).Take(100).ToListAsync();

            var revenue = reservations.Sum(r => r.Cmimi);
            var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            var sb = new StringBuilder();
            sb.AppendLine($"Raport Agjencie: {agencyName}");
            sb.AppendLine($"Gjeneruar më: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Paketa: {packages.Count}");
            sb.AppendLine($"Rezervime: {reservations.Count}");
            sb.AppendLine($"Të ardhura totale: {revenue:0.00} EUR");
            sb.AppendLine($"Rating mesatar: {avgRating:0.00}/5");
            sb.AppendLine();
            sb.AppendLine("--- Rezervimet ---");
            foreach (var r in reservations)
                sb.AppendLine($"#{r.Id}; {r.Customer?.Emri}; {r.Customer?.Email}; {r.Package?.Paketa}; Persona: {r.NrPersonave}; Dhoma: {r.NrDhomave}; Çmimi: {r.Cmimi:0.00}; Status: {(r.IsConfirmed ? "Aprovuar" : "Në pritje")}");
            sb.AppendLine();
            sb.AppendLine("--- Reviews ---");
            foreach (var r in reviews)
                sb.AppendLine($"{r.UserName} ({r.UserEmail}); Rating: {r.Rating}/5; {r.Comment}");
            sb.AppendLine();
            sb.AppendLine("--- Activity Logs ---");
            foreach (var l in logs)
                sb.AppendLine($"{l.CreatedAt:yyyy-MM-dd HH:mm}; {l.ActionType}; {l.Status}; {l.UserEmail}; {l.Description}; {l.ErrorMessage}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"agency-report-{agencyName.Replace(" ", "-").ToLower()}-{DateTime.Now:yyyyMMddHHmm}.txt";
            return File(bytes, "text/plain", fileName);
        }
    }
}
