# Contributing to Community.Microsoft.Extensions.Caching.PostgreSql

Thank you for your interest in contributing to our PostgreSQL Distributed Cache implementation for .NET! This document provides guidelines and standards for contributing to the project.

## Development Environment Setup

1. Install the following prerequisites:

   - .NET SDK (versions 8.0, 9.0, and 10.0)
   - PostgreSQL 11+
   - Your preferred IDE (Visual Studio, VS Code, Rider, etc.)

1. Clone the repository:

```bash
git clone https://github.com/leonibr/community-extensions-cache-postgres.git
```

## Code Standards

### Project Structure

- Source code goes in `Extensions.Caching.PostgreSql/`
- Sample projects in `PostgreSqlCacheSample/` and `WebSample/`
- Tests should be placed in a corresponding test project

### Coding Conventions

1. **Language Version**

   - Use C# 12 features (`<LangVersion>12</LangVersion>`)
   - Target multiple frameworks: net8.0, net9.0, and net10.0

1. **Naming Conventions**

   - Use PascalCase for public members and types
   - Use camelCase for private fields
   - Prefix private fields with underscore (\_)

   ```csharp
   private readonly ILogger<DatabaseOperations> _logger;
   ```

1. **String Interpolation**

   - Use raw string literals for SQL queries

   ```csharp
   public string CreateSchemaAndTableSql =>
       $"""
       CREATE SCHEMA IF NOT EXISTS "{_schemaName}";
       // ...
       """;
   ```

1. **Async/Await**

   - Always provide async versions of methods with CancellationToken support
   - Use the Async suffix for async methods

   ```csharp
   public async Task DeleteCacheItemAsync(string key, CancellationToken cancellationToken)
   ```

1. **Dependency Injection**

   - Use constructor injection
   - Mark dependencies as readonly when possible

   ```csharp
   private readonly ILogger<DatabaseOperations> _logger;
   ```

1. **Error Handling**
   - Use structured logging with ILogger
   - Validate input parameters
   - Throw appropriate exceptions with meaningful messages

### Documentation

1. **XML Comments**

   - Add XML comments for public APIs
   - Include parameter descriptions and examples where appropriate

   ```csharp
   /// <summary>
   /// The factory to create a NpgsqlDataSource instance.
   /// Either <see cref="DataSourceFactory"/> or <see cref="ConnectionString"/> should be set.
   /// </summary>
   public Func<NpgsqlDataSource> DataSourceFactory { get; set; }
   ```

1. **README Updates**
   - Update README.md when adding new features
   - Include usage examples for new functionality

### Testing (optional for now)

1. Write unit tests for new functionality
1. Ensure all tests pass before submitting PR
1. Include integration tests for database operations

## Pull Request Process

1. Create a feature branch from `master`
1. Make your changes following the coding standards
1. Update documentation as needed
1. Run all tests (if any)
1. Submit a pull request with:
   - Clear description of changes
   - Any related issue numbers
   - Breaking changes noted
   - Documentation updates

### Dependencies

Keep dependencies up to date with the latest stable versions:

## Release Process

1. Version numbers follow SemVer
1. Update version in .csproj:

   ```xml
   <Version>4.0.0</Version>
   ```

1. Update PackageReleaseNotes in .csproj
1. Document breaking changes in README.md

## Questions or Problems?

- Open an issue for bugs or feature requests
- For questions, use GitHub Discussions
- For security issues, please see [SECURITY.md](SECURITY.md)

## License

This project is licensed under the MIT License - see the LICENSE file for details.
