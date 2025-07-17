# Azure Key Vault Rotation Support

This document explains how to implement connection string reloading for Azure Key Vault rotation scenarios in the PostgreSQL Distributed Cache library.

## Overview

Azure Key Vault rotation is a security best practice that involves periodically updating secrets (like database connection strings) without application downtime. This library provides built-in support for automatically reloading connection strings when they are updated in Azure Key Vault.

## Features

- **Automatic Connection String Reloading**: Periodically checks for updated connection strings in configuration
- **Configurable Reload Intervals**: Set how often to check for updates (default: 5 minutes)
- **Thread-Safe Operations**: Safe concurrent access to connection string updates
- **Comprehensive Logging**: Detailed logging of connection string changes
- **Graceful Fallback**: Continues using existing connection string if reload fails

## Implementation Approaches

### Approach 1: Using the Reloadable Connection String Extension (Recommended)

This is the simplest approach using the new extension method:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddDistributedPostgreSqlCacheWithReloadableConnection(
    connectionStringKey: "PostgreSqlCache:ConnectionString",
    reloadInterval: TimeSpan.FromMinutes(5),
    setupAction: options =>
    {
        options.SchemaName = "cache";
        options.TableName = "cache_items";
        options.DisableRemoveExpired = false;
        options.UpdateOnGetCacheItem = true;
        options.ReadOnlyMode = false;
        options.CreateInfrastructure = true;
    });
```

### Approach 2: Manual Configuration

For more control, configure the reloadable connection string manually:

```csharp
builder.Services.AddDistributedPostgreSqlCache((serviceProvider, setup) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<PostgreSqlCache>>();

    // Enable reloadable connection string
    setup.ConnectionStringKey = "PostgreSqlCache:ConnectionString";
    setup.Configuration = configuration;
    setup.Logger = logger;
    setup.EnableConnectionStringReloading = true;
    setup.ConnectionStringReloadInterval = TimeSpan.FromMinutes(5);

    // Other configuration options
    setup.SchemaName = "cache";
    setup.TableName = "cache_items";
    setup.DisableRemoveExpired = false;
    setup.UpdateOnGetCacheItem = true;
    setup.ReadOnlyMode = false;
    setup.CreateInfrastructure = true;
});
```

## Azure Key Vault Configuration

### 1. Install Required Packages

```bash
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Identity
dotnet add package Microsoft.Extensions.Configuration.AzureKeyVault
```

### 2. Configure Azure Key Vault in Program.cs

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault
var keyVaultUrl = $"https://{builder.Configuration["AzureKeyVault:VaultName"]}.vault.azure.net/";
var credential = new ClientSecretCredential(
    builder.Configuration["AzureKeyVault:TenantId"],
    builder.Configuration["AzureKeyVault:ClientId"],
    builder.Configuration["AzureKeyVault:ClientSecret"]);

var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);

// Add Azure Key Vault as configuration source
builder.Configuration.AddAzureKeyVault(secretClient, new AzureKeyVaultConfigurationOptions());

// Configure PostgreSQL cache with reloadable connection string
builder.Services.AddDistributedPostgreSqlCacheWithReloadableConnection(
    connectionStringKey: "PostgreSqlCache:ConnectionString",
    reloadInterval: TimeSpan.FromMinutes(5));
```

### 3. Configuration Settings

Add the following to your `appsettings.json`:

```json
{
  "AzureKeyVault": {
    "VaultName": "your-key-vault-name",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "PgCache": {
    "ConnectionStringKey": "PostgreSqlCache:ConnectionString",
    "SchemaName": "cache",
    "TableName": "cache_items",
    "EnableConnectionStringReloading": true,
    "ConnectionStringReloadInterval": "00:05:00"
  }
}
```

## Azure Key Vault Setup

### 1. Create Key Vault Secret

Store your PostgreSQL connection string in Azure Key Vault:

```bash
# Using Azure CLI
az keyvault secret set --vault-name "your-key-vault-name" --name "PostgreSqlCache--ConnectionString" --value "Host=your-server;Database=your-db;Username=your-user;Password=your-password"
```

### 2. Configure Access Policies

Ensure your application has access to read secrets:

```bash
# Using Azure CLI
az keyvault set-policy --name "your-key-vault-name" --spn "your-app-service-principal-id" --secret-permissions get list
```

### 3. Enable Soft Delete (Recommended)

```bash
az keyvault update --name "your-key-vault-name" --enable-soft-delete true
```

## Rotation Process

### Manual Rotation

1. **Update the Secret in Azure Key Vault**:

   ```bash
   az keyvault secret set --vault-name "your-key-vault-name" --name "PostgreSqlCache--ConnectionString" --value "Host=new-server;Database=your-db;Username=your-user;Password=new-password"
   ```

2. **Application Automatically Picks Up Changes**:
   - The library checks for updates every 5 minutes (configurable)
   - When a change is detected, it logs the update
   - New connections use the updated connection string

### Automated Rotation

For automated rotation, you can:

1. **Use Azure Key Vault Rotation Policies**:

   ```bash
   az keyvault secret set-attributes --vault-name "your-key-vault-name" --name "PostgreSqlCache--ConnectionString" --expires 2024-12-31T23:59:59Z
   ```

