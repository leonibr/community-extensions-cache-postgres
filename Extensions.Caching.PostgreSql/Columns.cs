// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Extensions.Caching.PostgreSql
{
    internal static class Columns
    {
        public static class Names
        {
            public const string CacheItemId = "DistCacheId";
            public const string CacheItemValue = "DistCacheValue";
            public const string ExpiresAtTime = "DistCacheExpiresAtTime";
            public const string SlidingExpirationInSeconds = "DistCacheSlidingExpirationInSeconds";
            public const string AbsoluteExpiration = "DistCacheAbsoluteExpiration";
        }

        public static class Indexes
        {
            // The value of the following index positions is dependent on how the SQL queries
            // are selecting the columns.
            public const int CacheItemIdIndex = 0;
            public const int ExpiresAtTimeIndex = 1;
            public const int SlidingExpirationInSecondsIndex = 2;
            public const int AbsoluteExpirationIndex = 3;
            public const int CacheItemValueIndex = 4;
        }
    }
}
