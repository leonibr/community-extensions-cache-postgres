using System;
using Npgsql;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal static class PostgreSqlParameterCollectionExtensions
    {
        // For all values where the length is less than the below value, try setting the size of the
        // parameter for better performance.
        private const int DefaultValueColumnWidth = 8000;

        private const int CacheItemIdColumnWidth = 449;

        public static NpgsqlParameterCollection AddCacheItemId(this NpgsqlParameterCollection parameters, string value)
        {
            return parameters.AddParamWithValue(Columns.Names.CacheItemId, NpgsqlTypes.NpgsqlDbType.Text, CacheItemIdColumnWidth, value);
        }

        public static NpgsqlParameterCollection AddCacheItemValue(this NpgsqlParameterCollection parameters, byte[] value)
        {
            if (value != null && value.Length < DefaultValueColumnWidth)
            {
                return parameters.AddParamWithValue(
                    Columns.Names.CacheItemValue,
                    NpgsqlTypes.NpgsqlDbType.Bytea,
                    DefaultValueColumnWidth,
                    value);
            }
            else
            {
                // do not mention the size
                return parameters.AddParamWithValue(Columns.Names.CacheItemValue, NpgsqlTypes.NpgsqlDbType.Bytea, value);
            }
        }

        public static NpgsqlParameterCollection AddSlidingExpirationInSeconds(
            this NpgsqlParameterCollection parameters,
            TimeSpan? value)
        {
            if (value.HasValue)
            {
                return parameters.AddParamWithValue(
                    Columns.Names.SlidingExpirationInSeconds, NpgsqlTypes.NpgsqlDbType.Double, value.Value.TotalSeconds);
            }
            else
            {
                return parameters.AddParamWithValue(Columns.Names.SlidingExpirationInSeconds, NpgsqlTypes.NpgsqlDbType.Double, DBNull.Value);
            }
        }

        public static NpgsqlParameterCollection AddAbsoluteExpiration(
            this NpgsqlParameterCollection parameters,
            DateTimeOffset? utcTime)
        {
            if (utcTime.HasValue)
            {
                return parameters.AddParamWithValue(
                    Columns.Names.AbsoluteExpiration, NpgsqlTypes.NpgsqlDbType.TimestampTz, utcTime.Value);
            }
            else
            {
                return parameters.AddParamWithValue(
                    Columns.Names.AbsoluteExpiration, NpgsqlTypes.NpgsqlDbType.TimestampTz, DBNull.Value);
            }
        }

        public static NpgsqlParameterCollection AddParamWithValue(
            this NpgsqlParameterCollection parameters,
            string parameterName,
            NpgsqlTypes.NpgsqlDbType dbType,
            object value)
        {
            var parameter = new NpgsqlParameter(parameterName, dbType);
            parameter.Value = value;
            parameters.Add(parameter);
            if (value != DBNull.Value)
            {
                parameter.ResetDbType();
            }
            
            return parameters;
        }

        public static NpgsqlParameterCollection AddParamWithValue(
            this NpgsqlParameterCollection parameters,
            string parameterName,
            NpgsqlTypes.NpgsqlDbType dbType,
            int size,
            object value)
        {
            var parameter = new NpgsqlParameter(parameterName, dbType, size);
            parameter.Value = value;
            parameters.Add(parameter);
            if (value != DBNull.Value)
            {
                parameter.ResetDbType();
            }
            return parameters;
        }
    }
}
