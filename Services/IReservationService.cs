using ErenTravel3API.Models.DTOs;

namespace ErenTravel3API.Services
{
    public interface IReservationService
    {
        Task<IEnumerable<ReservationDto>> GetAllReservationsAsync(string? agencyName = null, string? email = null);
        Task<ReservationDto?> GetReservationByIdAsync(int id);
        Task<ReservationDto> CreateReservationAsync(CreateReservationDto createDto);
        Task<bool> UpdateReservationStatusAsync(int id, bool isConfirmed);
        Task<bool> DeleteReservationAsync(int id);
        Task<(bool Success, string Message, ReservationDto? Reservation)> CancelReservationAsync(int id, string email, string? reason);
        Task<bool> ConfirmAllReservationsAsync();
    }
}
