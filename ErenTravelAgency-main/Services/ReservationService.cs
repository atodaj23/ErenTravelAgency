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

        public async Task<IEnumerable<ReservationDto>> GetAllReservationsAsync(string? agencyName = null, string? email = null)
        {
            var query = _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Package)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(agencyName))
                query = query.Where(r => r.Package.AgencyName == agencyName);

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(r => r.Customer.Email == email);

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReservationDto
                {
                    Id = r.Id,
                    PackageId = r.PackageId,
                    Emri = r.Customer.Emri,
                    Email = r.Customer.Email,
                    Paketa = r.Package.Paketa,
                    AgencyName = r.Package.AgencyName ?? "Eren Travel",
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

        public async Task<ReservationDto?> GetReservationByIdAsync(int id)
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
                PackageId = reservation.PackageId,
                Emri = reservation.Customer.Emri,
                Email = reservation.Customer.Email,
                Paketa = reservation.Package.Paketa,
                AgencyName = reservation.Package.AgencyName ?? "Eren Travel",
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
            Package? package = null;

            if (createDto.PackageId.HasValue && createDto.PackageId.Value > 0)
            {
                package = await _context.Packages
                    .FirstOrDefaultAsync(p => p.Id == createDto.PackageId.Value && p.IsAvailable);
            }

            if (package == null && !string.IsNullOrWhiteSpace(createDto.Paketa))
            {
                package = await _context.Packages
                    .FirstOrDefaultAsync(p => p.Paketa == createDto.Paketa && p.IsAvailable);
            }

            if (package == null)
                throw new ArgumentException("Paketa nuk ekziston ose nuk është e disponueshme");

            if (createDto.DataNisjes == default || createDto.DataKthimit == default)
                throw new ArgumentException("Ju lutem plotësoni datën e nisjes dhe datën e kthimit");

            if (createDto.DataNisjes.Date < DateTime.Now.Date)
                throw new ArgumentException("Data e nisjes nuk mund të jetë në të kaluarën");

            if (createDto.DataKthimit <= createDto.DataNisjes)
                throw new ArgumentException("Data e kthimit duhet të jetë pas datës së nisjes");

            if (createDto.NrPersonave < 1 || createDto.NrDhomave < 1)
                throw new ArgumentException("Numri i personave dhe dhomave duhet të jetë të paktën 1");

            if (createDto.NrPersonave == 1 && createDto.NrDhomave > 1)
                throw new ArgumentException("Për 1 person mund të zgjedhësh vetëm 1 dhomë");

            if (createDto.NrDhomave > createDto.NrPersonave)
                throw new ArgumentException("Numri i dhomave nuk mund të jetë më i madh se numri i personave");

            var maxPersons = package.MaxPersons <= 0 ? 150 : package.MaxPersons;
            var alreadyReserved = await _context.Reservations
                .Where(r => r.PackageId == package.Id)
                .SumAsync(r => (int?)r.NrPersonave) ?? 0;

            if (alreadyReserved + createDto.NrPersonave > maxPersons)
            {
                var remaining = Math.Max(0, maxPersons - alreadyReserved);
                throw new ArgumentException($"Kapaciteti maksimal i kësaj pakete është {maxPersons} persona. Kanë mbetur vetëm {remaining} vende të lira.");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == createDto.Email);

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

            var unitPrice = string.Equals(createDto.Role, "B2B", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(package.Cmimi * 0.90m, 2)
                : package.Cmimi;

            var totalPrice = unitPrice * createDto.NrPersonave;

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

            return (await GetReservationByIdAsync(reservation.Id))!;
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
            var reservations = await _context.Reservations.Where(r => !r.IsConfirmed).ToListAsync();
            foreach (var reservation in reservations)
                reservation.IsConfirmed = true;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
