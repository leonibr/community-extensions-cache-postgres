using Community.Microsoft.Extensions.Caching.PostgreSql;

namespace CachingTest;

public class SqlCommandsTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var sqlCommands = new SqlCommands("test_schema", "test_table");

        // Assert
        Assert.NotNull(sqlCommands);
    }

    [Fact]
    public void CreateSchemaAndTableSql_WithValidParameters_GeneratesCorrectSql()
    {
        // Arrange
        var sqlCommands = new SqlCommands("test_schema", "test_table");

        // Act
        var sql = sqlCommands.CreateSchemaAndTableSql;

        // Assert
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS \"test_schema\"", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS \"test_schema\".\"test_table\"", sql);
        Assert.Contains("\"Id\" text COLLATE pg_catalog.\"default\" NOT NULL", sql);
        Assert.Contains("\"Value\" bytea", sql);
        Assert.Contains("\"ExpiresAtTime\" timestamp with time zone", sql);
        Assert.Contains("\"SlidingExpirationInSeconds\" double precision", sql);
        Assert.Contains("\"AbsoluteExpiration\" timestamp with time zone", sql);
        Assert.Contains("CONSTRAINT \"DistCache_pkey\" PRIMARY KEY (\"Id\")", sql);
    }

    [Fact]
    public void GetCacheItemSql_WithValidParameters_GeneratesCorrectSql()
    {
        // Arrange
        var sqlCommands = new SqlCommands("test_schema", "test_table");

        // Act
        var sql = sqlCommands.GetCacheItemSql;

        // Assert
        Assert.Contains("SELECT \"Value\"", sql);
        Assert.Contains("FROM \"test_schema\".\"test_table\"", sql);
        Assert.Contains("WHERE \"Id\" = @Id AND @UtcNow <= \"ExpiresAtTime\"", sql);
    }

    [Fact]
    public void SetCacheSql_WithValidParameters_GeneratesCorrectSql()
    {
        // Arrange
        var sqlCommands = new SqlCommands("test_schema", "test_table");

        // Act
        var sql = sqlCommands.SetCacheSql;

        // Assert
        Assert.Contains("INSERT INTO \"test_schema\".\"test_table\"", sql);
        Assert.Contains("(\"Id\", \"Value\", \"ExpiresAtTime\", \"SlidingExpirationInSeconds\", \"AbsoluteExpiration\")", sql);
        Assert.Contains("VALUES (@Id, @Value, @ExpiresAtTime, @SlidingExpirationInSeconds, @AbsoluteExpiration)", sql);
        Assert.Contains("ON CONFLICT(\"Id\") DO", sql);
        Assert.Contains("UPDATE SET", sql);
        Assert.Contains("\"Value\" = EXCLUDED.\"Value\"", sql);
        Assert.Contains("\"ExpiresAtTime\" = EXCLUDED.\"ExpiresAtTime\"", sql);
        Assert.Contains("\"SlidingExpirationInSeconds\" = EXCLUDED.\"SlidingExpirationInSeconds\"", sql);
        Assert.Contains("\"AbsoluteExpiration\" = EXCLUDED.\"AbsoluteExpiration\"", sql);
    }

    [Fact]
    public void UpdateCacheItemSql_WithValidParameters_GeneratesCorrectSql()
    {
        // Arrange
        var sqlCommands = new SqlCommands("test_schema", "test_table");

        // Act
        var sql = sqlCommands.UpdateCacheItemSql;

        // Assert
        Assert.Contains("UPDATE \"test_schema\".\"test_table\"", sql);
        Assert.Contains("SET \"ExpiresAtTime\" = LEAST(\"AbsoluteExpiration\", @UtcNow + \"SlidingExpirationInSeconds\" * interval '1 second')", sql);
        Assert.Contains("WHERE \"Id\" = @Id", sql);
        Assert.Contains("AND @UtcNow <= \"ExpiresAtTime\"", sql);
        Assert.Contains("AND \"SlidingExpirationInSeconds\" IS NOT NULL", sql);
        Assert.Contains("AND (\"AbsoluteExpiration\" IS NULL OR \"AbsoluteExpiration\" <> \"ExpiresAtTime\")", sql);
    }

    [Fact]
    public void DeleteCacheItemSql_WithValidParameters_GeneratesCorrectSql()
    {
        // Arrange
        var sqlCommands = new SqlCommands("test_schema", "test_table");

        // Act
        var sql = sqlCommands.DeleteCacheItemSql;

        // Assert
        Assert.Contains("DELETE FROM \"test_schema\".\"test_table\"", sql);
        Assert.Contains("WHERE \"Id\" = @Id", sql);
    }

    [Fact]
    public void DeleteExpiredCacheSql_WithValidParameters_GeneratesCorrectSql()
    {
        // Arrange
        var sqlCommands = new SqlCommands("test_schema", "test_table");

        // Act
        var sql = sqlCommands.DeleteExpiredCacheSql;

        // Assert
        Assert.Contains("DELETE FROM \"test_schema\".\"test_table\"", sql);
        Assert.Contains("WHERE @UtcNow > \"ExpiresAtTime\"", sql);
    }
}