# Security Policy

## Supported Versions

We maintain security updates for the following versions of Community.Microsoft.Extensions.Caching.PostgreSql:

| Version | Supported          |
| ------- | ------------------ |
| 6.0.x   | :white_check_mark: |
| 5.x.x   | :x:                |
| 4.0.x   | :x:                |
| < 4.0   | :x:                |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue, please follow these steps:

1. **Do Not** open a public GitHub issue for the vulnerability.

1. Send a detailed report to our security team by opening a [private security advisory](https://github.com/leonibr/community-extensions-cache-postgres/security/advisories/new)

### What to Include in Your Report

- A clear description of the vulnerability
- Steps to reproduce the issue
- Versions affected
- Potential impact
- Any possible mitigations you've identified

### What to Expect

1. **Acknowledgment**: We aim to acknowledge receipt of your report within 48 hours.

2. **Updates**: We will keep you informed about:

   - Our progress in investigating the issue
   - Any questions we have about the report
   - The timeline for releasing a fix

3. **Fix Development**: Once validated, we will:

   - Develop and test a fix
   - Prepare security advisory documentation
   - Release the fix following our standard release process

4. **Public Disclosure**: We will coordinate with you on the timing of public disclosure.

## Security Best Practices

When using Community.Microsoft.Extensions.Caching.PostgreSql in your applications, consider these security recommendations:

1. **Database Access**

   - Use a dedicated PostgreSQL user with minimal privileges
   - Restrict the user's access to only the cache schema and tables
   - Use connection string encryption in production

2. **Connection String Security**

   ```csharp
   // Do not store connection strings in code
   services.AddPostgreSqlCache(options =>
   {
       options.ConnectionString = Configuration.GetConnectionString("PostgreSqlCache");
       // ...
   });
   ```

3. **Schema Isolation**

   - Use a separate schema for cache tables
   - Implement proper access controls at the database level

4. **Data Protection**
   - Consider encrypting sensitive cached data before storage
   - Implement proper key rotation policies
   - Use HTTPS for all communications in distributed scenarios

## Security Updates

- Security updates are released as soon as possible after validation
- Updates follow semantic versioning
- Breaking changes in security updates are avoided when possible
- Security advisories will be published on our GitHub repository

## Audit Logging

The package includes basic audit logging through ILogger. To enhance security monitoring:

1. Configure appropriate log levels
1. Enable structured logging
1. Monitor cache operations through logs
1. Implement alerts for suspicious patterns, such as failed login attempts or suspicious activity.

## Known Security Best Practices for PostgreSQL Cache

1. **Connection Pooling**

   ```csharp
   services.AddPostgreSqlCache(options =>
   {
       options.DataSourceFactory = () =>
       {
           var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
           dataSourceBuilder.EnablePooling();
           dataSourceBuilder.MaxPoolSize = 50; // Adjust based on your application's requirements
           return dataSourceBuilder.Build();
       };
   });
   ```

2. **Timeout Settings**
   ```csharp
   services.AddPostgreSqlCache(options =>
   {
       options.CommandTimeout = TimeSpan.FromSeconds(30); // Adjust based on your application's requirements
       // ...
   });
   ```

## Contributing Security Improvements

If you want to contribute security improvements:

1. Follow the standard contribution process in CONTRIBUTING.md
2. Include security impact in pull request descriptions
3. Add tests for security-related changes
4. Update security documentation as needed

## License

Security-related contributions are subject to the same MIT License as the main project.
