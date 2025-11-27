using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Npgsql;

namespace Community.Microsoft.Extensions.Caching.PostgreSql;

/// <summary>
/// Extension methods for NpgsqlCommand to add type-safe parameter groups.
/// </summary>
internal static class NpgsqlCommandExtensions
{
    /// <summary>
    /// Converts a property reference to a SQL parameter name by prepending '@'.
    /// </summary>
    /// <typeparam name="T">The type of the property value</typeparam>
    /// <param name="value">The property value</param>
    /// <param name="expression">Automatically captured expression by the compiler</param>
    /// <returns>The parameter name with '@' prefix (e.g., "@Id")</returns>
    private static string AsName<T>(T value,
        [CallerArgumentExpression(nameof(value))] string expression = "") =>
            expression.Split('.').Select(s => $"@{s}").LastOrDefault() ?? "@NoName";

    /// <summary>
    /// Adds parameters for operations requiring an Id and UtcNow timestamp.
    /// </summary>
    public static void AddParameters(this NpgsqlCommand command, ItemIdUtcNow item)
    {
        command.Parameters.AddWithValue(AsName(item.Id), item.Id);
        command.Parameters.AddWithValue(AsName(item.UtcNow), item.UtcNow);
    }

    /// <summary>
    /// Adds all parameters for a full cache item.
    /// </summary>
    public static void AddParameters(this NpgsqlCommand command, ItemFull item)
    {
        command.Parameters.AddWithValue(AsName(item.Id), item.Id);
        command.Parameters.AddWithValue(AsName(item.Value), item.Value);
        command.Parameters.AddWithValue(AsName(item.ExpiresAtTime), item.ExpiresAtTime);
        command.Parameters.AddWithValue(AsName(item.SlidingExpirationInSeconds),
            (object)item.SlidingExpirationInSeconds ?? DBNull.Value);
        command.Parameters.AddWithValue(AsName(item.AbsoluteExpiration),
            (object)item.AbsoluteExpiration ?? DBNull.Value);
    }

    /// <summary>
    /// Adds a parameter for operations requiring only the current UTC timestamp.
    /// </summary>
    public static void AddParameters(this NpgsqlCommand command, CurrentUtcNow item)
    {
        command.Parameters.AddWithValue(AsName(item.UtcNow), item.UtcNow);
    }

    /// <summary>
    /// Adds a parameter for operations requiring only an Id.
    /// </summary>
    public static void AddParameters(this NpgsqlCommand command, ItemIdOnly item)
    {
        command.Parameters.AddWithValue(AsName(item.Id), item.Id);
    }
}

