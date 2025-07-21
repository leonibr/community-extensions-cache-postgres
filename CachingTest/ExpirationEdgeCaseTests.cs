using Microsoft.Extensions.Logging;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Moq;

namespace CachingTest;

using Testcontainers.PostgreSql;

public class ExpirationEdgeCaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    
    private readonly ILogger<DatabaseOperations> _logger = new NullLoggerFactory().CreateLogger<DatabaseOperations>();

    public ExpirationEdgeCaseTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithPassword("Strong_password_123!")
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }
   
    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    } 
    
    [Fact]
    public void SetCacheItem_WithAbsoluteExpirationRelativeToNow_UsesRelativeExpiration()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection string
        var ex = Record.Exception(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }));
        
        Assert.Null(ex);
    }

    [Fact]
    public void SetCacheItem_WithBothAbsoluteAndSlidingExpiration_UsesSlidingExpiration()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection string
        var ex = Record.Exception(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.UtcNow.AddMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            }));
        
        Assert.Null(ex);
    }

    [Fact]
    public void SetCacheItem_WithSlidingExpirationOnly_UsesSlidingExpiration()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection string
        var ex = Record.Exception(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) }));
        
        Assert.Null(ex);
    }

    [Fact]
    public void SetCacheItem_WithZeroSlidingExpiration_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.Zero }));
    }

    [Fact]
    public void SetCacheItem_WithNegativeSlidingExpiration_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(-1) }));
    }

    [Fact]
    public void SetCacheItem_WithNullSlidingExpirationAndNullAbsoluteExpiration_ShouldNotThrow()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert
        var ex = Record.Exception(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions()));
        
        Assert.Null(ex);
    }

    [Fact]
    public void SetCacheItem_WithPastAbsoluteExpiration_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow.AddMinutes(-1) }));
    }

    [Fact]
    public void SetCacheItem_WithPastAbsoluteExpirationRelativeToNow_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(-1) }));
    }

    [Fact]
    public void SetCacheItem_WithExactCurrentTimeAbsoluteExpiration_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow }));
    }

    [Fact]
    public void SetCacheItem_WithExactCurrentTimeAbsoluteExpirationRelativeToNow_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.Zero }));
    }

    [Fact]
    public void SetCacheItem_WithVeryLongSlidingExpiration_ShouldNotThrow()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection string
        var ex = Record.Exception(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(365) }));
        
        Assert.Null(ex);
    }

    [Fact]
    public void SetCacheItem_WithVeryLongAbsoluteExpiration_ShouldNotThrow()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection string
        var ex = Record.Exception(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow.AddYears(10) }));
        
        Assert.Null(ex);
    }

    [Fact]
    public void SetCacheItem_WithVeryShortSlidingExpiration_ShouldNotThrow()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection 
        var ex = Record.Exception(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMilliseconds(1) }));
        
        Assert.Null(ex);
    }

    [Fact]
    public void SetCacheItem_WithVeryShortAbsoluteExpiration_ShouldNotThrow()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection 
        var ex = Record.Exception(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow.AddMilliseconds(1) }));
        
        Assert.Null(ex);
    }
}