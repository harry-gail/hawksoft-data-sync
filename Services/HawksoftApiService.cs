using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HawksoftDataSync.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HawksoftDataSync.Services;

public class HawksoftApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HawksoftApiService> _logger;
    private readonly string _baseUrl;
    private readonly string _agencyId;

    public HawksoftApiService(HttpClient httpClient, IConfiguration configuration, ILogger<HawksoftApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["BASE_URL"] ?? throw new ArgumentException("BASE_URL not configured");
        _agencyId = configuration["AGENCY_ID"] ?? throw new ArgumentException("AGENCY_ID not configured");

        // Setup authentication
        var apiUser = configuration["API_USER"] ?? throw new ArgumentException("API_USER not configured");
        var apiPass = configuration["API_PASS"] ?? throw new ArgumentException("API_PASS not configured");

        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiUser}:{apiPass}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
    }

    public async Task<List<int>> GetAllClientNumbersAsync()
    {
        var url = $"{_baseUrl}/vendor/agency/{_agencyId}/clients?version=3.0";

        try
        {
            _logger.LogInformation("Fetching all client numbers from: {Url}", url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var clientNumbers = JsonSerializer.Deserialize<List<int>>(jsonString);

            _logger.LogInformation("Retrieved {Count} client numbers", clientNumbers?.Count ?? 0);
            return clientNumbers ?? new List<int>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching client numbers");
            throw;
        }
    }

    public async Task<List<int>> GetChangedClientNumbersAsync(DateTime since)
    {
        var sinceString = since.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"{_baseUrl}/vendor/agency/{_agencyId}/clients?version=3.0&asOf={sinceString}";

        try
        {
            _logger.LogInformation("Fetching changed client numbers since {Since} from: {Url}", since, url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var clientNumbers = JsonSerializer.Deserialize<List<int>>(jsonString);

            _logger.LogInformation("Retrieved {Count} changed client numbers", clientNumbers?.Count ?? 0);
            return clientNumbers ?? new List<int>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching changed client numbers since {Since}", since);
            throw;
        }
    }

    public async Task<ClientResponse?> GetClientRecordAsync(int clientNumber)
    {
        var url = $"{_baseUrl}/vendor/agency/{_agencyId}/client/{clientNumber}?version=3.0";

        try
        {
            _logger.LogDebug("Fetching client record for client {ClientNumber}", clientNumber);
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Client {ClientNumber} not found", clientNumber);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var clientRecord = JsonSerializer.Deserialize<ClientResponse>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogDebug("Retrieved client record for client {ClientNumber}", clientNumber);
            return clientRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching client record for client {ClientNumber}", clientNumber);
            throw;
        }
    }

    public async Task<List<ClientResponse>> GetAllClientRecordsAsync()
    {
        var clientNumbers = await GetAllClientNumbersAsync();
        var clientRecords = new List<ClientResponse>();

        _logger.LogInformation("Fetching detailed records for {Count} clients", clientNumbers.Count);

        // Process in batches to avoid overwhelming the API
        const int batchSize = 10;
        for (int i = 0; i < clientNumbers.Count; i += batchSize)
        {
            var batch = clientNumbers.Skip(i).Take(batchSize);
            var tasks = batch.Select(async clientNumber =>
            {
                try
                {
                    return await GetClientRecordAsync(clientNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch client {ClientNumber}", clientNumber);
                    return null;
                }
            });

            var batchResults = await Task.WhenAll(tasks);
            clientRecords.AddRange(batchResults.Where(r => r != null)!);

            // Small delay between batches to be respectful to the API
            if (i + batchSize < clientNumbers.Count)
            {
                await Task.Delay(100);
            }

            _logger.LogInformation("Processed batch {Current}/{Total}",
                Math.Min(i + batchSize, clientNumbers.Count), clientNumbers.Count);
        }

        _logger.LogInformation("Successfully retrieved {Count} client records", clientRecords.Count);
        return clientRecords;
    }

    public async Task<List<ClientResponse>> GetChangedClientRecordsAsync(DateTime since)
    {
        var clientNumbers = await GetChangedClientNumbersAsync(since);
        var clientRecords = new List<ClientResponse>();

        _logger.LogInformation("Fetching detailed records for {Count} changed clients", clientNumbers.Count);

        // Process in batches
        const int batchSize = 10;
        for (int i = 0; i < clientNumbers.Count; i += batchSize)
        {
            var batch = clientNumbers.Skip(i).Take(batchSize);
            var tasks = batch.Select(async clientNumber =>
            {
                try
                {
                    return await GetClientRecordAsync(clientNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch changed client {ClientNumber}", clientNumber);
                    return null;
                }
            });

            var batchResults = await Task.WhenAll(tasks);
            clientRecords.AddRange(batchResults.Where(r => r != null)!);

            if (i + batchSize < clientNumbers.Count)
            {
                await Task.Delay(100);
            }

            _logger.LogInformation("Processed changed batch {Current}/{Total}",
                Math.Min(i + batchSize, clientNumbers.Count), clientNumbers.Count);
        }

        _logger.LogInformation("Successfully retrieved {Count} changed client records", clientRecords.Count);
        return clientRecords;
    }
}