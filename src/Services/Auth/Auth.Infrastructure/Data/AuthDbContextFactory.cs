using Auth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auth.Infrastructure.Data;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseSqlServer("Server=sqlserver;Database=AuthServiceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;");

        return new AuthDbContext(optionsBuilder.Options);
    }
}
