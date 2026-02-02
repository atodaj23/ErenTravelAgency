using ErenTravel3API.Data;
using ErenTravel3API.Models;
using ErenTravel3API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ErenTravel3API.Services
{
    public class ReservationService : IReservationService
    {
        private readonly TravelDbContext _context;

        public ReservationService(TravelDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReservationDto>> GetAllReservationsAsync()
        {
            return await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Package)
                .Select(r => new ReservationDto
                {
                    Id = r.Id,
                    Emri = r.Customer.Emri,
                    Email = r.Customer.Email,
                    Paketa = r.Package.Paketa,
                    DataNisjes = r.DataNisjes,
                    DataKthimit = r.DataKthimit,
                    NrPersonave = r.NrPersonave,
                    NrDhomave = r.NrDhomave,
                    Cmimi = r.Cmimi,
                    Confirmed = r.IsConfirmed,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ReservationDto> GetReservationByIdAsync(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Package)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return null;

            return new ReservationDto
            {
                Id = reservation.Id,
                Emri = reservation.Customer.Emri,
                Email = reservation.Customer.Email,
                Paketa = reservation.Package.Paketa,
                DataNisjes = reservation.DataNisjes,
                DataKthimit = reservation.DataKthimit,
                NrPersonave = reservation.NrPersonave,
                NrDhomave = reservation.NrDhomave,
                Cmimi = reservation.Cmimi,
                Confirmed = reservation.IsConfirmed,
                CreatedAt = reservation.CreatedAt
            };
        }

        public async Task<ReservationDto> CreateReservationAsync(CreateReservationDto createDto)
        {
            // Find package by name
            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.Paketa == createDto.Paketa && p.IsAvailable);

            if (package == null)
                throw new ArgumentException("Paketa nuk ekziston ose nuk është e disponueshme");

            // Validate dates
            if (createDto.DataNisjes < DateTime.Now)
                throw new ArgumentException("Data e nisjes nuk mund të jetë në të kaluarën");

            if (createDto.DataKthimit <= createDto.DataNisjes)
                throw new ArgumentException("Data e kthimit duhet të jetë pas datës së nisjes");

            // Find or create customer
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == createDto.Email);

            if (customer == null)
            {
                customer = new Customer
                {
                    Emri = createDto.Emri,
                    Email = createDto.Email
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // Calculate total price
            var totalPrice = package.Cmimi * createDto.NrPersonave;

            // Create reservation
            var reservation = new Reservation
            {
                CustomerId = customer.Id,
                PackageId = package.Id,
                Customer = customer,
                Package = package,
                DataNisjes = createDto.DataNisjes,
                DataKthimit = createDto.DataKthimit,
                NrPersonave = createDto.NrPersonave,
                NrDhomave = createDto.NrDhomave,
                Cmimi = totalPrice,
                IsConfirmed = false
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return await GetReservationByIdAsync(reservation.Id);
        }

        public async Task<bool> UpdateReservationStatusAsync(int id, bool isConfirmed)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return false;

            reservation.IsConfirmed = isConfirmed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReservationAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return false;

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmAllReservationsAsync()
        {
            var reservations = await _context.Reservations
                .Where(r => !r.IsConfirmed)
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                reservation.IsConfirmed = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}