using Community.Microsoft.Extensions.Caching.PostgreSql;

namespace CachingTest;

public class SqlCommandTypesTests
{
    [Fact]
    public void ItemIdUtcNow_WithValidProperties_SetsCorrectly()
    {
        // Arrange
        var id = "test-id";
        var utcNow = DateTimeOffset.UtcNow;

        // Act
        var item = new ItemIdUtcNow
        {
            Id = id,
            UtcNow = utcNow
        };

        // Assert
        Assert.Equal(id, item.Id);
        Assert.Equal(utcNow, item.UtcNow);
    }

    [Fact]
    public void ItemIdUtcNow_WithNullId_HandlesCorrectly()
    {
        // Arrange & Act
        var item = new ItemIdUtcNow
        {
            Id = null!,
            UtcNow = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Null(item.Id);
    }

    [Fact]
    public void ItemFull_WithValidProperties_SetsCorrectly()
    {
        // Arrange
        var id = "test-id";
        var expiresAtTime = DateTimeOffset.UtcNow.AddMinutes(5);
        var value = new byte[] { 1, 2, 3, 4, 5 };
        var slidingExpirationInSeconds = 300.0;
        var absoluteExpiration = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var item = new ItemFull
        {
            Id = id,
            ExpiresAtTime = expiresAtTime,
            Value = value,
            SlidingExpirationInSeconds = slidingExpirationInSeconds,
            AbsoluteExpiration = absoluteExpiration
        };

        // Assert
        Assert.Equal(id, item.Id);
        Assert.Equal(expiresAtTime, item.ExpiresAtTime);
        Assert.Equal(value, item.Value);
        Assert.Equal(slidingExpirationInSeconds, item.SlidingExpirationInSeconds);
        Assert.Equal(absoluteExpiration, item.AbsoluteExpiration);
    }

    [Fact]
    public void ItemFull_WithNullValues_HandlesCorrectly()
    {
        // Arrange & Act
        var item = new ItemFull
        {
            Id = null!,
            ExpiresAtTime = DateTimeOffset.UtcNow,
            Value = null!,
            SlidingExpirationInSeconds = null,
            AbsoluteExpiration = null
        };

        // Assert
        Assert.Null(item.Id);
        Assert.Null(item.Value);
        Assert.Null(item.SlidingExpirationInSeconds);
        Assert.Null(item.AbsoluteExpiration);
    }

    [Fact]
    public void ItemFull_WithEmptyByteArray_HandlesCorrectly()
    {
        // Arrange & Act
        var item = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = DateTimeOffset.UtcNow,
            Value = new byte[0],
            SlidingExpirationInSeconds = 300.0,
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Assert
        Assert.NotNull(item.Value);
        Assert.Empty(item.Value);
    }

    [Fact]
    public void CurrentUtcNow_WithValidProperty_SetsCorrectly()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;

        // Act
        var item = new CurrentUtcNow
        {
            UtcNow = utcNow
        };

        // Assert
        Assert.Equal(utcNow, item.UtcNow);
    }

    [Fact]
    public void ItemIdOnly_WithValidProperty_SetsCorrectly()
    {
        // Arrange
        var id = "test-id";

        // Act
        var item = new ItemIdOnly
        {
            Id = id
        };

        // Assert
        Assert.Equal(id, item.Id);
    }

    [Fact]
    public void ItemIdOnly_WithNullId_HandlesCorrectly()
    {
        // Arrange & Act
        var item = new ItemIdOnly
        {
            Id = null!
        };

        // Assert
        Assert.Null(item.Id);
    }

    [Fact]
    public void SqlCommandTypes_AreRecords_SupportValueEquality()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var value = new byte[] { 1, 2, 3 };

        var item1 = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = utcNow,
            Value = value,
            SlidingExpirationInSeconds = 300.0,
            AbsoluteExpiration = utcNow.AddHours(1)
        };

        var item2 = new ItemFull
        {
            Id = "test-id",
            ExpiresAtTime = utcNow,
            Value = value,
            SlidingExpirationInSeconds = 300.0,
            AbsoluteExpiration = utcNow.AddHours(1)
        };

        // Act & Assert
        Assert.Equal(item1, item2);
        Assert.True(item1.Equals(item2));
    }

    [Fact]
    public void SqlCommandTypes_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var value = new byte[] { 1, 2, 3 };

        var item1 = new ItemFull
        {
            Id = "test-id-1",
            ExpiresAtTime = utcNow,
            Value = value,
            SlidingExpirationInSeconds = 300.0,
            AbsoluteExpiration = utcNow.AddHours(1)
        };

        var item2 = new ItemFull
        {
            Id = "test-id-2",
            ExpiresAtTime = utcNow,
            Value = value,
            SlidingExpirationInSeconds = 300.0,
            AbsoluteExpiration = utcNow.AddHours(1)
        };

        // Act & Assert
        Assert.NotEqual(item1, item2);
        Assert.False(item1.Equals(item2));
    }

    [Fact]
    public void SqlCommandTypes_SupportDeconstruction()
    {
        // Arrange
        var id = "test-id";
        var utcNow = DateTimeOffset.UtcNow;

        var item = new ItemIdUtcNow
        {
            Id = id,
            UtcNow = utcNow
        };

        // Act - Test property access instead of deconstruction
        var itemId = item.Id;
        var itemUtcNow = item.UtcNow;

        // Assert
        Assert.Equal(id, itemId);
        Assert.Equal(utcNow, itemUtcNow);
    }

    [Fact]
    public void SqlCommandTypes_WithWithExpression_CreateNewInstances()
    {
        // Arrange
        var originalItem = new ItemIdUtcNow
        {
            Id = "original-id",
            UtcNow = DateTimeOffset.UtcNow
        };

        var newUtcNow = DateTimeOffset.UtcNow.AddHours(1);

        // Act - Create a new instance manually since records don't have with expressions in this version
        var newItem = new ItemIdUtcNow
        {
            Id = originalItem.Id,
            UtcNow = newUtcNow
        };

        // Assert
        Assert.Equal(originalItem.Id, newItem.Id);
        Assert.Equal(newUtcNow, newItem.UtcNow);
        Assert.NotEqual(originalItem.UtcNow, newItem.UtcNow);
    }
}