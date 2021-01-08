// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal static class Columns
    {
        internal static class Names
        {
            internal const string CacheItemId = "DistCacheId";
            internal const string CacheItemValue = "DistCacheValue";
            internal const string ExpiresAtTime = "DistCacheExpiresAtTime";
            internal const string SlidingExpirationInSeconds = "DistCacheSlidingExpirationInSeconds";
            internal const string AbsoluteExpiration = "DistCacheAbsoluteExpiration";
        }

        internal static class Indexes
        {
            // The value of the following index positions is dependent on how the SQL queries
            // are selecting the columns.
            internal const int CacheItemIdIndex = 0;
            internal const int CacheItemValueIndex = 1;
            internal const int ExpiresAtTimeIndex = 2;
            internal const int SlidingExpirationInSecondsIndex = 3;
            internal const int AbsoluteExpirationIndex = 4;
        }
    }
}
