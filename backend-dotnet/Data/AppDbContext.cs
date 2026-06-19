using Microsoft.EntityFrameworkCore;
using BackendDotnet.Models;

namespace BackendDotnet.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Bus> Buses { get; set; } = null!;
        public DbSet<Trip> Trips { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Ticket> Tickets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration pour s'assurer que le numéro de siège est unique par trajet
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.TripId, r.SeatNumber })
                .IsUnique();

            // Rendre le nom de compagnie unique
            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Configurer le comportement ON DELETE pour les relations
            modelBuilder.Entity<Company>()
                .HasOne(c => c.Manager)
                .WithMany()
                .HasForeignKey(c => c.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Bus>()
                .HasOne(b => b.Company)
                .WithMany()
                .HasForeignKey(b => b.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Bus)
                .WithMany()
                .HasForeignKey(t => t.BusId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Trip)
                .WithMany()
                .HasForeignKey(r => r.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Reservation)
                .WithOne()
                .HasForeignKey<Payment>(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Reservation)
                .WithOne()
                .HasForeignKey<Ticket>(t => t.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
