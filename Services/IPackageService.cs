using ErenTravel3API.Models.DTOs;

namespace ErenTravel3API.Services
{
    public interface IPackageService
    {
        Task<IEnumerable<PackageDto>> GetAllPackagesAsync();
        Task<PackageDto> GetPackageByIdAsync(int id);
        Task<IEnumerable<PackageDto>> SearchPackagesAsync(string searchTerm);
    }
}