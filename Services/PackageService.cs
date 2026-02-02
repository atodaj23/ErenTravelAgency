using ErenTravel3API.Data;
using ErenTravel3API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ErenTravel3API.Services
{
    public class PackageService : IPackageService
    {
        private readonly TravelDbContext _context;

        public PackageService(TravelDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PackageDto>> GetAllPackagesAsync()
        {
            return await _context.Packages
                .Where(p => p.IsAvailable)
                .Select(p => new PackageDto
                {
                    Id = p.Id,
                    Paketa = p.Paketa,
                    Kohezgjatja = p.Kohezgjatja,
                    Cmimi = p.Cmimi,
                    Pershkrimi = p.Pershkrimi,
                    IsAvailable = p.IsAvailable
                })
                .ToListAsync();
        }

        public async Task<PackageDto> GetPackageByIdAsync(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null)
                return null;

            return new PackageDto
            {
                Id = package.Id,
                Paketa = package.Paketa,
                Kohezgjatja = package.Kohezgjatja,
                Cmimi = package.Cmimi,
                Pershkrimi = package.Pershkrimi,
                IsAvailable = package.IsAvailable
            };
        }

        public async Task<IEnumerable<PackageDto>> SearchPackagesAsync(string searchTerm)
        {
            return await _context.Packages
                .Where(p => p.IsAvailable && p.Paketa.Contains(searchTerm))
                .Select(p => new PackageDto
                {
                    Id = p.Id,
                    Paketa = p.Paketa,
                    Kohezgjatja = p.Kohezgjatja,
                    Cmimi = p.Cmimi,
                    Pershkrimi = p.Pershkrimi,
                    IsAvailable = p.IsAvailable
                })
                .ToListAsync();
        }
    }
}