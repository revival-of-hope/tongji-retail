using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RetailSystem.Api.Data;

namespace RetailSystem.Api.Tests;

public sealed class RetailApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"retail-tests-{Guid.NewGuid():N}";
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // EF Core keeps AddDbContext's provider configuration in this
            // registration. Remove the Oracle configuration before adding the
            // test provider, otherwise both providers are active at startup.
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Keep one database for the lifetime of this factory. Generating a
            // Guid inside the options callback creates a different database for
            // each service scope, so seed data would disappear between requests.
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));
        });
    }
}
