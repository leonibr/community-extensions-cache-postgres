# Community.Microsoft.Extensions.Caching.PostgreSQL

## Description

This implemantation uses PostgreSQL 11+ as distributed cache. Version 4 and up.

### For PostgreSQL <= 10

Use older versions of this packages (<= 3.1.2)

## Getting Started

1. Install the package into your project

```
dotnet add package Community.Microsoft.Extensions.Caching.PostgreSql
```

2. Add the following line to the `Startup` `Configure` method.

```c#
services.AddDistributedPostgreSqlCache(setup =>
{
    setup.ConnectionString = configuration["ConnectionString"];
    setup.SchemaName = configuration["SchemaName"];
    setup.TableName = configuration["TableName"];
    setup.DisableRemoveExpired = configuration["DisableRemoveExpired"];
    // Optional - DisableRemoveExpired default is FALSE
    setup.CreateInfrastructure = configuration["CreateInfrastructure"];
    // CreateInfrastructure is optional, default is TRUE
    // This means que every time starts the application the
    // creation of table and database functions will be verified.
    setup.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30)
    // ExpiredItemsDeletionInterval is optional
    // This is the periodic interval to scan and delete expired items in the cache. Default is 30 minutes.
    // Minimum allowed is 5 minutes. - If you need less than this please share your use case 😁, just for curiosity...
})
```

### `DisableRemoveExpired = True` use case:

When you have 2 or more instances/microservices/processes and you just to leave one of them removing expired items.

- Note 1: This is not mandatory, see if it fits in you cenario.
- Note 2: If you have only one instance and set to `True`, all the expired items will not be auto-removed, when you call `GetItem` those expired items are filtred out.
  In that case you are responsable to manually remove the expired key or update it

3. Then pull from DI like any other service

```c#
    /// this is extracted from the React+WebApi WebSample

    private readonly IDistributedCache _cache;

    public WeatherForecastController(IDistributedCache cache)
    {
        _cache = cache;
    }

```

## What it does to my database 🐘:

1. Creates a table (name is configurable)
2. Creates two functions

```
[schemaName].datediff
[schemaName].getcacheitemformat
```

3. Creates four stored procedures

```
[schemaName].deletecacheitemformat
[schemaName].deleteexpiredcacheitemsformat
[schemaName].setcache
[schemaName].updatecacheitemformat
```

For additional details please check the [scripts folder](./Extensions.Caching.PostgreSql/PostgreSqlScripts).

## Runing the console sample

You will need a local postgresql server with this configuration:

1. Server listening to port 5432 at localhost
1. The password of your `postgres` account, if not attached already to your user.
1. Clone this repo.
1. Run the following commands inside `PostgreSqlCacheSample`:

```shell
dotnet restore
prepare-database.cmd -create
dotnet run
```

![S](sample_project.gif)

## Runing the React+WebApi WebSample project

You will need a local postgresql server with this configuration:

1. Server listening to port 5432 at localhost
1. The password of your `postgres` account, if not attached already to your user.
1. You also need `npm` and `node` installed on your dev machine
1. Clone this repo.
1. Run the following commands inside `WebSample`:

```shell
dotnet restore
prepare-database.cmd -create
dotnet run
```

It takes some time to `npm` retore the packages, grab a ☕ while waiting...

Then you can delete the database with:

```
prepare-database.cmd -erase
```

## Change Log

1. v4.0.1 - Add suport to .net 7
   1. [BREAKING CHANGE] - Drop suport to .net 5
   1. [BREAKING CHANGE] - Make use of procedures (won't work with PostgreSQL <= 10, use version 3)
1. v3.1.2 - removed dependency for `IHostApplicationLifetime` if not supported on the platform: `AWS` for instance - issue #28
1. v3.1.0 - Added log messages on `Debug` Level, multitarget .net5 and .net6, dropped support to netstandard2.0, fix sample to match multitarget and sample database.
1. v3.0.2 - `CreateInfrastructure` also creates the schema issue #8
1. v3.0.1 - `DisableRemoveExpired` configuration added; If `TRUE` the cache instance won`t delete expired items.
1. v3.0
   1. [BREAKING CHANGE] - Direct instantiation not preferred
   2. Single thread loop remover
1. v2.0.x - Update everything to net5.0, more detailed sample project.
1. v1.0.8 - Update to latest dependencies -

# License

- MIT

### This is a fork from [repo](https://github.com/wullemsb/Extensions.Caching.PostgreSQL)
