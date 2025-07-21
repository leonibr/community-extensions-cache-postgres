using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Moq;

namespace Community.Microsoft.Extensions.Caching.PostgreSql.Tests
{
    public class ReloadableConnectionStringTests
    {
        [Fact]
        public void ReloadableConnectionStringProvider_InitializesCorrectly()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var logger = new Mock<ILogger>();
            var connectionStringKey = "TestConnectionString";
            var reloadInterval = TimeSpan.FromMinutes(5);

            configuration.Setup(c => c[connectionStringKey]).Returns("Host=localhost;Database=test");

            // Act
            using var provider = new ReloadableConnectionStringProvider(
                configuration.Object,
                logger.Object,
                connectionStringKey,
                reloadInterval);

            // Assert
            var connectionString = provider.GetConnectionString();
            Assert.Equal("Host=localhost;Database=test", connectionString);
        }

        [Fact]
        public void ReloadableConnectionStringProvider_HandlesNullConfiguration()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var logger = new Mock<ILogger>();
            var connectionStringKey = "TestConnectionString";
            var reloadInterval = TimeSpan.FromMinutes(5);

            configuration.Setup(c => c[connectionStringKey]).Returns((string)null);

            // Act
            using var provider = new ReloadableConnectionStringProvider(
                configuration.Object,
                logger.Object,
                connectionStringKey,
                reloadInterval);

            // Assert
            var connectionString = provider.GetConnectionString();
            Assert.Equal(string.Empty, connectionString);
        }

        [Fact]
        public void ReloadableConnectionStringProvider_HandlesConfigurationException()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var logger = new Mock<ILogger>();
            var connectionStringKey = "TestConnectionString";
            var reloadInterval = TimeSpan.FromMinutes(5);

            configuration.Setup(c => c[connectionStringKey]).Throws(new Exception("Configuration error"));

            // Act
            using var provider = new ReloadableConnectionStringProvider(
                configuration.Object,
                logger.Object,
                connectionStringKey,
                reloadInterval);

            // Assert
            var connectionString = provider.GetConnectionString();
            Assert.Equal(string.Empty, connectionString);
        }

        [Fact]
        public async Task ReloadableConnectionStringProvider_ManualReloadWorks()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var logger = new Mock<ILogger>();
            var connectionStringKey = "TestConnectionString";
            var reloadInterval = TimeSpan.FromMinutes(5);

            configuration.Setup(c => c[connectionStringKey]).Returns("Host=localhost;Database=test");

            using var provider = new ReloadableConnectionStringProvider(
                configuration.Object,
                logger.Object,
                connectionStringKey,
                reloadInterval);

            // Act
            var connectionString = await provider.ReloadConnectionStringAsync();

            // Assert
            Assert.Equal("Host=localhost;Database=test", connectionString);
        }

        [Fact]
        public void DatabaseOperations_WithReloadableConnectionString_InitializesCorrectly()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var logger = new Mock<ILogger<DatabaseOperations>>();
            var options = new PostgreSqlCacheOptions
            {
                ConnectionStringKey = "TestConnectionString",
                Configuration = configuration.Object,
                Logger = logger.Object,
                EnableConnectionStringReloading = true,
                ConnectionStringReloadInterval = TimeSpan.FromMinutes(5),
                SchemaName = "cache",
                TableName = "cache_items",
                CreateInfrastructure = false // Don't try to create schema/table for this test
            };

            configuration.Setup(c => c["TestConnectionString"]).Returns("Host=localhost;Database=test");

            var optionsWrapper = new Mock<IOptions<PostgreSqlCacheOptions>>();
            optionsWrapper.Setup(o => o.Value).Returns(options);

            // Act & Assert
            // This should not throw an exception
            var databaseOperations = new DatabaseOperations(optionsWrapper.Object, logger.Object);
            databaseOperations.Dispose();
        }

        [Fact]
        public void DatabaseOperations_WithReloadableConnectionString_ValidatesRequiredProperties()
        {
            // Arrange
            var logger = new Mock<ILogger<DatabaseOperations>>();
            var options = new PostgreSqlCacheOptions
            {
                // Missing required properties
            };

            var optionsWrapper = new Mock<IOptions<PostgreSqlCacheOptions>>();
            optionsWrapper.Setup(o => o.Value).Returns(options);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DatabaseOperations(optionsWrapper.Object, logger.Object));
        }

        [Fact]
        public void DatabaseOperations_WithReloadableConnectionString_ValidatesSchemaName()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var logger = new Mock<ILogger<DatabaseOperations>>();
            var options = new PostgreSqlCacheOptions
            {
                ConnectionStringKey = "TestConnectionString",
                Configuration = configuration.Object,
                Logger = logger.Object,
                EnableConnectionStringReloading = true,
                SchemaName = "", // Empty schema name
                TableName = "cache_items"
            };

            configuration.Setup(c => c["TestConnectionString"]).Returns("Host=localhost;Database=test");

            var optionsWrapper = new Mock<IOptions<PostgreSqlCacheOptions>>();
            optionsWrapper.Setup(o => o.Value).Returns(options);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DatabaseOperations(optionsWrapper.Object, logger.Object));
        }

        [Fact]
        public void DatabaseOperations_WithReloadableConnectionString_ValidatesTableName()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var logger = new Mock<ILogger<DatabaseOperations>>();
            var options = new PostgreSqlCacheOptions
            {
                ConnectionStringKey = "TestConnectionString",
                Configuration = configuration.Object,
                Logger = logger.Object,
                EnableConnectionStringReloading = true,
                SchemaName = "cache",
                TableName = "" // Empty table name
            };

            configuration.Setup(c => c["TestConnectionString"]).Returns("Host=localhost;Database=test");

            var optionsWrapper = new Mock<IOptions<PostgreSqlCacheOptions>>();
            optionsWrapper.Setup(o => o.Value).Returns(options);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DatabaseOperations(optionsWrapper.Object, logger.Object));
        }
    }
}