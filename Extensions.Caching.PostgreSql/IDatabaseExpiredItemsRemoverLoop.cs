namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal interface IDatabaseExpiredItemsRemoverLoop
    {
        void Start();
    }
}