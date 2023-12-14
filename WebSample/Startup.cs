using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Community.Microsoft.Extensions.Caching.PostgreSql;
namespace WebSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedPostgreSqlCache(setup =>
            {
                setup.ConnectionString = Configuration["PgCache:ConnectionString"];
                setup.SchemaName = Configuration["PgCache:SchemaName"];
                setup.TableName = Configuration["PgCache:TableName"];
                setup.CreateInfrastructure = !string.IsNullOrWhiteSpace(Configuration["PgCache:CreateInfrastructure"]);
                setup.ExpiredItemsDeletionInterval = System.TimeSpan.FromMinutes(5);
                // CreateInfrastructure is optional, default is TRUE
                // This means que every time starts the application the 
                // creation of table and database functions will be verified.
            });
            services.AddControllersWithViews();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");

                endpoints.MapFallbackToFile("index.html");
            });

            
        }
    }
}
