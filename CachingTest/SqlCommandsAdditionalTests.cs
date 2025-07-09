using Community.Microsoft.Extensions.Caching.PostgreSql;

namespace CachingTest;

public class SqlCommandsAdditionalTests
{
    [Fact]
    public void Constructor_WithValidSchemaAndTable_CreatesInstance()
    {
        // Arrange & Act
        var sqlCommands = new SqlCommands("test_schema", "test_table");

        // Assert
        Assert.NotNull(sqlCommands);
    }

    [Fact]
    public void Constructor_WithSpecialCharactersInSchemaAndTable_HandlesCorrectly()
    {
        // Arrange & Act
        var sqlCommands = new SqlCommands("test-schema", "test_table");

        // Assert
        Assert.NotNull(sqlCommands);
        Assert.Contains("test-schema", sqlCommands.CreateSchemaAndTableSql);
        Assert.Contains("test_table", sqlCommands.CreateSchemaAndTableSql);
    }

    [Fact]
    public void CreateSchemaAndTableSql_ContainsCorrectSchemaAndTable()
    {
        // Arrange
        var schemaName = "custom_schema";
        var tableName = "custom_table";
        var sqlCommands = new SqlCommands(schemaName, tableName);

        // Act
        var sql = sqlCommands.CreateSchemaAndTableSql;

        // Assert
        Assert.Contains(schemaName, sql);
        Assert.Contains(tableName, sql);
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS", sql);
    }

    [Fact]
    public void GetCacheItemSql_ContainsCorrectSchemaAndTable()
    {
        // Arrange
        var schemaName = "custom_schema";
        var tableName = "custom_table";
        var sqlCommands = new SqlCommands(schemaName, tableName);

        // Act
        var sql = sqlCommands.GetCacheItemSql;

        // Assert
        Assert.Contains(schemaName, sql);
        Assert.Contains(tableName, sql);
        Assert.Contains("SELECT", sql);
        Assert.Contains("Value", sql);
    }

    [Fact]
    public void SetCacheSql_ContainsCorrectSchemaAndTable()
    {
        // Arrange
        var schemaName = "custom_schema";
        var tableName = "custom_table";
        var sqlCommands = new SqlCommands(schemaName, tableName);

        // Act
        var sql = sqlCommands.SetCacheSql;

        // Assert
        Assert.Contains(schemaName, sql);
        Assert.Contains(tableName, sql);
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("ON CONFLICT", sql);
    }

    [Fact]
    public void DeleteCacheItemSql_ContainsCorrectSchemaAndTable()
    {
        // Arrange
        var schemaName = "custom_schema";
        var tableName = "custom_table";
        var sqlCommands = new SqlCommands(schemaName, tableName);

        // Act
        var sql = sqlCommands.DeleteCacheItemSql;

        // Assert
        Assert.Contains(schemaName, sql);
        Assert.Contains(tableName, sql);
        Assert.Contains("DELETE FROM", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void UpdateCacheItemSql_ContainsCorrectSchemaAndTable()
    {
        // Arrange
        var schemaName = "custom_schema";
        var tableName = "custom_table";
        var sqlCommands = new SqlCommands(schemaName, tableName);

        // Act
        var sql = sqlCommands.UpdateCacheItemSql;

        // Assert
        Assert.Contains(schemaName, sql);
        Assert.Contains(tableName, sql);
        Assert.Contains("UPDATE", sql);
        Assert.Contains("SET", sql);
    }

    [Fact]
    public void DeleteExpiredCacheSql_ContainsCorrectSchemaAndTable()
    {
        // Arrange
        var schemaName = "custom_schema";
        var tableName = "custom_table";
        var sqlCommands = new SqlCommands(schemaName, tableName);

        // Act
        var sql = sqlCommands.DeleteExpiredCacheSql;

        // Assert
        Assert.Contains(schemaName, sql);
        Assert.Contains(tableName, sql);
        Assert.Contains("DELETE FROM", sql);
        Assert.Contains("ExpiresAtTime", sql);
    }

    [Fact]
    public void SqlCommands_WithEmptySchema_DoesNotThrow()
    {
        // Act & Assert - SqlCommands constructor doesn't validate parameters
        var sqlCommands = new SqlCommands("", "test_table");
        Assert.NotNull(sqlCommands);
    }

    [Fact]
    public void SqlCommands_WithNullSchema_DoesNotThrow()
    {
        // Act & Assert - SqlCommands constructor doesn't validate parameters
        var sqlCommands = new SqlCommands(null!, "test_table");
        Assert.NotNull(sqlCommands);
    }

    [Fact]
    public void SqlCommands_WithEmptyTable_DoesNotThrow()
    {
        // Act & Assert - SqlCommands constructor doesn't validate parameters
        var sqlCommands = new SqlCommands("test_schema", "");
        Assert.NotNull(sqlCommands);
    }

    [Fact]
    public void SqlCommands_WithNullTable_DoesNotThrow()
    {
        // Act & Assert - SqlCommands constructor doesn't validate parameters
        var sqlCommands = new SqlCommands("test_schema", null!);
        Assert.NotNull(sqlCommands);
    }
}

public class SqlCommandTypesAdditionalTests
{
    [Fact]
    public void ItemIdOnly_Properties_WorkCorrectly()
    {
        // Arrange
        var itemIdOnly = new ItemIdOnly { Id = "test-key" };

        // Act & Assert
        Assert.Equal("test-key", itemIdOnly.Id);
    }

    [Fact]
    public void ItemIdUtcNow_Properties_WorkCorrectly()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var itemIdUtcNow = new ItemIdUtcNow { Id = "test-key", UtcNow = utcNow };

        // Act & Assert
        Assert.Equal("test-key", itemIdUtcNow.Id);
        Assert.Equal(utcNow, itemIdUtcNow.UtcNow);
    }

    [Fact]
    public void CurrentUtcNow_Properties_WorkCorrectly()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var currentUtcNow = new CurrentUtcNow { UtcNow = utcNow };

        // Act & Assert
        Assert.Equal(utcNow, currentUtcNow.UtcNow);
    }

