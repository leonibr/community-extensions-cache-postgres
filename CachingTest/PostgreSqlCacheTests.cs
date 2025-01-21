// using Community.Microsoft.Extensions.Caching.PostgreSql;
// using Microsoft.Extensions.Caching.Distributed;
// using Microsoft.Extensions.Options;
// using Testcontainers.PostgreSql;
// using System.Text;

// namespace CachingTest;

// public class PostgreSqlCacheTests : IAsyncLifetime
// {
//     private readonly PostgreSqlContainer _postgresContainer;
//     private PostgreSqlCache _cache;
//     private readonly PostgreSqlCacheOptions _options;

//     public PostgreSqlCacheTests()
//     {
//         _postgresContainer = new PostgreSqlBuilder()
//             .WithImage("postgres:latest")
//             .WithPassword("Strong_password_123!")
//             .Build();

//         _options = new PostgreSqlCacheOptions
//         {
//             ConnectionString = string.Empty, // Will be set after container starts
//             SchemaName = "cache",
//             TableName = "distributed_cache",
//             CreateInfrastructure = true
//         };
//     }

//     public async Task InitializeAsync()
//     {
//         await _postgresContainer.StartAsync();
//         _options.ConnectionString = _postgresContainer.GetConnectionString();
//         _cache = new PostgreSqlCache(Options.Create(_options));
//     }

//     public async Task DisposeAsync()
//     {
//         await _postgresContainer.DisposeAsync();
//     }

//     [Fact]
//     public async Task Set_And_Get_Should_Work()
//     {
//         // Arrange
//         var key = "test-key";
//         var expectedValue = "test-value";
//         var options = new DistributedCacheEntryOptions
//         {
//             AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
//         };

//         // Act
//         await _cache.SetAsync(key, Encoding.UTF8.GetBytes(expectedValue), options);
//         var result = await _cache.GetAsync(key);

//         // Assert
//         Assert.NotNull(result);
//         Assert.Equal(expectedValue, Encoding.UTF8.GetString(result));
//     }

//     [Fact]
//     public async Task Refresh_Should_Update_ExpirationTime()
//     {
//         // Arrange
//         var key = "refresh-test";
//         var value = "test-value";
//         var options = new DistributedCacheEntryOptions
//         {
//             AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)
//         };

//         // Act
//         await _cache.SetAsync(key, Encoding.UTF8.GetBytes(value), options);
//         await _cache.RefreshAsync(key);
//         await Task.Delay(1100); // Wait for original expiration
//         var result = await _cache.GetAsync(key);

//         // Assert
//         Assert.NotNull(result);
//         Assert.Equal(value, Encoding.UTF8.GetString(result));
//     }

//     [Fact]
//     public async Task Remove_Should_Delete_Cache_Entry()
//     {
//         // Arrange
//         var key = "remove-test";
//         var value = "test-value";

//         // Act
//         await _cache.SetAsync(key, Encoding.UTF8.GetBytes(value), new DistributedCacheEntryOptions());
//         await _cache.RemoveAsync(key);
//         var result = await _cache.GetAsync(key);

//         // Assert
//         Assert.Null(result);
//     }

//     [Fact]
//     public async Task Get_NonExistent_Key_Should_Return_Null()
//     {
//         // Act
//         var result = await _cache.GetAsync("non-existent-key");

//         // Assert
//         Assert.Null(result);
//     }

//     [Fact]
//     public async Task Set_With_Sliding_Expiration_Should_Work()
//     {
//         // Arrange
//         var key = "sliding-test";
//         var value = "test-value";
//         var options = new DistributedCacheEntryOptions
//         {
//             SlidingExpiration = TimeSpan.FromSeconds(2)
//         };

//         // Act
//         await _cache.SetAsync(key, Encoding.UTF8.GetBytes(value), options);
//         await Task.Delay(1000); // Wait 1 second
//         var result1 = await _cache.GetAsync(key); // This should reset the sliding window
//         await Task.Delay(1000); // Wait another second
//         var result2 = await _cache.GetAsync(key); // Should still exist

//         // Assert
//         Assert.NotNull(result1);
//         Assert.NotNull(result2);
//         Assert.Equal(value, Encoding.UTF8.GetString(result2));
//     }
// }