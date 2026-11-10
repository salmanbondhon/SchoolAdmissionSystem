
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolAdmission.API.Data;
using Testcontainers.PostgreSql;

namespace SchoolAdmission.Tests;

public class PostgresTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("school_admission_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

    public IConfiguration Configuration { get; private set; }
        = null!;

    public async Task InitializeAsync()
    {
        // Start a temporary PostgreSQL container.
        await _postgres.StartAsync();

        // Configure test settings.
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["HOLD_DURATION_SECONDS"] = "600",
                    ["PAYMENT_AMOUNT"] = "1000"
                })
            .Build();

        // Create the database schema using EF Core migrations.
        await using var context = CreateDbContext();

        await context.Database.MigrateAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
