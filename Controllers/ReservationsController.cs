using ErenTravel3API.Models.DTOs;
using ErenTravel3API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErenTravel3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        // Dependency Injection
        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        // GET: api/reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations()
        {
            var reservations = await _reservationService.GetAllReservationsAsync();
            return Ok(reservations);
        }

        // GET: api/reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDto>> GetReservation(int id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);

            if (reservation == null)
                return NotFound(new { message = "Rezervimi nuk u gjet" });

            return Ok(reservation);
        }

        // POST: api/reservations
        [HttpPost]
        public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var reservation = await _reservationService.CreateReservationAsync(createDto);

                return CreatedAtAction(
                    nameof(GetReservation),
                    new { id = reservation.Id },
                    reservation
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ndodhi një gabim gjatë krijimit të rezervimit", error = ex.Message });
            }
        }

        // PATCH: api/reservations/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateReservationStatus(int id, UpdateReservationStatusDto updateDto)
        {
            var success = await _reservationService.UpdateReservationStatusAsync(id, updateDto.IsConfirmed);

            if (!success)
                return NotFound(new { message = "Rezervimi nuk u gjet" });

            return NoContent();
        }

        // POST: api/reservations/confirm-all
        [HttpPost("confirm-all")]
        public async Task<IActionResult> ConfirmAllReservations()
        {
            await _reservationService.ConfirmAllReservationsAsync();
            return Ok(new { message = "Të gjitha rezervimet u konfirmuan me sukses" });
        }

        // DELETE: api/reservations/5
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