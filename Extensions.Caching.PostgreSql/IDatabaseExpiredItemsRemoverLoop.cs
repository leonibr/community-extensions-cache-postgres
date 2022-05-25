using System;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal interface IDatabaseExpiredItemsRemoverLoop : IDisposable
    {
        void Start();
    }
}