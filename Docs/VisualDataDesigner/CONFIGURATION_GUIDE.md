# Visual Data Designer - Configuration Guide

## Overview

This guide explains how to configure the Visual Data Designer services in your application.

## 1. Service Registration

Add the following to your `Program.cs` or `Startup.cs`:

```csharp
using Platform.Engine.Configuration;

// Add Visual Data Designer services
builder.Services.AddDataDesigner(builder.Configuration);
```

## 2. Configuration Settings

Add the following sections to your `appsettings.json`:

### 2.1 Blob Storage Configuration

```json
{
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=your_account;AccountKey=your_key;EndpointSuffix=core.windows.net",
    "UseSasTokens": true,
    "SasTokenExpirationDays": 7
  }
}
```

**Options**:
- `ConnectionString`: Azure Storage connection string
  - For local development: `"UseDevelopmentStorage=true"` (requires Azurite)
  - For production: Full Azure Storage connection string
- `UseSasTokens`: Whether to generate SAS tokens for download URLs (recommended: `true`)
- `SasTokenExpirationDays`: Number of days before SAS token expires (default: 7)

### 2.2 Notification Configuration

```json
{
  "Notifications": {
    "EnableEmail": true,
    "EnableSignalR": true,
    "SuccessEmailTemplate": "<h2>{Title}</h2><p>{Message}</p><a href='{ActionUrl}'>Download</a>",
    "ErrorEmailTemplate": "<h2 style='color: #f44336;'>{Title}</h2><p>{Message}</p>",
    "WarningEmailTemplate": "<h2 style='color: #ff9800;'>{Title}</h2><p>{Message}</p>",
    "InfoEmailTemplate": "<h2>{Title}</h2><p>{Message}</p>"
  }
}
```

**Options**:
- `EnableEmail`: Enable email notifications (default: `true`)
- `EnableSignalR`: Enable real-time SignalR notifications (default: `true`)
- Email templates support placeholders:
  - `{Title}`: Notification title
  - `{Message}`: Notification message
  - `{ActionUrl}`: Download or action URL

### 2.3 Data Execution Configuration

```json
{
  "DataExecution": {
    "QuickJobTimeoutSeconds": 30,
    "QuickJobMaxRows": 10000,
    "ChunkSize": 1000,
    "DefaultContainerName": "reports"
  }
}
```

**Options**:
- `QuickJobTimeoutSeconds`: Maximum execution time for quick jobs (default: 30)
- `QuickJobMaxRows`: Maximum rows for quick jobs (default: 10,000)
- `ChunkSize`: Number of rows to process per chunk in long-running jobs (default: 1,000)
- `DefaultContainerName`: Default blob container for reports (default: "reports")

## 3. Environment-Specific Configuration

### Development (appsettings.Development.json)

```json
{
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  },
  "Notifications": {
    "EnableEmail": false,
    "EnableSignalR": true
  },
  "DataExecution": {
    "QuickJobTimeoutSeconds": 60,
    "QuickJobMaxRows": 5000
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "BlobStorage": {
    "ConnectionString": "#{AzureStorageConnectionString}#",
    "UseSasTokens": true,
    "SasTokenExpirationDays": 3
  },
  "Notifications": {
    "EnableEmail": true,
    "EnableSignalR": true
  },
  "DataExecution": {
    "QuickJobTimeoutSeconds": 30,
    "QuickJobMaxRows": 10000,
    "ChunkSize": 2000
  }
}
```

## 4. Required Services

You need to implement the following services in your application:

### 4.1 Email Service

```csharp
public class EmailService : IEmailService
{
    public async Task SendAsync(string to, string subject, string body, bool isHtml = false)
    {
        // Implement using SendGrid, SMTP, or your preferred email service
    }
}

// Register in DI
services.AddScoped<IEmailService, EmailService>();
```

### 4.2 SignalR Service

```csharp
public class SignalRService : ISignalRService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    
    public SignalRService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }
    
    public async Task SendToUserAsync(string userId, string eventName, object data)
    {
        await _hubContext.Clients.User(userId).SendAsync(eventName, data);
    }
    
    public async Task SendToAllAsync(string eventName, object data)
    {
        await _hubContext.Clients.All.SendAsync(eventName, data);
    }
}

// Register in DI
services.AddScoped<ISignalRService, SignalRService>();
services.AddSignalR();
```

### 4.3 Repository Implementation

```csharp
public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;
    
    public Repository(DbContext context)
    {
        _context = context;
    }
    
    // Implement IRepository methods
}

// Register in DI
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

## 5. Elsa Workflow Configuration

Ensure Elsa is configured in your application:

```csharp
services.AddElsa(elsa => elsa
    .UseEntityFrameworkPersistence(ef => ef.UseSqlServer(connectionString))
    .AddConsoleActivities()
    .AddHttpActivities()
    .AddQuartzTemporalActivities()
    .AddWorkflowsFrom<LongRunningReportWorkflow>()
);

// Add Elsa Studio for monitoring
services.AddElsaApiEndpoints();
services.AddElsaStudio();
```

## 6. Database Setup

Run migrations to create required tables:

```bash
dotnet ef migrations add AddDataDesignerTables -c YourDbContext
dotnet ef database update -c YourDbContext
```

Required entities:
- `JobInstance`
- `ReportDefinition`
- `Notification`

## 7. Local Development Setup

### Using Azurite (Azure Storage Emulator)

1. Install Azurite:
```bash
npm install -g azurite
```

2. Start Azurite:
```bash
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

3. Use connection string:
```json
"ConnectionString": "UseDevelopmentStorage=true"
```

## 8. Testing Configuration

Test your configuration with a simple query:

```csharp
var engine = serviceProvider.GetRequiredService<DataExecutionEngine>();

var metadata = new DataOperationMetadata
{
    OperationType = "Query",
    RootEntity = "Users",
    Fields = new List<string> { "Id", "Name", "Email" }
};

var result = await engine.ExecuteQuickJobAsync(
    "Entity",
    metadata,
    new Dictionary<string, object>(),
    new ExecutionContext { UserId = "test@example.com" }
);
```

## 9. Monitoring

Access Elsa Studio to monitor workflow execution:
- URL: `https://your-app/elsa/studio`
- View running workflows
- Inspect workflow variables
- Retry failed workflows

## 10. Troubleshooting

### Blob Storage Connection Issues
- Verify connection string is correct
- Ensure Azurite is running for local development
- Check firewall rules for Azure Storage

### Notification Not Sending
- Check `EnableEmail` and `EnableSignalR` settings
- Verify email service implementation
- Check SignalR hub configuration

### Workflow Not Starting
- Verify Elsa activities are registered
- Check workflow definition is loaded
- Review Elsa logs for errors

## 11. Security Considerations

- **Never commit connection strings** to source control
- Use Azure Key Vault or environment variables for production
- Enable SAS tokens for secure blob access
- Implement proper authentication for download URLs
- Validate user permissions before executing queries
