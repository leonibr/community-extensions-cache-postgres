// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    public interface IDatabaseOperations
    {
        byte[] GetCacheItem(string key);

        Task<byte[]> GetCacheItemAsync(string key, CancellationToken cancellationToken);

        void RefreshCacheItem(string key);

        Task RefreshCacheItemAsync(string key, CancellationToken cancellationToken);

        void DeleteCacheItem(string key);

        Task DeleteCacheItemAsync(string key, CancellationToken cancellationToken);

        void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options);

        Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken cancellationToken);

        Task DeleteExpiredCacheItemsAsync(CancellationToken cancellationToken);
    }
}