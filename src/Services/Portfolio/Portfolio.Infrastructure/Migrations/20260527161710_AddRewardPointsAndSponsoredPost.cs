using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardPointsAndSponsoredPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('PortfolioPreview', 'CacheKey') IS NULL
    ALTER TABLE [PortfolioPreview] ADD [CacheKey] nvarchar(max) NULL;
IF COL_LENGTH('PortfolioPreview', 'ImageId') IS NULL
    ALTER TABLE [PortfolioPreview] ADD [ImageId] nvarchar(200) NULL;
IF COL_LENGTH('PortfolioPreview', 'ImageUrl') IS NULL
    ALTER TABLE [PortfolioPreview] ADD [ImageUrl] nvarchar(max) NULL;
IF COL_LENGTH('PortfolioPreview', 'ImagegenModel') IS NULL
    ALTER TABLE [PortfolioPreview] ADD [ImagegenModel] nvarchar(max) NULL;
IF COL_LENGTH('PortfolioPreview', 'RecruiterSummary') IS NULL
    ALTER TABLE [PortfolioPreview] ADD [RecruiterSummary] nvarchar(max) NULL;
IF COL_LENGTH('PortfolioPreview', 'SelectedTheme') IS NULL
    ALTER TABLE [PortfolioPreview] ADD [SelectedTheme] nvarchar(max) NULL;
IF COL_LENGTH('PortfolioPreview', 'SocialCaption') IS NULL
    ALTER TABLE [PortfolioPreview] ADD [SocialCaption] nvarchar(max) NULL;
IF COL_LENGTH('PortfolioPreview', 'VisualPrompt') IS NULL
    ALTER TABLE [PortfolioPreview] ADD [VisualPrompt] nvarchar(max) NULL;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[RewardPointTransaction]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RewardPointTransaction](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Points] decimal(10,2) NOT NULL,
        [Type] INT NOT NULL,
        [SourceType] INT NOT NULL,
        [SourceId] nvarchar(36) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_RewardPointTransaction_CreatedAt] DEFAULT GETDATE(),
        CONSTRAINT [PK_RewardPointTransaction] PRIMARY KEY ([Id])
    );
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[SponsoredPost]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SponsoredPost](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CreatedBy] INT NOT NULL,
        [ContentType] INT NOT NULL,
        [TextContent] nvarchar(max) NULL,
        [ImageUrl] nvarchar(500) NULL,
        [VideoUrl] nvarchar(500) NULL,
        [PointsSpent] decimal(10,2) NOT NULL,
        [DurationDays] INT NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [Status] INT NOT NULL,
        [ClickThroughUrl] nvarchar(500) NULL,
        [ViewCount] INT NOT NULL CONSTRAINT [DF_SponsoredPost_ViewCount] DEFAULT 0,
        [ClickCount] INT NOT NULL CONSTRAINT [DF_SponsoredPost_ClickCount] DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_SponsoredPost_CreatedAt] DEFAULT GETDATE(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_SponsoredPost] PRIMARY KEY ([Id])
    );
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[RewardPointTransaction]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RewardPointTransaction_UserId' AND object_id = OBJECT_ID(N'[dbo].[RewardPointTransaction]'))
        CREATE INDEX [IX_RewardPointTransaction_UserId] ON [dbo].[RewardPointTransaction]([UserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RewardPointTransaction_UserId_CreatedAt' AND object_id = OBJECT_ID(N'[dbo].[RewardPointTransaction]'))
        CREATE INDEX [IX_RewardPointTransaction_UserId_CreatedAt] ON [dbo].[RewardPointTransaction]([UserId], [CreatedAt]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RewardPointTransaction_UserId_Type' AND object_id = OBJECT_ID(N'[dbo].[RewardPointTransaction]'))
        CREATE INDEX [IX_RewardPointTransaction_UserId_Type] ON [dbo].[RewardPointTransaction]([UserId], [Type]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[SponsoredPost]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SponsoredPost_CreatedAt' AND object_id = OBJECT_ID(N'[dbo].[SponsoredPost]'))
        CREATE INDEX [IX_SponsoredPost_CreatedAt] ON [dbo].[SponsoredPost]([CreatedAt]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SponsoredPost_CreatedBy' AND object_id = OBJECT_ID(N'[dbo].[SponsoredPost]'))
        CREATE INDEX [IX_SponsoredPost_CreatedBy] ON [dbo].[SponsoredPost]([CreatedBy]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SponsoredPost_Status' AND object_id = OBJECT_ID(N'[dbo].[SponsoredPost]'))
        CREATE INDEX [IX_SponsoredPost_Status] ON [dbo].[SponsoredPost]([Status]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SponsoredPost_Status_ExpiryDate' AND object_id = OBJECT_ID(N'[dbo].[SponsoredPost]'))
        CREATE INDEX [IX_SponsoredPost_Status_ExpiryDate] ON [dbo].[SponsoredPost]([Status], [ExpiryDate]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RewardPointTransaction");

            migrationBuilder.DropTable(
                name: "SponsoredPost");

            migrationBuilder.DropColumn(
                name: "CacheKey",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "ImagegenModel",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "RecruiterSummary",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "SelectedTheme",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "SocialCaption",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "VisualPrompt",
                table: "PortfolioPreview");
        }
    }
}
