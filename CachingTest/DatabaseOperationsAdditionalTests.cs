using Microsoft.Extensions.Logging;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Npgsql;

namespace CachingTest;

/// <summary>
/// Additional tests for DatabaseOperations that test unique scenarios not covered in the main test suite.
/// Most redundant tests have been removed to avoid duplication.
/// </summary>
public class DatabaseOperationsAdditionalTests
{
    private readonly ILogger<DatabaseOperations> _logger = new NullLoggerFactory().CreateLogger<DatabaseOperations>();

    [Fact]
    public void Constructor_WithDataSourceFactory_CreatesInstanceSuccessfully()
    {
        // Arrange
        var mockDataSource = new Mock<NpgsqlDataSource>();
        var options = new PostgreSqlCacheOptions
        {
            DataSourceFactory = () => mockDataSource.Object,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false // Avoid infrastructure creation for this unit test
        };

        // Act & Assert - Constructor should not throw when DataSourceFactory is provided
        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);
        Assert.NotNull(dbOperations);

        // Note: Actual database operations would fail due to mocking limitations,
        // but this test verifies the constructor path works with DataSourceFactory
    }
}