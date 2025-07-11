# PostgreSQL Distributed Cache for .NET Core | Community Edition

[![Nuget](https://img.shields.io/nuget/v/Community.Microsoft.Extensions.Caching.PostgreSql)](https://www.nuget.org/packages/Community.Microsoft.Extensions.Caching.PostgreSql)
[![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/leonibr/4a15a6116d49e35415ce8e93de55a9fc/raw/33a8e6bf5c48317d2697b7da139efa910e74607c/coverage.json)](https://leonibr.github.io/community-extensions-cache-postgres/coverage/)

## Introduction

`Community.Microsoft.Extensions.Caching.PostgreSQL` is a robust and scalable distributed cache implementation for ASP.NET Core applications using PostgreSQL 11+ as the underlying data store. This package provides a simple and efficient way to leverage your existing PostgreSQL infrastructure for caching, offering a cost-effective alternative to dedicated caching servers. Ideal for scenarios where minimizing infrastructure complexity and maximizing performance is critical.

This library allows you to seamlessly integrate caching into your ASP.NET / .NET Core applications using the standard `IDistributedCache` interface. It's designed to be production-ready and configurable, giving you fine-grained control over caching behavior.

**Why choose this package?**

1. If you already use PostgreSQL, this package avoids the need for additional caching solutions like Redis, reducing infrastructure overhead.
1. Optimized for fast read and write operations with PostgreSQL, providing excellent caching performance. It is not a competitor to Redis, but it is a good alternative for some scenarios.
1. Distributed cache supports scaling of multiple instances and high loads.
1. Simple setup process using standard ASP.NET Core / .NET Core dependency injection.
1. Provides flexible configuration options including cache expiration policies, background cleanup tasks, read-only mode, and more.
1. Benefit from the power of open source and a community-driven approach to caching.

## Table of Contents

1.  [Getting Started](#getting-started)
    - [Installation](#installation)
    - [Basic Configuration](#basic-configuration)
1.  [Configuration Options](#configuration-options)
    - [Disable Remove Expired](#disable-remove-expired-true-use-case-default-false)
    - [Update on Get Cache Item](#updateongetcacheitem--false-use-case-default-true)
    - [Read Only Mode](#readonlymode--true-use-case-default-false)
    - [Create Infrastructure](#createinfrastructure--true-use-case)
1.  [Usage Examples](#usage-examples)
    - [Basic Example](#basic-example)
    - [Using Custom Options](#using-custom-options)
1.  [Code Coverage](#code-coverage)
1.  [Running the Console Sample](#running-the-console-sample)
1.  [Running the React+WebApi Web Sample](#running-the-reactwebapi-websample-project)
1.  [Change Log](#change-log)
1.  [Contributing](#contributing)
1.  [License](#license)
1.  [FAQ](#faq)
1.  [Troubleshooting](#troubleshooting)

## Getting Started

### Installation

Install the package via the .NET CLI:

```bash
dotnet add package Community.Microsoft.Extensions.Caching.PostgreSql
```

### Basic Configuration

Add the following line to your `Startup.cs` or `Program.cs`'s `ConfigureServices` method:

```csharp
services.AddDistributedPostgreSqlCache(setup =>
{
    setup.ConnectionString = configuration["ConnectionString"];
    setup.SchemaName = configuration["SchemaName"];
    setup.TableName = configuration["TableName"];
    setup.DisableRemoveExpired = configuration["DisableRemoveExpired"];
    // Optional - DisableRemoveExpired default is FALSE
    setup.CreateInfrastructure = configuration["CreateInfrastructure"];
    // CreateInfrastructure is optional, default is TRUE
    // This means that every time the application starts the
    // creation of the table and database functions will be verified.
    setup.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
    // ExpiredItemsDeletionInterval is optional
    // This is the periodic interval to scan and delete expired items in the cache. Default is 30 minutes.
    // Minimum allowed is 5 minutes. - If you need less than this please share your use case 😁, just for curiosity...
});
```

#### Configuring with `IServiceProvider` access:

```csharp
services.AddDistributedPostgreSqlCache((serviceProvider, setup) =>
{
    // IConfiguration is used as an example here
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    setup.ConnectionString = configuration["ConnectionString"];
    ...
});
```

#### Configuring via `IConfigureOptions<PostgreSqlCacheOptions>`

Use:

```csharp
services.AddDistributedPostgreSqlCache();
```

And implement and register:

```csharp
IConfigureOptions<PostgreSqlCacheOptions>
```

## Configuration Options

The following options can be set when configuring the PostgreSQL distributed cache. Each option is described with its purpose, recommended use cases, and any pros/cons to help you decide the best configuration for your scenario.

| Option                                                            | Type     | Default  | Description                                                      |
| ----------------------------------------------------------------- | -------- | -------- | ---------------------------------------------------------------- |
| `ConnectionString`                                                | string   | —        | The PostgreSQL connection string. **Required.**                  |
| `SchemaName`                                                      | string   | "public" | The schema where the cache table will be created.                |
| `TableName`                                                       | string   | "cache"  | The name of the cache table.                                     |
| [`DisableRemoveExpired`](#1-disableremoveexpired)                 | bool     | false    | Disables automatic removal of expired cache items.               |
| [`UpdateOnGetCacheItem`](#2-updateongetcacheitem)                 | bool     | true     | Updates sliding expiration on cache reads.                       |
| [`ReadOnlyMode`](#3-readonlymode)                                 | bool     | false    | Enables read-only mode (no writes, disables sliding expiration). |
| [`CreateInfrastructure`](#4-createinfrastructure)                 | bool     | true     | Automatically creates the schema/table if they do not exist.     |
| [`ExpiredItemsDeletionInterval`](#5-expireditemsdeletioninterval) | TimeSpan | 30 min   | How often expired items are deleted (min: 5 min).                |

---

### Option Details & Usage Guidance

#### `ConnectionString`, `SchemaName`, `TableName`

- **What they do:**  
  Standard DB connection and naming options.
- **When to use:**
  - Always set `ConnectionString`.
  - Customize `SchemaName`/`TableName` if you want to use non-default names or schemas.

#### 1. `DisableRemoveExpired`

- **What it does:**  
  If `true`, this instance will not automatically remove expired cache items in the background.
- **When to use:**
  - You have multiple app instances and want only one to perform cleanup (set `true` on all but one).
  - You want to handle expired item cleanup yourself.
- **Pros:**
  - Reduces DB load if you have many instances.
- **Cons:**
  - If all instances have this set to `true`, expired items will accumulate unless you remove them manually.
- **Recommendation:**
  - For single-instance deployments, leave as `false`.
  - For multi-instance, set `true` on all but one instance.

#### 2. `UpdateOnGetCacheItem`

- **What it does:**  
  If `true`, reading a cache item with sliding expiration will update its expiration in the database.
- **When to use:**
  - Leave as `true` for most scenarios.
  - Set to `false` if you explicitly call `IDistributedCache.Refresh` (e.g., with ASP.NET Core Session).
- **Pros:**
  - Ensures sliding expiration works automatically.
- **Cons:**
  - Slightly more DB writes on cache reads.
- **Recommendation:**
  - Set to `false` only if you manage sliding expiration yourself.

#### 3. `ReadOnlyMode`

- **What it does:**  
  If `true`, disables all write operations (including sliding expiration updates).
- **When to use:**
  - Your database user has only read permissions.
  - You want to ensure the cache is never modified by this app.
- **Pros:**
  - Prevents accidental writes.
  - Can improve performance (no write queries).
- **Cons:**
  - Sliding expiration is disabled; only absolute expiration works.
  - Cache cannot be updated or cleared by this instance.
- **Recommendation:**
  - Use for read-only replicas or when you want strict read-only cache access.

#### 4. `CreateInfrastructure`

- **What it does:**  
  If `true`, creates the schema and table for the cache if they do not exist.
- **When to use:**
  - You want the app to auto-provision the cache table/schema.
- **Pros:**
  - Simplifies setup.
- **Cons:**
  - May not be desirable in environments with strict DB change controls.
- **Recommendation:**
  - Set to `false` if you want to manage DB schema manually.

#### 5. `ExpiredItemsDeletionInterval`

- **What it does:**  
  Sets how often the background process checks for and deletes expired items.
- **When to use:**
  - Adjust for your cache churn and DB performance needs.
- **Pros:**
  - Lower intervals mean expired items are removed sooner.
- **Cons:**
  - Too frequent can increase DB load; too infrequent can leave expired data longer.
- **Recommendation:**
  - Default (30 min) is suitable for most; minimum is 5 min.

---

### Example: Custom Configuration

```csharp
services.AddDistributedPostgreSqlCache(options =>
{
    options.ConnectionString = configuration["CacheConnectionString"];
    options.SchemaName = "my_schema";
    options.TableName = "my_cache_table";
    options.DisableRemoveExpired = true; // Only if another instance is cleaning up
    options.CreateInfrastructure = false; // If you manage schema manually
    options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(15);
    options.UpdateOnGetCacheItem = false; // If you call Refresh explicitly
    options.ReadOnlyMode = false; // Set true for read-only DB users
});
```

## Usage Examples

### Basic Example

```csharp
    // This is extracted from the React+WebApi WebSample

    private readonly IDistributedCache _cache;

    public WeatherForecastController(IDistributedCache cache)
    {
        _cache = cache;
    }

    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
       var cachedValue = await _cache.GetStringAsync("weather-forecast");
       if(string.IsNullOrEmpty(cachedValue))
       {
          //Do stuff to fetch data...
          var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
          {
              Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
              TemperatureC = Random.Shared.Next(-20, 55),
              Summary = Summaries[Random.Shared.Next(Summaries.Length)]
          })
          .ToArray();
          var byteArray = JsonSerializer.SerializeToUtf8Bytes(forecast);
          await _cache.SetAsync("weather-forecast", byteArray, new DistributedCacheEntryOptions
          {
              AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
          });

          return forecast;
       }

       var result = JsonSerializer.Deserialize<WeatherForecast[]>(cachedValue);
       return result;
    }
```

### Using Custom Options

```csharp
 services.AddDistributedPostgreSqlCache((serviceProvider, setup) =>
 {
     var configuration = serviceProvider.GetRequiredService<IConfiguration>();
     setup.ConnectionString = configuration["CacheConnectionString"];
     setup.SchemaName = "my_custom_cache_schema";
     setup.TableName = "my_custom_cache_table";
     setup.DisableRemoveExpired = true;
     setup.CreateInfrastructure = true;
     setup.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(15);
     setup.UpdateOnGetCacheItem = false;
     setup.ReadOnlyMode = true;
 });
```

### Azure Key Vault Rotation Support

For applications using Azure Key Vault for secret management, this library provides built-in support for automatic connection string reloading when secrets are rotated.

#### Quick Setup

```csharp
// Install required packages
// dotnet add package Azure.Security.KeyVault.Secrets
// dotnet add package Azure.Identity
// dotnet add package Microsoft.Extensions.Configuration.AzureKeyVault

// Configure Azure Key Vault
var keyVaultUrl = $"https://{builder.Configuration["AzureKeyVault:VaultName"]}.vault.azure.net/";
var credential = new ClientSecretCredential(
    builder.Configuration["AzureKeyVault:TenantId"],
    builder.Configuration["AzureKeyVault:ClientId"],
    builder.Configuration["AzureKeyVault:ClientSecret"]);

var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
builder.Configuration.AddAzureKeyVault(secretClient, new AzureKeyVaultConfigurationOptions());

// Configure cache with reloadable connection string
builder.Services.AddDistributedPostgreSqlCacheWithReloadableConnection(
    connectionStringKey: "PostgreSqlCache:ConnectionString",
    reloadInterval: TimeSpan.FromMinutes(5),
    setupAction: options =>
    {
        options.SchemaName = "cache";
        options.TableName = "cache_items";
        options.CreateInfrastructure = true;
    });
```

#### Features

- **Automatic Reloading**: Periodically checks for updated connection strings
- **Configurable Intervals**: Set how often to check for updates (default: 5 minutes)
- **Thread-Safe**: Safe concurrent access to connection string updates
- **Comprehensive Logging**: Detailed logging of connection string changes
- **Graceful Fallback**: Continues using existing connection string if reload fails

For detailed implementation guide, see [Azure Key Vault Rotation Support](AZURE_KEY_VAULT_ROTATION.md).

## Code Coverage

This project maintains comprehensive test coverage to ensure reliability and quality. You can view the current coverage status and detailed reports in several ways:

### Coverage Badge

The coverage badge in the header shows the current test coverage percentage. Click on it to view the detailed HTML coverage report.

### Coverage Reports

- **HTML Report**: Available at [https://leonibr.github.io/community-extensions-cache-postgres/coverage/](https://leonibr.github.io/community-extensions-cache-postgres/coverage/)
- **GitHub Actions**: Coverage reports are generated automatically on every push to the main branch
- **Local Generation**: Run `dotnet test --collect:"XPlat Code Coverage"` to generate coverage reports locally

### Coverage Details

The coverage report includes:

- Line coverage for all source files
- Branch coverage analysis
- Detailed breakdown by class and method
- Historical coverage trends

## Running the Console Sample

You will need a local PostgreSQL server with the following:

1.  Server listening on port 5432 at localhost
1.  The password of your `postgres` account, if not attached already to your user.
1.  Clone this repository.
1.  Run the following commands inside `PostgreSqlCacheSample`:

```shell
dotnet restore
prepare-database.cmd -create
dotnet run
```

## Running the React+WebApi WebSample project

You will need a local PostgreSQL server with the following:

1.  Server listening on port 5432 at localhost
1.  The password of your `postgres` account, if not attached already to your user.
1.  You also need `npm` and `node` installed on your development machine.
1.  Clone this repository.
1.  Run the following commands inside the `WebSample` directory:

```shell
dotnet restore
prepare-database.cmd -create // windows
chmod +x prepare-database.sh // linux - just once to make the script executable
./prepare-database.sh -create // linux
dotnet run
```

It may take some time for `npm` to restore the packages.

Then, you can delete the database using:

```shell
prepare-database.cmd -erase // windows
./prepare-database.sh -erase // linux
```

**if you don't want to use the bash/cmd script, you can run the SQL script directly on your PostgreSQL database.**

- Option `-create` executes [`create-sample-database.sql`](PostgreSqlCacheSample/create-sample-database.sql)

- Option `-erase` executes [`erase-sample-database.sql`](PostgreSqlCacheSample/erase-sample-database.sql)

## Change Log

- [v5.0.1](https://github.com/leonibr/community-extensions-cache-postgres/releases/tag/5.0.1) - Added unit tests and improve multitarget frameworks
- [v5.0.0](https://github.com/leonibr/community-extensions-cache-postgres/releases/tag/5.0.0) - Added support for .NET 9
  - [BREAKING CHANGE] - Dropped support for .NETStandard2.0
  - [BREAKING CHANGE] - Supports .NET 9, .NET 8 and .NET 6
- [v4.0.1](https://github.com/leonibr/community-extensions-cache-postgres/releases/tag/4.0.1) - Added support for .NET 7
  - [BREAKING CHANGE] - Dropped support for .NET 5
  - [BREAKING CHANGE] - Now uses stored procedures (won't work with PostgreSQL <= 10, use version 3)
- [v3.1.2](https://github.com/leonibr/community-extensions-cache-postgres/releases/tag/v3.1.2) - Removed dependency for `IHostApplicationLifetime` if not supported on the platform (e.g., AWS) - issue #28
- [v3.1.0](https://github.com/leonibr/community-extensions-cache-postgres/releases/tag/3.1.0) - Added log messages on `Debug` Level, multitarget .NET 5 and .NET 6, dropped support for netstandard2.0, fixed sample to match multi-targeting and sample database.
- [v3.0.2](https://github.com/leonibr/community-extensions-cache-postgres/releases/tag/v3.0.2) - `CreateInfrastructure` also creates the schema - issue #8
- [v3.0.1](https://github.com/leonibr/community-extensions-cache-postgres/releases/tag/v3.0.1) - Added `DisableRemoveExpired` configuration; if `TRUE`, the cache instance won't delete expired items.
- [v3.0](https://github.com/leonibr/community-extensions-cache-postgres/releases/tag/3.0.0) - [BREAKING CHANGE] - Direct instantiation not preferred. Single-threaded loop remover.
- [v2.0.x commits](https://github.com/leonibr/community-extensions-cache-postgres/commits/main?utf8=%E2%9C%93&search=v2.0) - Updated everything to .NET 5.0, more detailed sample project.
- [v1.0.8](https://github.com/leonibr/community-extensions-cache-postgres/releases/tag/v1.0.8) - Updated to the latest dependencies.

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for more details on how to get involved.

## License

[MIT License](LICENSE)

## FAQ

**Q: What versions of PostgreSQL are supported?**

A: This package supports PostgreSQL 11 and higher. For older versions (<= 10), use older versions of this package (<= 3.1.2).

**Q: How do I handle database connection issues?**

A: Ensure your connection string is correct and that your database server is accessible. Verify your username, password, and host settings.

**Q: Is this package production-ready?**

A: Yes, this package is designed for production use. It includes features like background expired item removal and configurable options, but it's always recommended to thoroughly test it in your specific environment.

**Q: What are the performance characteristics of this library?**

A: The library is optimized to perform well when using PostgreSQL. The performance of this library is tied to your database, so make sure your server is appropriately configured.
We have plans to provide benchmarks in the future.

## Troubleshooting

Please check the [Github issues page](https://github.com/leonibr/community-extensions-cache-postgres/issues) to see if the issue is already reported. If not, please create a new issue describing the problem with all the necessary details to reproduce it.

### Known issues:

- The library does not perform well with large objects in the cache due to the nature of PostgreSQL, large objects may cause performance bottlenecks.
