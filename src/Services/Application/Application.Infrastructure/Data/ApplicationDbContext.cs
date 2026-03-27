using Microsoft.EntityFrameworkCore;

namespace Application.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Application> Applications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.Entities.Application>(e =>
        {
            e.ToTable("Application");
            e.HasKey(x => x.ApplicationId);
            
            e.Property(x => x.EmployeeId).IsRequired();
            e.Property(x => x.CompanyId).IsRequired();
            e.Property(x => x.CompanyPostId).IsRequired();
            e.Property(x => x.PortfolioId).IsRequired();
            e.Property(x => x.RoomId).IsRequired(false);
            e.Property(x => x.Status).IsRequired().HasDefaultValue(Domain.Entities.ApplicationStatus.WAITING);
            e.Property(x => x.AppliedAt).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.UpdatedAt).IsRequired(false);

            // UNIQUE constraint
            e.HasIndex(x => new { x.EmployeeId, x.CompanyPostId })
             .IsUnique()
             .HasDatabaseName("UX_Application_Employee_Post");

            // Indexes for performance
            e.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_Application_EmployeeId");
            e.HasIndex(x => x.CompanyId).HasDatabaseName("IX_Application_CompanyId");
            e.HasIndex(x => x.CompanyPostId).HasDatabaseName("IX_Application_CompanyPostId");
            e.HasIndex(x => x.Status).HasDatabaseName("IX_Application_Status");
        });
    }
}
