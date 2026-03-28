using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Subscription.Infrastructure.Data;

public class SubscriptionDbContextFactory : IDesignTimeDbContextFactory<SubscriptionDbContext>
{
    public SubscriptionDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SubscriptionDbContext>();
        
        // Use a connection string for design-time only
        optionsBuilder.UseSqlServer("Server=localhost;Database=SubscriptionServiceDb;Integrated Security=true;TrustServerCertificate=True;");
        
        return new SubscriptionDbContext(optionsBuilder.Options);
    }
}
