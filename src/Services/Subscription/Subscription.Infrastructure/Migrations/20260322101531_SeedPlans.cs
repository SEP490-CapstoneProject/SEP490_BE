using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Subscription.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Id", "BillingCycle", "CreatedAt", "Description", "IsActive", "Name", "Price", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Basic free plan", true, "Free", 0m, null },
                    { 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Professional plan with advanced features", true, "Pro", 9.99m, null },
                    { 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Premium plan with unlimited access", true, "Premium", 19.99m, null }
                });

            migrationBuilder.InsertData(
                table: "PlanFeatures",
                columns: new[] { "Id", "FeatureKey", "FeatureName", "IsActive", "PlanId", "Type", "Value" },
                values: new object[,]
                {
                    { 1, "MAX_APPLY", "Max Applications", true, 1, 2, "5" },
                    { 2, "MAX_PORTFOLIOS", "Max Portfolios", true, 1, 2, "1" },
                    { 3, "AI_MATCHING", "AI Matching", true, 1, 1, "false" },
                    { 4, "BOOST_PROFILE", "Profile Boost", true, 1, 1, "false" },
                    { 5, "COMPLIMENT_ACCESS", "Compliment Access", true, 1, 1, "false" },
                    { 6, "MAX_APPLY", "Max Applications", true, 2, 2, "20" },
                    { 7, "MAX_PORTFOLIOS", "Max Portfolios", true, 2, 2, "5" },
                    { 8, "AI_MATCHING", "AI Matching", true, 2, 1, "true" },
                    { 9, "BOOST_PROFILE", "Profile Boost", true, 2, 1, "true" },
                    { 10, "COMPLIMENT_ACCESS", "Compliment Access", true, 2, 1, "false" },
                    { 11, "MAX_APPLY", "Max Applications", true, 3, 2, "-1" },
                    { 12, "MAX_PORTFOLIOS", "Max Portfolios", true, 3, 2, "-1" },
                    { 13, "AI_MATCHING", "AI Matching", true, 3, 1, "true" },
                    { 14, "BOOST_PROFILE", "Profile Boost", true, 3, 1, "true" },
                    { 15, "COMPLIMENT_ACCESS", "Compliment Access", true, 3, 1, "true" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
