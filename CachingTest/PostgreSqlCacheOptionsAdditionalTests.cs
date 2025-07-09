using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Internal;
using Moq;
using Npgsql;

namespace CachingTest;

public class PostgreSqlCacheOptionsAdditionalTests
{
    [Fact]
    public void Constructor_WithDefaultValues_SetsCorrectDefaults()
    {
        // Act
        var options = new PostgreSqlCacheOptions();

        // Assert
        Assert.Null(options.ConnectionString);
        Assert.Null(options.DataSourceFactory);
        Assert.Null(options.SchemaName);
        Assert.Null(options.TableName);
        Assert.True(options.CreateInfrastructure);
        Assert.False(options.ReadOnlyMode);
        Assert.True(options.UpdateOnGetCacheItem);
        Assert.False(options.DisableRemoveExpired);
        Assert.Null(options.ExpiredItemsDeletionInterval);
        Assert.Equal(TimeSpan.FromMinutes(20), options.DefaultSlidingExpiration);
        Assert.NotNull(options.SystemClock);
    }

    [Fact]
    public void ConnectionString_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var connectionString = "Host=localhost;Database=test;Username=test;Password=test";

        // Act
        options.ConnectionString = connectionString;

        // Assert
        Assert.Equal(connectionString, options.ConnectionString);
    }

    [Fact]
    public void ConnectionString_CanBeSetToNull()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        options.ConnectionString = "test";

        // Act
        options.ConnectionString = null;

        // Assert
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void ConnectionString_CanBeSetToEmpty()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.ConnectionString = "";

        // Assert
        Assert.Equal("", options.ConnectionString);
    }

    [Fact]
    public void SchemaName_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var schemaName = "custom_schema";

        // Act
        options.SchemaName = schemaName;

        // Assert
        Assert.Equal(schemaName, options.SchemaName);
    }

    [Fact]
    public void SchemaName_CanBeSetToEmpty()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.SchemaName = "";

        // Assert
        Assert.Equal("", options.SchemaName);
    }

    [Fact]
    public void SchemaName_CanBeSetToNull()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.SchemaName = null;

        // Assert
        Assert.Null(options.SchemaName);
    }

    [Fact]
    public void TableName_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var tableName = "custom_table";

        // Act
        options.TableName = tableName;

        // Assert
        Assert.Equal(tableName, options.TableName);
    }

    [Fact]
    public void TableName_CanBeSetToEmpty()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.TableName = "";

        // Assert
        Assert.Equal("", options.TableName);
    }

    [Fact]
    public void TableName_CanBeSetToNull()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.TableName = null;

        // Assert
        Assert.Null(options.TableName);
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
    public void UpdateOnGetCacheItem_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.UpdateOnGetCacheItem = true;

        // Assert
        Assert.True(options.UpdateOnGetCacheItem);
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
    public void ExpiredItemsDeletionInterval_CanBeSetToNull()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(15);

        // Act
        options.ExpiredItemsDeletionInterval = null;

        // Assert
        Assert.Null(options.ExpiredItemsDeletionInterval);
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
    public void DefaultSlidingExpiration_CanBeSetToZero()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.DefaultSlidingExpiration = TimeSpan.Zero;

        // Assert
        Assert.Equal(TimeSpan.Zero, options.DefaultSlidingExpiration);
    }

    [Fact]
    public void DefaultSlidingExpiration_CanBeSetToNegative()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();

        // Act
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(-1);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(-1), options.DefaultSlidingExpiration);
    }

    [Fact]
    public void SystemClock_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var mockClock = new Mock<ISystemClock>();

        // Act
        options.SystemClock = mockClock.Object;

        // Assert
        Assert.Same(mockClock.Object, options.SystemClock);
    }

    [Fact]
    public void SystemClock_CanBeSetToNull()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var mockClock = new Mock<ISystemClock>();
        options.SystemClock = mockClock.Object;

        // Act
        options.SystemClock = null;

        // Assert
        Assert.Null(options.SystemClock);
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
    public void DataSourceFactory_CanBeSetToNull()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        Func<NpgsqlDataSource> factory = () => null!;
        options.DataSourceFactory = factory;

        // Act
        options.DataSourceFactory = null;

        // Assert
        Assert.Null(options.DataSourceFactory);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var mockClock = new Mock<ISystemClock>();
        Func<NpgsqlDataSource> factory = () => null!;

        // Act
        options.ConnectionString = "test-connection";
        options.DataSourceFactory = factory;
        options.SchemaName = "custom_schema";
        options.TableName = "custom_table";
        options.CreateInfrastructure = false;
        options.ReadOnlyMode = true;
        options.UpdateOnGetCacheItem = true;
        options.DisableRemoveExpired = true;
        options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(10);
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(45);
        options.SystemClock = mockClock.Object;

        // Assert
        Assert.Equal("test-connection", options.ConnectionString);
        Assert.Same(factory, options.DataSourceFactory);
        Assert.Equal("custom_schema", options.SchemaName);
        Assert.Equal("custom_table", options.TableName);
        Assert.False(options.CreateInfrastructure);
        Assert.True(options.ReadOnlyMode);
        Assert.True(options.UpdateOnGetCacheItem);
        Assert.True(options.DisableRemoveExpired);
        Assert.Equal(TimeSpan.FromMinutes(10), options.ExpiredItemsDeletionInterval);
        Assert.Equal(TimeSpan.FromMinutes(45), options.DefaultSlidingExpiration);
        Assert.Same(mockClock.Object, options.SystemClock);
    }

    [Fact]
    public void ExpiredItemsDeletionInterval_WithVeryShortInterval_CanBeSet()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var shortInterval = TimeSpan.FromMinutes(5);

        // Act
        options.ExpiredItemsDeletionInterval = shortInterval;

        // Assert
        Assert.Equal(shortInterval, options.ExpiredItemsDeletionInterval);
    }

    [Fact]
    public void ExpiredItemsDeletionInterval_WithVeryLongInterval_CanBeSet()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var longInterval = TimeSpan.FromHours(24);

        // Act
        options.ExpiredItemsDeletionInterval = longInterval;

        // Assert
        Assert.Equal(longInterval, options.ExpiredItemsDeletionInterval);
    }

    [Fact]
    public void DefaultSlidingExpiration_WithVeryShortExpiration_CanBeSet()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var shortExpiration = TimeSpan.FromMilliseconds(1);

        // Act
        options.DefaultSlidingExpiration = shortExpiration;

        // Assert
        Assert.Equal(shortExpiration, options.DefaultSlidingExpiration);
    }

    [Fact]
    public void DefaultSlidingExpiration_WithVeryLongExpiration_CanBeSet()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var longExpiration = TimeSpan.FromDays(365);

        // Act
        options.DefaultSlidingExpiration = longExpiration;

        // Assert
        Assert.Equal(longExpiration, options.DefaultSlidingExpiration);
    }

    [Fact]
    public void SchemaName_WithSpecialCharacters_CanBeSet()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var specialSchema = "test-schema_with.special@chars";

        // Act
        options.SchemaName = specialSchema;

        // Assert
        Assert.Equal(specialSchema, options.SchemaName);
    }

    [Fact]
    public void TableName_WithSpecialCharacters_CanBeSet()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions();
        var specialTable = "test-table_with.special@chars";

        // Act
        options.TableName = specialTable;

        // Assert
        Assert.Equal(specialTable, options.TableName);
    }
}