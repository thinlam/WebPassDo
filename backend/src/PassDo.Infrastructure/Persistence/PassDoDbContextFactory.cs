using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PassDo.Application.Common.Interfaces;
using PassDo.Infrastructure.Services;

namespace PassDo.Infrastructure.Persistence;

public class PassDoDbContextFactory : IDesignTimeDbContextFactory<PassDoDbContext>
{
    public PassDoDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "PassDo.Api");
        if (!Directory.Exists(basePath))
        {
            basePath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=DESKTOP-CKNT19A\\SQLEXPRESS;Database=PassDoDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False";

        var optionsBuilder = new DbContextOptionsBuilder<PassDoDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new PassDoDbContext(
            optionsBuilder.Options,
            new DesignTimeCurrentUserService(),
            new DateTimeProvider());
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Email => null;
        public string? Role => null;
        public bool IsAuthenticated => false;
    }
}
