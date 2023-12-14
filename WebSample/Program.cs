using Community.Microsoft.Extensions.Caching.PostgreSql;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDistributedPostgreSqlCache(setup =>
{
    setup.ConnectionString = builder.Configuration["PgCache:ConnectionString"];
    setup.SchemaName = builder.Configuration["PgCache:SchemaName"];
    setup.TableName = builder.Configuration["PgCache:TableName"];
    setup.CreateInfrastructure = !string.IsNullOrWhiteSpace(builder.Configuration["PgCache:CreateInfrastructure"]);
    setup.ExpiredItemsDeletionInterval = System.TimeSpan.FromMinutes(1);
    // CreateInfrastructure is optional, default is TRUE
    // This means que every time starts the application the 
    // creation of table and database functions will be verified.
});

// Add services to the container.

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();

