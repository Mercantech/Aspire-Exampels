using Microsoft.AspNetCore.Mvc;

namespace Aspire.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoggingTestController : ControllerBase
{
    private readonly ILogger<LoggingTestController> _logger;

    public LoggingTestController(ILogger<LoggingTestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint der logger på forskellige niveauer
    /// </summary>
    [HttpGet("levels")]
    public IActionResult TestLogLevels()
    {
        _logger.LogTrace("Dette er en Trace log - mest detaljeret niveau");
        _logger.LogDebug("Dette er en Debug log - til debugging");
        _logger.LogInformation("Dette er en Information log - generel information");
        _logger.LogWarning("Dette er en Warning log - advarsel");
        _logger.LogError("Dette er en Error log - fejl");
        _logger.LogCritical("Dette er en Critical log - kritisk fejl");

        return Ok(new { message = "Logging test gennemført - tjek Aspire dashboard for logs" });
    }

    /// <summary>
    /// Test endpoint der logger med struktureret data
    /// </summary>
    [HttpGet("structured")]
    public IActionResult TestStructuredLogging()
    {
        var userId = 123;
        var action = "TestStructuredLogging";
        var timestamp = DateTime.UtcNow;

        _logger.LogInformation("Struktureret log: User {UserId} udførte {Action} på {Timestamp}", 
            userId, action, timestamp);

        _logger.LogInformation("Bruger aktivitet: {UserId} - {Action} - {Timestamp}", 
            userId, action, timestamp);

        return Ok(new { 
            message = "Struktureret logging test gennemført",
            userId,
            action,
            timestamp
        });
    }

    /// <summary>
    /// Test endpoint der simulerer en fejl og logger exception
    /// </summary>
    [HttpGet("exception")]
    public IActionResult TestExceptionLogging()
    {
        try
        {
            _logger.LogInformation("Simulerer en fejl...");
            
            // Simuler en fejl
            throw new InvalidOperationException("Dette er en test fejl for logging");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Der opstod en fejl i TestExceptionLogging: {Message}", ex.Message);
            
            return BadRequest(new { 
                message = "Fejl simuleret og logget",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test endpoint der logger performance metrics
    /// </summary>
    [HttpGet("performance")]
    public async Task<IActionResult> TestPerformanceLogging()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Performance test startet");

        // Simuler noget arbejde
        await Task.Delay(100);

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation("Performance test afsluttet - Varighed: {ElapsedMs}ms", elapsedMs);

        return Ok(new { 
            message = "Performance logging test gennemført",
            elapsedMilliseconds = elapsedMs
        });
    }

    /// <summary>
    /// Test endpoint der logger med scoped information
    /// </summary>
    [HttpGet("scoped")]
    public IActionResult TestScopedLogging()
    {
        using var scope = _logger.BeginScope("ScopedLoggingTest");
        
        _logger.LogInformation("Dette er en scoped log - del af en større operation");
        _logger.LogDebug("Debug information inden for scope");
        _logger.LogWarning("Advarsel inden for scope");

        return Ok(new { message = "Scoped logging test gennemført" });
    }

    /// <summary>
    /// Test endpoint der logger med custom properties
    /// </summary>
    [HttpGet("custom-properties")]
    public IActionResult TestCustomProperties()
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = Guid.NewGuid().ToString(),
            ["Environment"] = "Development",
            ["Version"] = "1.0.0"
        });

        _logger.LogInformation("Custom properties log med RequestId, Environment og Version");
        _logger.LogDebug("Debug med custom properties");
        _logger.LogWarning("Warning med custom properties");

        return Ok(new { 
            message = "Custom properties logging test gennemført",
            requestId = scope.ToString()
        });
    }

    /// <summary>
    /// Test endpoint der logger med forskellige log levels baseret på parameter
    /// </summary>
    [HttpGet("dynamic-level/{level}")]
    public IActionResult TestDynamicLogLevel(string level)
    {
        var logLevel = level.ToLower() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "information" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, "Dynamisk log på niveau: {LogLevel} - {Message}", 
            level, $"Dette er en {level} log");

        return Ok(new { 
            message = $"Dynamisk logging test gennemført med niveau: {level}",
            requestedLevel = level,
            actualLogLevel = logLevel.ToString()
        });
    }
}
