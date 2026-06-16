using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertySaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyPlaybookFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmenitySummary",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LeasingNotes",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NeighborhoodNotes",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OperationalNotes",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PropertyType",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Properties",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "AmenitySummary", "LeasingNotes", "NeighborhoodNotes", "OperationalNotes", "PropertyType" },
                values: new object[] { "Gym access, rooftop terrace, bike storage", "Position as design-forward downtown living for professionals and couples.", "Walkable King West location with strong renter demand and transit access.", "Monitor turnover windows closely and prioritize same-week suite refreshes.", "Urban mid-rise" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmenitySummary",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "LeasingNotes",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "NeighborhoodNotes",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "OperationalNotes",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PropertyType",
                table: "Properties");
        }
    }
}
