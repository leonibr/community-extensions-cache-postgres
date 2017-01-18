using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions.Caching.PostgreSql
{
    internal static class Functions
    {
        public static class Names
        {
            public const string SetCache = "SetCache";
            public const string DeleteCacheItemFormat = "DeleteCacheItemFormat";
            public const string UpdateCacheItemFormat = "UpdateCacheItemFormat";
            public const string DeleteExpiredCacheItemsFormat = "DeleteExpiredCacheItemsFormat";
            public const string GetCacheItemFormat = "GetCacheItemFormat";


        }
    }
}