2. **Implement Custom Rotation Logic**:
   ```csharp
   // In your rotation service
   public async Task RotateConnectionStringAsync()
   {
       var newConnectionString = GenerateNewConnectionString();
       await secretClient.SetSecretAsync("PostgreSqlCache--ConnectionString", newConnectionString);
   }
   ```

## Monitoring and Logging

The library provides comprehensive logging for connection string operations:

```csharp
// Configure logging in appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Community.Microsoft.Extensions.Caching.PostgreSql": "Information"
    }
  }
}
```

### Log Messages

- `Connection string updated from configuration key: {Key}` - When connection string is updated
- `Connection string manually reloaded from configuration key: {Key}` - When manually reloaded
- `Connection string reload timer triggered for key: {Key}` - Timer events
- `Connection string not found for key: {Key}` - Configuration issues
- `Error loading connection string from configuration key: {Key}` - Errors

## Best Practices

### 1. Security

- **Use Managed Identity**: Prefer managed identity over client secrets when possible
- **Least Privilege**: Grant only necessary permissions to your application
- **Secret Rotation**: Implement automated rotation policies
- **Monitoring**: Monitor access to Key Vault secrets

### 2. Performance

- **Reasonable Reload Intervals**: Don't check too frequently (minimum 1 minute recommended)
- **Connection Pooling**: The library handles connection pooling automatically
- **Monitoring**: Monitor connection string reload performance

### 3. Reliability

- **Graceful Degradation**: The library continues using existing connections if reload fails
- **Error Handling**: Comprehensive error handling and logging
- **Fallback Strategy**: Consider having a fallback connection string

### 4. Configuration

```csharp
// Recommended configuration
builder.Services.AddDistributedPostgreSqlCacheWithReloadableConnection(
    connectionStringKey: "PostgreSqlCache:ConnectionString",
    reloadInterval: TimeSpan.FromMinutes(5), // Check every 5 minutes
    setupAction: options =>
    {
        options.SchemaName = "cache";
        options.TableName = "cache_items";
        options.CreateInfrastructure = true;
        options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(20);
    });
```

## Troubleshooting

### Common Issues

1. **Connection String Not Found**:

   - Verify the configuration key exists in Azure Key Vault
   - Check application permissions to Key Vault
   - Ensure the secret name matches the configuration key

2. **Reload Not Working**:

   - Check if `EnableConnectionStringReloading` is set to `true`
   - Verify `Configuration` and `Logger` are properly set
   - Check logs for error messages

3. **Performance Issues**:
   - Increase reload interval if checking too frequently
   - Monitor connection pool usage
   - Check for connection leaks

### Debug Configuration

```csharp
// Enable detailed logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// Add configuration debugging
builder.Services.AddDistributedPostgreSqlCacheWithReloadableConnection(
    connectionStringKey: "PostgreSqlCache:ConnectionString",
    reloadInterval: TimeSpan.FromMinutes(1), // Shorter interval for testing
    setupAction: options =>
    {
        options.Logger.LogInformation("Cache configured with reloadable connection string");
    });
```

## Migration from Static Connection Strings

If you're currently using static connection strings, here's how to migrate:

### Before (Static)

```csharp
builder.Services.AddDistributedPostgreSqlCache(setup =>
{
    setup.ConnectionString = "Host=localhost;Database=cache;Username=user;Password=pass";
    setup.SchemaName = "cache";
    setup.TableName = "cache_items";
});
```

### After (Reloadable)

```csharp
builder.Services.AddDistributedPostgreSqlCacheWithReloadableConnection(
    connectionStringKey: "PostgreSqlCache:ConnectionString",
    setupAction: options =>
    {
        options.SchemaName = "cache";
        options.TableName = "cache_items";
    });
```

## Advanced Scenarios

### Custom Reload Logic

For custom reload logic, you can extend the `ReloadableConnectionStringProvider`:

```csharp
public class CustomConnectionStringProvider : ReloadableConnectionStringProvider
{
    public CustomConnectionStringProvider(
        IConfiguration configuration,
        ILogger logger,
        string connectionStringKey,
        TimeSpan reloadInterval)
        : base(configuration, logger, connectionStringKey, reloadInterval)
    {
    }

    protected override string LoadConnectionString()
    {
        // Custom logic here
        var connectionString = base.LoadConnectionString();

        // Add custom validation or transformation
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string cannot be empty");
        }

        return connectionString;
    }
}
```

### Multiple Connection Strings

For applications with multiple databases:

```csharp
// Primary cache
builder.Services.AddDistributedPostgreSqlCacheWithReloadableConnection(
    connectionStringKey: "PrimaryCache:ConnectionString",
    reloadInterval: TimeSpan.FromMinutes(5));

// Secondary cache
builder.Services.AddDistributedPostgreSqlCacheWithReloadableConnection(
    connectionStringKey: "SecondaryCache:ConnectionString",
    reloadInterval: TimeSpan.FromMinutes(10));
```

## Conclusion

This implementation provides a robust, secure, and efficient way to handle Azure Key Vault rotation for PostgreSQL connection strings. The automatic reloading mechanism ensures your application stays up-to-date with the latest secrets without manual intervention or application restarts.

For more information about Azure Key Vault rotation, see the [official Microsoft documentation](https://docs.microsoft.com/en-us/azure/key-vault/secrets/overview-soft-delete).
