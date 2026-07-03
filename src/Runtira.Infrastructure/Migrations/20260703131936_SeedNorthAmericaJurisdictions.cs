using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Runtira.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedNorthAmericaJurisdictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "RuntiraConversations",
                keyColumn: "Id",
                keyValue: new Guid("33333333-aaaa-bbbb-cccc-444444444444"),
                column: "Subject",
                value: "Créer une facture mensuelle Alberta");

            migrationBuilder.UpdateData(
                table: "RuntiraJurisdictionProfiles",
                keyColumn: "Id",
                keyValue: new Guid("12121212-aaaa-bbbb-cccc-343434343434"),
                column: "InvoiceRulesJson",
                value: "{\"generatePdf\":true,\"addAutomaticSalesTax\":false}");

            migrationBuilder.UpdateData(
                table: "RuntiraOrganizations",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                column: "LegalProfileJson",
                value: "{\"jurisdiction\":\"CA-AB\",\"supports\":[\"fr-CA\",\"en-CA\",\"es-MX\"]}");

            migrationBuilder.InsertData(
                table: "RuntiraOrganizations",
                columns: new[] { "Id", "AdditionalSettingsJson", "CountryCode", "CreatedUtc", "DefaultLocale", "IsActive", "LegalProfileJson", "ModifiedUtc", "Name", "OwnerEmail", "RegionCode", "Slug", "TimeZone" },
                values: new object[,]
                {
                    { new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc"), "{\"tenantMode\":\"path\",\"archive\":\"blob\"}", "CA", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "en-CA", true, "{\"jurisdiction\":\"CA-ON\",\"supports\":[\"en-CA\",\"fr-CA\"]}", null, "Runtira Demo Ontario", "michelfopa@gmail.com", "ON", "demo-ontario", "America/Toronto" },
                    { new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd"), "{\"tenantMode\":\"path\",\"archive\":\"blob\"}", "US", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "en-US", true, "{\"jurisdiction\":\"US-TX\",\"supports\":[\"en-US\",\"es-MX\"]}", null, "Runtira Demo Texas", "michelfopa@gmail.com", "TX", "demo-texas", "America/Chicago" }
                });

            migrationBuilder.UpdateData(
                table: "RuntiraWorkflowTemplates",
                keyColumn: "Id",
                keyValue: new Guid("77777777-aaaa-bbbb-cccc-888888888888"),
                columns: new[] { "Description", "Name", "PromptTemplate" },
                values: new object[] { "Collecte les champs requis du profil CA-AB et prépare une facture PDF envoyable.", "Create invoice draft for CA-AB", "Demande les champs requis par la juridiction active avant génération." });

            migrationBuilder.InsertData(
                table: "RuntiraAssets",
                columns: new[] { "Id", "AdditionalDataJson", "AddressLine1", "AssetType", "City", "CountryCode", "CreatedUtc", "LegalProfileJson", "ModifiedUtc", "Name", "RegionCode", "TenantId", "UnitCount", "WorkflowSummaryJson" },
                values: new object[,]
                {
                    { new Guid("13131313-aaaa-bbbb-cccc-242424242424"), "{\"source\":\"seed\"}", "25 Carlton Street", "Property", "Toronto", "CA", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "{\"requiredQuestions\":[\"propertyAddress\",\"billingPeriod\",\"tenantName\",\"monthlyRent\"]}", null, "25 Carlton Street", "ON", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc"), 20, "{\"status\":\"ready\"}" },
                    { new Guid("15151515-aaaa-bbbb-cccc-262626262626"), "{\"source\":\"seed\"}", "2400 McKinney Avenue", "Property", "Dallas", "US", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "{\"requiredQuestions\":[\"propertyAddress\",\"billingPeriod\",\"ownerName\",\"monthlyRent\"]}", null, "2400 McKinney Avenue", "TX", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd"), 18, "{\"status\":\"ready\"}" }
                });

            migrationBuilder.InsertData(
                table: "RuntiraBlobArchives",
                columns: new[] { "Id", "BlobPath", "Category", "ContentType", "CreatedUtc", "Hash", "MetadataJson", "ModifiedUtc", "SizeBytes", "SourceSystem", "TenantId" },
                values: new object[,]
                {
                    { new Guid("a1a1a1a1-aaaa-bbbb-cccc-020202020202"), "demo-ontario/invoices/2026/07/invoice-july.json", "InvoiceDraft", "application/json", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "seed-demo-ontario-invoice", "{\"period\":\"2026-07\"}", null, 544L, "seed", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc") },
                    { new Guid("a3a3a3a3-aaaa-bbbb-cccc-040404040404"), "demo-texas/invoices/2026/07/invoice-july.json", "InvoiceDraft", "application/json", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "seed-demo-texas-invoice", "{\"period\":\"2026-07\"}", null, 536L, "seed", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd") }
                });

            migrationBuilder.InsertData(
                table: "RuntiraConversations",
                columns: new[] { "Id", "Channel", "CreatedUtc", "Intent", "JurisdictionCode", "LastMessageUtc", "Locale", "ModifiedUtc", "Status", "Subject", "SummaryJson", "TenantId" },
                values: new object[,]
                {
                    { new Guid("35353535-aaaa-bbbb-cccc-464646464646"), "Chat", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "CreateInvoice", "CA-ON", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "en-CA", null, "Open", "Create an Ontario monthly invoice", "{\"nextQuestion\":\"What tenant name should appear on the invoice?\"}", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc") },
                    { new Guid("37373737-aaaa-bbbb-cccc-484848484848"), "Chat", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "CreateInvoice", "US-TX", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "en-US", null, "Open", "Create a Texas invoice draft", "{\"nextQuestion\":\"Which owner name should appear on the invoice?\"}", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd") }
                });

            migrationBuilder.InsertData(
                table: "RuntiraJurisdictionProfiles",
                columns: new[] { "Id", "AssetRulesJson", "CountryCode", "CreatedUtc", "InvoiceRulesJson", "MaintenanceRulesJson", "ModifiedUtc", "RegionCode", "RequiredQuestionsJson", "SupportedLanguagesJson", "TenantId", "ValidationRulesJson" },
                values: new object[,]
                {
                    { new Guid("14141414-aaaa-bbbb-cccc-363636363636"), "{\"supportsMultiUnit\":true}", "CA", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "{\"generatePdf\":true,\"addAutomaticSalesTax\":false}", "{\"supportInboxClassification\":true}", null, "ON", "[\"propertyAddress\",\"billingPeriod\",\"tenantName\",\"monthlyRent\"]", "[\"en-CA\",\"fr-CA\"]", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc"), "{\"billingPeriod\":{\"required\":true},\"tenantName\":{\"required\":true}}" },
                    { new Guid("16161616-aaaa-bbbb-cccc-383838383838"), "{\"supportsMultiUnit\":true}", "US", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "{\"generatePdf\":true,\"addAutomaticSalesTax\":false}", "{\"supportInboxClassification\":true}", null, "TX", "[\"propertyAddress\",\"billingPeriod\",\"ownerName\",\"monthlyRent\"]", "[\"en-US\",\"es-MX\"]", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd"), "{\"billingPeriod\":{\"required\":true},\"ownerName\":{\"required\":true}}" }
                });

            migrationBuilder.InsertData(
                table: "RuntiraMemberships",
                columns: new[] { "Id", "CreatedUtc", "LastSelectedUtc", "ModifiedUtc", "Role", "Status", "TenantId", "UserId" },
                values: new object[,]
                {
                    { new Guid("efefefef-1111-2222-3333-f0f0f0f0f0f0"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Owner", "Active", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc"), new Guid("cccccccc-1111-2222-3333-dddddddddddd") },
                    { new Guid("f1f1f1f1-1111-2222-3333-f2f2f2f2f2f2"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Owner", "Active", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd"), new Guid("cccccccc-1111-2222-3333-dddddddddddd") }
                });

            migrationBuilder.InsertData(
                table: "RuntiraQuotaPolicies",
                columns: new[] { "Id", "CreatedUtc", "EnforceHardLimit", "MaxActiveWorkflows", "MaxAssets", "MaxBlobStorageMb", "MaxDocuments", "MaxMonthlyAiRequests", "ModifiedUtc", "TenantId" },
                values: new object[,]
                {
                    { new Guid("58585858-aaaa-bbbb-cccc-808080808080"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, 50, 100, 2048, 1000, 5000, null, new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc") },
                    { new Guid("60606060-aaaa-bbbb-cccc-828282828282"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, 50, 100, 2048, 1000, 5000, null, new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd") }
                });

            migrationBuilder.InsertData(
                table: "RuntiraWorkflowTemplates",
                columns: new[] { "Id", "CreatedUtc", "Description", "IsActive", "ModifiedUtc", "Name", "PromptTemplate", "RequiredQuestionsJson", "TenantId", "TriggerType", "ValidationSchemaJson" },
                values: new object[,]
                {
                    { new Guid("79797979-aaaa-bbbb-cccc-909090909090"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Collects the Ontario-required invoice fields before generation.", true, null, "Create invoice draft for CA-ON", "Ask for tenant and billing details required by the active jurisdiction.", "[\"propertyAddress\",\"billingPeriod\",\"tenantName\",\"monthlyRent\"]", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc"), "CreateInvoice", "{\"monthlyRent\":{\"min\":1},\"tenantName\":{\"required\":true}}" },
                    { new Guid("81818181-aaaa-bbbb-cccc-929292929292"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Collects the Texas-required invoice fields before generation.", true, null, "Create invoice draft for US-TX", "Ask for owner, billing period and property details required by the active jurisdiction.", "[\"propertyAddress\",\"billingPeriod\",\"ownerName\",\"monthlyRent\"]", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd"), "CreateInvoice", "{\"monthlyRent\":{\"min\":1},\"ownerName\":{\"required\":true}}" }
                });

            migrationBuilder.InsertData(
                table: "RuntiraMessages",
                columns: new[] { "Id", "AuthorType", "Content", "ConversationId", "CreatedByEmail", "CreatedUtc", "Direction", "ModifiedUtc", "RequiresAction", "StructuredPayloadJson", "TenantId" },
                values: new object[,]
                {
                    { new Guid("57575757-aaaa-bbbb-cccc-686868686868"), "User", "Create the July invoice for 25 Carlton Street.", new Guid("35353535-aaaa-bbbb-cccc-464646464646"), "michelfopa@gmail.com", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Incoming", null, true, "{\"intent\":\"CreateInvoice\"}", new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc") },
                    { new Guid("59595959-aaaa-bbbb-cccc-707070707070"), "User", "Create the monthly invoice draft for 2400 McKinney Avenue.", new Guid("37373737-aaaa-bbbb-cccc-484848484848"), "michelfopa@gmail.com", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Incoming", null, true, "{\"intent\":\"CreateInvoice\"}", new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RuntiraAssets",
                keyColumn: "Id",
                keyValue: new Guid("13131313-aaaa-bbbb-cccc-242424242424"));

            migrationBuilder.DeleteData(
                table: "RuntiraAssets",
                keyColumn: "Id",
                keyValue: new Guid("15151515-aaaa-bbbb-cccc-262626262626"));

            migrationBuilder.DeleteData(
                table: "RuntiraBlobArchives",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-aaaa-bbbb-cccc-020202020202"));

            migrationBuilder.DeleteData(
                table: "RuntiraBlobArchives",
                keyColumn: "Id",
                keyValue: new Guid("a3a3a3a3-aaaa-bbbb-cccc-040404040404"));

            migrationBuilder.DeleteData(
                table: "RuntiraJurisdictionProfiles",
                keyColumn: "Id",
                keyValue: new Guid("14141414-aaaa-bbbb-cccc-363636363636"));

            migrationBuilder.DeleteData(
                table: "RuntiraJurisdictionProfiles",
                keyColumn: "Id",
                keyValue: new Guid("16161616-aaaa-bbbb-cccc-383838383838"));

            migrationBuilder.DeleteData(
                table: "RuntiraMemberships",
                keyColumn: "Id",
                keyValue: new Guid("efefefef-1111-2222-3333-f0f0f0f0f0f0"));

            migrationBuilder.DeleteData(
                table: "RuntiraMemberships",
                keyColumn: "Id",
                keyValue: new Guid("f1f1f1f1-1111-2222-3333-f2f2f2f2f2f2"));

            migrationBuilder.DeleteData(
                table: "RuntiraMessages",
                keyColumn: "Id",
                keyValue: new Guid("57575757-aaaa-bbbb-cccc-686868686868"));

            migrationBuilder.DeleteData(
                table: "RuntiraMessages",
                keyColumn: "Id",
                keyValue: new Guid("59595959-aaaa-bbbb-cccc-707070707070"));

            migrationBuilder.DeleteData(
                table: "RuntiraQuotaPolicies",
                keyColumn: "Id",
                keyValue: new Guid("58585858-aaaa-bbbb-cccc-808080808080"));

            migrationBuilder.DeleteData(
                table: "RuntiraQuotaPolicies",
                keyColumn: "Id",
                keyValue: new Guid("60606060-aaaa-bbbb-cccc-828282828282"));

            migrationBuilder.DeleteData(
                table: "RuntiraWorkflowTemplates",
                keyColumn: "Id",
                keyValue: new Guid("79797979-aaaa-bbbb-cccc-909090909090"));

            migrationBuilder.DeleteData(
                table: "RuntiraWorkflowTemplates",
                keyColumn: "Id",
                keyValue: new Guid("81818181-aaaa-bbbb-cccc-929292929292"));

            migrationBuilder.DeleteData(
                table: "RuntiraConversations",
                keyColumn: "Id",
                keyValue: new Guid("35353535-aaaa-bbbb-cccc-464646464646"));

            migrationBuilder.DeleteData(
                table: "RuntiraConversations",
                keyColumn: "Id",
                keyValue: new Guid("37373737-aaaa-bbbb-cccc-484848484848"));

            migrationBuilder.DeleteData(
                table: "RuntiraOrganizations",
                keyColumn: "Id",
                keyValue: new Guid("abababab-1111-2222-3333-bcbcbcbcbcbc"));

            migrationBuilder.DeleteData(
                table: "RuntiraOrganizations",
                keyColumn: "Id",
                keyValue: new Guid("acacacac-1111-2222-3333-bdbdbdbdbdbd"));

            migrationBuilder.UpdateData(
                table: "RuntiraConversations",
                keyColumn: "Id",
                keyValue: new Guid("33333333-aaaa-bbbb-cccc-444444444444"),
                column: "Subject",
                value: "Créer la facture mensuelle Alberta");

            migrationBuilder.UpdateData(
                table: "RuntiraJurisdictionProfiles",
                keyColumn: "Id",
                keyValue: new Guid("12121212-aaaa-bbbb-cccc-343434343434"),
                column: "InvoiceRulesJson",
                value: "{\"generatePdf\":true,\"addAutomaticGst\":false}");

            migrationBuilder.UpdateData(
                table: "RuntiraOrganizations",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                column: "LegalProfileJson",
                value: "{\"jurisdiction\":\"AB\",\"supports\":[\"fr-CA\",\"en-CA\",\"es-MX\"]}");

            migrationBuilder.UpdateData(
                table: "RuntiraWorkflowTemplates",
                keyColumn: "Id",
                keyValue: new Guid("77777777-aaaa-bbbb-cccc-888888888888"),
                columns: new[] { "Description", "Name", "PromptTemplate" },
                values: new object[] { "Collecte les champs requis et prépare une facture PDF envoyable sans GST automatique.", "Create Alberta invoice PDF", "Demande l'adresse du bien, le mois/période et le montant avant génération." });
        }
    }
}
