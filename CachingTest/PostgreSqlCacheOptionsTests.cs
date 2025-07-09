using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Npgsql;
using Moq;

namespace CachingTest;

public class PostgreSqlCacheOptionsTests
{
    [Fact]
    public void Constructor_WithDefaultValues_SetsCorrectDefaults()
    {
        // Act
        var options = new PostgreSqlCacheOptions();

        // Assert
        Assert.Null(options.DataSourceFactory);
        Assert.Null(options.ConnectionString);
        Assert.NotNull(options.SystemClock);
        Assert.IsType<SystemClock>(options.SystemClock);
        Assert.Null(options.ExpiredItemsDeletionInterval);
        Assert.Null(options.SchemaName);
        Assert.Null(options.TableName);
        Assert.True(options.CreateInfrastructure);
        Assert.Equal(TimeSpan.FromMinutes(20), options.DefaultSlidingExpiration);
        Assert.False(options.DisableRemoveExpired);
        Assert.True(options.UpdateOnGetCacheItem);
        Assert.False(options.ReadOnlyMode);
    }

    [Fact]
    public void DataSourceFactory_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        Func<NpgsqlDataSource> factory = () => null!;

        // Act
        options.DataSourceFactory = factory;

        // Assert
        Assert.Same(factory, options.DataSourceFactory);
    }

    [Fact]
    public void ConnectionString_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var connectionString = "Host=localhost;Database=test;Username=user;Password=pass";

        // Act
        options.ConnectionString = connectionString;

        // Assert
        Assert.Equal(connectionString, options.ConnectionString);
    }

    [Fact]
    public void SystemClock_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var mockClock = new Mock<ISystemClock>().Object;

        // Act
        options.SystemClock = mockClock;

        // Assert
        Assert.Same(mockClock, options.SystemClock);
    }

    [Fact]
    public void ExpiredItemsDeletionInterval_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var interval = TimeSpan.FromMinutes(15);

        // Act
        options.ExpiredItemsDeletionInterval = interval;

        // Assert
        Assert.Equal(interval, options.ExpiredItemsDeletionInterval);
    }

    [Fact]
    public void SchemaName_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var schemaName = "test_schema";

        // Act
        options.SchemaName = schemaName;

        // Assert
        Assert.Equal(schemaName, options.SchemaName);
    }

    [Fact]
    public void TableName_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var tableName = "test_table";

        // Act
        options.TableName = tableName;

        // Assert
        Assert.Equal(tableName, options.TableName);
    }

    [Fact]
    public void CreateInfrastructure_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.CreateInfrastructure = false;

        // Assert
        Assert.False(options.CreateInfrastructure);
    }

    [Fact]
    public void DefaultSlidingExpiration_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var expiration = TimeSpan.FromMinutes(30);

        // Act
        options.DefaultSlidingExpiration = expiration;

        // Assert
        Assert.Equal(expiration, options.DefaultSlidingExpiration);
    }

    [Fact]
    public void DisableRemoveExpired_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.DisableRemoveExpired = true;

        // Assert
        Assert.True(options.DisableRemoveExpired);
    }

    [Fact]
    public void UpdateOnGetCacheItem_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.UpdateOnGetCacheItem = false;

        // Assert
        Assert.False(options.UpdateOnGetCacheItem);
    }

    [Fact]
    public void ReadOnlyMode_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.ReadOnlyMode = true;

        // Assert
        Assert.True(options.ReadOnlyMode);
    }

    [Fact]
    public void Value_Property_ReturnsSelf()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        var value = ((IOptions<PostgreSqlCacheOptions>)options).Value;

        // Assert
        Assert.Same(options, value);
    }

    [Fact]
    public void PostgreSqlCacheOptions_ImplementsIOptionsCorrectly()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var iOptions = (IOptions<PostgreSqlCacheOptions>)options;

        // Act & Assert
        Assert.Same(options, iOptions.Value);
    }

    [Fact]
    public void PostgreSqlCacheOptions_WithAllPropertiesSet_RetainsValues()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var mockClock = new Mock<ISystemClock>().Object;
        Func<NpgsqlDataSource> factory = () => null!;

        // Act
        options.DataSourceFactory = factory;
        options.ConnectionString = "test-connection";
        options.SystemClock = mockClock;
        options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(10);
        options.SchemaName = "my_schema";
        options.TableName = "my_table";
        options.CreateInfrastructure = false;
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(45);
        options.DisableRemoveExpired = true;
        options.UpdateOnGetCacheItem = false;
        options.ReadOnlyMode = true;

        // Assert
        Assert.Same(factory, options.DataSourceFactory);
        Assert.Equal("test-connection", options.ConnectionString);
        Assert.Same(mockClock, options.SystemClock);
        Assert.Equal(TimeSpan.FromMinutes(10), options.ExpiredItemsDeletionInterval);
        Assert.Equal("my_schema", options.SchemaName);
        Assert.Equal("my_table", options.TableName);
        Assert.False(options.CreateInfrastructure);
        Assert.Equal(TimeSpan.FromMinutes(45), options.DefaultSlidingExpiration);
        Assert.True(options.DisableRemoveExpired);
        Assert.False(options.UpdateOnGetCacheItem);
        Assert.True(options.ReadOnlyMode);
    }

    [Fact]
    public void PostgreSqlCacheOptions_WithNullValues_HandlesCorrectly()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.DataSourceFactory = null;
        options.ConnectionString = null;
        options.SystemClock = null;
        options.ExpiredItemsDeletionInterval = null;
        options.SchemaName = null;
        options.TableName = null;

        // Assert
        Assert.Null(options.DataSourceFactory);
        Assert.Null(options.ConnectionString);
        Assert.Null(options.SystemClock);
        Assert.Null(options.ExpiredItemsDeletionInterval);
        Assert.Null(options.SchemaName);
        Assert.Null(options.TableName);
    }

    [Fact]
    public void PostgreSqlCacheOptions_WithEmptyStrings_HandlesCorrectly()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.ConnectionString = "";
        options.SchemaName = "";
        options.TableName = "";

        // Assert
        Assert.Equal("", options.ConnectionString);
        Assert.Equal("", options.SchemaName);
        Assert.Equal("", options.TableName);
    }

    [Fact]
    public void PostgreSqlCacheOptions_WithZeroTimeSpan_HandlesCorrectly()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.DefaultSlidingExpiration = TimeSpan.Zero;
        options.ExpiredItemsDeletionInterval = TimeSpan.Zero;

        // Assert
        Assert.Equal(TimeSpan.Zero, options.DefaultSlidingExpiration);
        Assert.Equal(TimeSpan.Zero, options.ExpiredItemsDeletionInterval);
    }

    [Fact]
    public void PostgreSqlCacheOptions_WithNegativeTimeSpan_HandlesCorrectly()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(-5);
        options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(-10);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(-5), options.DefaultSlidingExpiration);
        Assert.Equal(TimeSpan.FromMinutes(-10), options.ExpiredItemsDeletionInterval);
    }
}