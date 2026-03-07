using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Connection.Infrastructure.Data;

#nullable disable

namespace Connection.Infrastructure.Migrations
{
    [DbContext(typeof(ConnectionDbContext))]
    partial class ConnectionDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Connection.Domain.Entities.Connection", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<int>("UserIdFrom").HasColumnType("int");
                b.Property<int>("UserIdTo").HasColumnType("int");
                b.Property<int>("ProfileId").HasColumnType("int");
                b.Property<string>("Status").HasMaxLength(50).HasColumnType("nvarchar(50)");
                b.Property<DateTime>("CreateAt").HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
                b.Property<DateTime?>("ConnectionAt").HasColumnType("datetime2");

                b.HasKey("Id");

                b.ToTable("Connection");
            });

            modelBuilder.Entity("Connection.Domain.Entities.Room", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<int>("ConnectionId").HasColumnType("int");
                b.Property<DateTime>("CreatedAt").HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
                b.Property<DateTime?>("LastMessAt").HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("ConnectionId");

                b.ToTable("Room");
            });

            modelBuilder.Entity("Connection.Domain.Entities.Message", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<int>("UserId").HasColumnType("int");
                b.Property<int>("MessageRoomId").HasColumnType("int");
                b.Property<string>("Content").HasMaxLength(2000).HasColumnType("nvarchar(2000)");
                b.Property<DateTime>("CreatedAt").HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
                b.Property<int>("Status").HasColumnType("int").HasDefaultValue(0);

                b.HasKey("Id");

                b.HasIndex("MessageRoomId");

                b.ToTable("Message");
            });

            modelBuilder.Entity("Connection.Domain.Entities.Room", b =>
            {
                b.HasOne("Connection.Domain.Entities.Connection")
                    .WithMany("Rooms")
                    .HasForeignKey("ConnectionId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("FK_Room_Connection_ConnectionId");

                b.Navigation("Messages");
            });

            modelBuilder.Entity("Connection.Domain.Entities.Message", b =>
            {
                b.HasOne("Connection.Domain.Entities.Room")
                    .WithMany("Messages")
                    .HasForeignKey("MessageRoomId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired()
                    .HasConstraintName("FK_Message_Room_MessageRoomId");
            });
        }
    }
}
