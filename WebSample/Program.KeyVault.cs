using Community.Microsoft.Extensions.Caching.PostgreSql;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
var keyVaultUrl = $"https://{builder.Configuration["AzureKeyVault:VaultName"]}.vault.azure.net/";
var credential = new ClientSecretCredential(
    builder.Configuration["AzureKeyVault:TenantId"],
    builder.Configuration["AzureKeyVault:ClientId"],
    builder.Configuration["AzureKeyVault:ClientSecret"]);

var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);

// Add configuration source for Azure Key Vault
builder.Configuration.AddAzureKeyVault(secretClient, new AzureKeyVaultConfigurationOptions());

// Add services to the container
builder.Services.AddControllers();

// Configure PostgreSQL cache with reloadable connection string
builder.Services.AddDistributedPostgreSqlCacheWithReloadableConnection(
    connectionStringKey: "PostgreSqlCache:ConnectionString",
    reloadInterval: TimeSpan.FromMinutes(5),
    setupAction: options =>
    {
        options.SchemaName = builder.Configuration["PgCache:SchemaName"];
        options.TableName = builder.Configuration["PgCache:TableName"];
        options.DisableRemoveExpired = bool.Parse(builder.Configuration["PgCache:DisableRemoveExpired"]);
        options.UpdateOnGetCacheItem = bool.Parse(builder.Configuration["PgCache:UpdateOnGetCacheItem"]);
        options.ReadOnlyMode = bool.Parse(builder.Configuration["PgCache:ReadOnlyMode"]);
        options.CreateInfrastructure = bool.Parse(builder.Configuration["PgCache:CreateInfrastructure"]);

        if (TimeSpan.TryParse(builder.Configuration["PgCache:ExpiredItemsDeletionInterval"], out var deletionInterval))
        {
            options.ExpiredItemsDeletionInterval = deletionInterval;
        }

        if (TimeSpan.TryParse(builder.Configuration["PgCache:DefaultSlidingExpiration"], out var slidingExpiration))
        {
            options.DefaultSlidingExpiration = slidingExpiration;
        }
    });

// Alternative configuration using the standard method with manual setup
/*
builder.Services.AddDistributedPostgreSqlCache((serviceProvider, setup) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<PostgreSqlCache>>();
    
    // Enable reloadable connection string
    setup.ConnectionStringKey = "PostgreSqlCache:ConnectionString";
    setup.Configuration = configuration;
    setup.Logger = logger;
    setup.EnableConnectionStringReloading = true;
    setup.ConnectionStringReloadInterval = TimeSpan.FromMinutes(5);
    
    // Other configuration options
    setup.SchemaName = configuration["PgCache:SchemaName"];
    setup.TableName = configuration["PgCache:TableName"];
    setup.DisableRemoveExpired = bool.Parse(configuration["PgCache:DisableRemoveExpired"]);
    setup.UpdateOnGetCacheItem = bool.Parse(configuration["PgCache:UpdateOnGetCacheItem"]);
    setup.ReadOnlyMode = bool.Parse(configuration["PgCache:ReadOnlyMode"]);
    setup.CreateInfrastructure = bool.Parse(configuration["PgCache:CreateInfrastructure"]);
    
    if (TimeSpan.TryParse(configuration["PgCache:ExpiredItemsDeletionInterval"], out var deletionInterval))
    {
        setup.ExpiredItemsDeletionInterval = deletionInterval;
    }
    
    if (TimeSpan.TryParse(configuration["PgCache:DefaultSlidingExpiration"], out var slidingExpiration))
    {
        setup.DefaultSlidingExpiration = slidingExpiration;
    }
});
*/

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();