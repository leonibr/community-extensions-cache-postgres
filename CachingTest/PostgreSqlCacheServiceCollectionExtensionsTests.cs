using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CachingTest;

public class PostgreSqlCacheServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services;

    public PostgreSqlCacheServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
        // Add logging services to avoid dependency injection issues
        _services.AddLogging();
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PostGreSqlCachingServicesExtensions.AddDistributedPostgreSqlCache(null!));
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithValidServices_RegistersServices()
    {
        // Act
        var result = _services.AddDistributedPostgreSqlCache(options =>
        {
            options.ConnectionString = "test-connection-string";
            options.SchemaName = "cache";
            options.TableName = "distributed_cache";
            options.CreateInfrastructure = false;
        });

        // Assert
        Assert.Same(_services, result);

        // Verify services are registered
        var serviceProvider = _services.BuildServiceProvider();

        // Should be able to resolve IDistributedCache
        var distributedCache = serviceProvider.GetService<IDistributedCache>();
        Assert.NotNull(distributedCache);
        Assert.IsType<PostgreSqlCache>(distributedCache);

        // Should be able to resolve IDatabaseOperations
        var dbOperations = serviceProvider.GetService<IDatabaseOperations>();
        Assert.NotNull(dbOperations);
        Assert.IsType<DatabaseOperations>(dbOperations);

        // Should be able to resolve IDatabaseExpiredItemsRemoverLoop
        var removerLoop = serviceProvider.GetService<IDatabaseExpiredItemsRemoverLoop>();
        Assert.NotNull(removerLoop);
        Assert.IsType<DatabaseExpiredItemsRemoverLoop>(removerLoop);
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithSetupAction_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        Action<PostgreSqlCacheOptions> setupAction = options => { };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PostGreSqlCachingServicesExtensions.AddDistributedPostgreSqlCache(null!, setupAction));
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithSetupAction_WithNullSetupAction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PostGreSqlCachingServicesExtensions.AddDistributedPostgreSqlCache(_services, (Action<PostgreSqlCacheOptions>)null!));
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithSetupAction_RegistersServicesWithConfiguration()
    {
        // Arrange
        var expectedConnectionString = "test-connection-string";
        var expectedSchemaName = "test-schema";
        var expectedTableName = "test-table";

        // Act
        var result = _services.AddDistributedPostgreSqlCache(options =>
        {
            options.ConnectionString = expectedConnectionString;
            options.SchemaName = expectedSchemaName;
            options.TableName = expectedTableName;
        });

        // Assert
        Assert.Same(_services, result);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<PostgreSqlCacheOptions>>();

        Assert.Equal(expectedConnectionString, options.Value.ConnectionString);
        Assert.Equal(expectedSchemaName, options.Value.SchemaName);
        Assert.Equal(expectedTableName, options.Value.TableName);
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithServiceProviderSetupAction_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        Action<IServiceProvider, PostgreSqlCacheOptions> setupAction = (sp, options) => { };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PostGreSqlCachingServicesExtensions.AddDistributedPostgreSqlCache(null!, setupAction));
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithServiceProviderSetupAction_WithNullSetupAction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PostGreSqlCachingServicesExtensions.AddDistributedPostgreSqlCache(_services, (Action<IServiceProvider, PostgreSqlCacheOptions>)null!));
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithServiceProviderSetupAction_RegistersServicesWithConfiguration()
    {
        // Arrange
        var expectedConnectionString = "test-connection-string";
        var expectedSchemaName = "test-schema";
        var expectedTableName = "test-table";

        // Act
        var result = _services.AddDistributedPostgreSqlCache((serviceProvider, options) =>
        {
            options.ConnectionString = expectedConnectionString;
            options.SchemaName = expectedSchemaName;
            options.TableName = expectedTableName;
        });

        // Assert
        Assert.Same(_services, result);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<PostgreSqlCacheOptions>>();

        Assert.Equal(expectedConnectionString, options.Value.ConnectionString);
        Assert.Equal(expectedSchemaName, options.Value.SchemaName);
        Assert.Equal(expectedTableName, options.Value.TableName);
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithServiceProviderSetupAction_CanAccessServiceProvider()
    {
        // Arrange
        var testService = new TestService();
        _services.AddSingleton(testService);
        var setupActionCalled = false;

        // Act
        var result = _services.AddDistributedPostgreSqlCache((serviceProvider, options) =>
        {
            var testServiceFromProvider = serviceProvider.GetService<TestService>();
            Assert.Same(testService, testServiceFromProvider);
            options.ConnectionString = "test-connection-string";
            options.SchemaName = "cache";
            options.TableName = "distributed_cache";
            options.CreateInfrastructure = false;
            setupActionCalled = true;
        });

        // Assert
        Assert.Same(_services, result);
        var serviceProvider = _services.BuildServiceProvider();

        // Trigger the setup action by resolving a service that depends on the options
        var cache = serviceProvider.GetService<IDistributedCache>();
        Assert.NotNull(cache);
        Assert.True(setupActionCalled);
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_RegistersServicesAsSingletons()
    {
        // Act
        _services.AddDistributedPostgreSqlCache(options =>
        {
            options.ConnectionString = "test-connection-string";
            options.SchemaName = "cache";
            options.TableName = "distributed_cache";
            options.CreateInfrastructure = false;
        });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // Get instances multiple times
        var cache1 = serviceProvider.GetService<IDistributedCache>();
        var cache2 = serviceProvider.GetService<IDistributedCache>();

        var dbOps1 = serviceProvider.GetService<IDatabaseOperations>();
        var dbOps2 = serviceProvider.GetService<IDatabaseOperations>();

        var removerLoop1 = serviceProvider.GetService<IDatabaseExpiredItemsRemoverLoop>();
        var removerLoop2 = serviceProvider.GetService<IDatabaseExpiredItemsRemoverLoop>();

        // Should be the same instances (singletons)
        Assert.Same(cache1, cache2);
        Assert.Same(dbOps1, dbOps2);
        Assert.Same(removerLoop1, removerLoop2);
    }

    [Fact]
    public void AddDistributedPostgreSqlCache_WithCustomOptions_AppliesConfiguration()
    {
        // Arrange
        var expectedDefaultSlidingExpiration = TimeSpan.FromMinutes(30);
        var expectedExpiredItemsDeletionInterval = TimeSpan.FromMinutes(15);
        var expectedCreateInfrastructure = false;
        var expectedDisableRemoveExpired = true;
        var expectedUpdateOnGetCacheItem = false;
        var expectedReadOnlyMode = true;

        // Act
        _services.AddDistributedPostgreSqlCache(options =>
        {
            options.DefaultSlidingExpiration = expectedDefaultSlidingExpiration;
            options.ExpiredItemsDeletionInterval = expectedExpiredItemsDeletionInterval;
            options.CreateInfrastructure = expectedCreateInfrastructure;
            options.DisableRemoveExpired = expectedDisableRemoveExpired;
            options.UpdateOnGetCacheItem = expectedUpdateOnGetCacheItem;
            options.ReadOnlyMode = expectedReadOnlyMode;
        });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<PostgreSqlCacheOptions>>();

        Assert.Equal(expectedDefaultSlidingExpiration, options.Value.DefaultSlidingExpiration);
        Assert.Equal(expectedExpiredItemsDeletionInterval, options.Value.ExpiredItemsDeletionInterval);
        Assert.Equal(expectedCreateInfrastructure, options.Value.CreateInfrastructure);
        Assert.Equal(expectedDisableRemoveExpired, options.Value.DisableRemoveExpired);
        Assert.Equal(expectedUpdateOnGetCacheItem, options.Value.UpdateOnGetCacheItem);
        Assert.Equal(expectedReadOnlyMode, options.Value.ReadOnlyMode);
    }

    private class TestService
    {
        public string Name { get; set; } = "Test";
    }
}