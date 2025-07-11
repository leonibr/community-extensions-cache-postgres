# Options Details & Usage Guidance

## `ConnectionString`, `SchemaName`, `TableName`

- **What they do:**  
  Standard DB connection and naming options.
- **When to use:**
  - Always set `ConnectionString`.
  - Customize `SchemaName`/`TableName` if you want to use non-default names or schemas.

## 1. `DisableRemoveExpired`

- **What it does:**  
  If `true`, this instance will not automatically remove expired cache items in the background.
- **When to use:**
  - You have multiple app instances and want only one to perform cleanup (set `true` on all but one).
  - You want to handle expired item cleanup yourself.
- **Pros:**
  - Reduces DB load if you have many instances.
- **Cons:**
  - If all instances have this set to `true`, expired items will accumulate unless you remove them manually.
- **Recommendation:**
  - For single-instance deployments, leave as `false`.
  - For multi-instance, set `true` on all but one instance.

## 2. `UpdateOnGetCacheItem`

- **What it does:**  
  If `true`, reading a cache item with sliding expiration will update its expiration in the database.
- **When to use:**
  - Leave as `true` for most scenarios.
  - Set to `false` if you explicitly call `IDistributedCache.Refresh` (e.g., with ASP.NET Core Session).
- **Pros:**
  - Ensures sliding expiration works automatically.
- **Cons:**
  - Slightly more DB writes on cache reads.
- **Recommendation:**
  - Set to `false` only if you manage sliding expiration yourself.

## 3. `ReadOnlyMode`

- **What it does:**  
  If `true`, disables all write operations (including sliding expiration updates).
- **When to use:**
  - Your database user has only read permissions.
  - You want to ensure the cache is never modified by this app.
- **Pros:**
  - Prevents accidental writes.
  - Can improve performance (no write queries).
- **Cons:**
  - Sliding expiration is disabled; only absolute expiration works.
  - Cache cannot be updated or cleared by this instance.
- **Recommendation:**
  - Use for read-only replicas or when you want strict read-only cache access.

## 4. `CreateInfrastructure`

- **What it does:**  
  If `true`, creates the schema and table for the cache if they do not exist.
- **When to use:**
  - You want the app to auto-provision the cache table/schema.
- **Pros:**
  - Simplifies setup.
- **Cons:**
  - May not be desirable in environments with strict DB change controls.
- **Recommendation:**
  - Set to `false` if you want to manage DB schema manually.

## 5. `ExpiredItemsDeletionInterval`

- **What it does:**  
  Sets how often the background process checks for and deletes expired items.
- **When to use:**
  - Adjust for your cache churn and DB performance needs.
- **Pros:**
  - Lower intervals mean expired items are removed sooner.
- **Cons:**
  - Too frequent can increase DB load; too infrequent can leave expired data longer.
- **Recommendation:**
  - Default (30 min) is suitable for most; minimum is 5 min.

---

## Example: Custom Configuration

```csharp
services.AddDistributedPostgreSqlCache(options =>
{
    options.ConnectionString = configuration["CacheConnectionString"];
    options.SchemaName = "my_schema";
    options.TableName = "my_cache_table";
    options.DisableRemoveExpired = true; // Only if another instance is cleaning up
    options.CreateInfrastructure = false; // If you manage schema manually
    options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(15);
    options.UpdateOnGetCacheItem = false; // If you call Refresh explicitly
    options.ReadOnlyMode = false; // Set true for read-only DB users
});
```
