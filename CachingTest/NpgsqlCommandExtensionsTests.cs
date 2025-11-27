using System;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Npgsql;

namespace CachingTest;

public class NpgsqlCommandExtensionsTests
{
    [Fact]
    public void AddParameters_WithItemIdUtcNow_AddsCorrectParameters()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
        using var command = connection.CreateCommand();
        var utcNow = DateTimeOffset.UtcNow;
        var item = new ItemIdUtcNow
        {
            Id = "test-id",
            UtcNow = utcNow
        };

        // Act
        command.AddParameters(item);

        // Assert
        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("test-id", command.Parameters["@Id"].Value);
        Assert.Equal(utcNow, command.Parameters["@UtcNow"].Value);
    }

    [Fact]
    public void AddParameters_WithItemFull_AddsAllParameters()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
        using var command = connection.CreateCommand();
        var utcNow = DateTimeOffset.UtcNow;
        var value = new byte[] { 1, 2, 3, 4, 5 };
        var item = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = utcNow.AddHours(1),
            Value = value,
            SlidingExpirationInSeconds = 3600,
            AbsoluteExpiration = utcNow.AddDays(1)
        };

        // Act
        command.AddParameters(item);

        // Assert
        Assert.Equal(5, command.Parameters.Count);
        Assert.Equal("test-id", command.Parameters["@Id"].Value);
        Assert.Equal(value, command.Parameters["@Value"].Value);
        Assert.Equal(utcNow.AddHours(1), command.Parameters["@ExpiresAtTime"].Value);
        Assert.Equal(3600.0, command.Parameters["@SlidingExpirationInSeconds"].Value);
        Assert.Equal(utcNow.AddDays(1), command.Parameters["@AbsoluteExpiration"].Value);
    }

    [Fact]
    public void AddParameters_WithItemFull_WithNullValues_AddsDBNull()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
        using var command = connection.CreateCommand();
        var utcNow = DateTimeOffset.UtcNow;
        var value = new byte[] { 1, 2, 3 };
        var item = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = utcNow.AddHours(1),
            Value = value,
            SlidingExpirationInSeconds = null,
            AbsoluteExpiration = null
        };

        // Act
        command.AddParameters(item);

        // Assert
        Assert.Equal(5, command.Parameters.Count);
        Assert.Equal(DBNull.Value, command.Parameters["@SlidingExpirationInSeconds"].Value);
        Assert.Equal(DBNull.Value, command.Parameters["@AbsoluteExpiration"].Value);
    }

    [Fact]
    public void AddParameters_WithCurrentUtcNow_AddsCorrectParameter()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
        using var command = connection.CreateCommand();
        var utcNow = DateTimeOffset.UtcNow;
        var item = new CurrentUtcNow
        {
            UtcNow = utcNow
        };

        // Act
        command.AddParameters(item);

        // Assert
        Assert.Single(command.Parameters);
        Assert.Equal(utcNow, command.Parameters["@UtcNow"].Value);
    }

    [Fact]
    public void AddParameters_WithItemIdOnly_AddsCorrectParameter()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
        using var command = connection.CreateCommand();
        var item = new ItemIdOnly
        {
            Id = "test-id"
        };

        // Act
        command.AddParameters(item);

        // Assert
        Assert.Single(command.Parameters);
        Assert.Equal("test-id", command.Parameters["@Id"].Value);
    }

    [Fact]
    public void AddParameters_MultipleCallsWithSameParameterType_AppendsParameters()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
        using var command = connection.CreateCommand();
        var item1 = new ItemIdOnly { Id = "test-id-1" };
        var item2 = new CurrentUtcNow { UtcNow = DateTimeOffset.UtcNow };

        // Act
        command.AddParameters(item1);
        command.AddParameters(item2);

        // Assert
        Assert.Equal(2, command.Parameters.Count);
    }

    [Fact]
    public void AddParameters_WithItemIdUtcNow_ParameterNamesMatchSqlConvention()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
        using var command = connection.CreateCommand();
        var item = new ItemIdUtcNow
        {
            Id = "test-id",
            UtcNow = DateTimeOffset.UtcNow
        };

        // Act
        command.AddParameters(item);

        // Assert - Parameter names should start with @
        Assert.Contains(command.Parameters, p => p.ParameterName == "@Id");
        Assert.Contains(command.Parameters, p => p.ParameterName == "@UtcNow");
    }

    [Fact]
    public void AddParameters_WithItemFull_ParameterNamesMatchSqlConvention()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
        using var command = connection.CreateCommand();
        var item = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = DateTimeOffset.UtcNow.AddHours(1),
            Value = new byte[] { 1, 2, 3 },
            SlidingExpirationInSeconds = 3600,
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        command.AddParameters(item);

        // Assert - Parameter names should start with @
        Assert.Contains(command.Parameters, p => p.ParameterName == "@Id");
        Assert.Contains(command.Parameters, p => p.ParameterName == "@Value");
        Assert.Contains(command.Parameters, p => p.ParameterName == "@ExpiresAtTime");
        Assert.Contains(command.Parameters, p => p.ParameterName == "@SlidingExpirationInSeconds");
        Assert.Contains(command.Parameters, p => p.ParameterName == "@AbsoluteExpiration");
    }
}

