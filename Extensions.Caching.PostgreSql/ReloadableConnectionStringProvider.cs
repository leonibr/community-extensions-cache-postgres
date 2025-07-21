using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    /// <summary>
    /// Provides reloadable connection strings from configuration sources like Azure Key Vault.
    /// This enables automatic connection string updates when secrets are rotated in Azure Key Vault.
    /// </summary>
    internal class ReloadableConnectionStringProvider : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly string _connectionStringKey;
        private readonly TimeSpan _reloadInterval;
        private readonly Timer _reloadTimer;
        private string _currentConnectionString;
        private DateTime _lastReloadTime;
        private readonly object _lockObject = new object();

        public ReloadableConnectionStringProvider(
            IConfiguration configuration,
            ILogger logger,
            string connectionStringKey,
            TimeSpan reloadInterval)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionStringKey = connectionStringKey ?? throw new ArgumentNullException(nameof(connectionStringKey));
            _reloadInterval = reloadInterval;

            // Initial load
            _currentConnectionString = LoadConnectionString();
            _lastReloadTime = DateTime.UtcNow;

            // Start timer for periodic reloading
            _reloadTimer = new Timer(OnReloadTimer, null, _reloadInterval, _reloadInterval);
        }

        /// <summary>
        /// Gets the current connection string, reloading it if necessary.
        /// </summary>
        public string GetConnectionString()
        {
            lock (_lockObject)
            {
                // Check if it's time to reload
                if (DateTime.UtcNow - _lastReloadTime >= _reloadInterval)
                {
                    var newConnectionString = LoadConnectionString();
                    if (newConnectionString != _currentConnectionString)
                    {
                        _logger.LogInformation("Connection string updated from configuration key: {Key}", _connectionStringKey);
                        _currentConnectionString = newConnectionString;
                    }
                    _lastReloadTime = DateTime.UtcNow;
                }

                return _currentConnectionString;
            }
        }

        /// <summary>
        /// Forces a reload of the connection string from configuration.
        /// </summary>
        public async Task<string> ReloadConnectionStringAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    var newConnectionString = LoadConnectionString();
                    if (newConnectionString != _currentConnectionString)
                    {
                        _logger.LogInformation("Connection string manually reloaded from configuration key: {Key}", _connectionStringKey);
                        _currentConnectionString = newConnectionString;
                    }
                    _lastReloadTime = DateTime.UtcNow;
                    return _currentConnectionString;
                }
            });
        }

        private string LoadConnectionString()
        {
            try
            {
                var connectionString = _configuration[_connectionStringKey];
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("Connection string not found for key: {Key}", _connectionStringKey);
                    return _currentConnectionString ?? string.Empty;
                }

                return connectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading connection string from configuration key: {Key}", _connectionStringKey);
                return _currentConnectionString ?? string.Empty;
            }
        }

        private void OnReloadTimer(object state)
        {
            try
            {
                // This will trigger a reload check on the next GetConnectionString call
                _logger.LogDebug("Connection string reload timer triggered for key: {Key}", _connectionStringKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in connection string reload timer for key: {Key}", _connectionStringKey);
            }
        }

        public void Dispose()
        {
            _reloadTimer?.Dispose();
        }
    }
}