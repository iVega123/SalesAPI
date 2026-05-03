using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesAPI.Application.Interfaces;
using SalesAPI.Infrastructure.Data;

namespace SalesAPI.BDD.Shared.Support;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"sales-test-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(AppDbContext)
                         || d.ServiceType == typeof(IAppDbContext))
                .ToList();
            foreach (var d in descriptors)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(_dbName));
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
