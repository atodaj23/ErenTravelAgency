using ErenTravel3API.Models.DTOs;
using ErenTravel3API.Data;
using ErenTravel3API.Models;
using ErenTravel3API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErenTravel3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly TravelDbContext _context;

        public ReservationsController(IReservationService reservationService, TravelDbContext context)
        {
            _reservationService = reservationService;
            _context = context;
        }

        private async Task SaveActivityLogAsync(string status, CreateReservationDto dto, string description, string? errorMessage = null)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                ActionType = "Booking",
                Status = status,
                UserEmail = dto.Email,
                UserRole = dto.Role,
                Description = description,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations([FromQuery] string? agencyName = null, [FromQuery] string? email = null)
        {
            var reservations = await _reservationService.GetAllReservationsAsync(agencyName, email);
            return Ok(reservations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDto>> GetReservation(int id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);

            if (reservation == null)
                return NotFound(new { message = "Rezervimi nuk u gjet" });

            return Ok(reservation);
        }

        [HttpPost]
        public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto createDto)
        {
            if (!ModelState.IsValid)
            {
                await SaveActivityLogAsync("Failed", createDto, "Tentativë rezervimi me fusha të paplota ose të pavlefshme", "Invalid model state");
                return BadRequest(ModelState);
            }

            try
            {
                var reservation = await _reservationService.CreateReservationAsync(createDto);
                _context.ActivityLogs.Add(new ActivityLog
                {
                    ActionType = "Booking",
                    Status = "Success",
                    UserEmail = createDto.Email,
                    UserRole = createDto.Role,
                    AgencyName = reservation.AgencyName,
                    Description = $"Rezervim i suksesshëm për paketën {reservation.Paketa}, {reservation.NrPersonave} persona, {reservation.Cmimi:0.00} EUR",
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
            }
            catch (ArgumentException ex)
            {
                await SaveActivityLogAsync("Failed", createDto, "Rezervimi dështoi për arsye validimi", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                await SaveActivityLogAsync("Error", createDto, "Ndodhi error gjatë krijimit të rezervimit", ex.Message);
                return StatusCode(500, new { message = "Ndodhi një gabim gjatë krijimit të rezervimit", error = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateReservationStatus(int id, UpdateReservationStatusDto updateDto)
        {
            var success = await _reservationService.UpdateReservationStatusAsync(id, updateDto.IsConfirmed);

            if (!success)
                return NotFound(new { message = "Rezervimi nuk u gjet" });

            return NoContent();
        }

        [HttpPost("confirm-all")]
        public async Task<IActionResult> ConfirmAllReservations()
        {
            await _reservationService.ConfirmAllReservationsAsync();
            return Ok(new { message = "Të gjitha rezervimet u konfirmuan me sukses" });
        }


        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelReservation(int id, CancelReservationDto cancelDto)
        {
            try
            {
                var result = await _reservationService.CancelReservationAsync(id, cancelDto.Email, cancelDto.Reason);

                _context.ActivityLogs.Add(new ActivityLog
                {
                    ActionType = "Cancellation",
                    Status = result.Success ? "Success" : "Failed",
                    UserEmail = cancelDto.Email,
                    UserRole = "Client/B2B",
                    AgencyName = result.Reservation?.AgencyName,
                    Description = result.Success
                        ? $"Rezervimi #{id} u anulua. Arsyeja: {cancelDto.Reason ?? "Anulim nga klienti"}"
                        : $"Tentativë anulimi për rezervimin #{id} u refuzua nga policy.",
                    ErrorMessage = result.Success ? null : result.Message,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                if (!result.Success)
                    return BadRequest(new { message = result.Message });

                return Ok(new { message = result.Message, reservation = result.Reservation });
            }
            catch (Exception ex)
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    ActionType = "Cancellation",
                    Status = "Error",
                    UserEmail = cancelDto.Email,
                    UserRole = "Client/B2B",
                    Description = $"Ndodhi error gjatë anulimit të rezervimit #{id}.",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return StatusCode(500, new { message = "Ndodhi një gabim gjatë anulimit të rezervimit", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var success = await _reservationService.DeleteReservationAsync(id);

            if (!success)
                return NotFound(new { message = "Rezervimi nuk u gjet" });

            return NoContent();
        }
    }
}
