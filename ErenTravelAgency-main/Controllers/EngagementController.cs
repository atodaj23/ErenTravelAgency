using ErenTravel3API.Data;
using ErenTravel3API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErenTravel3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EngagementController : ControllerBase
    {
        private readonly TravelDbContext _context;
        public EngagementController(TravelDbContext context) => _context = context;

        public class FollowDto
        {
            public required string UserEmail { get; set; }
            public required string AgencyName { get; set; }
        }

        public class ReviewDto
        {
            public required string UserEmail { get; set; }
            public required string UserName { get; set; }
            public required string AgencyName { get; set; }
            public int? PackageId { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
        }

        public class IssueReportDto
        {
            public string? ReporterEmail { get; set; }
            public required string TargetType { get; set; }
            public int? TargetId { get; set; }
            public string? AgencyName { get; set; }
            public required string Reason { get; set; }
        }

        [HttpGet("follows")]
        public async Task<IActionResult> GetFollows([FromQuery] string? email = null, [FromQuery] string? agencyName = null)
        {
            var query = _context.AgencyFollows.AsQueryable();
            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(f => f.UserEmail == email);
            if (!string.IsNullOrWhiteSpace(agencyName))
                query = query.Where(f => f.AgencyName == agencyName);

            var follows = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
            return Ok(follows);
        }

        [HttpPost("follows")]
        public async Task<IActionResult> FollowAgency(FollowDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserEmail) || string.IsNullOrWhiteSpace(dto.AgencyName))
                return BadRequest(new { message = "Email dhe agjencia janë të detyrueshme." });

            var exists = await _context.AgencyFollows.AnyAsync(f => f.UserEmail == dto.UserEmail && f.AgencyName == dto.AgencyName);
            if (!exists)
            {
                _context.AgencyFollows.Add(new AgencyFollow { UserEmail = dto.UserEmail, AgencyName = dto.AgencyName });
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "Agjencia u ndoq me sukses." });
        }

        [HttpDelete("follows")]
        public async Task<IActionResult> UnfollowAgency([FromQuery] string email, [FromQuery] string agencyName)
        {
            var follow = await _context.AgencyFollows.FirstOrDefaultAsync(f => f.UserEmail == email && f.AgencyName == agencyName);
            if (follow == null)
                return NotFound(new { message = "Nuk u gjet ndjekja." });

            _context.AgencyFollows.Remove(follow);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetReviews([FromQuery] string? agencyName = null, [FromQuery] int? packageId = null)
        {
            var query = _context.Reviews.AsQueryable();
            if (!string.IsNullOrWhiteSpace(agencyName))
                query = query.Where(r => r.AgencyName == agencyName);
            if (packageId.HasValue)
                query = query.Where(r => r.PackageId == packageId.Value);

            var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return Ok(reviews);
        }

        [HttpPost("reviews")]
        public async Task<IActionResult> CreateReview(ReviewDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserEmail) || string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.AgencyName))
                return BadRequest(new { message = "Useri dhe agjencia janë të detyrueshme." });
            if (dto.Rating < 0 || dto.Rating > 5)
                return BadRequest(new { message = "Rating duhet të jetë nga 0 deri në 5 yje." });

            // Çdo klient ka një review aktive për të njëjtën paketë/agjenci.
            // Nëse klienti e ndryshon mendimin, review ekzistuese përditësohet
            // që mesatarja të llogaritet saktë dhe një klient të mos numërohet disa herë.
            var normalizedEmail = dto.UserEmail.Trim().ToLower();
            var normalizedAgency = dto.AgencyName.Trim();
            var review = await _context.Reviews.FirstOrDefaultAsync(r =>
                r.UserEmail.ToLower() == normalizedEmail &&
                r.AgencyName == normalizedAgency &&
                r.PackageId == dto.PackageId);

            if (review == null)
            {
                review = new Review
                {
                    UserEmail = dto.UserEmail.Trim(),
                    UserName = dto.UserName.Trim(),
                    AgencyName = normalizedAgency,
                    PackageId = dto.PackageId,
                    Rating = dto.Rating,
                    Comment = dto.Comment
                };
                _context.Reviews.Add(review);
            }
            else
            {
                review.UserName = dto.UserName.Trim();
                review.Rating = dto.Rating;
                review.Comment = dto.Comment;
                review.CreatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(review);
        }


        [HttpGet("reviews/summary")]
        public async Task<IActionResult> GetReviewSummary([FromQuery] string? agencyName = null, [FromQuery] int? packageId = null)
        {
            var query = _context.Reviews.AsQueryable();
            if (!string.IsNullOrWhiteSpace(agencyName))
                query = query.Where(r => r.AgencyName == agencyName);
            if (packageId.HasValue)
                query = query.Where(r => r.PackageId == packageId.Value);

            var count = await query.CountAsync();
            var average = count == 0 ? 0 : await query.AverageAsync(r => (double)r.Rating);
            var distribution = await query
                .GroupBy(r => r.Rating)
                .Select(g => new { rating = g.Key, count = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                count,
                averageRating = Math.Round(average, 2),
                distribution = Enumerable.Range(0, 6).Select(star => new
                {
                    rating = star,
                    count = distribution.FirstOrDefault(x => x.rating == star)?.count ?? 0
                })
            });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _context.IssueReports.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return Ok(reports);
        }

        [HttpPost("reports")]
        public async Task<IActionResult> CreateReport(IssueReportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TargetType) || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { message = "Lloji i raportimit dhe arsyeja janë të detyrueshme." });

            var report = new IssueReport
            {
                ReporterEmail = dto.ReporterEmail,
                TargetType = dto.TargetType,
                TargetId = dto.TargetId,
                AgencyName = dto.AgencyName,
                Reason = dto.Reason,
                Status = "Open"
            };
            _context.IssueReports.Add(report);
            await _context.SaveChangesAsync();
            return Ok(report);
        }

        [HttpPatch("reports/{id}/close")]
        public async Task<IActionResult> CloseReport(int id)
        {
            var report = await _context.IssueReports.FindAsync(id);
            if (report == null)
                return NotFound(new { message = "Raportimi nuk u gjet." });
            report.Status = "Closed";
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
