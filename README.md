
# Community.Microsoft.Extensions.Caching.PostgreSQL

## Description

This implemantation uses PostgreSQL as distributed cache.

## Getting Started
1. Install the package into your project
```
dotnet add package Communit.Microsoft.Extensions.Caching.PostgreSQL
```
2. Add the following line to the `Startup`  `Configure` method.
```
services.AddDistributedPostgreSqlCache()
```


Distributed cache implementation of IDistributedCache using PostgreSQL

Available on NuGet at [https://www.nuget.org/packages/Extensions.Caching.PostgreSql/](https://www.nuget.org/packages/Extensions.Caching.PostgreSql/)

# License
* MIT
### This is a fork from [repo](https://github.com/wullemsb/Extensions.Caching.PostgreSQL)