    [Fact]
    public void ItemFull_Properties_WorkCorrectly()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var absoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10);
        var itemFull = new ItemFull
        {
            Id = "test-key",
            Value = new byte[] { 1, 2, 3 },
            ExpiresAtTime = utcNow.AddMinutes(5),
            SlidingExpirationInSeconds = 300.0,
            AbsoluteExpiration = absoluteExpiration
        };

        // Act & Assert
        Assert.Equal("test-key", itemFull.Id);
        Assert.Equal(new byte[] { 1, 2, 3 }, itemFull.Value);
        Assert.Equal(utcNow.AddMinutes(5), itemFull.ExpiresAtTime);
        Assert.Equal(300.0, itemFull.SlidingExpirationInSeconds);
        Assert.Equal(absoluteExpiration, itemFull.AbsoluteExpiration);
    }

    [Fact]
    public void ItemFull_WithNullValue_WorksCorrectly()
    {
        // Arrange
        var itemFull = new ItemFull
        {
            Id = "test-key",
            Value = null,
            ExpiresAtTime = DateTimeOffset.UtcNow.AddMinutes(5),
            SlidingExpirationInSeconds = null,
            AbsoluteExpiration = null
        };

        // Act & Assert
        Assert.Equal("test-key", itemFull.Id);
        Assert.Null(itemFull.Value);
        Assert.Null(itemFull.SlidingExpirationInSeconds);
        Assert.Null(itemFull.AbsoluteExpiration);
    }

    [Fact]
    public void ItemFull_WithZeroSlidingExpiration_WorksCorrectly()
    {
        // Arrange
        var itemFull = new ItemFull
        {
            Id = "test-key",
            Value = new byte[] { 1, 2, 3 },
            ExpiresAtTime = DateTimeOffset.UtcNow.AddMinutes(5),
            SlidingExpirationInSeconds = 0.0,
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        // Act & Assert
        Assert.Equal(0.0, itemFull.SlidingExpirationInSeconds);
    }

    [Fact]
    public void ItemFull_WithNegativeSlidingExpiration_WorksCorrectly()
    {
        // Arrange
        var itemFull = new ItemFull
        {
            Id = "test-key",
            Value = new byte[] { 1, 2, 3 },
            ExpiresAtTime = DateTimeOffset.UtcNow.AddMinutes(5),
            SlidingExpirationInSeconds = -1.0,
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        // Act & Assert
        Assert.Equal(-1.0, itemFull.SlidingExpirationInSeconds);
    }

    [Fact]
    public void ItemFull_WithVeryLargeSlidingExpiration_WorksCorrectly()
    {
        // Arrange
        var itemFull = new ItemFull
        {
            Id = "test-key",
            Value = new byte[] { 1, 2, 3 },
            ExpiresAtTime = DateTimeOffset.UtcNow.AddMinutes(5),
            SlidingExpirationInSeconds = double.MaxValue,
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        // Act & Assert
        Assert.Equal(double.MaxValue, itemFull.SlidingExpirationInSeconds);
    }

    [Fact]
    public void ItemFull_WithEmptyByteArray_WorksCorrectly()
    {
        // Arrange
        var itemFull = new ItemFull
        {
            Id = "test-key",
            Value = new byte[0],
            ExpiresAtTime = DateTimeOffset.UtcNow.AddMinutes(5),
            SlidingExpirationInSeconds = 300.0,
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        // Act & Assert
        Assert.Equal(new byte[0], itemFull.Value);
    }

    [Fact]
    public void ItemFull_WithLargeByteArray_WorksCorrectly()
    {
        // Arrange
        var largeArray = new byte[10000];
        for (int i = 0; i < largeArray.Length; i++)
        {
            largeArray[i] = (byte)(i % 256);
        }

        var itemFull = new ItemFull
        {
            Id = "test-key",
            Value = largeArray,
            ExpiresAtTime = DateTimeOffset.UtcNow.AddMinutes(5),
            SlidingExpirationInSeconds = 300.0,
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        // Act & Assert
        Assert.Equal(largeArray, itemFull.Value);
        Assert.Equal(10000, itemFull.Value.Length);
    }
}