using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PropertySaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreatePersisted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoticeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceReminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequestedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionTier = table.Column<int>(type: "int", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditScore = table.Column<int>(type: "int", nullable: false),
                    ScreeningCompleted = table.Column<bool>(type: "bit", nullable: false),
                    ScreeningProvider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YearBuilt = table.Column<int>(type: "int", nullable: false),
                    MonthlyRevenueTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Properties_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClerkUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bedrooms = table.Column<int>(type: "int", nullable: false),
                    Bathrooms = table.Column<int>(type: "int", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsOccupied = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StandardOntarioLeaseSigned = table.Column<bool>(type: "bit", nullable: false),
                    N1IncreaseNoticeScheduled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leases_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AuditLogs",
                columns: new[] { "Id", "Action", "CreatedUtc", "Details", "EntityName", "ModifiedUtc", "OrganizationId", "PerformedBy" },
                values: new object[] { new Guid("88888888-8888-8888-8888-888888888888"), "Seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seeded demo Ontario lease", "Lease", null, new Guid("11111111-1111-1111-1111-111111111111"), "system" });

            migrationBuilder.InsertData(
                table: "ComplianceReminders",
                columns: new[] { "Id", "CreatedUtc", "DueDate", "IsCompleted", "ModifiedUtc", "NoticeType", "OrganizationId", "Reference", "Title" },
                values: new object[,]
                {
                    { new Guid("99999999-9999-9999-9999-999999999999"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 9, 1), false, null, "N1", new Guid("11111111-1111-1111-1111-111111111111"), "90 days notice recommended workflow", "N1 rent increase notice window" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 15), false, null, "SOL", new Guid("11111111-1111-1111-1111-111111111111"), "Ensure latest government template is attached", "Standard Ontario Lease review" }
                });

            migrationBuilder.InsertData(
                table: "DocumentTemplates",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "ModifiedUtc", "Name", "OrganizationId", "Province" },
                values: new object[,]
                {
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Lease", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Prebuilt package with required Ontario clauses and signature checklist.", null, "Ontario Standard Lease Package", new Guid("11111111-1111-1111-1111-111111111111"), "ON" },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Notice", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Guided landlord workflow for arrears communication.", null, "N4 Non-payment Notice Template", new Guid("11111111-1111-1111-1111-111111111111"), "ON" }
                });

            migrationBuilder.InsertData(
                table: "MaintenanceRequests",
                columns: new[] { "Id", "CreatedUtc", "Description", "EstimatedCost", "ModifiedUtc", "OrganizationId", "Priority", "PropertyId", "RequestedDate", "Status", "Title", "UnitId", "VendorName" },
                values: new object[] { new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ontario compliance inspection and battery replacement.", 180m, null, new Guid("11111111-1111-1111-1111-111111111111"), 2, new Guid("22222222-2222-2222-2222-222222222222"), new DateOnly(2026, 6, 15), "Open", "Annual smoke detector certification", new Guid("33333333-3333-3333-3333-333333333333"), "SafeHome Fire Services" });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "CreatedUtc", "IsActive", "ModifiedUtc", "Name", "Province", "Slug", "StripeCustomerId", "StripeSubscriptionId", "SubscriptionTier", "TimeZone" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "Maple Leaf Property Group", "ON", "maple-leaf", "cus_demo_mapleleaf", "sub_demo_mapleleaf", 2, "America/Toronto" });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "CreatedUtc", "CreditScore", "Email", "FullName", "ModifiedUtc", "OrganizationId", "PhoneNumber", "ScreeningCompleted", "ScreeningProvider" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 731, "jordan.patel@example.com", "Jordan Patel", null, new Guid("11111111-1111-1111-1111-111111111111"), "647-555-0134", true, "SingleKey" });

            migrationBuilder.InsertData(
                table: "Properties",
                columns: new[] { "Id", "AddressLine1", "City", "CreatedUtc", "ModifiedUtc", "MonthlyRevenueTarget", "Name", "OrganizationId", "PostalCode", "Province", "YearBuilt" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), "18 Stafford Street", "Toronto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 14800m, "King West Lofts", new Guid("11111111-1111-1111-1111-111111111111"), "M6J 2R9", "ON", 2017 });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClerkUserId", "CreatedUtc", "Email", "FullName", "IsActive", "ModifiedUtc", "OrganizationId", "Role" },
                values: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), "user_demo_owner", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "owner@mapleleafpm.ca", "Morgan Chen", true, null, new Guid("11111111-1111-1111-1111-111111111111"), "Owner" });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Bathrooms", "Bedrooms", "CreatedUtc", "IsOccupied", "ModifiedUtc", "MonthlyRent", "OrganizationId", "PropertyId", "UnitNumber" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, 2895m, new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222"), "508" });

            migrationBuilder.InsertData(
                table: "Leases",
                columns: new[] { "Id", "CreatedUtc", "EndDate", "ModifiedUtc", "MonthlyRent", "N1IncreaseNoticeScheduled", "OrganizationId", "StandardOntarioLeaseSigned", "StartDate", "Status", "TenantId", "UnitId" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 12, 31), null, 2895m, true, new Guid("11111111-1111-1111-1111-111111111111"), true, new DateOnly(2026, 1, 1), 1, new Guid("44444444-4444-4444-4444-444444444444"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.CreateIndex(
                name: "IX_Leases_TenantId",
                table: "Leases",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_UnitId",
                table: "Leases",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_OrganizationId",
                table: "Properties",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_PropertyId",
                table: "Units",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ComplianceReminders");

            migrationBuilder.DropTable(
                name: "DocumentTemplates");

            migrationBuilder.DropTable(
                name: "Leases");

            migrationBuilder.DropTable(
                name: "MaintenanceRequests");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
