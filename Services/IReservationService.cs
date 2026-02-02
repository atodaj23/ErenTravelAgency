using ErenTravel3API.Models.DTOs;

namespace ErenTravel3API.Services
{
    public interface IReservationService
    {
        Task<IEnumerable<ReservationDto>> GetAllReservationsAsync();
        Task<ReservationDto> GetReservationByIdAsync(int id);
        Task<ReservationDto> CreateReservationAsync(CreateReservationDto createDto);
        Task<bool> UpdateReservationStatusAsync(int id, bool isConfirmed);
        Task<bool> DeleteReservationAsync(int id);
        Task<bool> ConfirmAllReservationsAsync();
    }
}