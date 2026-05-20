using ErenTravel3API.Data;
using ErenTravel3API.Models;
using ErenTravel3API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ErenTravel3API.Services
{
    public class PackageService : IPackageService
    {
        private readonly TravelDbContext _context;
        private const decimal B2BDiscount = 0.10m;

        public PackageService(TravelDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PackageDto>> GetAllPackagesAsync(string? role = null, string? agencyName = null)
        {
            var query = _context.Packages.Where(p => p.IsAvailable);

            if (!string.IsNullOrWhiteSpace(agencyName))
                query = query.Where(p => p.AgencyName == agencyName);

            var packages = await query.OrderByDescending(p => p.Id).ToListAsync();
            var dtos = packages.Select(p => ToDto(p, role)).ToList();
            await FillRatingInfoAsync(dtos);
            await FillCapacityInfoAsync(dtos);
            return dtos;
        }

        public async Task<PackageDto?> GetPackageByIdAsync(int id, string? role = null)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null)
                return null;

            var dto = ToDto(package, role);
            var list = new List<PackageDto> { dto };
            await FillRatingInfoAsync(list);
            await FillCapacityInfoAsync(list);
            return dto;
        }

        public async Task<IEnumerable<PackageDto>> SearchPackagesAsync(string searchTerm, string? role = null)
        {
            var packages = await _context.Packages
                .Where(p => p.IsAvailable && (p.Paketa.Contains(searchTerm) || (p.Pershkrimi != null && p.Pershkrimi.Contains(searchTerm))))
                .ToListAsync();

            var dtos = packages.Select(p => ToDto(p, role)).ToList();
            await FillRatingInfoAsync(dtos);
            await FillCapacityInfoAsync(dtos);
            return dtos;
        }

        public async Task<PackageDto> CreatePackageAsync(CreatePackageDto createDto)
        {
            var package = new Package
            {
                Paketa = createDto.Paketa,
                Kohezgjatja = createDto.Kohezgjatja,
                Cmimi = createDto.Cmimi,
                TravelDate = createDto.TravelDate,
                Pershkrimi = createDto.Pershkrimi,
                AgencyName = string.IsNullOrWhiteSpace(createDto.AgencyName) ? "Eren Travel" : createDto.AgencyName,
                ImageUrl = string.IsNullOrWhiteSpace(createDto.ImageUrl) ? null : createDto.ImageUrl,
                MaxPersons = createDto.MaxPersons <= 0 ? 150 : createDto.MaxPersons,
                IsAvailable = true
            };

            _context.Packages.Add(package);
            await _context.SaveChangesAsync();

            return ToDto(package, null);
        }

        public async Task<PackageDto?> UpdatePackageAsync(int id, CreatePackageDto updateDto, string? agencyName = null)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null)
                return null;

            if (!string.IsNullOrWhiteSpace(agencyName) && package.AgencyName != agencyName)
                return null;

            package.Paketa = updateDto.Paketa;
            package.Kohezgjatja = updateDto.Kohezgjatja;
            package.Cmimi = updateDto.Cmimi;
            package.TravelDate = updateDto.TravelDate;
            package.Pershkrimi = updateDto.Pershkrimi;
            package.AgencyName = string.IsNullOrWhiteSpace(updateDto.AgencyName) ? package.AgencyName : updateDto.AgencyName;
            if (!string.IsNullOrWhiteSpace(updateDto.ImageUrl))
                package.ImageUrl = updateDto.ImageUrl;
            if (updateDto.MaxPersons > 0)
            {
                var reservedPersons = await _context.Reservations
                    .Where(r => r.PackageId == id)
                    .SumAsync(r => (int?)r.NrPersonave) ?? 0;

                if (updateDto.MaxPersons < reservedPersons)
                    throw new ArgumentException($"Kapaciteti nuk mund të jetë më i vogël se rezervimet ekzistuese ({reservedPersons} persona).");

                package.MaxPersons = updateDto.MaxPersons;
            }
            package.IsAvailable = true;

            await _context.SaveChangesAsync();
            return ToDto(package, null);
        }

        public async Task<bool> DeletePackageAsync(int id, string? agencyName = null)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null)
                return false;

            if (!string.IsNullOrWhiteSpace(agencyName) && package.AgencyName != agencyName)
                return false;

            package.IsAvailable = false;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task FillRatingInfoAsync(List<PackageDto> packages)
        {
            if (packages.Count == 0)
                return;

            var packageIds = packages.Select(p => p.Id).ToList();
            var ratingInfo = await _context.Reviews
                .Where(r => r.PackageId.HasValue && packageIds.Contains(r.PackageId.Value))
                .GroupBy(r => r.PackageId!.Value)
                .Select(g => new
                {
                    PackageId = g.Key,
                    ReviewCount = g.Count(),
                    AverageRating = Math.Round(g.Average(x => (double)x.Rating), 2)
                })
                .ToListAsync();

            foreach (var package in packages)
            {
                var info = ratingInfo.FirstOrDefault(x => x.PackageId == package.Id);
                package.ReviewCount = info?.ReviewCount ?? 0;
                package.AverageRating = info?.AverageRating ?? 0;
            }
        }


        private async Task FillCapacityInfoAsync(List<PackageDto> packages)
        {
            if (packages.Count == 0)
                return;

            var packageIds = packages.Select(p => p.Id).ToList();
            var reservedInfo = await _context.Reservations
                .Where(r => packageIds.Contains(r.PackageId))
                .GroupBy(r => r.PackageId)
                .Select(g => new
                {
                    PackageId = g.Key,
                    ReservedPersons = g.Sum(x => x.NrPersonave)
                })
                .ToListAsync();

            foreach (var package in packages)
            {
                var info = reservedInfo.FirstOrDefault(x => x.PackageId == package.Id);
                package.ReservedPersons = info?.ReservedPersons ?? 0;
                package.RemainingPersons = Math.Max(0, package.MaxPersons - package.ReservedPersons);
            }
        }

        private static PackageDto ToDto(Package package, string? role)
        {
            var isB2B = string.Equals(role, "B2B", StringComparison.OrdinalIgnoreCase);
            var discountedPrice = Math.Round(package.Cmimi * (1 - B2BDiscount), 2);

            return new PackageDto
            {
                Id = package.Id,
                Paketa = package.Paketa,
                Kohezgjatja = package.Kohezgjatja,
                Cmimi = isB2B ? discountedPrice : package.Cmimi,
                TravelDate = package.TravelDate,
                CmimiOrigjinal = isB2B ? package.Cmimi : null,
                CmimiB2B = isB2B ? discountedPrice : null,
                KaZbritjeB2B = isB2B,
                Pershkrimi = package.Pershkrimi,
                AgencyName = package.AgencyName ?? "Eren Travel",
                ImageUrl = package.ImageUrl,
                IsAvailable = package.IsAvailable,
                MaxPersons = package.MaxPersons <= 0 ? 150 : package.MaxPersons,
                ReservedPersons = 0,
                RemainingPersons = package.MaxPersons <= 0 ? 150 : package.MaxPersons,
                AverageRating = 0,
                ReviewCount = 0
            };
        }
    }
}
