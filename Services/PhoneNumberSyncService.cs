using HawksoftDataSync.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HawksoftDataSync.Services;

public class PhoneNumberSyncService
{
    private readonly HawksoftApiService _apiService;
    private readonly ILogger<PhoneNumberSyncService> _logger;
    private readonly HashSet<string> _phoneTypes = new() { "WorkPhone", "CellPhone", "HomePhone" };

    public PhoneNumberSyncService(HawksoftApiService apiService, ILogger<PhoneNumberSyncService> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<List<PhoneNumberSync>> SyncAllPhoneNumbersAsync()
    {
        _logger.LogInformation("Starting full phone number sync");

        var clientRecords = await _apiService.GetAllClientRecordsAsync();
        var phoneNumberSyncs = ExtractPhoneNumbers(clientRecords);

        _logger.LogInformation("Extracted {Count} phone number mappings", phoneNumberSyncs.Count);

        return phoneNumberSyncs;
    }

    public async Task<List<PhoneNumberSync>> SyncChangedPhoneNumbersAsync(DateTime since)
    {
        _logger.LogInformation("Starting incremental phone number sync since {Since}", since);

        var clientRecords = await _apiService.GetChangedClientRecordsAsync(since);
        var phoneNumberSyncs = ExtractPhoneNumbers(clientRecords);

        _logger.LogInformation("Extracted {Count} changed phone number mappings", phoneNumberSyncs.Count);

        return phoneNumberSyncs;
    }

    public async Task<List<PhoneNumberSync>> SyncSpecificClientPhoneNumbersAsync(int clientNumber)
    {
        _logger.LogInformation("Starting phone number sync for client {ClientNumber}", clientNumber);

        var clientRecord = await _apiService.GetClientRecordAsync(clientNumber);
        if (clientRecord == null)
        {
            _logger.LogWarning("Client {ClientNumber} not found", clientNumber);
            return new List<PhoneNumberSync>();
        }

        var phoneNumberSyncs = ExtractPhoneNumbers(new List<ClientResponse> { clientRecord });

        _logger.LogInformation("Extracted {Count} phone number mappings for client {ClientNumber}",
            phoneNumberSyncs.Count, clientNumber);

        return phoneNumberSyncs;
    }

    private List<PhoneNumberSync> ExtractPhoneNumbers(List<ClientResponse> clientRecords)
    {
        var phoneNumberSyncs = new List<PhoneNumberSync>();

        foreach (var client in clientRecords)
        {
            if (client.Contacts == null) continue;

            // Extract phone number contacts
            var phoneContacts = client.Contacts
                .Where(c => _phoneTypes.Contains(c.Type ?? ""))
                .Where(c => !string.IsNullOrWhiteSpace(c.Data))
                .ToList();

            foreach (var contact in phoneContacts)
            {
                var personName = GetPersonName(client.People, contact.PersonId);

                var phoneSync = new PhoneNumberSync
                {
                    ClientNumber = client.ClientNumber,
                    PhoneNumber = CleanPhoneNumber(contact.Data),
                    PhoneType = contact.Type,
                    PersonId = contact.PersonId,
                    PersonName = personName,
                    Priority = contact.Priority,
                    LastModified = contact.Modified
                };

                phoneNumberSyncs.Add(phoneSync);

                _logger.LogDebug("Extracted phone: Client {ClientNumber} - {PhoneType}: {PhoneNumber} ({PersonName})",
                    client.ClientNumber, contact.Type, phoneSync.PhoneNumber, personName ?? "Client");
            }
        }

        return phoneNumberSyncs;
    }

    private string? GetPersonName(List<Person>? people, string? personId)
    {
        if (string.IsNullOrWhiteSpace(personId) || people == null)
            return null;

        var person = people.FirstOrDefault(p => p.Id == personId);
        if (person == null) return null;

        var names = new List<string>();
        if (!string.IsNullOrWhiteSpace(person.FirstName)) names.Add(person.FirstName);
        if (!string.IsNullOrWhiteSpace(person.LastName)) names.Add(person.LastName);

        return names.Count > 0 ? string.Join(" ", names) : null;
    }

    private static string? CleanPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Remove all non-digit characters
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Handle different formats
        if (digits.Length == 10)
        {
            // Format as (XXX) XXX-XXXX
            return $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
        }
        else if (digits.Length == 11 && digits[0] == '1')
        {
            // Remove leading 1 and format
            var localDigits = digits[1..];
            return $"({localDigits[..3]}) {localDigits[3..6]}-{localDigits[6..]}";
        }

        // Return original if can't parse
        return phoneNumber;
    }

    public async Task ExportToJsonAsync(List<PhoneNumberSync> phoneNumberSyncs, string filePath)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(phoneNumberSyncs, options);
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Exported {Count} phone number mappings to {FilePath}",
                phoneNumberSyncs.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting phone number mappings to {FilePath}", filePath);
            throw;
        }
    }

    public async Task ExportToCsvAsync(List<PhoneNumberSync> phoneNumberSyncs, string filePath)
    {
        try
        {
            var csv = new List<string>
            {
                "ClientNumber,PhoneNumber,PhoneType,PersonId,PersonName,Priority,LastModified"
            };

            foreach (var phone in phoneNumberSyncs)
            {
                csv.Add($"{phone.ClientNumber}," +
                       $"\"{phone.PhoneNumber}\"," +
                       $"\"{phone.PhoneType}\"," +
                       $"\"{phone.PersonId}\"," +
                       $"\"{phone.PersonName}\"," +
                       $"{phone.Priority}," +
                       $"\"{phone.LastModified:yyyy-MM-dd HH:mm:ss}\"");
            }

            await File.WriteAllLinesAsync(filePath, csv);

            _logger.LogInformation("Exported {Count} phone number mappings to {FilePath}",
                phoneNumberSyncs.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting phone number mappings to {FilePath}", filePath);
            throw;
        }
    }

    public void PrintSummary(List<PhoneNumberSync> phoneNumberSyncs)
    {
        var totalClients = phoneNumberSyncs.Select(p => p.ClientNumber).Distinct().Count();
        var totalPhoneNumbers = phoneNumberSyncs.Count;
        var phoneTypeGroups = phoneNumberSyncs
            .Where(p => p.PhoneType != null)
            .GroupBy(p => p.PhoneType!)
            .ToDictionary(g => g.Key, g => g.Count());

        _logger.LogInformation("=== Phone Number Sync Summary ===");
        _logger.LogInformation("Total Clients with Phone Numbers: {TotalClients}", totalClients);
        _logger.LogInformation("Total Phone Number Entries: {TotalPhoneNumbers}", totalPhoneNumbers);

        foreach (var phoneType in phoneTypeGroups)
        {
            _logger.LogInformation("{PhoneType}: {Count}", phoneType.Key, phoneType.Value);
        }

        // Show some examples
        var examples = phoneNumberSyncs.Take(5).ToList();
        if (examples.Count > 0)
        {
            _logger.LogInformation("=== Examples ===");
            foreach (var example in examples)
            {
                _logger.LogInformation("Client {ClientNumber}: {PhoneType} {PhoneNumber} ({PersonName})",
                    example.ClientNumber, example.PhoneType, example.PhoneNumber,
                    example.PersonName ?? "Client");
            }
        }
    }
}