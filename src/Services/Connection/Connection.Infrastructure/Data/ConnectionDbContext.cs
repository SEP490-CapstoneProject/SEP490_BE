using Microsoft.EntityFrameworkCore;
using Connection.Domain.Entities;

namespace Connection.Infrastructure.Data;

public class ConnectionDbContext : DbContext
{
    public ConnectionDbContext(DbContextOptions<ConnectionDbContext> options) : base(options) { }

    public DbSet<Connection.Domain.Entities.Connection> Connections { get; set; }
    public DbSet<Connection.Domain.Entities.Room> Rooms { get; set; }
    public DbSet<Connection.Domain.Entities.Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Connection.Domain.Entities.Connection>(entity =>
        {
            entity.ToTable("Connection");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserIdFrom).IsRequired();
            entity.Property(e => e.UserIdTo).IsRequired();
            entity.Property(e => e.ProfileId);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.BlockId).HasDefaultValue(0);
            entity.Property(e => e.CreateAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.ConnectionAt);
        });

        modelBuilder.Entity<Connection.Domain.Entities.Room>(entity =>
        {
            entity.ToTable("Room");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConnectionId).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.LastMessAt);

            entity.HasOne(e => e.Connection)
                .WithMany(c => c.Rooms)
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Connection.Domain.Entities.Message>(entity =>
        {
            entity.ToTable("Message");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.MessageRoomId).IsRequired();
            entity.Property(e => e.Content).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status).HasDefaultValue(0);

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(e => e.MessageRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
