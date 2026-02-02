using Microsoft.EntityFrameworkCore;
using ErenTravel3API.Models;

namespace ErenTravel3API.Data
{
    public class TravelDbContext : DbContext
    {
        public TravelDbContext(DbContextOptions<TravelDbContext> options) : base(options)
        {
        }

        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reservations)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Package)
                .WithMany(p => p.Reservations)
                .HasForeignKey(r => r.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Package>().HasData(
                new Package { Id = 1, Paketa = "Budapest – Hungari", Kohezgjatja = 5, Cmimi = 299, Pershkrimi = "Fluturim + Hotel • Nisja nga Tirana" },
                new Package { Id = 2, Paketa = "Pragë – Çeki", Kohezgjatja = 4, Cmimi = 305, Pershkrimi = "Hotel 4★ • Vizita panoramike" },
                new Package { Id = 3, Paketa = "Dubai – Emiratet", Kohezgjatja = 5, Cmimi = 699, Pershkrimi = "Guide • Hotel 4★ • Safari në shkretëtirë" },
                new Package { Id = 4, Paketa = "Paris – Francë", Kohezgjatja = 4, Cmimi = 399, Pershkrimi = "Guidë • Hotel • Qyteti i dritave" },
                new Package { Id = 5, Paketa = "DisneyLand Paris – Francë", Kohezgjatja = 5, Cmimi = 499, Pershkrimi = "Guide • Hotel 4★ • Argëtim familjar" },
                new Package { Id = 6, Paketa = "Venecia, Verona – Itali", Kohezgjatja = 4, Cmimi = 344, Pershkrimi = "Guide • Hotel 4★ • Qytetet romantike" },
                new Package { Id = 7, Paketa = "Firence – Itali", Kohezgjatja = 2, Cmimi = 99, Pershkrimi = "Hotel 3★ • Arti dhe kultura" },
                new Package { Id = 8, Paketa = "Bruge – Belgjikë", Kohezgjatja = 3, Cmimi = 179, Pershkrimi = "Fluturim + Hotel + Mëngjes" },
                new Package { Id = 9, Paketa = "Stockholm – Suedi", Kohezgjatja = 4, Cmimi = 149, Pershkrimi = "Fluturim + Hotel • Kryeqyteti skandinav" },
                new Package { Id = 10, Paketa = "Strasburg - Francë • Zyrih - Zvicërr", Kohezgjatja = 4, Cmimi = 419, Pershkrimi = "Guide • Hotel 4★ • Dy vende" },
                new Package { Id = 11, Paketa = "Arabi Saudite", Kohezgjatja = 7, Cmimi = 699, Pershkrimi = "Guide • Hotel 4★ • Eksplorim kulturor" },
                new Package { Id = 12, Paketa = "Jordani", Kohezgjatja = 6, Cmimi = 1199, Pershkrimi = "Guide • Hotel 4★ • Petra dhe më shumë" },
                new Package { Id = 13, Paketa = "Bukuresht – Rumani", Kohezgjatja = 4, Cmimi = 175, Pershkrimi = "Fluturim + Hotel 3★" },
                new Package { Id = 14, Paketa = "Barcelonë – Spanjë", Kohezgjatja = 4, Cmimi = 399, Pershkrimi = "Fluturim + Hotel • Oferta fundviti" },
                new Package { Id = 15, Paketa = "Alberobello – Itali", Kohezgjatja = 3, Cmimi = 135, Pershkrimi = "Fluturim + Hotel • Fshati karakteristik" },
                new Package { Id = 16, Paketa = "Korçë – Shqipëri", Kohezgjatja = 3, Cmimi = 249, Pershkrimi = "Hotel Kocibelli • Viti i Ri tradicional" }
            );
        }
    }
}