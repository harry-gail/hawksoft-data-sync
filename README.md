# Hawksoft Data Sync

A C# console application that syncs phone numbers to customer numbers from the Hawksoft Integration API.

## Features

- **Full Sync**: Retrieve all client records and extract phone number mappings
- **Incremental Sync**: Retrieve only clients changed since a specific date
- **Single Client Sync**: Retrieve phone numbers for a specific client
- **Multiple Export Formats**: Export data to JSON or CSV
- **Phone Number Normalization**: Automatically formats phone numbers consistently
- **Person Association**: Links phone numbers to specific people when available
- **Batch Processing**: Handles large datasets efficiently with rate limiting
- **Comprehensive Logging**: Detailed logging for monitoring and debugging

## Setup

### Prerequisites

- .NET 8.0 SDK
- Hawksoft API credentials

### Installation

1. Clone or download this repository
2. Navigate to the project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```

### Configuration

Set up your environment variables. You can use either:

#### Option 1: Environment Variables

```bash
export API_USER="your_api_user_id"
export API_PASS="your_api_password"
export AGENCY_ID="your_agency_id"
```

#### Option 2: .env File

Create a `.env` file in the project root:

```
API_USER=your_api_user_id
API_PASS=your_api_password
AGENCY_ID=your_agency_id
```

## Usage

### Build the Application

```bash
dotnet build
```

### Run the Application

```bash
dotnet run [options]
```

### Command Line Options

| Option                  | Description                                        | Default      |
| ----------------------- | -------------------------------------------------- | ------------ |
| `-m, --mode <mode>`     | Sync mode: Full, Incremental, or SingleClient      | Full         |
| `-s, --since <date>`    | For incremental sync: sync changes since this date | 7 days ago   |
| `-c, --client <number>` | For single client sync: client number to sync      | -            |
| `-o, --output <path>`   | Output file path (.json or .csv)                   | Console only |
| `-h, --help`            | Show help message                                  | -            |

### Examples

#### Full Sync with JSON Export

```bash
dotnet run -- --mode Full --output phone_numbers.json
```

#### Incremental Sync with CSV Export

```bash
dotnet run -- --mode Incremental --since "2025-01-01" --output changes.csv
```

#### Single Client Sync

```bash
dotnet run -- --mode SingleClient --client 123 --output client_123.json
```

#### Quick Test (Console Output Only)

```bash
dotnet run -- --mode SingleClient --client 1
```

## Output Format

### Phone Number Sync Object

Each phone number mapping contains:

```json
{
  "clientNumber": 1,
  "phoneNumber": "(503) 777-7777",
  "phoneType": "CellPhone",
  "personId": "29e607c2-bfaf-4385-b9e0-1f4ff1f2856b",
  "personName": "LARRY LASTNAME",
  "priority": 100,
  "lastModified": "2023-11-30T22:06:01.8Z"
}
```

### CSV Format

```csv
ClientNumber,PhoneNumber,PhoneType,PersonId,PersonName,Priority,LastModified
1,"(503) 777-7777","CellPhone","29e607c2-bfaf-4385-b9e0-1f4ff1f2856b","LARRY LASTNAME",100,"2023-11-30 22:06:01"
```

## Phone Number Types

The application extracts the following phone number types:

- **WorkPhone**: Business phone numbers
- **CellPhone**: Mobile phone numbers
- **HomePhone**: Residential phone numbers

## Phone Number Normalization

Phone numbers are automatically normalized to the format `(XXX) XXX-XXXX`:

- Removes all non-digit characters
- Handles 10-digit numbers: `5037777777` → `(503) 777-7777`
- Handles 11-digit numbers with country code: `15037777777` → `(503) 777-7777`
- Preserves original format if parsing fails

## Person Association

When a phone number is associated with a specific person:

- The `personId` field contains the person's unique identifier
- The `personName` field contains the person's first and last name
- When not associated with a person, these fields are null (client-level contact)

## Error Handling

The application includes comprehensive error handling:

- API authentication errors
- Network connectivity issues
- Invalid client numbers
- Missing configuration
- Rate limiting protection

## Logging

The application provides detailed logging:

- **Information**: Progress updates and summaries
- **Warning**: Non-critical issues (e.g., client not found)
- **Error**: Critical errors with stack traces
- **Debug**: Detailed API calls and data processing

## Performance Considerations

- **Batch Processing**: Processes clients in batches of 10 to avoid overwhelming the API
- **Rate Limiting**: Includes small delays between batches
- **Memory Efficient**: Streams data rather than loading everything into memory
- **Parallel Processing**: Uses async/await for concurrent API calls within batches

## API Rate Limits

The application is designed to be respectful of the Hawksoft API:

- 100ms delay between batches
- Batch size of 10 concurrent requests
- Proper error handling for rate limit responses

## Troubleshooting

### Common Issues

1. **Authentication Failed**

   - Verify your `API_USER` and `API_PASS` are correct
   - Ensure credentials have appropriate permissions

2. **Agency Not Found**

   - Verify your `AGENCY_ID` is correct
   - Check if the agency exists in the system

3. **No Phone Numbers Found**

   - Verify clients have contact information
   - Check if phone number types are supported

4. **Network Errors**
   - Check internet connectivity
   - Verify API endpoint is accessible

### Debug Mode

For more detailed logging, you can modify the log level in `Program.cs`:

```csharp
builder.SetMinimumLevel(LogLevel.Debug);
```

## Development

### Project Structure

```
HawksoftDataSync/
├── Models/
│   └── ClientModels.cs      # Data models for API responses
├── Services/
│   ├── HawksoftApiService.cs        # API communication
│   └── PhoneNumberSyncService.cs    # Business logic
├── Program.cs               # Main application entry point
├── HawksoftDataSync.csproj  # Project configuration
└── README.md               # This file
```

### Building for Distribution

```bash
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
```

## License

This project is provided as-is for integration with the Hawksoft system.
