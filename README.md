
# Community.Microsoft.Extensions.Caching.PostgreSQL

## Description

This implemantation uses PostgreSQL as distributed cache.

## Getting Started
1. Install the package into your project
```
dotnet add package Community.Microsoft.Extensions.Caching.PostgreSql
```
2. Add the following line to the `Startup`  `Configure` method.
```c#
services.AddDistributedPostgreSqlCache(setup => 
{
	ConnectionString = configuration["ConnectionString"], 
	SchemaName = configuration["SchemaName"],
	TableName = configuration["TableName"],
	RemoveExpiredDisabled = configuration["RemoveExpiredDisabled"],
    // Optional - RemoveExpiredDisabled default is FALSE
	CreateInfrastructure = configuration["CreateInfrastructure"] 
	// CreateInfrastructure is optional, default is TRUE
	// This means que every time starts the application the 
	// creation of table and database functions will be verified.
})
```
3. Then pull from DI like any other service

```c#
    private readonly IDistributedCache _cache;

    public WeatherForecastController(IDistributedCache cache)
    {
        _cache = cache;
    }

```
## What it does to my database:

Creates a table and six functions, see scripts folder for more details.
```
[schemaName].datediff
[schemaName].deletecacheitemformat
[schemaName].deleteexpiredcacheitemsformat
[schemaName].getcacheitemformat
[schemaName].setcache
[schemaName].updatecacheitemformat

```

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

Then you can delete the database with:
```
prepare-database.cmd -erase
```
## Change Log
1. v3.0.2 - `CreateInfrastructure` also creates the schema issue #8 
1. v3.0.1 - `DisableRemoveExpired` configuration added; If `TRUE` the cache instance won`t delete expired items.
1. v3.0
   1. [BREAKING CHANGE] - Direct instantiation not preferred
   2. Single thread loop remover
1. v2.0.x - Update everything to net5.0, more detailed sample project.
1. v1.0.8 - Update to latest dependencies -


# License
* MIT
### This is a fork from [repo](https://github.com/wullemsb/Extensions.Caching.PostgreSQL)