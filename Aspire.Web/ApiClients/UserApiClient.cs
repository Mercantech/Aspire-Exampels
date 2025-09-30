using System.Text.Json;

namespace Aspire.Web.ApiClients;

public class UserApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserApiClient> _logger;

    public UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserApiResponse?> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 UserApiClient.GetUsersAsync() - Starter API kald");
        _logger.LogInformation("📡 BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
        _logger.LogInformation("🎯 Endpoint: /api/User");
        
        try
        {
            var response = await _httpClient.GetAsync("/api/User", cancellationToken);
            _logger.LogInformation("📊 Response Status: {StatusCode}", response.StatusCode);
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("📄 Response Content Length: {Length} characters", content.Length);
            _logger.LogInformation("📄 Response Content Preview: {Preview}", content.Length > 200 ? content.Substring(0, 200) + "..." : content);
            
            var users = JsonSerializer.Deserialize<IEnumerable<User>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            var result = new UserApiResponse
            {
                Source = "Cache/Database",
                UserCount = users?.Count() ?? 0,
                ElapsedMilliseconds = 0, // API returnerer ikke timing info
                Users = users ?? new List<User>()
            };
            
            _logger.LogInformation("✅ UserApiClient.GetUsersAsync() - Succes! Hentede {Count} brugere", result.UserCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ UserApiClient.GetUsersAsync() - Fejl: {Message}", ex.Message);
            throw new InvalidOperationException($"Fejl ved hentning af brugere: {ex.Message}", ex);
        }
    }

    public async Task<UserApiResponse?> GetUsersFromDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 UserApiClient.GetUsersFromDatabaseAsync() - Starter API kald");
        _logger.LogInformation("🎯 Endpoint: /api/User/from-database");
        
        try
        {
            var response = await _httpClient.GetAsync("/api/User/from-database", cancellationToken);
            _logger.LogInformation("📊 Response Status: {StatusCode}", response.StatusCode);
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("📄 Response Content Length: {Length} characters", content.Length);
            
            var result = JsonSerializer.Deserialize<UserApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _logger.LogInformation("✅ UserApiClient.GetUsersFromDatabaseAsync() - Succes! Hentede {Count} brugere fra database", result?.UserCount ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ UserApiClient.GetUsersFromDatabaseAsync() - Fejl: {Message}", ex.Message);
            throw new InvalidOperationException($"Fejl ved hentning af brugere fra database: {ex.Message}", ex);
        }
    }

    public async Task<UserApiResponse?> GetUsersFromCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 UserApiClient.GetUsersFromCacheAsync() - Starter API kald");
        _logger.LogInformation("🎯 Endpoint: /api/User/from-cache");
        
        try
        {
            var response = await _httpClient.GetAsync("/api/User/from-cache", cancellationToken);
            _logger.LogInformation("📊 Response Status: {StatusCode}", response.StatusCode);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("⚠️ Cache ikke fundet - 404 response");
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("📄 404 Response Content: {Content}", content);
                
                var notFoundResult = JsonSerializer.Deserialize<UserApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("✅ UserApiClient.GetUsersFromCacheAsync() - Cache ikke fundet, returnerer 404 response");
                return notFoundResult;
            }
            
            response.EnsureSuccessStatusCode();
            
            var successContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("📄 Cache Response Content Length: {Length} characters", successContent.Length);
            
            var successResult = JsonSerializer.Deserialize<UserApiResponse>(successContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _logger.LogInformation("✅ UserApiClient.GetUsersFromCacheAsync() - Succes! Hentede {Count} brugere fra cache", successResult?.UserCount ?? 0);
            return successResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ UserApiClient.GetUsersFromCacheAsync() - Fejl: {Message}", ex.Message);
            throw new InvalidOperationException($"Fejl ved hentning af brugere fra cache: {ex.Message}", ex);
        }
    }

    public async Task<PerformanceTestResponse?> GetPerformanceTestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/User/performance-test", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<PerformanceTestResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fejl ved performance test: {ex.Message}", ex);
        }
    }

    public async Task<CacheStatusResponse?> GetCacheStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/User/cache/status", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CacheStatusResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fejl ved cache status: {ex.Message}", ex);
        }
    }

    public async Task<ApiResponse?> ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/User/cache/clear", null, cancellationToken);
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

    public async Task<TestDataResponse?> GenerateTestDataAsync(int count = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/User/generate-test-data?count={count}", null, cancellationToken);
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
            var response = await _httpClient.DeleteAsync("/api/User/clear-test-data", cancellationToken);
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
}

public record User(int Id, string Name, string Email);

public record UserApiResponse
{
    public string Source { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public IEnumerable<User> Users { get; set; } = new List<User>();
}

public record PerformanceTestResponse
{
    public object DatabaseTest { get; set; } = new();
    public object CacheTest { get; set; } = new();
}

public record CacheStatusResponse
{
    public bool AllUsersCached { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public record ApiResponse
{
    public string Message { get; set; } = string.Empty;
}

public record TestDataResponse
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public double AverageTimePerUser { get; set; }
    public int? DeletedCount { get; set; }
}
