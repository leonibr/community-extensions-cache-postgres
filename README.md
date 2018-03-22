
# Community.Microsoft.Extensions.Caching.PostgreSQL

## Description

This implemantation uses PostgreSQL as distributed cache.

## Getting Started
1. Install the package into your project
```
dotnet add package Communit.Microsoft.Extensions.Caching.PostgreSQL
```
2. Add the following line to the `Startup`  `Configure` method.
```c#
services.AddDistributedPostgreSqlCache(setup => 
{
	ConnectionString = configuration["ConnectionString"], 
	SchemaName = configuration["SchemaName"],
	TableName = configuration["TableName"],
	CreateInfrastructure = configuration["CreateInfrastructure"] 
	// CreateInfrastructure is optional, default is TRUE
	// This means que every time starts the application the 
	// creation of table and database functions will be verified.
})
```
## What it does to my database:

Creates a table and six functions, see scripts folder for more details.

## Runing the sample
You will need a local postgresql server with this configuration:
1. Server listening to port 5432 at localhost
1. The password of your `postgres` account, if not attached already to your user.
1. Clone this repo.
1. Run the following commands inside `PostgreSqlCacheSample`:
```
dotnet restore
prepare-database.cmd -create
dotnet run
```
Then you can delete the database with:
```
prepare-database.cmd -erase
```

# License
* MIT
### This is a fork from [repo](https://github.com/wullemsb/Extensions.Caching.PostgreSQL)