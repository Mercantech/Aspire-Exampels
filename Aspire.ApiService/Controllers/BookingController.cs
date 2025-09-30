using Aspire.ApiService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Models;
using System.Text.Json;

namespace Aspire.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<BookingController> _logger;

    public BookingController(ApplicationDbContext context, IDistributedCache cache, ILogger<BookingController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Hent alle bookinger med komplekse joins (Cache-First)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetBookings()
    {
        const string cacheKey = "bookings:all";
        
        // Prøv at hente fra cache først
        var cachedBookings = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedBookings))
        {
            _logger.LogInformation("Bookinger hentet fra cache");
            var bookings = JsonSerializer.Deserialize<IEnumerable<object>>(cachedBookings);
            return Ok(bookings);
        }

        // Hvis ikke i cache, hent fra database
        _logger.LogInformation("Henter bookinger fra database og cacher");
        var bookingsFromDb = await GetBookingsWithJoins();
        
        // Cache i 5 minutter
        var serializedBookings = JsonSerializer.Serialize(bookingsFromDb);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(cacheKey, serializedBookings, cacheOptions);
        
        return Ok(bookingsFromDb);
    }

    /// <summary>
    /// Hent bookinger direkte fra database (ingen cache) - Komplekse joins
    /// </summary>
    [HttpGet("from-database")]
    public async Task<ActionResult<object>> GetBookingsFromDatabase()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Henter bookinger direkte fra database med komplekse joins");
        
        var bookings = await GetBookingsWithJoins();
        
        stopwatch.Stop();
        _logger.LogInformation("Database query med joins completed in {ElapsedMs}ms for {BookingCount} bookings", 
            stopwatch.ElapsedMilliseconds, bookings.Count());
        
        return Ok(new
        {
            source = "Database",
            bookingCount = bookings.Count(),
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            averageTimePerBooking = bookings.Any() ? Math.Round((double)stopwatch.ElapsedMilliseconds / bookings.Count(), 4) : 0,
            bookings = bookings
        });
    }

    /// <summary>
    /// Hent bookinger fra Redis cache (kun cache)
    /// </summary>
    [HttpGet("from-cache")]
    public async Task<ActionResult<object>> GetBookingsFromCache()
    {
        const string cacheKey = "bookings:all";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Henter bookinger fra Redis cache");
        
        var cachedBookings = await _cache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(cachedBookings))
        {
            stopwatch.Stop();
            _logger.LogWarning("Ingen data i cache - brug /api/Booking først for at cache data");
            
            return NotFound(new
            {
                source = "Cache",
                message = "Ingen data i cache. Kald /api/Booking først for at cache data.",
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }

        var bookings = JsonSerializer.Deserialize<IEnumerable<object>>(cachedBookings);
        stopwatch.Stop();
        
        _logger.LogInformation("Cache query completed in {ElapsedMs}ms for {BookingCount} bookings", 
            stopwatch.ElapsedMilliseconds, bookings?.Count() ?? 0);
        
        return Ok(new
        {
            source = "Redis Cache",
            bookingCount = bookings?.Count() ?? 0,
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            averageTimePerBooking = bookings?.Any() == true ? Math.Round((double)stopwatch.ElapsedMilliseconds / bookings.Count(), 4) : 0,
            bookings = bookings
        });
    }

    /// <summary>
    /// Kompleks query med alle joins - Performance test
    /// </summary>
    [HttpGet("complex-query")]
    public async Task<ActionResult<object>> GetComplexQuery()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Meget kompleks query med multiple joins og aggregations
        var result = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Stylist)
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
            .Where(b => b.BookingDate >= DateTime.UtcNow.Date.AddDays(-30))
            .GroupBy(b => new { 
                b.Stylist.FirstName, 
                b.Stylist.LastName, 
                b.Stylist.Specialization,
                Month = b.BookingDate.Month,
                Year = b.BookingDate.Year
            })
            .Select(g => new
            {
                StylistName = $"{g.Key.FirstName} {g.Key.LastName}",
                Specialization = g.Key.Specialization,
                Month = g.Key.Month,
                Year = g.Key.Year,
                TotalBookings = g.Count(),
                TotalRevenue = g.Sum(b => b.TotalPrice),
                AverageBookingValue = g.Average(b => b.TotalPrice),
                Bookings = g.Select(b => new
                {
                    b.Id,
                    CustomerName = $"{b.Customer.FirstName} {b.Customer.LastName}",
                    b.BookingDate,
                    b.StartTime,
                    b.EndTime,
                    b.TotalPrice,
                    b.Status,
                    Services = b.BookingServices.Select(bs => new
                    {
                        ServiceName = bs.Service.Name,
                        ServicePrice = bs.Price,
                        Duration = bs.DurationMinutes
                    })
                })
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToListAsync();

        stopwatch.Stop();
        
        _logger.LogInformation("Kompleks query completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        
        return Ok(new
        {
            source = "Database - Complex Query",
            resultCount = result.Count,
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            description = "Stylist performance med bookings, services og revenue analysis",
            data = result
        });
    }

    /// <summary>
    /// Generer test data for frisør system
    /// </summary>
    [HttpPost("generate-test-data")]
    public async Task<ActionResult<object>> GenerateTestData([FromQuery] int customerCount = 100, [FromQuery] int stylistCount = 10, [FromQuery] int bookingCount = 500)
    {
        if (customerCount < 1 || customerCount > 1000 || stylistCount < 1 || stylistCount > 50 || bookingCount < 1 || bookingCount > 5000)
        {
            return BadRequest(new { message = "Invalid counts. Customer: 1-1000, Stylist: 1-50, Booking: 1-5000" });
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Starter generering af test data: {CustomerCount} kunder, {StylistCount} stylister, {BookingCount} bookinger", 
            customerCount, stylistCount, bookingCount);

        var random = new Random();
        var services = await _context.Services.ToListAsync();

        // Generer kunder
        var customers = new List<Customer>();
        for (int i = 1; i <= customerCount; i++)
        {
            customers.Add(new Customer
            {
                Id = 0,
                FirstName = $"Kunde{i}",
                LastName = $"Efternavn{i}",
                Email = $"kunde{i}@example.com",
                Phone = $"+45 {random.Next(10000000, 99999999)}",
                DateOfBirth = DateTime.UtcNow.AddYears(-random.Next(18, 80)),
                Notes = $"Test kunde {i}"
            });
        }

        _context.Customers.AddRange(customers);
        await _context.SaveChangesAsync();

        // Generer stylister
        var stylists = new List<Stylist>();
        var specializations = new[] { "Klipning", "Farve", "Højde", "Styling", "Skæg", "Bryn" };
        for (int i = 1; i <= stylistCount; i++)
        {
            stylists.Add(new Stylist
            {
                Id = 0,
                FirstName = $"Stylist{i}",
                LastName = $"Efternavn{i}",
                Email = $"stylist{i}@example.com",
                Phone = $"+45 {random.Next(10000000, 99999999)}",
                Specialization = specializations[random.Next(specializations.Length)],
                ExperienceYears = random.Next(1, 20),
                HourlyRate = random.Next(200, 800),
                IsActive = true
            });
        }

        _context.Stylists.AddRange(stylists);
        await _context.SaveChangesAsync();

        // Opret StylistService relationer
        var stylistServices = new List<StylistService>();
        foreach (var stylist in stylists)
        {
            var availableServices = services.Take(random.Next(3, services.Count + 1));
            foreach (var service in availableServices)
            {
                stylistServices.Add(new StylistService
                {
                    StylistId = stylist.Id,
                    ServiceId = service.Id,
                    CustomPrice = service.BasePrice * (decimal)(0.8 + random.NextDouble() * 0.4), // ±20% variation
                    CustomDurationMinutes = service.DurationMinutes + random.Next(-10, 11), // ±10 min variation
                    IsAvailable = true
                });
            }
        }

        _context.StylistServices.AddRange(stylistServices);
        await _context.SaveChangesAsync();

        // Generer bookinger
        var bookings = new List<Booking>();
        var statuses = new[] { "Confirmed", "Completed", "Cancelled", "NoShow" };
        
        for (int i = 1; i <= bookingCount; i++)
        {
            var customer = customers[random.Next(customers.Count)];
            var stylist = stylists[random.Next(stylists.Count)];
            var bookingDate = DateTime.UtcNow.Date.AddDays(random.Next(-30, 30));
            var startTime = new TimeSpan(random.Next(9, 17), random.Next(0, 60), 0);
            var duration = random.Next(30, 180); // 30 min til 3 timer
            var endTime = startTime.Add(TimeSpan.FromMinutes(duration));

            var booking = new Booking
            {
                Id = 0,
                CustomerId = customer.Id,
                StylistId = stylist.Id,
                BookingDate = bookingDate,
                StartTime = startTime,
                EndTime = endTime,
                TotalPrice = 0, // Vil blive beregnet
                Status = statuses[random.Next(statuses.Length)],
                Notes = $"Test booking {i}"
            };

            // Tilføj services til booking
            var availableStylistServices = stylistServices.Where(ss => ss.StylistId == stylist.Id).ToList();
            var selectedServices = availableStylistServices.Take(random.Next(1, 4)).ToList(); // 1-3 services

            var bookingServices = new List<BookingService>();
            decimal totalPrice = 0;

            foreach (var stylistService in selectedServices)
            {
                var bookingService = new BookingService
                {
                    BookingId = 0, // Vil blive sat efter booking er gemt
                    ServiceId = stylistService.ServiceId,
                    Price = stylistService.CustomPrice,
                    DurationMinutes = stylistService.CustomDurationMinutes,
                    Notes = $"Test service for booking {i}"
                };
                bookingServices.Add(bookingService);
                totalPrice += stylistService.CustomPrice;
            }

            booking.TotalPrice = totalPrice;
            booking.BookingServices = bookingServices;
            bookings.Add(booking);
        }

        _context.Bookings.AddRange(bookings);
        await _context.SaveChangesAsync();

        // Opdater BookingService med korrekte BookingId
        foreach (var booking in bookings)
        {
            foreach (var bookingService in booking.BookingServices)
            {
                bookingService.BookingId = booking.Id;
            }
        }

        await _context.SaveChangesAsync();

        stopwatch.Stop();
        _logger.LogInformation("Genererede test data på {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        return Ok(new
        {
            message = "Test data genereret succesfuldt",
            customers = customerCount,
            stylists = stylistCount,
            bookings = bookingCount,
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds
        });
    }

    /// <summary>
    /// Ryd test data
    /// </summary>
    [HttpDelete("clear-test-data")]
    public async Task<ActionResult<object>> ClearTestData()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Slet i korrekt rækkefølge pga. foreign keys
        var deletedCounts = new Dictionary<string, int>();

        // Slet BookingServices først
        var bookingServices = await _context.BookingServices.ToListAsync();
        _context.BookingServices.RemoveRange(bookingServices);
        deletedCounts["BookingServices"] = bookingServices.Count;

        // Slet Bookings
        var bookings = await _context.Bookings.ToListAsync();
        _context.Bookings.RemoveRange(bookings);
        deletedCounts["Bookings"] = bookings.Count;

        // Slet StylistServices
        var stylistServices = await _context.StylistServices.ToListAsync();
        _context.StylistServices.RemoveRange(stylistServices);
        deletedCounts["StylistServices"] = stylistServices.Count;

        // Slet Customers
        var customers = await _context.Customers.ToListAsync();
        _context.Customers.RemoveRange(customers);
        deletedCounts["Customers"] = customers.Count;

        // Slet Stylists
        var stylists = await _context.Stylists.ToListAsync();
        _context.Stylists.RemoveRange(stylists);
        deletedCounts["Stylists"] = stylists.Count;

        await _context.SaveChangesAsync();
        
        // Ryd også cache
        await _cache.RemoveAsync("bookings:all");
        
        stopwatch.Stop();
        
        _logger.LogInformation("Slettede test data på {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        
        return Ok(new
        {
            message = "Test data slettet",
            deletedCounts = deletedCounts,
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds
        });
    }

    /// <summary>
    /// Ryd cache
    /// </summary>
    [HttpPost("cache/clear")]
    public async Task<ActionResult<object>> ClearCache()
    {
        await _cache.RemoveAsync("bookings:all");
        _logger.LogInformation("Booking cache cleared");
        
        return Ok(new { message = "Booking cache cleared" });
    }

    private async Task<IEnumerable<object>> GetBookingsWithJoins()
    {
        return await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Stylist)
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
            .Select(b => new
            {
                b.Id,
                Customer = new
                {
                    b.Customer.Id,
                    b.Customer.FirstName,
                    b.Customer.LastName,
                    b.Customer.Email,
                    b.Customer.Phone
                },
                Stylist = new
                {
                    b.Stylist.Id,
                    b.Stylist.FirstName,
                    b.Stylist.LastName,
                    b.Stylist.Specialization,
                    b.Stylist.HourlyRate
                },
                b.BookingDate,
                b.StartTime,
                b.EndTime,
                b.TotalPrice,
                b.Status,
                b.Notes,
                Services = b.BookingServices.Select(bs => new
                {
                    bs.Service.Name,
                    bs.Service.Category,
                    bs.Price,
                    bs.DurationMinutes
                })
            })
            .ToListAsync();
    }
}
