// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Npgsql;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using System.Reflection;
using System.IO;
using System.Text;
using System.Data;
using System.Threading;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal sealed class DatabaseOperations : IDatabaseOperations
    {
        private readonly ILogger<DatabaseOperations> logger;

        public DatabaseOperations(IOptions<PostgreSqlCacheOptions> options, ILogger<DatabaseOperations> logger)
        {
            var cacheOptions = options.Value;

            if (string.IsNullOrEmpty(cacheOptions.ConnectionString))
            {
                throw new ArgumentException(
                    $"{nameof(PostgreSqlCacheOptions.ConnectionString)} cannot be empty or null.");
            }
            if (string.IsNullOrEmpty(cacheOptions.SchemaName))
            {
                throw new ArgumentException(
                    $"{nameof(PostgreSqlCacheOptions.SchemaName)} cannot be empty or null.");
            }
            if (string.IsNullOrEmpty(cacheOptions.TableName))
            {
                throw new ArgumentException(
                    $"{nameof(PostgreSqlCacheOptions.TableName)} cannot be empty or null.");
            }

            ConnectionString = cacheOptions.ConnectionString;
            SchemaName = cacheOptions.SchemaName;
            TableName = cacheOptions.TableName;
            SystemClock = cacheOptions.SystemClock;
            this.logger = logger;
            if (cacheOptions.CreateInfrastructure)
            {
                CreateTableIfNotExist();
            }


        }

        private string ConnectionString { get; }

        private string SchemaName { get; }

        private string TableName { get; }

        private ISystemClock SystemClock { get; }

        private string ReadScript(string scriptName)
        {
            var assembly = Assembly.Load("Community.Microsoft.Extensions.Caching.PostgreSql");
            var resourceStream = assembly.GetManifestResourceStream($"Community.Microsoft.Extensions.Caching.PostgreSql.PostgreSqlScripts.{scriptName}");
            using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
        /// <summary>
        /// Replaces the schema and table names for the ones in config file
        /// </summary>
        /// <returns>The text</returns>
        private string FormatName(string text)
        {
            return text
                    .Replace("[schemaName]", SchemaName)
                    .Replace("[tableName]", TableName);
        }

        private void CreateTableIfNotExist()
        {
            var sql = (
             table: ReadScript("Create_Table_DistCache.sql"),
             funcDateDiff: ReadScript("Create_Function_DateDiff.sql"),
             funcGetCacheItem: ReadScript("Create_Function_GetCacheItemFormat.sql"),
             procDeleteCacheItem: ReadScript("Create_Procedure_DeleteCacheItemFormat.sql"),
             procDeleteExpired: ReadScript("Create_Procedure_DeleteExpiredCacheItemsFormat.sql"),
             procSetCache: ReadScript("Create_Procedure_SetCache.sql"),
             procUpdateCache: ReadScript("Create_Procedure_UpdateCacheItemFormat.sql")
             );

            var sb = new StringBuilder()
                .Append(FormatName(sql.table))
                .Append(FormatName(sql.funcDateDiff))
                .Append(FormatName(sql.funcGetCacheItem))
                .Append(FormatName(sql.procSetCache))
                .Append(FormatName(sql.procUpdateCache))
                .Append(FormatName(sql.procDeleteCacheItem))
                .Append(FormatName(sql.procDeleteExpired));

            using (var cn = new NpgsqlConnection(ConnectionString))
            {
                cn.Open();
                cn.Notice += LogNotice;
                using (var transaction = cn.BeginTransaction())
                {
                    try
                    {
                        var cmd = new NpgsqlCommand(
                            cmdText: sb.ToString(),
                            connection: cn,
                            transaction: transaction);
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, "CreateTableIfNotExist failed.");
                        transaction.Rollback();

                    }
                }
                cn.Notice -= LogNotice;
                cn.Close();
                logger.LogDebug("CreateTableIfNotExist executed");
            }

        }

        private void LogNotice(object sender, NpgsqlNoticeEventArgs e) => logger.LogDebug($"PGSQL: {e.Notice.MessageText}");

        public void DeleteCacheItem(string key)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                var command = CreateProcedureCallCommand(Functions.Names.DeleteCacheItemFormat, connection);
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddCacheItemId(key);

                connection.Notice += LogNotice;
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                connection.Notice += LogNotice;
                logger.LogDebug($"Cache key deleted: {key}");
            }
        }

        public async Task DeleteCacheItemAsync(string key, CancellationToken cancellationToken)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                var command = CreateProcedureCallCommand(Functions.Names.DeleteCacheItemFormat, connection);
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddCacheItemId(key);

                connection.Notice += LogNotice;
                await connection.OpenAsync(cancellationToken);
                await command.ExecuteNonQueryAsync(cancellationToken);
                await connection.CloseAsync();
                connection.Notice += LogNotice;
                logger.LogDebug($"Cache key deleted: {key}");
            }
        }

        public byte[] GetCacheItem(string key)
        {
            return GetCacheItem(key, includeValue: true);
        }

        public async Task<byte[]> GetCacheItemAsync(string key, CancellationToken cancellationToken)
        {
            return await GetCacheItemAsync(key, includeValue: true, cancellationToken);
        }

        public void RefreshCacheItem(string key)
        {
            GetCacheItem(key, includeValue: false);
        }

        public async Task RefreshCacheItemAsync(string key, CancellationToken cancellationToken)
        {
            await GetCacheItemAsync(key, includeValue: false, cancellationToken);
        }

        public async Task DeleteExpiredCacheItemsAsync(CancellationToken cancellationToken)
        {
            var utcNow = SystemClock.UtcNow;

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Notice += LogNotice;
                var command = CreateProcedureCallCommand(Functions.Names.DeleteExpiredCacheItemsFormat, connection);
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                await connection.OpenAsync(cancellationToken);

                await command.ExecuteNonQueryAsync(cancellationToken);

                await connection.CloseAsync();
                connection.Notice -= LogNotice;
            }
        }

        public void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                var command = CreateProcedureCallCommand(Functions.Names.SetCache, connection);
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddCacheItemId(key)
                    .AddCacheItemValue(value)
                    .AddSlidingExpirationInSeconds(options.SlidingExpiration)
                    .AddAbsoluteExpiration(absoluteExpiration)
                    .AddParamWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                connection.Open();

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (PostgresException ex)
                {
                    if (IsDuplicateKeyException(ex))
                    {
                        // There is a possibility that multiple requests can try to add the same item to the cache, in
                        // which case we receive a 'duplicate key' exception on the primary key column.
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
        {
            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                var command = CreateProcedureCallCommand(Functions.Names.SetCache, connection);
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddCacheItemId(key)
                    .AddCacheItemValue(value)
                    .AddSlidingExpirationInSeconds(options.SlidingExpiration)
                    .AddAbsoluteExpiration(absoluteExpiration)
                    .AddParamWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                await connection.OpenAsync(cancellationToken);

                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (PostgresException ex)
                {
                    if (IsDuplicateKeyException(ex))
                    {
                        // There is a possibility that multiple requests can try to add the same item to the cache, in
                        // which case we receive a 'duplicate key' exception on the primary key column.
                        logger.LogError(exception: ex, $"Duplicate key: {key}");
                    }
                    else
                    {
                        logger.LogError(exception: ex, $"SetCacheItemAsync with key: {key} could not be stored");
                        throw;
                    }
                }
            }
        }

        private byte[] GetCacheItem(string key, bool includeValue)
        {
            var utcNow = SystemClock.UtcNow;

            byte[] value = null;
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Notice += LogNotice;
                var command = CreateProcedureCallCommand(Functions.Names.UpdateCacheItemFormat, connection);
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddCacheItemId(key)
                    .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                connection.Open();
                command.ExecuteNonQuery();

                if (includeValue)
                {
                    command = CreateGetCacheItemFunctionCallCommand(connection);
                    command.Parameters
                        .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                        .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                        .AddCacheItemId(key)
                        .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                    var reader = command.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow | CommandBehavior.SingleResult);

                    if (reader.Read())
                    {
                        var id = reader.GetFieldValue<string>(Columns.Indexes.CacheItemIdIndex);

                        value = reader.GetFieldValue<byte[]>(Columns.Indexes.CacheItemValueIndex);

                        _ = reader.GetFieldValue<DateTimeOffset>(Columns.Indexes.ExpiresAtTimeIndex);

                        if (!reader.IsDBNull(Columns.Indexes.SlidingExpirationInSecondsIndex))
                        {
                            _ = reader.GetFieldValue<long>(Columns.Indexes.SlidingExpirationInSecondsIndex);
                        }

                        if (!reader.IsDBNull(Columns.Indexes.AbsoluteExpirationIndex))
                        {
                            _ = reader.GetFieldValue<DateTimeOffset>(Columns.Indexes.AbsoluteExpirationIndex);
                        }

                    }

                }
                connection.Notice -= LogNotice;
                connection.Close();
            }
            return value;
        }

        private async Task<byte[]> GetCacheItemAsync(string key, bool includeValue, CancellationToken cancellationToken)
        {
            var utcNow = SystemClock.UtcNow;

            byte[] value = null;
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Notice += LogNotice;
                var command = CreateProcedureCallCommand(Functions.Names.UpdateCacheItemFormat, connection);
                command.Parameters
                   .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                   .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                   .AddCacheItemId(key)
                   .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                await connection.OpenAsync(cancellationToken);
                await command.ExecuteNonQueryAsync(cancellationToken);

                if (includeValue)
                {
                    command = CreateGetCacheItemFunctionCallCommand(connection);
                    command.Parameters
                        .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                        .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                        .AddCacheItemId(key)
                        .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                    var reader = await command.ExecuteReaderAsync(
                        CommandBehavior.SequentialAccess | CommandBehavior.SingleRow | CommandBehavior.SingleResult,
                        cancellationToken);

                    if (await reader.ReadAsync(cancellationToken))
                    {
                        _ = await reader.GetFieldValueAsync<string>(Columns.Indexes.CacheItemIdIndex, cancellationToken);

                        value = await reader.GetFieldValueAsync<byte[]>(Columns.Indexes.CacheItemValueIndex, cancellationToken);

                        _ = await reader.GetFieldValueAsync<DateTimeOffset>(Columns.Indexes.ExpiresAtTimeIndex, cancellationToken);

                        if (!await reader.IsDBNullAsync(Columns.Indexes.SlidingExpirationInSecondsIndex, cancellationToken))
                        {
                            _ = await reader.GetFieldValueAsync<long>(Columns.Indexes.SlidingExpirationInSecondsIndex, cancellationToken);
                        }

                        if (!await reader.IsDBNullAsync(Columns.Indexes.AbsoluteExpirationIndex, cancellationToken))
                        {
                            _ = await reader.GetFieldValueAsync<DateTimeOffset>(Columns.Indexes.AbsoluteExpirationIndex, cancellationToken);
                        }

                    }
                }
                await connection.CloseAsync();
                connection.Notice -= LogNotice;

            }

            return value;
        }

        private bool IsDuplicateKeyException(PostgresException ex)
        {
            return ex.SqlState == "23505";
        }

        private DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset utcNow, DistributedCacheEntryOptions options)
        {
            // calculate absolute expiration
            DateTimeOffset? absoluteExpiration = null;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = utcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value.ToUniversalTime() <= utcNow.ToUniversalTime())
                {
                    throw new InvalidOperationException("The absolute expiration value must be in the future.");
                }

                absoluteExpiration = options.AbsoluteExpiration.Value;
            }
            return absoluteExpiration;
        }

        private void ValidateOptions(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
        {
            if (!slidingExpiration.HasValue && !absoluteExpiration.HasValue)
            {
                throw new InvalidOperationException("Either absolute or sliding expiration needs " +
                    "to be provided.");
            }
        }

        private NpgsqlCommand CreateProcedureCallCommand(
            string functionName,
            NpgsqlConnection connection)
        {
            return new NpgsqlCommand($"{SchemaName}.{functionName}", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
        }

        private NpgsqlCommand CreateGetCacheItemFunctionCallCommand(NpgsqlConnection connection)
        {
            return new NpgsqlCommand(
                $"SELECT * FROM {SchemaName}.{Functions.Names.GetCacheItemFormat}(@SchemaName, @TableName, @{Columns.Names.CacheItemId}, @UtcNow)",
                connection);
        }
    }
}