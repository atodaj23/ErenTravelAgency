using ErenTravel3API.Models.DTOs;
using ErenTravel3API.Data;
using Microsoft.EntityFrameworkCore;
using ErenTravel3API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErenTravel3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackagesController : ControllerBase
    {
        private readonly IPackageService _packageService;
        private readonly TravelDbContext _context;

        public PackagesController(IPackageService packageService, TravelDbContext context)
        {
            _packageService = packageService;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PackageDto>>> GetPackages([FromQuery] string? role = null, [FromQuery] string? agencyName = null)
        {
            var packages = await _packageService.GetAllPackagesAsync(role, agencyName);
            return Ok(packages);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PackageDto>> GetPackage(int id, [FromQuery] string? role = null)
        {
            var package = await _packageService.GetPackageByIdAsync(id, role);

            if (package == null)
                return NotFound(new { message = "Paketa nuk u gjet" });

            return Ok(package);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<PackageDto>>> SearchPackages([FromQuery] string term, [FromQuery] string? role = null)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { message = "Termi i kërkimit nuk mund të jetë bosh" });

            var packages = await _packageService.SearchPackagesAsync(term, role);
            return Ok(packages);
        }


        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Ju lutem zgjidhni një foto." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Lejohen vetëm foto JPG, PNG ose WEBP." });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"package_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return Ok(new { imageUrl = $"/uploads/{fileName}" });
        }

        [HttpPost]
        public async Task<ActionResult<PackageDto>> CreatePackage(CreatePackageDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var package = await _packageService.CreatePackageAsync(createDto);
            return CreatedAtAction(nameof(GetPackage), new { id = package.Id }, package);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PackageDto>> UpdatePackage(int id, CreatePackageDto updateDto, [FromQuery] string? agencyName = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var package = await _packageService.UpdatePackageAsync(id, updateDto, agencyName);
                if (package == null)
                    return NotFound(new { message = "Paketa nuk u gjet ose nuk i përket kësaj agjencie" });

                return Ok(package);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPatch("{id}/capacity")]
        public async Task<IActionResult> UpdateCapacity(int id, UpdatePackageCapacityDto dto, [FromQuery] string? agencyName = null)
        {
            if (dto == null || dto.MaxPersons < 1)
                return BadRequest(new { message = "Kapaciteti duhet të jetë të paktën 1 person." });

            var package = await _context.Packages.FirstOrDefaultAsync(p => p.Id == id && p.IsAvailable);
            if (package == null)
                return NotFound(new { message = "Paketa nuk u gjet." });

            if (!string.IsNullOrWhiteSpace(agencyName) && package.AgencyName != agencyName)
                return NotFound(new { message = "Kjo paketë nuk i përket kësaj agjencie." });

            var reservedPersons = await _context.Reservations
                .Where(r => r.PackageId == id && !r.IsCancelled)
                .SumAsync(r => (int?)r.NrPersonave) ?? 0;

            if (dto.MaxPersons < reservedPersons)
                return BadRequest(new { message = $"Kapaciteti nuk mund të jetë më i vogël se rezervimet ekzistuese ({reservedPersons} persona)." });

            package.MaxPersons = dto.MaxPersons;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                package.Id,
                package.MaxPersons,
                ReservedPersons = reservedPersons,
                RemainingPersons = Math.Max(0, package.MaxPersons - reservedPersons)
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePackage(int id, [FromQuery] string? agencyName = null)
        {
            var success = await _packageService.DeletePackageAsync(id, agencyName);
            if (!success)
                return NotFound(new { message = "Paketa nuk u gjet ose nuk i përket kësaj agjencie" });

            return NoContent();
        }
    }
}
