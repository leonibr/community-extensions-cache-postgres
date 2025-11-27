using System;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Npgsql;

namespace CachingTest;

public class SqlCommandTypesTests
{
    [Fact]
    public void ItemIdUtcNow_CanBeInstantiated()
    {
        // Arrange & Act
        var item = new ItemIdUtcNow
        {
            Id = "test-id",
            UtcNow = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.NotNull(item);
        Assert.Equal("test-id", item.Id);
        Assert.NotEqual(default(DateTimeOffset), item.UtcNow);
    }

    [Fact]
    public void ItemFull_CanBeInstantiated()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var value = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var item = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = utcNow.AddHours(1),
            Value = value,
            SlidingExpirationInSeconds = 3600,
            AbsoluteExpiration = utcNow.AddDays(1)
        };

        // Assert
        Assert.NotNull(item);
        Assert.Equal("test-id", item.Id);
        Assert.Equal(utcNow.AddHours(1), item.ExpiresAtTime);
        Assert.Equal(value, item.Value);
        Assert.Equal(3600, item.SlidingExpirationInSeconds);
        Assert.Equal(utcNow.AddDays(1), item.AbsoluteExpiration);
    }

    [Fact]
    public void ItemFull_WithNullableProperties_CanBeInstantiated()
    {
        // Arrange & Act
        var item = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = DateTimeOffset.UtcNow.AddHours(1),
            Value = new byte[] { 1, 2, 3 },
            SlidingExpirationInSeconds = null,
            AbsoluteExpiration = null
        };

        // Assert
        Assert.NotNull(item);
        Assert.Null(item.SlidingExpirationInSeconds);
        Assert.Null(item.AbsoluteExpiration);
    }

    [Fact]
    public void CurrentUtcNow_CanBeInstantiated()
    {
        // Arrange & Act
        var item = new CurrentUtcNow
        {
            UtcNow = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.NotNull(item);
        Assert.NotEqual(default(DateTimeOffset), item.UtcNow);
    }

    [Fact]
    public void ItemIdOnly_CanBeInstantiated()
    {
        // Arrange & Act
        var item = new ItemIdOnly
        {
            Id = "test-id"
        };

        // Assert
        Assert.NotNull(item);
        Assert.Equal("test-id", item.Id);
    }

    [Fact]
    public void ItemIdUtcNow_IsRecord_SupportsValueEquality()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var item1 = new ItemIdUtcNow { Id = "test-id", UtcNow = utcNow };
        var item2 = new ItemIdUtcNow { Id = "test-id", UtcNow = utcNow };

        // Act & Assert
        Assert.Equal(item1, item2);
    }

    [Fact]
    public void ItemFull_IsRecord_SupportsValueEquality()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var value = new byte[] { 1, 2, 3 };
        var item1 = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = utcNow,
            Value = value,
            SlidingExpirationInSeconds = 3600,
            AbsoluteExpiration = utcNow.AddDays(1)
        };
        var item2 = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = utcNow,
            Value = value,
            SlidingExpirationInSeconds = 3600,
            AbsoluteExpiration = utcNow.AddDays(1)
        };

        // Act & Assert
        Assert.Equal(item1, item2);
    }

    [Fact]
    public void CurrentUtcNow_IsRecord_SupportsValueEquality()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var item1 = new CurrentUtcNow { UtcNow = utcNow };
        var item2 = new CurrentUtcNow { UtcNow = utcNow };

        // Act & Assert
        Assert.Equal(item1, item2);
    }

    [Fact]
    public void ItemIdOnly_IsRecord_SupportsValueEquality()
    {
        // Arrange
        var item1 = new ItemIdOnly { Id = "test-id" };
        var item2 = new ItemIdOnly { Id = "test-id" };

        // Act & Assert
        Assert.Equal(item1, item2);
    }
}

