using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Test configuration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] =
                    "ThisIsASecretKeyForTestingPurposesOnly123456!",

                ["Jwt:Secret"] =
                    "ThisIsASecretKeyForTestingPurposesOnly123456!",

                ["Jwt:Issuer"] =
                    "TmsTestIssuer",

                ["Jwt:Audience"] =
                    "TmsTestAudience"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the application's DbContext registration
            services.RemoveAll<TmsDbContext>();

            // Remove DbContext options
            services.RemoveAll<DbContextOptions<TmsDbContext>>();

            // Remove the EF Core database provider registrations
            services.RemoveAll<
                Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<TmsDbContext>>();

            // Register InMemory database for integration tests
            services.AddDbContext<TmsDbContext>(options =>
            {
                options.UseInMemoryDatabase("TmsTestDb");
            });
        });
    }
}

