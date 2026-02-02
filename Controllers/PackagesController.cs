using ErenTravel3API.Models.DTOs;
using ErenTravel3API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErenTravel3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackagesController : ControllerBase
    {
        private readonly IPackageService _packageService;

        public PackagesController(IPackageService packageService)
        {
            _packageService = packageService;
        }

        // GET: api/packages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PackageDto>>> GetPackages()
        {
            var packages = await _packageService.GetAllPackagesAsync();
            return Ok(packages);
        }

        // GET: api/packages/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PackageDto>> GetPackage(int id)
        {
            var package = await _packageService.GetPackageByIdAsync(id);

            if (package == null)
                return NotFound(new { message = "Paketa nuk u gjet" });

            return Ok(package);
        }

        // GET: api/packages/search?term=paris
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<PackageDto>>> SearchPackages([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { message = "Termi i kërkimit nuk mund të jetë bosh" });

            var packages = await _packageService.SearchPackagesAsync(term);
            return Ok(packages);
        }
    }
}