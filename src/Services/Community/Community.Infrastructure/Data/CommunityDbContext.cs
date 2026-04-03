using Microsoft.EntityFrameworkCore;
using Community.Domain.Entities;

namespace Community.Infrastructure.Data;

public class CommunityDbContext : DbContext
{
    public CommunityDbContext(DbContextOptions<CommunityDbContext> options) : base(options) { }

    public DbSet<CommunityPost> CommunityPosts { get; set; }
    public DbSet<CommunityPostSave> CommunityPostSaves { get; set; }
    public DbSet<CommunityPostFavorite> CommunityPostFavorites { get; set; }
    public DbSet<CommunityPostMedia> CommunityPostMedia { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ReplyComment> ReplyComments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CommunityPost configuration
        modelBuilder.Entity<CommunityPost>(entity =>
        {
            entity.ToTable("communityPost");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userId").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CoverImageVideo).HasColumnName("coverImageVideo").HasMaxLength(255);
            entity.Property(e => e.PortfolioId).HasColumnName("portfolioId");
            entity.Property(e => e.FavoriteCount).HasColumnName("favoriteCount").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("createAt");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_CommunityPost_UserId");
        });

        // CommunityPostSave configuration
        modelBuilder.Entity<CommunityPostSave>(entity =>
        {
            entity.ToTable("communityPostSave");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CommunityPostId).HasColumnName("communityPostId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("userId").IsRequired();

            entity.HasOne(e => e.CommunityPost)
                .WithMany(p => p.Saves)
                .HasForeignKey(e => e.CommunityPostId)
                .HasConstraintName("FK_PostSave_Post")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CommunityPostId, e.UserId })
                .IsUnique()
                .HasDatabaseName("UQ_PostSave");
        });

        // CommunityPostFavorite configuration
        modelBuilder.Entity<CommunityPostFavorite>(entity =>
        {
            entity.ToTable("communityPostFavorite");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CommunityPostId).HasColumnName("communityPostId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("userId").IsRequired();

            entity.HasOne(e => e.CommunityPost)
                .WithMany(p => p.Favorites)
                .HasForeignKey(e => e.CommunityPostId)
                .HasConstraintName("FK_PostFavorite_Post")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CommunityPostId, e.UserId })
                .IsUnique()
                .HasDatabaseName("UQ_PostFavorite");
        });

        // CommunityPostMedia configuration
        modelBuilder.Entity<CommunityPostMedia>(entity =>
        {
            entity.ToTable("communityPostMedia");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CommunityPostId).HasColumnName("communityPostId").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);

            entity.HasOne(e => e.CommunityPost)
                .WithMany(p => p.Media)
                .HasForeignKey(e => e.CommunityPostId)
                .HasConstraintName("FK_PostMedia_Post")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.CommunityPostId).HasDatabaseName("IX_Media_PostId");
        });

        // Comment configuration
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CommunityPostId).HasColumnName("communityPostId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("userId").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasColumnName("createAt");

            entity.HasOne(e => e.CommunityPost)
                .WithMany(p => p.Comments)
                .HasForeignKey(e => e.CommunityPostId)
                .HasConstraintName("FK_Comment_Post")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.CommunityPostId).HasDatabaseName("IX_Comment_PostId");
        });

        // ReplyComment configuration
        modelBuilder.Entity<ReplyComment>(entity =>
        {
            entity.ToTable("ReplyComment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CommentId).HasColumnName("commentId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("userId").IsRequired();
            entity.Property(e => e.ReplyToUserId).HasColumnName("replyToUserId");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasColumnName("createAt");

            entity.HasOne(e => e.Comment)
                .WithMany(c => c.Replies)
                .HasForeignKey(e => e.CommentId)
                .HasConstraintName("FK_ReplyComment_Comment")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.CommentId).HasDatabaseName("IX_ReplyComment_CommentId");
        });
    }
}
