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