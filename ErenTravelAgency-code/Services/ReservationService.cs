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

        private sealed class CancellationPolicyResult
        {
            public bool CanCancel { get; set; }
            public string Message { get; set; } = string.Empty;
            public decimal CancellationFee { get; set; }
            public decimal RefundAmount { get; set; }
            public string RefundStatus { get; set; } = "Not applicable";
        }

        private static CancellationPolicyResult EvaluateCancellationPolicy(Reservation reservation)
        {
            if (reservation.IsCancelled)
                return new CancellationPolicyResult { CanCancel = false, Message = "Ky rezervim është anuluar më parë.", RefundStatus = reservation.RefundStatus ?? "Cancelled" };

            var now = DateTime.Now;
            var departure = reservation.DataNisjes;
            var daysBeforeDeparture = (departure.Date - now.Date).Days;

            if (departure <= now)
                return new CancellationPolicyResult { CanCancel = false, Message = "Rezervimi nuk mund të anulohet sepse data e nisjes ka kaluar ose udhëtimi ka filluar.", RefundStatus = "Not refundable" };

            if (daysBeforeDeparture <= 1)
                return new CancellationPolicyResult { CanCancel = false, Message = "Rezervimi nuk mund të anulohet një ditë para datës së nisjes ose në ditën e nisjes.", RefundStatus = "Not refundable" };

            if (reservation.IsConfirmed && daysBeforeDeparture <= 3)
                return new CancellationPolicyResult { CanCancel = false, Message = "Rezervimet e aprovuara nuk mund të anulohen në 3 ditët e fundit para nisjes.", RefundStatus = "Not refundable" };

            decimal feePercent = daysBeforeDeparture >= 14 ? 0m : daysBeforeDeparture >= 7 ? 0.10m : 0.25m;
            var fee = Math.Round(reservation.Cmimi * feePercent, 2);
            var refund = Math.Max(0, reservation.Cmimi - fee);
            var text = feePercent == 0m
                ? "Anulimi lejohet me rimbursim të plotë sepse është më shumë se 14 ditë para nisjes."
                : feePercent == 0.10m
                    ? "Anulimi lejohet, por aplikohet 10% cancellation fee sepse është 7–13 ditë para nisjes."
                    : "Anulimi lejohet, por aplikohet 25% cancellation fee sepse është më pak se 7 ditë para nisjes.";

            return new CancellationPolicyResult
            {
                CanCancel = true,
                Message = text,
                CancellationFee = fee,
                RefundAmount = refund,
                RefundStatus = fee == 0 ? "Full refund" : "Partial refund"
            };
        }

        private static ReservationDto ToDto(Reservation reservation)
        {
            var policy = EvaluateCancellationPolicy(reservation);
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
                IsCancelled = reservation.IsCancelled,
                CancelledAt = reservation.CancelledAt,
                CancellationReason = reservation.CancellationReason,
                CancellationFee = reservation.IsCancelled ? reservation.CancellationFee : policy.CancellationFee,
                RefundAmount = reservation.IsCancelled ? reservation.RefundAmount : policy.RefundAmount,
                RefundStatus = reservation.IsCancelled ? reservation.RefundStatus : policy.RefundStatus,
                CanCancel = policy.CanCancel,
                CancellationPolicyMessage = policy.Message,
                CreatedAt = reservation.CreatedAt
            };
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

            var reservations = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return reservations.Select(ToDto).ToList();
        }

        public async Task<ReservationDto?> GetReservationByIdAsync(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Package)
                .FirstOrDefaultAsync(r => r.Id == id);

            return reservation == null ? null : ToDto(reservation);
        }

        public async Task<ReservationDto> CreateReservationAsync(CreateReservationDto createDto)
        {
            Package? package = null;

            if (createDto.PackageId.HasValue && createDto.PackageId.Value > 0)
                package = await _context.Packages.FirstOrDefaultAsync(p => p.Id == createDto.PackageId.Value && p.IsAvailable);

            if (package == null && !string.IsNullOrWhiteSpace(createDto.Paketa))
                package = await _context.Packages.FirstOrDefaultAsync(p => p.Paketa == createDto.Paketa && p.IsAvailable);

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
                .Where(r => r.PackageId == package.Id && !r.IsCancelled)
                .SumAsync(r => (int?)r.NrPersonave) ?? 0;

            if (alreadyReserved + createDto.NrPersonave > maxPersons)
            {
                var remaining = Math.Max(0, maxPersons - alreadyReserved);
                throw new ArgumentException($"Kapaciteti maksimal i kësaj pakete është {maxPersons} persona. Kanë mbetur vetëm {remaining} vende të lira.");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == createDto.Email);
            if (customer == null)
            {
                customer = new Customer { Emri = createDto.Emri, Email = createDto.Email };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var unitPrice = string.Equals(createDto.Role, "B2B", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(package.Cmimi * 0.90m, 2)
                : package.Cmimi;

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
                Cmimi = unitPrice * createDto.NrPersonave,
                IsConfirmed = false
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return (await GetReservationByIdAsync(reservation.Id))!;
        }

        public async Task<bool> UpdateReservationStatusAsync(int id, bool isConfirmed)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null || reservation.IsCancelled)
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

        public async Task<(bool Success, string Message, ReservationDto? Reservation)> CancelReservationAsync(int id, string email, string? reason)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Package)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return (false, "Rezervimi nuk u gjet.", null);

            if (!string.Equals(reservation.Customer.Email, email, StringComparison.OrdinalIgnoreCase))
                return (false, "Nuk ke të drejtë të anulosh këtë rezervim.", ToDto(reservation));

            var policy = EvaluateCancellationPolicy(reservation);
            if (!policy.CanCancel)
                return (false, policy.Message, ToDto(reservation));

            reservation.IsCancelled = true;
            reservation.CancelledAt = DateTime.UtcNow;
            reservation.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Anulim nga klienti" : reason.Trim();
            reservation.CancellationFee = policy.CancellationFee;
            reservation.RefundAmount = policy.RefundAmount;
            reservation.RefundStatus = policy.RefundStatus;
            reservation.IsConfirmed = false;

            await _context.SaveChangesAsync();
            return (true, $"Rezervimi u anulua me sukses. {policy.RefundStatus}: rimbursim {policy.RefundAmount:0.00} EUR, fee {policy.CancellationFee:0.00} EUR.", ToDto(reservation));
        }

        public async Task<bool> ConfirmAllReservationsAsync()
        {
            var reservations = await _context.Reservations.Where(r => !r.IsConfirmed && !r.IsCancelled).ToListAsync();
            foreach (var reservation in reservations)
                reservation.IsConfirmed = true;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
