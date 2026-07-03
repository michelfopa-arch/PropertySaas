using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Runtira.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RebuildRuntiraForRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingPlan",
                table: "RuntiraOrganizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "RuntiraOrganizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "RuntiraOrganizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "RuntiraOrganizations",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                columns: new[] { "BillingPlan", "StripeCustomerId", "StripeSubscriptionId" },
                values: new object[] { "Trial", "", "" });

            migrationBuilder.UpdateData(
                table: "RuntiraOrganizations",
                keyColumn: "Id",
                keyValue: new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                columns: new[] { "BillingPlan", "StripeCustomerId", "StripeSubscriptionId" },
                values: new object[] { "Trial", "", "" });

            migrationBuilder.UpdateData(
                table: "RuntiraOrganizations",
                keyColumn: "Id",
                keyValue: new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                columns: new[] { "BillingPlan", "StripeCustomerId", "StripeSubscriptionId" },
                values: new object[] { "Trial", "", "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingPlan",
                table: "RuntiraOrganizations");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "RuntiraOrganizations");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "RuntiraOrganizations");
        }
    }
}
