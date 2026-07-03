using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Runtira.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncRuntiraSchemaAfterTypedPayloads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuntiraInboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviewText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HasAttachments = table.Column<bool>(type: "bit", nullable: false),
                    ClassificationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraInboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QualificationScore = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RuntiraOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraLeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuntiraLeads_RuntiraAssets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "RuntiraAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RuntiraLeads_RuntiraOrganizations_RuntiraOrganizationId",
                        column: x => x.RuntiraOrganizationId,
                        principalTable: "RuntiraOrganizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RuntiraResidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraResidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UnitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarketRent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdditionalDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuntiraUnits_RuntiraAssets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "RuntiraAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BlobArchiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuntiraAttachments_RuntiraInboxMessages_InboxMessageId",
                        column: x => x.InboxMessageId,
                        principalTable: "RuntiraInboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuntiraLeases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaseEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BillingPeriod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TermsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntiraLeases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuntiraLeases_RuntiraAssets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "RuntiraAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RuntiraLeases_RuntiraResidents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "RuntiraResidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuntiraLeases_RuntiraUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "RuntiraUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RuntiraInboxMessages",
                columns: new[] { "Id", "Category", "ClassificationJson", "CreatedUtc", "ExternalMessageId", "FromEmail", "HasAttachments", "ModifiedUtc", "PreviewText", "Provider", "ReceivedUtc", "RelatedEntityId", "RelatedEntityType", "Status", "Subject", "TenantId" },
                values: new object[,]
                {
                    { new Guid("62626262-aaaa-bbbb-cccc-848484848484"), "Lead", "{\"confidence\":0.92,\"suggestedAction\":\"CallBack\"}", new DateTime(2026, 7, 3, 14, 0, 0, 0, DateTimeKind.Utc), "mock-ab-001", "prospect.ab@example.com", true, null, "Bonjour, je cherche un 2 chambres disponible en août à Calgary.", "MockMicrosoft365", new DateTime(2026, 7, 3, 14, 0, 0, 0, DateTimeKind.Utc), new Guid("47474747-aaaa-bbbb-cccc-676767676767"), "Lead", "Classified", "Disponibilité pour août", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb") },
                    { new Guid("64646464-aaaa-bbbb-cccc-868686868686"), "Invoice", "{\"confidence\":0.88,\"suggestedAction\":\"SendInvoice\"}", new DateTime(2026, 7, 3, 15, 0, 0, 0, DateTimeKind.Utc), "mock-on-001", "resident.on@example.com", false, null, "Can you resend the July invoice for unit 1204?", "MockMicrosoft365", new DateTime(2026, 7, 3, 15, 0, 0, 0, DateTimeKind.Utc), new Guid("43434343-aaaa-bbbb-cccc-636363636363"), "Lease", "Classified", "Need invoice copy for July", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc") },
                    { new Guid("66666666-aaaa-bbbb-cccc-888888888889"), "Document", "{\"confidence\":0.81,\"suggestedAction\":\"ArchiveDocument\"}", new DateTime(2026, 7, 3, 16, 0, 0, 0, DateTimeKind.Utc), "mock-tx-001", "owner.tx@example.com", true, null, "Attaching a quote for the next lease renewal package.", "MockMicrosoft365", new DateTime(2026, 7, 3, 16, 0, 0, 0, DateTimeKind.Utc), new Guid("15151515-aaaa-bbbb-cccc-262626262626"), "Asset", "Classified", "Please archive this vendor quote", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd") }
                });

            migrationBuilder.InsertData(
                table: "RuntiraLeads",
                columns: new[] { "Id", "AssetId", "CreatedUtc", "Email", "FullName", "ModifiedUtc", "NotesJson", "PhoneNumber", "PreferredLanguage", "QualificationScore", "RuntiraOrganizationId", "Source", "Status", "Summary", "TenantId" },
                values: new object[,]
                {
                    { new Guid("47474747-aaaa-bbbb-cccc-676767676767"), new Guid("11111111-aaaa-bbbb-cccc-222222222222"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "nora.bouchard@example.com", "Nora Bouchard", null, "{\"budget\":2500,\"moveInMonth\":\"2026-08\"}", "+1-403-555-0171", "fr-CA", 92, null, "InboxMock", "Qualified", "Lead chaud pour un 2 chambres avec entrée en août.", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb") },
                    { new Guid("49494949-aaaa-bbbb-cccc-696969696969"), new Guid("13131313-aaaa-bbbb-cccc-242424242424"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "kevin.thompson@example.com", "Kevin Thompson", null, "{\"budget\":3200,\"petFriendly\":true}", "+1-647-555-0112", "en-CA", 78, null, "ImportCsv", "New", "Lead imported from listing export, interested in downtown condo.", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc") },
                    { new Guid("51515151-aaaa-bbbb-cccc-717171717171"), new Guid("15151515-aaaa-bbbb-cccc-262626262626"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "elena.perez@example.com", "Elena Perez", null, "{\"budget\":2900,\"preferredLanguage\":\"es-MX\"}", "+1-214-555-0126", "es-MX", 84, null, "Manual", "Pending", "Lead bilingue demandant un logement proche du centre-ville.", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd") }
                });

            migrationBuilder.InsertData(
                table: "RuntiraResidents",
                columns: new[] { "Id", "CreatedUtc", "Email", "FullName", "ModifiedUtc", "NotesJson", "PhoneNumber", "PreferredLanguage", "Status", "TenantId" },
                values: new object[,]
                {
                    { new Guid("27272727-aaaa-bbbb-cccc-494949494949"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "amelie.gagnon@example.com", "Amelie Gagnon", null, "{\"leaseIntent\":\"renewal\"}", "+1-403-555-0147", "fr-CA", "Active", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb") },
                    { new Guid("29292929-aaaa-bbbb-cccc-515151515151"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "lucas.martin@example.com", "Lucas Martin", null, "{\"preferredChannel\":\"email\"}", "+1-416-555-0120", "en-CA", "Active", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc") },
                    { new Guid("31313131-aaaa-bbbb-cccc-535353535353"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "maya.rodriguez@example.com", "Maya Rodriguez", null, "{\"moveInWindow\":\"2026-08\"}", "+1-214-555-0191", "es-MX", "Active", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd") }
                });

            migrationBuilder.InsertData(
                table: "RuntiraUnits",
                columns: new[] { "Id", "AdditionalDataJson", "AssetId", "CreatedUtc", "MarketRent", "ModifiedUtc", "Status", "TenantId", "UnitCode", "UnitType" },
                values: new object[,]
                {
                    { new Guid("21212121-aaaa-bbbb-cccc-434343434343"), "{\"bedrooms\":2}", new Guid("11111111-aaaa-bbbb-cccc-222222222222"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), 2450m, null, "Occupied", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), "A-101", "Apartment" },
                    { new Guid("23232323-aaaa-bbbb-cccc-454545454545"), "{\"bedrooms\":1}", new Guid("13131313-aaaa-bbbb-cccc-242424242424"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), 3100m, null, "Occupied", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc"), "1204", "Condo" },
                    { new Guid("25252525-aaaa-bbbb-cccc-474747474747"), "{\"bedrooms\":2}", new Guid("15151515-aaaa-bbbb-cccc-262626262626"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), 2800m, null, "Occupied", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd"), "8B", "Apartment" }
                });

            migrationBuilder.InsertData(
                table: "RuntiraAttachments",
                columns: new[] { "Id", "BlobArchiveId", "Category", "ContentType", "CreatedUtc", "FileName", "InboxMessageId", "MetadataJson", "ModifiedUtc", "SizeBytes", "TenantId" },
                values: new object[,]
                {
                    { new Guid("68686868-aaaa-bbbb-cccc-909090909091"), null, "LeadDocument", "text/plain", new DateTime(2026, 7, 3, 14, 0, 0, 0, DateTimeKind.Utc), "budget-range.txt", new Guid("62626262-aaaa-bbbb-cccc-848484848484"), "{\"source\":\"mock-inbox\"}", null, 256L, new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb") },
                    { new Guid("70707070-aaaa-bbbb-cccc-929292929293"), null, "VendorQuote", "application/pdf", new DateTime(2026, 7, 3, 16, 0, 0, 0, DateTimeKind.Utc), "vendor-quote.pdf", new Guid("66666666-aaaa-bbbb-cccc-888888888889"), "{\"source\":\"mock-inbox\"}", null, 40960L, new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd") }
                });

            migrationBuilder.InsertData(
                table: "RuntiraLeases",
                columns: new[] { "Id", "AssetId", "BillingPeriod", "CreatedUtc", "LeaseEndUtc", "LeaseStartUtc", "ModifiedUtc", "MonthlyRent", "ResidentId", "Status", "TenantId", "TermsJson", "UnitId" },
                values: new object[,]
                {
                    { new Guid("41414141-aaaa-bbbb-cccc-616161616161"), new Guid("11111111-aaaa-bbbb-cccc-222222222222"), "Monthly", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2450m, new Guid("27272727-aaaa-bbbb-cccc-494949494949"), "Active", new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), "{\"deposit\":2450}", new Guid("21212121-aaaa-bbbb-cccc-434343434343") },
                    { new Guid("43434343-aaaa-bbbb-cccc-636363636363"), new Guid("13131313-aaaa-bbbb-cccc-242424242424"), "Monthly", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3100m, new Guid("29292929-aaaa-bbbb-cccc-515151515151"), "Active", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc"), "{\"noticeDays\":60}", new Guid("23232323-aaaa-bbbb-cccc-454545454545") },
                    { new Guid("45454545-aaaa-bbbb-cccc-656565656565"), new Guid("15151515-aaaa-bbbb-cccc-262626262626"), "Monthly", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 4, 30, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2800m, new Guid("31313131-aaaa-bbbb-cccc-535353535353"), "Active", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd"), "{\"lateFee\":75}", new Guid("25252525-aaaa-bbbb-cccc-474747474747") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraAttachments_InboxMessageId",
                table: "RuntiraAttachments",
                column: "InboxMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraInboxMessages_TenantId_ExternalMessageId",
                table: "RuntiraInboxMessages",
                columns: new[] { "TenantId", "ExternalMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraLeads_AssetId",
                table: "RuntiraLeads",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraLeads_RuntiraOrganizationId",
                table: "RuntiraLeads",
                column: "RuntiraOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraLeases_AssetId",
                table: "RuntiraLeases",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraLeases_ResidentId",
                table: "RuntiraLeases",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraLeases_UnitId",
                table: "RuntiraLeases",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraResidents_TenantId_Email",
                table: "RuntiraResidents",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraUnits_AssetId",
                table: "RuntiraUnits",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntiraUnits_TenantId_AssetId_UnitCode",
                table: "RuntiraUnits",
                columns: new[] { "TenantId", "AssetId", "UnitCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuntiraAttachments");

            migrationBuilder.DropTable(
                name: "RuntiraLeads");

            migrationBuilder.DropTable(
                name: "RuntiraLeases");

            migrationBuilder.DropTable(
                name: "RuntiraInboxMessages");

            migrationBuilder.DropTable(
                name: "RuntiraResidents");

            migrationBuilder.DropTable(
                name: "RuntiraUnits");
        }
    }
}
