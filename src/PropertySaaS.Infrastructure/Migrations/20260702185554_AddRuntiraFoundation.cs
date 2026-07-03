using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertySaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRuntiraFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuntiraBlobArchives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlobPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraBlobArchives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JurisdictionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastMessageUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraJurisdictionProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportedLanguagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredQuestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvoiceRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssetRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaintenanceRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraJurisdictionProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraOrganizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultLocale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegalProfileJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdditionalSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraOrganizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraQuotaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaxAssets = table.Column<int>(type: "int", nullable: false),
                    MaxDocuments = table.Column<int>(type: "int", nullable: false),
                    MaxMonthlyAiRequests = table.Column<int>(type: "int", nullable: false),
                    MaxBlobStorageMb = table.Column<int>(type: "int", nullable: false),
                    MaxActiveWorkflows = table.Column<int>(type: "int", nullable: false),
                    EnforceHardLimit = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraQuotaPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClerkUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraWorkflowTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromptTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredQuestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraWorkflowTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuredPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresAction = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuntiraMessages_RuntiraConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "RuntiraConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitCount = table.Column<int>(type: "int", nullable: false),
                    LegalProfileJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdditionalDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkflowSummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuntiraAssets_RuntiraOrganizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "RuntiraOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastSelectedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuntiraMemberships_RuntiraOrganizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "RuntiraOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuntiraMemberships_RuntiraUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "RuntiraUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RuntiraBlobArchives",
                columns: new[] { "Id", "BlobPath", "Category", "ContentType", "CreatedUtc", "Hash", "MetadataJson", "ModifiedUtc", "SizeBytes", "SourceSystem", "TenantId" },
                values: new object[] { new Guid("99999999-aaaa-bbbb-cccc-000000000000"), "demo/activity/2026/07/02/create-asset.json", "Activity", "application/json", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), "seed-demo-activity", "{\"intent\":\"CreateAsset\"}", null, 256L, "seed", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb") });

            migrationBuilder.InsertData(
                table: "RuntiraConversations",
                columns: new[] { "Id", "Channel", "CreatedUtc", "Intent", "JurisdictionCode", "LastMessageUtc", "Locale", "ModifiedUtc", "Status", "Subject", "SummaryJson", "TenantId" },
                values: new object[] { new Guid("33333333-aaaa-bbbb-cccc-444444444444"), "Chat", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), "CreateAsset", "CA-ON", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), "en", null, "Open", "Create a 3-unit property", "{\"nextQuestion\":\"What is the full property address?\"}", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb") });

            migrationBuilder.InsertData(
                table: "RuntiraJurisdictionProfiles",
                columns: new[] { "Id", "AssetRulesJson", "CountryCode", "CreatedUtc", "InvoiceRulesJson", "MaintenanceRulesJson", "ModifiedUtc", "RegionCode", "RequiredQuestionsJson", "SupportedLanguagesJson", "TenantId", "ValidationRulesJson" },
                values: new object[] { new Guid("12121212-aaaa-bbbb-cccc-343434343434"), "{\"supportsMultiUnit\":true}", "CA", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), "{\"supportsMonthlyInvoice\":true}", "{\"supportInboxClassification\":true}", null, "ON", "[\"address\",\"unitCount\",\"ownerName\",\"leaseTemplate\"]", "[\"en\",\"fr\",\"es\"]", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), "{\"unitCount\":{\"required\":true,\"min\":1}}" });

            migrationBuilder.InsertData(
                table: "RuntiraOrganizations",
                columns: new[] { "Id", "AdditionalSettingsJson", "CountryCode", "CreatedUtc", "DefaultLocale", "IsActive", "LegalProfileJson", "ModifiedUtc", "Name", "OwnerEmail", "RegionCode", "Slug", "TimeZone" },
                values: new object[] { new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), "{\"tenantMode\":\"subdomain\",\"archive\":\"blob\"}", "CA", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), "en", true, "{\"jurisdiction\":\"ON\",\"supports\":[\"en\",\"fr\",\"es\"]}", null, "Runtira Demo Org", "michelfopa@gmail.com", "ON", "demo", "America/Toronto" });

            migrationBuilder.InsertData(
                table: "RuntiraQuotaPolicies",
                columns: new[] { "Id", "CreatedUtc", "EnforceHardLimit", "MaxActiveWorkflows", "MaxAssets", "MaxBlobStorageMb", "MaxDocuments", "MaxMonthlyAiRequests", "ModifiedUtc", "TenantId" },
                values: new object[] { new Guid("56565656-aaaa-bbbb-cccc-787878787878"), new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, 50, 100, 2048, 1000, 5000, null, new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb") });

            migrationBuilder.InsertData(
                table: "RuntiraUsers",
                columns: new[] { "Id", "ClerkUserId", "CreatedUtc", "Email", "FullName", "IsActive", "IsSuperAdmin", "ModifiedUtc", "PreferredLanguage" },
                values: new object[] { new Guid("cccccccc-1111-2222-3333-dddddddddddd"), "runtira_demo_owner", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), "michelfopa@gmail.com", "Michel Fopa", true, true, null, "fr" });

            migrationBuilder.InsertData(
                table: "RuntiraWorkflowTemplates",
                columns: new[] { "Id", "CreatedUtc", "Description", "IsActive", "ModifiedUtc", "Name", "PromptTemplate", "RequiredQuestionsJson", "TenantId", "TriggerType", "ValidationSchemaJson" },
                values: new object[] { new Guid("77777777-aaaa-bbbb-cccc-888888888888"), new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Guides the user through mandatory jurisdiction-aware asset questions.", true, null, "Create asset from natural language", "Ask only for missing required fields and confirm before creation.", "[\"address\",\"unitCount\",\"jurisdiction\",\"rentSchedule\"]", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), "CreateAsset", "{\"unitCount\":{\"min\":1}}" });

            migrationBuilder.InsertData(
                table: "RuntiraAssets",
                columns: new[] { "Id", "AdditionalDataJson", "AddressLine1", "AssetType", "City", "CountryCode", "CreatedUtc", "LegalProfileJson", "ModifiedUtc", "Name", "RegionCode", "TenantId", "UnitCount", "WorkflowSummaryJson" },
                values: new object[] { new Guid("11111111-aaaa-bbbb-cccc-222222222222"), "{\"source\":\"seed\"}", "100 King Street West", "Property", "Toronto", "CA", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), "{\"requiredQuestions\":[\"address\",\"unitCount\",\"rentSchedule\"]}", null, "Runtira AI Asset", "ON", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), 3, "{\"status\":\"ready\"}" });

            migrationBuilder.InsertData(
                table: "RuntiraMemberships",
                columns: new[] { "Id", "CreatedUtc", "LastSelectedUtc", "ModifiedUtc", "Role", "Status", "TenantId", "UserId" },
                values: new object[] { new Guid("eeeeeeee-1111-2222-3333-ffffffffffff"), new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Owner", "Active", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), new Guid("cccccccc-1111-2222-3333-dddddddddddd") });

            migrationBuilder.InsertData(
                table: "RuntiraMessages",
                columns: new[] { "Id", "AuthorType", "Content", "ConversationId", "CreatedByEmail", "CreatedUtc", "Direction", "ModifiedUtc", "RequiresAction", "StructuredPayloadJson", "TenantId" },
                values: new object[] { new Guid("55555555-aaaa-bbbb-cccc-666666666666"), "User", "I want to create a property with 3 units.", new Guid("33333333-aaaa-bbbb-cccc-444444444444"), "michelfopa@gmail.com", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Incoming", null, true, "{\"intent\":\"CreateAsset\"}", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb") });

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraAssets_TenantId",
                table: "RuntiraAssets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraMemberships_TenantId",
                table: "RuntiraMemberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraMemberships_UserId",
                table: "RuntiraMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraMessages_ConversationId",
                table: "RuntiraMessages",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuntiraAssets");

            migrationBuilder.DropTable(
                name: "RuntiraBlobArchives");

            migrationBuilder.DropTable(
                name: "RuntiraJurisdictionProfiles");

            migrationBuilder.DropTable(
                name: "RuntiraMemberships");

            migrationBuilder.DropTable(
                name: "RuntiraMessages");

            migrationBuilder.DropTable(
                name: "RuntiraQuotaPolicies");

            migrationBuilder.DropTable(
                name: "RuntiraWorkflowTemplates");

            migrationBuilder.DropTable(
                name: "RuntiraOrganizations");

            migrationBuilder.DropTable(
                name: "RuntiraUsers");

            migrationBuilder.DropTable(
                name: "RuntiraConversations");
        }
    }
}
