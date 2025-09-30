using Aspire.ApiService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Models;
using System.Text.Json;

namespace Aspire.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<UserController> _logger;

    public UserController(ApplicationDbContext context, IDistributedCache cache, ILogger<UserController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        const string cacheKey = "users:all";
        
        // Prøv at hente fra cache først
        var cachedUsers = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedUsers))
        {
            _logger.LogInformation("Bruger hentet fra cache");
            var users = JsonSerializer.Deserialize<IEnumerable<User>>(cachedUsers);
            return Ok(users);
        }

        // Hvis ikke i cache, hent fra database
        _logger.LogInformation("Henter brugere fra database og cacher");
        var usersFromDb = await _context.Users.ToListAsync();
        
        // Cache i 5 minutter
        var serializedUsers = JsonSerializer.Serialize(usersFromDb);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(cacheKey, serializedUsers, cacheOptions);
        
        return Ok(usersFromDb);
    }

    /// <summary>
    /// Hent brugere direkte fra database (ingen cache)
    /// </summary>
    [HttpGet("from-database")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsersFromDatabase()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Henter brugere direkte fra database");
        
        var users = await _context.Users.ToListAsync();
        
        stopwatch.Stop();
        _logger.LogInformation("Database query completed in {ElapsedMs}ms for {UserCount} users", 
            stopwatch.ElapsedMilliseconds, users.Count);
        
        return Ok(new
        {
            source = "Database",
            userCount = users.Count,
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            users = users
        });
    }

    /// <summary>
    /// Hent brugere fra Redis cache (kun cache)
    /// </summary>
    [HttpGet("from-cache")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsersFromCache()
    {
        const string cacheKey = "users:all";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Henter brugere fra Redis cache");
        
        var cachedUsers = await _cache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(cachedUsers))
        {
            stopwatch.Stop();
            _logger.LogWarning("Ingen data i cache - brug /api/User først for at cache data");
            
            return NotFound(new
            {
                source = "Cache",
                message = "Ingen data i cache. Kald /api/User først for at cache data.",
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }

        var users = JsonSerializer.Deserialize<IEnumerable<User>>(cachedUsers);
        stopwatch.Stop();
        
        _logger.LogInformation("Cache query completed in {ElapsedMs}ms for {UserCount} users", 
            stopwatch.ElapsedMilliseconds, users?.Count() ?? 0);
        
        return Ok(new
        {
            source = "Redis Cache",
            userCount = users?.Count() ?? 0,
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            users = users
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var cacheKey = $"user:{id}";
        
        // Prøv at hente fra cache først
        var cachedUser = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedUser))
        {
            _logger.LogInformation("Bruger {UserId} hentet fra cache", id);
            var user = JsonSerializer.Deserialize<User>(cachedUser);
            return Ok(user);
        }

        // Hvis ikke i cache, hent fra database
        _logger.LogInformation("Henter bruger {UserId} fra database og cacher", id);
        var userFromDb = await _context.Users.FindAsync(id);

        if (userFromDb == null)
        {
            return NotFound();
        }

        // Cache i 10 minutter
        var serializedUser = JsonSerializer.Serialize(userFromDb);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        await _cache.SetStringAsync(cacheKey, serializedUser, cacheOptions);

        return Ok(userFromDb);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Invalider cache efter oprettelse
        await InvalidateUserCache();
        _logger.LogInformation("Ny bruger oprettet med ID {UserId} - cache invalidated", user.Id);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        if (id != user.Id)
        {
            return BadRequest();
        }

        _context.Entry(user).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            
            // Invalider cache efter opdatering
            await InvalidateUserCache();
            await InvalidateUserCache(id);
            _logger.LogInformation("Bruger {UserId} opdateret - cache invalidated", id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Invalider cache efter sletning
        await InvalidateUserCache();
        await InvalidateUserCache(id);
        _logger.LogInformation("Bruger {UserId} slettet - cache invalidated", id);

        return NoContent();
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.Id == id);
    }

    /// <summary>
    /// Invaliderer alle user-relaterede cache entries
    /// </summary>
    private async Task InvalidateUserCache()
    {
        await _cache.RemoveAsync("users:all");
        _logger.LogDebug("Cache invalidated for users:all");
    }

    /// <summary>
    /// Invaliderer cache for en specifik bruger
    /// </summary>
    private async Task InvalidateUserCache(int userId)
    {
        await _cache.RemoveAsync($"user:{userId}");
        _logger.LogDebug("Cache invalidated for user:{UserId}", userId);
    }

    /// <summary>
    /// Test endpoint til at se cache status
    /// </summary>
    [HttpGet("cache/status")]
    public async Task<IActionResult> GetCacheStatus()
    {
        var allUsersKey = "users:all";
        var allUsersExists = !string.IsNullOrEmpty(await _cache.GetStringAsync(allUsersKey));

        return Ok(new
        {
            allUsersCached = allUsersExists,
            message = "Cache status checked - IDistributedCache doesn't support key enumeration",
            note = "Use cache/clear to clear all user-related cache entries"
        });
    }

    /// <summary>
    /// Test endpoint til at rydde cache
    /// </summary>
    [HttpPost("cache/clear")]
    public async Task<IActionResult> ClearCache()
    {
        // Ryd alle kendte cache keys
        await _cache.RemoveAsync("users:all");
        
        // Note: IDistributedCache understøtter ikke key enumeration
        // Så vi rydder kun de kendte keys
        _logger.LogInformation("Cache cleared - users:all removed");
        
        return Ok(new { message = "Cache cleared - users:all removed" });
    }

    /// <summary>
    /// Generer test brugere til performance testing
    /// </summary>
    [HttpPost("generate-test-data")]
    public async Task<IActionResult> GenerateTestData([FromQuery] int count = 1000)
    {
        if (count < 1 || count > 10000)
        {
            return BadRequest(new { message = "Count must be between 1 and 10000" });
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Starter generering af {Count} test brugere", count);

        var users = new List<User>();
        var random = new Random();

        // Generer test brugere
        for (int i = 1; i <= count; i++)
        {
            users.Add(new User
            {
                Id = 0, // EF Core vil auto-generere ID
                Name = $"TestUser{i}",
                Email = $"testuser{i}@example.com"
            });
        }

        // Batch insert for bedre performance
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation("Genererede {Count} test brugere på {ElapsedMs}ms", count, elapsedMs);

        return Ok(new
        {
            message = $"Genererede {count} test brugere",
            count = count,
            elapsedMilliseconds = elapsedMs,
            averageTimePerUser = Math.Round((double)elapsedMs / count, 2)
        });
    }

    /// <summary>
    /// Performance test endpoint - sammenlign cache vs database
    /// </summary>
    [HttpGet("performance-test")]
    public async Task<IActionResult> PerformanceTest()
    {
        var results = new
        {
            DatabaseTest = await TestDatabasePerformance(),
            CacheTest = await TestCachePerformance()
        };

        return Ok(results);
    }

    private async Task<object> TestDatabasePerformance()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Ryd cache først for at sikre database test
        await _cache.RemoveAsync("users:all");
        
        var users = await _context.Users.ToListAsync();
        
        stopwatch.Stop();
        
        return new
        {
            source = "Database",
            userCount = users.Count,
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            averageTimePerUser = users.Count > 0 ? Math.Round((double)stopwatch.ElapsedMilliseconds / users.Count, 4) : 0
        };
    }

    private async Task<object> TestCachePerformance()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Først cache data
        var users = await _context.Users.ToListAsync();
        var serializedUsers = JsonSerializer.Serialize(users);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync("users:all", serializedUsers, cacheOptions);
        
        // Nu test cache read
        stopwatch.Restart();
        var cachedUsers = await _cache.GetStringAsync("users:all");
        var deserializedUsers = JsonSerializer.Deserialize<IEnumerable<User>>(cachedUsers);
        
        stopwatch.Stop();
        
        return new
        {
            source = "Cache",
            userCount = deserializedUsers?.Count() ?? 0,
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            averageTimePerUser = deserializedUsers?.Count() > 0 ? Math.Round((double)stopwatch.ElapsedMilliseconds / deserializedUsers.Count(), 4) : 0
        };
    }

    /// <summary>
    /// Ryd alle test brugere
    /// </summary>
    [HttpDelete("clear-test-data")]
    public async Task<IActionResult> ClearTestData()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Find alle test brugere
        var testUsers = await _context.Users
            .Where(u => u.Name.StartsWith("TestUser"))
            .ToListAsync();

        _context.Users.RemoveRange(testUsers);
        await _context.SaveChangesAsync();
        
        // Ryd også cache
        await _cache.RemoveAsync("users:all");
        
        stopwatch.Stop();
        
        _logger.LogInformation("Slettede {Count} test brugere på {ElapsedMs}ms", testUsers.Count, stopwatch.ElapsedMilliseconds);
        
        return Ok(new
        {
            message = $"Slettede {testUsers.Count} test brugere",
            deletedCount = testUsers.Count,
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds
        });
    }
}
