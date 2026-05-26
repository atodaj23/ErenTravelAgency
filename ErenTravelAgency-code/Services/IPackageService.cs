using ErenTravel3API.Models.DTOs;

namespace ErenTravel3API.Services
{
    public interface IPackageService
    {
        Task<IEnumerable<PackageDto>> GetAllPackagesAsync(string? role = null, string? agencyName = null);
        Task<PackageDto?> GetPackageByIdAsync(int id, string? role = null);
        Task<IEnumerable<PackageDto>> SearchPackagesAsync(string searchTerm, string? role = null);
        Task<PackageDto> CreatePackageAsync(CreatePackageDto createDto);
        Task<PackageDto?> UpdatePackageAsync(int id, CreatePackageDto updateDto, string? agencyName = null);
        Task<bool> DeletePackageAsync(int id, string? agencyName = null);
    }
}
