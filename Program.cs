using HawksoftDataSync.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HawksoftDataSync;

class Program
{
    static async Task Main(string[] args)
    {
        // Create host builder with services
        var host = CreateHostBuilder(args).Build();

        try
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var phoneNumberSyncService = host.Services.GetRequiredService<PhoneNumberSyncService>();

            logger.LogInformation("Hawksoft Data Sync Started");

            // Parse command line arguments
            var options = ParseArguments(args);

            List<HawksoftDataSync.Models.PhoneNumberSync> phoneNumberSyncs;

            switch (options.Mode)
            {
                case SyncMode.Full:
                    logger.LogInformation("Running full sync");
                    phoneNumberSyncs = await phoneNumberSyncService.SyncAllPhoneNumbersAsync();
                    break;

                case SyncMode.Incremental:
                    var since = options.Since ?? DateTime.Now.AddDays(-7); // Default to last 7 days
                    logger.LogInformation("Running incremental sync since {Since}", since);
                    phoneNumberSyncs = await phoneNumberSyncService.SyncChangedPhoneNumbersAsync(since);
                    break;

                case SyncMode.SingleClient:
                    if (!options.ClientNumber.HasValue)
                    {
                        logger.LogError("Client number is required for single client sync");
                        ShowUsage();
                        return;
                    }
                    logger.LogInformation("Running single client sync for client {ClientNumber}", options.ClientNumber);
                    phoneNumberSyncs = await phoneNumberSyncService.SyncSpecificClientPhoneNumbersAsync(options.ClientNumber.Value);
                    break;

                default:
                    logger.LogError("Invalid sync mode specified");
                    ShowUsage();
                    return;
            }

            // Print summary
            phoneNumberSyncService.PrintSummary(phoneNumberSyncs);

            // Export data if requested
            if (!string.IsNullOrWhiteSpace(options.OutputPath))
            {
                if (options.OutputPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    await phoneNumberSyncService.ExportToCsvAsync(phoneNumberSyncs, options.OutputPath);
                }
                else if (options.OutputPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    await phoneNumberSyncService.ExportToJsonAsync(phoneNumberSyncs, options.OutputPath);
                }
                else
                {
                    // Default to JSON
                    var jsonPath = options.OutputPath.EndsWith(".json") ? options.OutputPath : options.OutputPath + ".json";
                    await phoneNumberSyncService.ExportToJsonAsync(phoneNumberSyncs, jsonPath);
                }
            }

            logger.LogInformation("Hawksoft Data Sync Completed Successfully");
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetService<ILogger<Program>>();
            logger?.LogError(ex, "An error occurred during sync");
            throw;
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Add environment variables
                config.AddEnvironmentVariables();

                // Add .env file if it exists
                var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
                if (File.Exists(envFile))
                {
                    var envVars = new Dictionary<string, string?>();
                    foreach (var line in File.ReadAllLines(envFile))
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                            continue;

                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            envVars[parts[0].Trim()] = parts[1].Trim().Trim('"');
                        }
                    }
                    config.AddInMemoryCollection(envVars);
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient<HawksoftApiService>();
                services.AddScoped<PhoneNumberSyncService>();
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });

    static SyncOptions ParseArguments(string[] args)
    {
        var options = new SyncOptions { Mode = SyncMode.Full };

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--mode":
                case "-m":
                    if (i + 1 < args.Length)
                    {
                        if (Enum.TryParse<SyncMode>(args[i + 1], true, out var mode))
                        {
                            options.Mode = mode;
                        }
                        i++;
                    }
                    break;

                case "--since":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        if (DateTime.TryParse(args[i + 1], out var since))
                        {
                            options.Since = since;
                        }
                        i++;
                    }
                    break;

                case "--client":
                case "-c":
                    if (i + 1 < args.Length)
                    {
                        if (int.TryParse(args[i + 1], out var clientNumber))
                        {
                            options.ClientNumber = clientNumber;
                        }
                        i++;
                    }
                    break;

                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputPath = args[i + 1];
                        i++;
                    }
                    break;

                case "--help":
                case "-h":
                    ShowUsage();
                    Environment.Exit(0);
                    break;
            }
        }

        return options;
    }

    static void ShowUsage()
    {
        Console.WriteLine("Hawksoft Data Sync - Phone Number to Customer Number Sync");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  HawksoftDataSync [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -m, --mode <mode>        Sync mode: Full, Incremental, or SingleClient (default: Full)");
        Console.WriteLine("  -s, --since <date>       For incremental sync: sync changes since this date (default: 7 days ago)");
        Console.WriteLine("  -c, --client <number>    For single client sync: client number to sync");
        Console.WriteLine("  -o, --output <path>      Output file path (.json or .csv)");
        Console.WriteLine("  -h, --help               Show this help message");
        Console.WriteLine();
        Console.WriteLine("Environment Variables Required:");
        Console.WriteLine("  API_USER     - Hawksoft API user ID");
        Console.WriteLine("  API_PASS     - Hawksoft API password");
        Console.WriteLine("  AGENCY_ID    - Agency ID");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  HawksoftDataSync --mode Full --output phone_numbers.json");
        Console.WriteLine("  HawksoftDataSync --mode Incremental --since \"2025-01-01\" --output changes.csv");
        Console.WriteLine("  HawksoftDataSync --mode SingleClient --client 123 --output client_123.json");
    }
}

public class SyncOptions
{
    public SyncMode Mode { get; set; }
    public DateTime? Since { get; set; }
    public int? ClientNumber { get; set; }
    public string? OutputPath { get; set; }
}

public enum SyncMode
{
    Full,
    Incremental,
    SingleClient
}