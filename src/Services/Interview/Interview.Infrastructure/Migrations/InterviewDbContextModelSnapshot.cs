using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Interview.Infrastructure.Data;

#nullable disable

namespace Interview.Infrastructure.Migrations
{
    [DbContext(typeof(InterviewDbContext))]
    partial class InterviewDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Interview.Domain.Entities.Interview", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<int>("ApplicationId")
                    .HasColumnType("int");

                b.Property<int>("UserId")
                    .HasColumnType("int");

                b.Property<int>("PostId")
                    .HasColumnType("int");

                b.Property<string>("PostPosition")
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<DateTime>("Date")
                    .HasColumnType("date");

                b.Property<TimeSpan>("Time")
                    .HasColumnType("time");

                b.Property<int>("Round")
                    .HasColumnType("int");

                b.Property<string>("Type")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("Platform")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("Link")
                    .IsRequired()
                    .HasMaxLength(1000)
                    .HasColumnType("nvarchar(1000)");

                b.Property<string>("Building")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("Room")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<string>("InterviewerName")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.HasKey("Id");

                b.ToTable("Interviews");
            });
        }
    }
}
