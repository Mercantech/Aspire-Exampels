using Microsoft.EntityFrameworkCore;
using Models;

namespace Aspire.ApiService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Original User table
    public DbSet<User> Users { get; set; }
    
    // Frisør Booking System tables
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Stylist> Stylists { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<StylistService> StylistServices { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingService> BookingServices { get; set; }
    public DbSet<Salon> Salons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Stylist
        modelBuilder.Entity<Stylist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Specialization).HasMaxLength(100);
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Service
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.BasePrice).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure StylistService (Many-to-Many with custom properties)
        modelBuilder.Entity<StylistService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomPrice).HasColumnType("decimal(10,2)");
            
            entity.HasOne(e => e.Stylist)
                .WithMany(s => s.StylistServices)
                .HasForeignKey(e => e.StylistId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Service)
                .WithMany(s => s.StylistServices)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.StylistId, e.ServiceId }).IsUnique();
        });

        // Configure Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BookingDate).IsRequired();
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime).IsRequired();
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Stylist)
                .WithMany(s => s.Bookings)
                .HasForeignKey(e => e.StylistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure BookingService (Many-to-Many with custom properties)
        modelBuilder.Entity<BookingService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(e => e.Booking)
                .WithMany(b => b.BookingServices)
                .HasForeignKey(e => e.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Service)
                .WithMany(s => s.BookingServices)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Salon
        modelBuilder.Entity<Salon>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Services
        var services = new[]
        {
            new Service { Id = 1, Name = "Klipning", Description = "Standard klipning", BasePrice = 300, DurationMinutes = 30, Category = "Haircut", IsActive = true },
            new Service { Id = 2, Name = "Farve", Description = "Hårfarve", BasePrice = 500, DurationMinutes = 60, Category = "Coloring", IsActive = true },
            new Service { Id = 3, Name = "Højde", Description = "Højde behandling", BasePrice = 400, DurationMinutes = 45, Category = "Treatment", IsActive = true },
            new Service { Id = 4, Name = "Skæg trim", Description = "Skæg trimning", BasePrice = 200, DurationMinutes = 20, Category = "Beard", IsActive = true },
            new Service { Id = 5, Name = "Bryn", Description = "Bryn formning", BasePrice = 150, DurationMinutes = 15, Category = "Eyebrows", IsActive = true },
            new Service { Id = 6, Name = "Styling", Description = "Hår styling", BasePrice = 250, DurationMinutes = 25, Category = "Styling", IsActive = true }
        };

        modelBuilder.Entity<Service>().HasData(services);

        // Seed Salon
        modelBuilder.Entity<Salon>().HasData(new Salon
        {
            Id = 1,
            Name = "Elegant Hair Salon",
            Address = "Hovedgade 123, 2100 København",
            Phone = "+45 12 34 56 78",
            Email = "info@eleganthair.dk",
            OpenTime = new TimeSpan(9, 0, 0),
            CloseTime = new TimeSpan(18, 0, 0),
            IsActive = true
        });
    }
}
