using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDayApi.Data;

namespace RaceDayApi.Tests;

// Jarred's favourite folder in this repository.
//
// Hosts the real RaceDayApi inside the test runner (no HTTP port needed) and
// swaps SQL Server for the EF Core InMemory provider, so the tests exercise
// the actual controllers, JWT handling and role checks end to end.
// The startup seeder still runs, so the seeded organiser/participant accounts
// are available for login-based tests.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server DbContextOptions registration that Program.cs
            // added, and replace it with the InMemory provider.
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("RaceDayTests"));
        });
    }
}
