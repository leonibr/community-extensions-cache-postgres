# PostgreSQL Distributed Cache for .NET Core | Community Edition

[![Nuget](https://img.shields.io/nuget/v/Community.Microsoft.Extensions.Caching.PostgreSql)](https://www.nuget.org/packages/Community.Microsoft.Extensions.Caching.PostgreSql)

## Introduction

`Community.Microsoft.Extensions.Caching.PostgreSQL` is a robust and scalable distributed cache implementation for ASP.NET Core applications using PostgreSQL 11+ as the underlying data store. This package provides a simple and efficient way to leverage your existing PostgreSQL infrastructure for caching, offering a cost-effective alternative to dedicated caching servers. Ideal for scenarios where minimizing infrastructure complexity and maximizing performance is critical.

This library allows you to seamlessly integrate caching into your ASP.NET / .NET Core applications using the standard `IDistributedCache` interface. It's designed to be production-ready and configurable, giving you fine-grained control over caching behavior.

**Why choose this package?**

1. If you already use PostgreSQL, this package avoids the need for additional caching solutions like Redis, reducing infrastructure overhead.
1. Optimized for fast read and write operations with PostgreSQL, providing excellent caching performance. It is not a competitor to Redis, but it is a good alternative for some scenarios.
1. Dstributed cache supports scaling of multiple instances and high loads.
1. Simple setup process using standard ASP.NET Core / .NET Core dependency injection.
1. Provides flexible configuration options including cache expiration policies, background cleanup tasks, read-only mode, and more.
1. Benefit from the power of open source and a community-driven approach to caching.

## Table of Contents

1.  [Getting Started](#getting-started)
2.  [Installation](#installation)
3.  [Basic Configuration](#basic-configuration)
4.  [Configuration Options](#configuration-options)
    - [Disable Remove Expired](#disable-remove-expired-true-use-case-default-false)
    - [Update on Get Cache Item](#updateongetcacheitem--false-use-case-default-true)
    - [Read Only Mode](#readonlymode--true-use-case-default-false)
    - [Create Infrastructure](#createinfrastructure--true-use-case)
5.  [Usage Examples](#usage-examples)
    - [Basic Example](#basic-example)
    - [Using Custom Options](#using-custom-options)
6.  [Running the Console Sample](#runing-the-console-sample)
7.  [Running the React+WebApi Web Sample](#runing-the-reactwebapi-websample-project)
8.  [Change Log](#change-log)
9.  [Contributing](#contributing)
10. [License](#license)
11. [FAQ](#faq)
12. [Troubleshooting](#troubleshooting)

## Getting Started

### 1. Installation

Install the package via the .NET CLI:

```bash
dotnet add package Community.Microsoft.Extensions.Caching.PostgreSql
```

### 2. Basic Configuration

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

### `DisableRemoveExpired = True` use case (default false):

When you have 2 or more instances/microservices/processes and you want to leave only one instance to remove expired items.

- **Note 1:** This is not mandatory; assess whether it fits your needs.
- **Note 2:** If you have only one instance and set this to `True`, expired items will not be automatically removed. When calling `GetItem`, expired items are filtered out. In this scenario, you are responsible for manually removing the expired keys or updating them.

### `UpdateOnGetCacheItem = false` use case (default true):

If you (or the implementation using this cache) are explicitly calling `IDistributedCache.Refresh` to update the sliding window, you can turn off `UpdateOnGetCacheItem` to remove the extra DB expiration update call prior to reading the cached value. This is useful when used with ASP.NET Core Session handling.

```csharp
services.AddDistributedPostgreSqlCache((serviceProvider, setup) =>
{
    ...
    setup.UpdateOnGetCacheItem = false;
    // Or
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    setup.UpdateOnGetCacheItem = configuration["UpdateOnGetCacheItem"];
    ...
});
```

### `ReadOnlyMode = true` use case (default false):

For read-only databases, or if the database user lacks `write` permissions, you can set `ReadOnlyMode = true`.

- **Note 1:** This will disable sliding expiration; only absolute expiration will work.
- **Note 2:** This can improve performance, but you will not be able to change any cache values.

```csharp
services.AddDistributedPostgreSqlCache((serviceProvider, setup) =>
{
    ...
    setup.ReadOnlyMode = true;
    // Or
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    setup.ReadOnlyMode = configuration["UpdateOnGetCacheItem"];
    ...
});
```

### `CreateInfrastructure = true` use case:

This creates the table and schema for storing the cache (names are configurable) if they don't exist.

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

1.  v6.0.0 - Added support for .NET 10
    1.  [BREAKING CHANGE] - Dropped support for .NET 6 (end-of-life)
    1.  [BREAKING CHANGE] - Supports .NET 10, .NET 9, and .NET 8
1.  v5.0.0 - Added support for .NET 9
    1.  [BREAKING CHANGE] - Dropped support for .NETStandard2.0
    1.  [BREAKING CHANGE] - Supports .NET 9, .NET 8 and .NET 6
1.  v4.0.1 - Added support for .NET 7
    1.  [BREAKING CHANGE] - Dropped support for .NET 5
    2.  [BREAKING CHANGE] - Now uses stored procedures (won't work with PostgreSQL <= 10, use version 3)
1.  v3.1.2 - Removed dependency for `IHostApplicationLifetime` if not supported on the platform (e.g., AWS) - issue #28
1.  v3.1.0 - Added log messages on `Debug` Level, multitarget .NET 5 and .NET 6, dropped support for netstandard2.0, fixed sample to match multi-targeting and sample database.
1.  v3.0.2 - `CreateInfrastructure` also creates the schema - issue #8
1.  v3.0.1 - Added `DisableRemoveExpired` configuration; if `TRUE`, the cache instance won't delete expired items.
1.  v3.0
    1.  [BREAKING CHANGE] - Direct instantiation not preferred.
    2.  Single-threaded loop remover.
1.  v2.0.x - Updated everything to .NET 5.0, more detailed sample project.
1.  v1.0.8 - Updated to the latest dependencies.

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for more details on how to get involved.

## License

[Apache-2.0](LICENSE)

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

---

### This is a fork from [repo](https://github.com/wullemsb/Extensions.Caching.PostgreSQL)
