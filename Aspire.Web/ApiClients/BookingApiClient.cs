using System.Text.Json;

namespace Aspire.Web.ApiClients;

public class BookingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BookingApiClient> _logger;

    public BookingApiClient(HttpClient httpClient, ILogger<BookingApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BookingApiResponse?> GetBookingsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 BookingApiClient.GetBookingsAsync() - Starter API kald");
        _logger.LogInformation("📡 BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
        _logger.LogInformation("🎯 Endpoint: /api/Booking");
        
        try
        {
            var response = await _httpClient.GetAsync("/api/Booking", cancellationToken);
            _logger.LogInformation("📊 Response Status: {StatusCode}", response.StatusCode);
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("📄 Response Content Length: {Length} characters", content.Length);
            _logger.LogInformation("📄 Response Content Preview: {Preview}", content.Length > 200 ? content.Substring(0, 200) + "..." : content);
            
            var bookings = JsonSerializer.Deserialize<IEnumerable<object>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            var result = new BookingApiResponse
            {
                Source = "Cache/Database",
                BookingCount = bookings?.Count() ?? 0,
                ElapsedMilliseconds = 0,
                Bookings = bookings ?? new List<object>()
            };
            
            _logger.LogInformation("✅ BookingApiClient.GetBookingsAsync() - Succes! Hentede {Count} bookinger", result.BookingCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ BookingApiClient.GetBookingsAsync() - Fejl: {Message}", ex.Message);
            throw new InvalidOperationException($"Fejl ved hentning af bookinger: {ex.Message}", ex);
        }
    }

    public async Task<BookingApiResponse?> GetBookingsFromDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Booking/from-database", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<BookingApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fejl ved hentning af bookinger fra database: {ex.Message}", ex);
        }
    }

    public async Task<BookingApiResponse?> GetBookingsFromCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Booking/from-cache", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<BookingApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            
            response.EnsureSuccessStatusCode();
            
            var successContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<BookingApiResponse>(successContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fejl ved hentning af bookinger fra cache: {ex.Message}", ex);
        }
    }

    public async Task<ComplexQueryResponse?> GetComplexQueryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Booking/complex-query", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ComplexQueryResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fejl ved kompleks query: {ex.Message}", ex);
        }
    }

    public async Task<TestDataResponse?> GenerateTestDataAsync(int customerCount = 100, int stylistCount = 10, int bookingCount = 500, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/Booking/generate-test-data?customerCount={customerCount}&stylistCount={stylistCount}&bookingCount={bookingCount}", null, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TestDataResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fejl ved generering af test data: {ex.Message}", ex);
        }
    }

    public async Task<TestDataResponse?> ClearTestDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync("/api/Booking/clear-test-data", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TestDataResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fejl ved rydning af test data: {ex.Message}", ex);
        }
    }

    public async Task<ApiResponse?> ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/Booking/cache/clear", null, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fejl ved rydning af cache: {ex.Message}", ex);
        }
    }
}

public record BookingApiResponse
{
    public string Source { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public double AverageTimePerBooking { get; set; }
    public IEnumerable<object> Bookings { get; set; } = new List<object>();
}

public record ComplexQueryResponse
{
    public string Source { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string Description { get; set; } = string.Empty;
    public IEnumerable<object> Data { get; set; } = new List<object>();
}
