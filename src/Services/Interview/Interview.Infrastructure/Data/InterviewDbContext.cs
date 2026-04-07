using Interview.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Interview.Infrastructure.Data;

public class InterviewDbContext : DbContext
{
    public InterviewDbContext(DbContextOptions<InterviewDbContext> options) : base(options) { }

    public DbSet<Interview.Domain.Entities.Interview> Interviews { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Interview.Domain.Entities.Interview>(entity =>
        {
            entity.ToTable("Interviews");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ApplicationId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PostId).IsRequired();
            entity.Property(e => e.PostPosition).HasMaxLength(500);
            entity.Property(e => e.Date).HasColumnType("date");
            entity.Property(e => e.Time).HasColumnType("time");
            entity.Property(e => e.Type).HasMaxLength(200);
            entity.Property(e => e.Platform).HasMaxLength(200);
            entity.Property(e => e.Link).HasMaxLength(1000);
            entity.Property(e => e.Building).HasMaxLength(200);
            entity.Property(e => e.Room).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(100);
            entity.Property(e => e.InterviewerName).HasMaxLength(200);
        });
    }
}
