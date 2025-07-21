using System;

namespace Community.Microsoft.Extensions.Caching.PostgreSql;

public record ItemIdUtcNow
{
    public string Id { get; internal set; }

    public DateTimeOffset UtcNow { get; internal set; }
};

public record ItemFull
{
    public string Id { get; internal set; }
    public DateTimeOffset? ExpiresAtTime { get; internal set; }
    public byte[] Value { get; internal set; }
    public double? SlidingExpirationInSeconds { get; internal set; }
    public DateTimeOffset? AbsoluteExpiration { get; internal set; }
}

public record CurrentUtcNow
{
    public DateTimeOffset UtcNow{ get; internal set; }
}

public record ItemIdOnly
{
    public string Id { get; internal set; }
 
}