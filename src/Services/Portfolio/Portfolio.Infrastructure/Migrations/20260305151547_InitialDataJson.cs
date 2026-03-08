using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDataJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── BlockType ─────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[BlockType]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [BlockType] (
                        [Id]         int           NOT NULL IDENTITY,
                        [Code]       nvarchar(100) NOT NULL,
                        [IsMultiple] bit           NOT NULL DEFAULT CAST(1 AS bit),
                        CONSTRAINT [PK_BlockType] PRIMARY KEY ([Id])
                    );
                END
            ");

            // ── Portfolio ─────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Portfolio]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Portfolio] (
                        [Id]         int            NOT NULL IDENTITY,
                        [EmployeeId] int            NOT NULL,
                        [Name]       nvarchar(255)  NOT NULL,
                        [Status]     nvarchar(50)   NOT NULL DEFAULT N'active',
                        [CreatedAt]  datetime2      NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt]  datetime2      NULL,
                        CONSTRAINT [PK_Portfolio] PRIMARY KEY ([Id])
                    );
                END
            ");

            // ── PortfolioBlock ────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[PortfolioBlock]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [PortfolioBlock] (
                        [Id]           int           NOT NULL IDENTITY,
                        [PortfolioId]  int           NOT NULL,
                        [BlockTypeId]  int           NOT NULL,
                        [Variant]      nvarchar(100) NOT NULL,
                        [DisplayOrder] int           NOT NULL,
                        [IsVisible]    bit           NOT NULL DEFAULT CAST(1 AS bit),
                        [DataJson]     nvarchar(max) NOT NULL DEFAULT N'{}',
                        CONSTRAINT [PK_PortfolioBlock] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PortfolioBlock_BlockType_BlockTypeId]
                            FOREIGN KEY ([BlockTypeId]) REFERENCES [BlockType] ([Id])
                            ON DELETE NO ACTION,
                        CONSTRAINT [FK_PortfolioBlock_Portfolio_PortfolioId]
                            FOREIGN KEY ([PortfolioId]) REFERENCES [Portfolio] ([Id])
                            ON DELETE CASCADE
                    );
                END
                ELSE
                BEGIN
                    -- Table exists (old schema): ensure DataJson column is present
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.columns
                        WHERE object_id = OBJECT_ID(N'[PortfolioBlock]')
                          AND name = N'DataJson'
                    )
                    BEGIN
                        ALTER TABLE [PortfolioBlock]
                            ADD [DataJson] nvarchar(max) NOT NULL DEFAULT N'{}';
                    END
                END
            ");

            // ── Seed BlockType ────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 1)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (1, N'INTRO', 0);
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 2)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (2, N'SKILL', 1);
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 3)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (3, N'EDUCATION', 1);
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 4)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (4, N'DIPLOMA', 1);
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 5)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (5, N'EXPERIMENT', 1);
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 6)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (6, N'PROJECT', 1);
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 7)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (7, N'AWARD', 1);
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 8)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (8, N'ACTIVITIES', 1);
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 9)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (9, N'OTHERINFO', 1);
                IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 10)
                    INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (10, N'REFERENCE', 1);
            ");

            // ── Indexes ───────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_BlockType_Code'
                      AND object_id = OBJECT_ID(N'[BlockType]')
                )
                    CREATE UNIQUE INDEX [IX_BlockType_Code] ON [BlockType] ([Code]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_Portfolio_EmployeeId'
                      AND object_id = OBJECT_ID(N'[Portfolio]')
                )
                    CREATE INDEX [IX_Portfolio_EmployeeId] ON [Portfolio] ([EmployeeId]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_Block_Portfolio_Type_Order'
                      AND object_id = OBJECT_ID(N'[PortfolioBlock]')
                )
                    CREATE INDEX [IX_Block_Portfolio_Type_Order]
                        ON [PortfolioBlock] ([PortfolioId], [BlockTypeId], [DisplayOrder]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_PortfolioBlock_BlockTypeId'
                      AND object_id = OBJECT_ID(N'[PortfolioBlock]')
                )
                    CREATE INDEX [IX_PortfolioBlock_BlockTypeId] ON [PortfolioBlock] ([BlockTypeId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[PortfolioBlock]', N'U') IS NOT NULL
                    DROP TABLE [PortfolioBlock];
                IF OBJECT_ID(N'[BlockType]', N'U') IS NOT NULL
                    DROP TABLE [BlockType];
                IF OBJECT_ID(N'[Portfolio]', N'U') IS NOT NULL
                    DROP TABLE [Portfolio];
            ");
        }
    }
}
