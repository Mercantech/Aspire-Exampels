namespace Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    public enum Role
    {
        Admin,
        User
    }

    // Frisør Booking System Models
    public class Customer
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    public class Stylist
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public decimal HourlyRate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<StylistService> StylistServices { get; set; } = new List<StylistService>();
    }

    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public int DurationMinutes { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<StylistService> StylistServices { get; set; } = new List<StylistService>();
        public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    }

    public class StylistService
    {
        public int Id { get; set; }
        public int StylistId { get; set; }
        public int ServiceId { get; set; }
        public decimal CustomPrice { get; set; }
        public int CustomDurationMinutes { get; set; }
        public bool IsAvailable { get; set; } = true;
        
        // Navigation properties
        public virtual Stylist Stylist { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
    }

    public class Booking
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int StylistId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Confirmed"; // Confirmed, Completed, Cancelled, NoShow
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        
        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual Stylist Stylist { get; set; } = null!;
        public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    }

    public class BookingService
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int ServiceId { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public string Notes { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual Booking Booking { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
    }

    public class Salon
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Stylist> Stylists { get; set; } = new List<Stylist>();
    }

    public enum BookingStatus
    {
        Confirmed,
        Completed,
        Cancelled,
        NoShow
    }

    public enum ServiceCategory
    {
        Haircut,
        Coloring,
        Styling,
        Treatment,
        Beard,
        Eyebrows
    }
}
