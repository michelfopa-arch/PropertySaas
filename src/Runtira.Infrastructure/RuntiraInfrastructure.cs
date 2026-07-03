using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Runtira.Application.Abstractions;
using Runtira.Domain.Common;
using Runtira.Domain.Entities;

namespace Runtira.Infrastructure.Options
{
    public sealed class ClerkOptions
    {
        public string Authority { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string SignInUrl { get; set; } = string.Empty;
        public string SignUpUrl { get; set; } = string.Empty;
        public string UnauthorizedSignInUrl { get; set; } = string.Empty;
        public string UserProfileUrl { get; set; } = string.Empty;
    }

    public sealed class StripeOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string StarterPriceId { get; set; } = string.Empty;
        public string GrowthPriceId { get; set; } = string.Empty;
        public string ProPriceId { get; set; } = string.Empty;
    }

    public sealed class AzureBlobOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string TenantArchiveContainer { get; set; } = "tenant-archive";
    }

    public sealed class Microsoft365Options
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SupportMailbox { get; set; } = "support@runtira.com";
    }

    public sealed class AiOptions
    {
        public string Provider { get; set; } = "MicrosoftAgentFramework";
        public string ModelFast { get; set; } = "gpt-4.1-mini";
        public string ModelReasoning { get; set; } = "gpt-4.1";
    }

    public sealed class ResendOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "onboarding@resend.dev";
        public string FromName { get; set; } = "PropertySaaS";
        public string SupportEmail { get; set; } = "michelfopa@gmail.com";
    }
}

namespace Runtira.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly Guid? _tenantId;
        private readonly bool _bypassTenantFilter;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContextAccessor? tenantContextAccessor = null) : base(options)
        {
            _tenantId = tenantContextAccessor?.TenantId;
            _bypassTenantFilter = tenantContextAccessor?.BypassTenantFilter == true;
        }

        public DbSet<RuntiraOrganization> RuntiraOrganizations => Set<RuntiraOrganization>();
        public DbSet<RuntiraUser> RuntiraUsers => Set<RuntiraUser>();
        public DbSet<RuntiraMembership> RuntiraMemberships => Set<RuntiraMembership>();
        public DbSet<RuntiraAsset> RuntiraAssets => Set<RuntiraAsset>();
        public DbSet<RuntiraUnit> RuntiraUnits => Set<RuntiraUnit>();
        public DbSet<RuntiraResident> RuntiraResidents => Set<RuntiraResident>();
        public DbSet<RuntiraLease> RuntiraLeases => Set<RuntiraLease>();
        public DbSet<RuntiraLead> RuntiraLeads => Set<RuntiraLead>();
        public DbSet<RuntiraConversation> RuntiraConversations => Set<RuntiraConversation>();
        public DbSet<RuntiraMessage> RuntiraMessages => Set<RuntiraMessage>();
        public DbSet<RuntiraInboxMessage> RuntiraInboxMessages => Set<RuntiraInboxMessage>();
        public DbSet<RuntiraAttachment> RuntiraAttachments => Set<RuntiraAttachment>();
        public DbSet<RuntiraWorkflowTemplate> RuntiraWorkflowTemplates => Set<RuntiraWorkflowTemplate>();
        public DbSet<RuntiraBlobArchive> RuntiraBlobArchives => Set<RuntiraBlobArchive>();
        public DbSet<RuntiraJurisdictionProfile> RuntiraJurisdictionProfiles => Set<RuntiraJurisdictionProfile>();
        public DbSet<RuntiraQuotaPolicy> RuntiraQuotaPolicies => Set<RuntiraQuotaPolicy>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RuntiraOrganization>().HasIndex(x => x.Slug).IsUnique();
            modelBuilder.Entity<RuntiraUser>().HasIndex(x => x.Email).IsUnique();
            modelBuilder.Entity<RuntiraMembership>().HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            modelBuilder.Entity<RuntiraMembership>().HasOne(x => x.Tenant).WithMany(x => x.Memberships).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraMembership>().HasOne(x => x.User).WithMany(x => x.Memberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraAsset>().HasOne(x => x.Tenant).WithMany(x => x.Assets).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraUnit>().HasIndex(x => new { x.TenantId, x.AssetId, x.UnitCode }).IsUnique();
            modelBuilder.Entity<RuntiraUnit>().HasOne(x => x.Asset).WithMany(x => x.Units).HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraResident>().HasIndex(x => new { x.TenantId, x.Email });
            modelBuilder.Entity<RuntiraLease>().HasOne(x => x.Asset).WithMany(x => x.Leases).HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RuntiraLease>().HasOne(x => x.Unit).WithMany(x => x.Leases).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraLease>().HasOne(x => x.Resident).WithMany(x => x.Leases).HasForeignKey(x => x.ResidentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraLead>().HasOne(x => x.Asset).WithMany(x => x.Leads).HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<RuntiraConversation>().HasOne<RuntiraOrganization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraMessage>().HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraInboxMessage>().HasIndex(x => new { x.TenantId, x.ExternalMessageId }).IsUnique();
            modelBuilder.Entity<RuntiraAttachment>().HasOne(x => x.InboxMessage).WithMany(x => x.Attachments).HasForeignKey(x => x.InboxMessageId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraWorkflowTemplate>().HasOne<RuntiraOrganization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraBlobArchive>().HasOne<RuntiraOrganization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraJurisdictionProfile>().HasOne<RuntiraOrganization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraQuotaPolicy>().HasOne<RuntiraOrganization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RuntiraMembership>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraAsset>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraUnit>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraResident>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraLease>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraLead>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraConversation>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraMessage>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraInboxMessage>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraAttachment>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraWorkflowTemplate>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraBlobArchive>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraJurisdictionProfile>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraQuotaPolicy>().HasQueryFilter(x => _bypassTenantFilter || (_tenantId.HasValue && x.TenantId == _tenantId.Value));
            modelBuilder.Entity<RuntiraOrganization>().HasData(
                new RuntiraOrganization
                {
                    Id = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    Name = "Runtira Demo Alberta",
                    Slug = "demo-alberta",
                    OwnerEmail = "michelfopa@gmail.com",
                    DefaultLocale = "fr-CA",
                    CountryCode = "CA",
                    RegionCode = "AB",
                    TimeZone = "America/Edmonton",
                    LegalProfileJson = "{\"jurisdiction\":\"CA-AB\",\"supports\":[\"fr-CA\",\"en-CA\",\"es-MX\"]}",
                    AdditionalSettingsJson = "{\"tenantMode\":\"path\",\"archive\":\"blob\"}",
                    StripeCustomerId = string.Empty,
                    StripeSubscriptionId = string.Empty,
                    BillingPlan = "Trial",
                    IsActive = true,
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraOrganization
                {
                    Id = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    Name = "Runtira Demo Ontario",
                    Slug = "demo-ontario",
                    OwnerEmail = "michelfopa@gmail.com",
                    DefaultLocale = "en-CA",
                    CountryCode = "CA",
                    RegionCode = "ON",
                    TimeZone = "America/Toronto",
                    LegalProfileJson = "{\"jurisdiction\":\"CA-ON\",\"supports\":[\"en-CA\",\"fr-CA\"]}",
                    AdditionalSettingsJson = "{\"tenantMode\":\"path\",\"archive\":\"blob\"}",
                    StripeCustomerId = string.Empty,
                    StripeSubscriptionId = string.Empty,
                    BillingPlan = "Trial",
                    IsActive = true,
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraOrganization
                {
                    Id = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    Name = "Runtira Demo Texas",
                    Slug = "demo-texas",
                    OwnerEmail = "michelfopa@gmail.com",
                    DefaultLocale = "en-US",
                    CountryCode = "US",
                    RegionCode = "TX",
                    TimeZone = "America/Chicago",
                    LegalProfileJson = "{\"jurisdiction\":\"US-TX\",\"supports\":[\"en-US\",\"es-MX\"]}",
                    AdditionalSettingsJson = "{\"tenantMode\":\"path\",\"archive\":\"blob\"}",
                    StripeCustomerId = string.Empty,
                    StripeSubscriptionId = string.Empty,
                    BillingPlan = "Trial",
                    IsActive = true,
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraUser>().HasData(new RuntiraUser
            {
                Id = Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd"),
                ClerkUserId = "runtira_demo_owner",
                Email = "michelfopa@gmail.com",
                FullName = "Michel Fopa",
                PreferredLanguage = "fr-CA",
                IsSuperAdmin = true,
                IsActive = true,
                CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
            });
            modelBuilder.Entity<RuntiraMembership>().HasData(
                new RuntiraMembership
                {
                    Id = Guid.Parse("eeeeeeee-1111-2222-3333-ffffffffffff"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    UserId = Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd"),
                    Role = "Owner",
                    Status = "Active",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraMembership
                {
                    Id = Guid.Parse("efefefef-1111-2222-3333-f0f0f0f0f0f0"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    UserId = Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd"),
                    Role = "Owner",
                    Status = "Active",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraMembership
                {
                    Id = Guid.Parse("f1f1f1f1-1111-2222-3333-f2f2f2f2f2f2"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    UserId = Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd"),
                    Role = "Owner",
                    Status = "Active",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraAsset>().HasData(
                new RuntiraAsset
                {
                    Id = Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    Name = "1180 17 Ave SW · Atlas 50",
                    AssetType = "Property",
                    AddressLine1 = "1180 17 Ave SW",
                    City = "Calgary",
                    RegionCode = "AB",
                    CountryCode = "CA",
                    UnitCount = 50,
                    LegalProfileJson = "{\"requiredQuestions\":[\"address\",\"period\",\"monthlyRent\"]}",
                    AdditionalDataJson = "{\"source\":\"seed\"}",
                    WorkflowSummaryJson = "{\"status\":\"ready\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraAsset
                {
                    Id = Guid.Parse("13131313-aaaa-bbbb-cccc-242424242424"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    Name = "25 Carlton Street",
                    AssetType = "Property",
                    AddressLine1 = "25 Carlton Street",
                    City = "Toronto",
                    RegionCode = "ON",
                    CountryCode = "CA",
                    UnitCount = 20,
                    LegalProfileJson = "{\"requiredQuestions\":[\"propertyAddress\",\"billingPeriod\",\"tenantName\",\"monthlyRent\"]}",
                    AdditionalDataJson = "{\"source\":\"seed\"}",
                    WorkflowSummaryJson = "{\"status\":\"ready\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraAsset
                {
                    Id = Guid.Parse("15151515-aaaa-bbbb-cccc-262626262626"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    Name = "2400 McKinney Avenue",
                    AssetType = "Property",
                    AddressLine1 = "2400 McKinney Avenue",
                    City = "Dallas",
                    RegionCode = "TX",
                    CountryCode = "US",
                    UnitCount = 18,
                    LegalProfileJson = "{\"requiredQuestions\":[\"propertyAddress\",\"billingPeriod\",\"ownerName\",\"monthlyRent\"]}",
                    AdditionalDataJson = "{\"source\":\"seed\"}",
                    WorkflowSummaryJson = "{\"status\":\"ready\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            var albertaSeedUnits = Enumerable.Range(1, 50)
                .Select(index => new RuntiraUnit
                {
                    Id = Guid.Parse($"10000000-0000-0000-0000-{index:D12}"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    AssetId = Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"),
                    UnitCode = $"A-{100 + index}",
                    UnitType = index % 5 == 0 ? "Studio" : index % 2 == 0 ? "Apartment" : "Loft",
                    Status = index == 1 ? "Occupied" : index % 11 == 0 ? "Maintenance" : index % 3 == 0 ? "Reserved" : "Available",
                    MarketRent = 1750m + (index * 22m),
                    AdditionalDataJson = $"{{\"bedrooms\":{(index % 3) + 1},\"floor\":{((index - 1) / 10) + 1}}}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                })
                .ToArray();

            modelBuilder.Entity<RuntiraUnit>().HasData(
                albertaSeedUnits[0],
                albertaSeedUnits[1],
                albertaSeedUnits[2],
                albertaSeedUnits[3],
                albertaSeedUnits[4],
                albertaSeedUnits[5],
                albertaSeedUnits[6],
                albertaSeedUnits[7],
                albertaSeedUnits[8],
                albertaSeedUnits[9],
                albertaSeedUnits[10],
                albertaSeedUnits[11],
                albertaSeedUnits[12],
                albertaSeedUnits[13],
                albertaSeedUnits[14],
                albertaSeedUnits[15],
                albertaSeedUnits[16],
                albertaSeedUnits[17],
                albertaSeedUnits[18],
                albertaSeedUnits[19],
                albertaSeedUnits[20],
                albertaSeedUnits[21],
                albertaSeedUnits[22],
                albertaSeedUnits[23],
                albertaSeedUnits[24],
                albertaSeedUnits[25],
                albertaSeedUnits[26],
                albertaSeedUnits[27],
                albertaSeedUnits[28],
                albertaSeedUnits[29],
                albertaSeedUnits[30],
                albertaSeedUnits[31],
                albertaSeedUnits[32],
                albertaSeedUnits[33],
                albertaSeedUnits[34],
                albertaSeedUnits[35],
                albertaSeedUnits[36],
                albertaSeedUnits[37],
                albertaSeedUnits[38],
                albertaSeedUnits[39],
                albertaSeedUnits[40],
                albertaSeedUnits[41],
                albertaSeedUnits[42],
                albertaSeedUnits[43],
                albertaSeedUnits[44],
                albertaSeedUnits[45],
                albertaSeedUnits[46],
                albertaSeedUnits[47],
                albertaSeedUnits[48],
                albertaSeedUnits[49],
                new RuntiraUnit
                {
                    Id = Guid.Parse("23232323-aaaa-bbbb-cccc-454545454545"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    AssetId = Guid.Parse("13131313-aaaa-bbbb-cccc-242424242424"),
                    UnitCode = "1204",
                    UnitType = "Condo",
                    Status = "Occupied",
                    MarketRent = 3100m,
                    AdditionalDataJson = "{\"bedrooms\":1}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraUnit
                {
                    Id = Guid.Parse("25252525-aaaa-bbbb-cccc-474747474747"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    AssetId = Guid.Parse("15151515-aaaa-bbbb-cccc-262626262626"),
                    UnitCode = "8B",
                    UnitType = "Apartment",
                    Status = "Occupied",
                    MarketRent = 2800m,
                    AdditionalDataJson = "{\"bedrooms\":2}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraResident>().HasData(
                new RuntiraResident
                {
                    Id = Guid.Parse("27272727-aaaa-bbbb-cccc-494949494949"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    FullName = "Amelie Gagnon",
                    Email = "amelie.gagnon@example.com",
                    PhoneNumber = "+1-403-555-0147",
                    PreferredLanguage = "fr-CA",
                    Status = "Active",
                    NotesJson = "{\"leaseIntent\":\"renewal\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraResident
                {
                    Id = Guid.Parse("29292929-aaaa-bbbb-cccc-515151515151"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    FullName = "Lucas Martin",
                    Email = "lucas.martin@example.com",
                    PhoneNumber = "+1-416-555-0120",
                    PreferredLanguage = "en-CA",
                    Status = "Active",
                    NotesJson = "{\"preferredChannel\":\"email\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraResident
                {
                    Id = Guid.Parse("31313131-aaaa-bbbb-cccc-535353535353"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    FullName = "Maya Rodriguez",
                    Email = "maya.rodriguez@example.com",
                    PhoneNumber = "+1-214-555-0191",
                    PreferredLanguage = "es-MX",
                    Status = "Active",
                    NotesJson = "{\"moveInWindow\":\"2026-08\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraLease>().HasData(
                new RuntiraLease
                {
                    Id = Guid.Parse("41414141-aaaa-bbbb-cccc-616161616161"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    AssetId = Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"),
                    UnitId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    ResidentId = Guid.Parse("27272727-aaaa-bbbb-cccc-494949494949"),
                    LeaseStartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LeaseEndUtc = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    MonthlyRent = 2450m,
                    BillingPeriod = "Monthly",
                    Status = "Active",
                    TermsJson = "{\"deposit\":2450}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraLease
                {
                    Id = Guid.Parse("43434343-aaaa-bbbb-cccc-636363636363"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    AssetId = Guid.Parse("13131313-aaaa-bbbb-cccc-242424242424"),
                    UnitId = Guid.Parse("23232323-aaaa-bbbb-cccc-454545454545"),
                    ResidentId = Guid.Parse("29292929-aaaa-bbbb-cccc-515151515151"),
                    LeaseStartUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    LeaseEndUtc = new DateTime(2027, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                    MonthlyRent = 3100m,
                    BillingPeriod = "Monthly",
                    Status = "Active",
                    TermsJson = "{\"noticeDays\":60}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraLease
                {
                    Id = Guid.Parse("45454545-aaaa-bbbb-cccc-656565656565"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    AssetId = Guid.Parse("15151515-aaaa-bbbb-cccc-262626262626"),
                    UnitId = Guid.Parse("25252525-aaaa-bbbb-cccc-474747474747"),
                    ResidentId = Guid.Parse("31313131-aaaa-bbbb-cccc-535353535353"),
                    LeaseStartUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    LeaseEndUtc = new DateTime(2027, 4, 30, 0, 0, 0, DateTimeKind.Utc),
                    MonthlyRent = 2800m,
                    BillingPeriod = "Monthly",
                    Status = "Active",
                    TermsJson = "{\"lateFee\":75}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraLead>().HasData(
                new RuntiraLead
                {
                    Id = Guid.Parse("47474747-aaaa-bbbb-cccc-676767676767"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    AssetId = Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"),
                    FullName = "Nora Bouchard",
                    Email = "nora.bouchard@example.com",
                    PhoneNumber = "+1-403-555-0171",
                    Source = "InboxMock",
                    Status = "Qualified",
                    PreferredLanguage = "fr-CA",
                    QualificationScore = 92,
                    Summary = "Lead chaud pour un 2 chambres avec entrée en août.",
                    NotesJson = "{\"budget\":2500,\"moveInMonth\":\"2026-08\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraLead
                {
                    Id = Guid.Parse("49494949-aaaa-bbbb-cccc-696969696969"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    AssetId = Guid.Parse("13131313-aaaa-bbbb-cccc-242424242424"),
                    FullName = "Kevin Thompson",
                    Email = "kevin.thompson@example.com",
                    PhoneNumber = "+1-647-555-0112",
                    Source = "ImportCsv",
                    Status = "New",
                    PreferredLanguage = "en-CA",
                    QualificationScore = 78,
                    Summary = "Lead imported from listing export, interested in downtown condo.",
                    NotesJson = "{\"budget\":3200,\"petFriendly\":true}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraLead
                {
                    Id = Guid.Parse("51515151-aaaa-bbbb-cccc-717171717171"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    AssetId = Guid.Parse("15151515-aaaa-bbbb-cccc-262626262626"),
                    FullName = "Elena Perez",
                    Email = "elena.perez@example.com",
                    PhoneNumber = "+1-214-555-0126",
                    Source = "Manual",
                    Status = "Pending",
                    PreferredLanguage = "es-MX",
                    QualificationScore = 84,
                    Summary = "Lead bilingue demandant un logement proche du centre-ville.",
                    NotesJson = "{\"budget\":2900,\"preferredLanguage\":\"es-MX\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraConversation>().HasData(
                new RuntiraConversation
                {
                    Id = Guid.Parse("33333333-aaaa-bbbb-cccc-444444444444"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    Channel = "Chat",
                    Subject = "Créer une facture mensuelle Alberta",
                    Locale = "fr-CA",
                    Status = "Open",
                    Intent = "CreateInvoice",
                    JurisdictionCode = "CA-AB",
                    LastMessageUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
                    SummaryJson = "{\"nextQuestion\":\"Quel mois doit apparaître sur la facture PDF ?\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraConversation
                {
                    Id = Guid.Parse("35353535-aaaa-bbbb-cccc-464646464646"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    Channel = "Chat",
                    Subject = "Create an Ontario monthly invoice",
                    Locale = "en-CA",
                    Status = "Open",
                    Intent = "CreateInvoice",
                    JurisdictionCode = "CA-ON",
                    LastMessageUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
                    SummaryJson = "{\"nextQuestion\":\"What tenant name should appear on the invoice?\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraConversation
                {
                    Id = Guid.Parse("37373737-aaaa-bbbb-cccc-484848484848"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    Channel = "Chat",
                    Subject = "Create a Texas invoice draft",
                    Locale = "en-US",
                    Status = "Open",
                    Intent = "CreateInvoice",
                    JurisdictionCode = "US-TX",
                    LastMessageUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
                    SummaryJson = "{\"nextQuestion\":\"Which owner name should appear on the invoice?\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraMessage>().HasData(
                new RuntiraMessage
                {
                    Id = Guid.Parse("55555555-aaaa-bbbb-cccc-666666666666"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    ConversationId = Guid.Parse("33333333-aaaa-bbbb-cccc-444444444444"),
                    Direction = "Incoming",
                    AuthorType = "User",
                    Content = "Crée la facture PDF de juillet pour le 1180 17 Ave SW.",
                    StructuredPayloadJson = "{\"intent\":\"CreateInvoice\"}",
                    RequiresAction = true,
                    CreatedByEmail = "michelfopa@gmail.com",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraMessage
                {
                    Id = Guid.Parse("57575757-aaaa-bbbb-cccc-686868686868"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    ConversationId = Guid.Parse("35353535-aaaa-bbbb-cccc-464646464646"),
                    Direction = "Incoming",
                    AuthorType = "User",
                    Content = "Create the July invoice for 25 Carlton Street.",
                    StructuredPayloadJson = "{\"intent\":\"CreateInvoice\"}",
                    RequiresAction = true,
                    CreatedByEmail = "michelfopa@gmail.com",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraMessage
                {
                    Id = Guid.Parse("59595959-aaaa-bbbb-cccc-707070707070"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    ConversationId = Guid.Parse("37373737-aaaa-bbbb-cccc-484848484848"),
                    Direction = "Incoming",
                    AuthorType = "User",
                    Content = "Create the monthly invoice draft for 2400 McKinney Avenue.",
                    StructuredPayloadJson = "{\"intent\":\"CreateInvoice\"}",
                    RequiresAction = true,
                    CreatedByEmail = "michelfopa@gmail.com",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraWorkflowTemplate>().HasData(
                new RuntiraWorkflowTemplate
                {
                    Id = Guid.Parse("77777777-aaaa-bbbb-cccc-888888888888"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    Name = "Create invoice draft for CA-AB",
                    TriggerType = "CreateInvoice",
                    Description = "Collecte les champs requis du profil CA-AB et prépare une facture PDF envoyable.",
                    PromptTemplate = "Demande les champs requis par la juridiction active avant génération.",
                    RequiredQuestionsJson = "[\"propertyAddress\",\"billingPeriod\",\"monthlyRent\"]",
                    ValidationSchemaJson = "{\"monthlyRent\":{\"min\":1}}",
                    IsActive = true,
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraWorkflowTemplate
                {
                    Id = Guid.Parse("79797979-aaaa-bbbb-cccc-909090909090"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    Name = "Create invoice draft for CA-ON",
                    TriggerType = "CreateInvoice",
                    Description = "Collects the Ontario-required invoice fields before generation.",
                    PromptTemplate = "Ask for tenant and billing details required by the active jurisdiction.",
                    RequiredQuestionsJson = "[\"propertyAddress\",\"billingPeriod\",\"tenantName\",\"monthlyRent\"]",
                    ValidationSchemaJson = "{\"monthlyRent\":{\"min\":1},\"tenantName\":{\"required\":true}}",
                    IsActive = true,
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraWorkflowTemplate
                {
                    Id = Guid.Parse("81818181-aaaa-bbbb-cccc-929292929292"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    Name = "Create invoice draft for US-TX",
                    TriggerType = "CreateInvoice",
                    Description = "Collects the Texas-required invoice fields before generation.",
                    PromptTemplate = "Ask for owner, billing period and property details required by the active jurisdiction.",
                    RequiredQuestionsJson = "[\"propertyAddress\",\"billingPeriod\",\"ownerName\",\"monthlyRent\"]",
                    ValidationSchemaJson = "{\"monthlyRent\":{\"min\":1},\"ownerName\":{\"required\":true}}",
                    IsActive = true,
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraBlobArchive>().HasData(
                new RuntiraBlobArchive
                {
                    Id = Guid.Parse("99999999-aaaa-bbbb-cccc-000000000000"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    BlobPath = "demo-alberta/invoices/2026/07/invoice-july.json",
                    ContentType = "application/json",
                    Category = "InvoiceDraft",
                    MetadataJson = "{\"period\":\"2026-07\"}",
                    SizeBytes = 512,
                    SourceSystem = "seed",
                    Hash = "seed-demo-alberta-invoice",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraBlobArchive
                {
                    Id = Guid.Parse("a1a1a1a1-aaaa-bbbb-cccc-020202020202"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    BlobPath = "demo-ontario/invoices/2026/07/invoice-july.json",
                    ContentType = "application/json",
                    Category = "InvoiceDraft",
                    MetadataJson = "{\"period\":\"2026-07\"}",
                    SizeBytes = 544,
                    SourceSystem = "seed",
                    Hash = "seed-demo-ontario-invoice",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraBlobArchive
                {
                    Id = Guid.Parse("a3a3a3a3-aaaa-bbbb-cccc-040404040404"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    BlobPath = "demo-texas/invoices/2026/07/invoice-july.json",
                    ContentType = "application/json",
                    Category = "InvoiceDraft",
                    MetadataJson = "{\"period\":\"2026-07\"}",
                    SizeBytes = 536,
                    SourceSystem = "seed",
                    Hash = "seed-demo-texas-invoice",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraInboxMessage>().HasData(
                new RuntiraInboxMessage
                {
                    Id = Guid.Parse("62626262-aaaa-bbbb-cccc-848484848484"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    ExternalMessageId = "mock-ab-001",
                    Provider = "MockMicrosoft365",
                    FromEmail = "prospect.ab@example.com",
                    Subject = "Disponibilité pour août",
                    PreviewText = "Bonjour, je cherche un 2 chambres disponible en août à Calgary.",
                    ReceivedUtc = new DateTime(2026, 7, 3, 14, 0, 0, DateTimeKind.Utc),
                    Status = "Classified",
                    Category = "Lead",
                    RelatedEntityType = "Lead",
                    RelatedEntityId = Guid.Parse("47474747-aaaa-bbbb-cccc-676767676767"),
                    HasAttachments = true,
                    ClassificationJson = "{\"confidence\":0.92,\"suggestedAction\":\"CallBack\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 14, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraInboxMessage
                {
                    Id = Guid.Parse("64646464-aaaa-bbbb-cccc-868686868686"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    ExternalMessageId = "mock-on-001",
                    Provider = "MockMicrosoft365",
                    FromEmail = "resident.on@example.com",
                    Subject = "Need invoice copy for July",
                    PreviewText = "Can you resend the July invoice for unit 1204?",
                    ReceivedUtc = new DateTime(2026, 7, 3, 15, 0, 0, DateTimeKind.Utc),
                    Status = "Classified",
                    Category = "Invoice",
                    RelatedEntityType = "Lease",
                    RelatedEntityId = Guid.Parse("43434343-aaaa-bbbb-cccc-636363636363"),
                    HasAttachments = false,
                    ClassificationJson = "{\"confidence\":0.88,\"suggestedAction\":\"SendInvoice\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 15, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraInboxMessage
                {
                    Id = Guid.Parse("66666666-aaaa-bbbb-cccc-888888888889"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    ExternalMessageId = "mock-tx-001",
                    Provider = "MockMicrosoft365",
                    FromEmail = "owner.tx@example.com",
                    Subject = "Please archive this vendor quote",
                    PreviewText = "Attaching a quote for the next lease renewal package.",
                    ReceivedUtc = new DateTime(2026, 7, 3, 16, 0, 0, DateTimeKind.Utc),
                    Status = "Classified",
                    Category = "Document",
                    RelatedEntityType = "Asset",
                    RelatedEntityId = Guid.Parse("15151515-aaaa-bbbb-cccc-262626262626"),
                    HasAttachments = true,
                    ClassificationJson = "{\"confidence\":0.81,\"suggestedAction\":\"ArchiveDocument\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 16, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraAttachment>().HasData(
                new RuntiraAttachment
                {
                    Id = Guid.Parse("68686868-aaaa-bbbb-cccc-909090909091"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    InboxMessageId = Guid.Parse("62626262-aaaa-bbbb-cccc-848484848484"),
                    FileName = "budget-range.txt",
                    ContentType = "text/plain",
                    SizeBytes = 256,
                    Category = "LeadDocument",
                    MetadataJson = "{\"source\":\"mock-inbox\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 14, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraAttachment
                {
                    Id = Guid.Parse("70707070-aaaa-bbbb-cccc-929292929293"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    InboxMessageId = Guid.Parse("66666666-aaaa-bbbb-cccc-888888888889"),
                    FileName = "vendor-quote.pdf",
                    ContentType = "application/pdf",
                    SizeBytes = 40960,
                    Category = "VendorQuote",
                    MetadataJson = "{\"source\":\"mock-inbox\"}",
                    CreatedUtc = new DateTime(2026, 7, 3, 16, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraJurisdictionProfile>().HasData(
                new RuntiraJurisdictionProfile
                {
                    Id = Guid.Parse("12121212-aaaa-bbbb-cccc-343434343434"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    CountryCode = "CA",
                    RegionCode = "AB",
                    SupportedLanguagesJson = "[\"fr-CA\",\"en-CA\",\"es-MX\"]",
                    RequiredQuestionsJson = "[\"propertyAddress\",\"billingPeriod\",\"monthlyRent\"]",
                    ValidationRulesJson = "{\"billingPeriod\":{\"required\":true}}",
                    InvoiceRulesJson = "{\"generatePdf\":true,\"addAutomaticSalesTax\":false}",
                    AssetRulesJson = "{\"supportsMultiUnit\":true}",
                    MaintenanceRulesJson = "{\"supportInboxClassification\":true}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraJurisdictionProfile
                {
                    Id = Guid.Parse("14141414-aaaa-bbbb-cccc-363636363636"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    CountryCode = "CA",
                    RegionCode = "ON",
                    SupportedLanguagesJson = "[\"en-CA\",\"fr-CA\"]",
                    RequiredQuestionsJson = "[\"propertyAddress\",\"billingPeriod\",\"tenantName\",\"monthlyRent\"]",
                    ValidationRulesJson = "{\"billingPeriod\":{\"required\":true},\"tenantName\":{\"required\":true}}",
                    InvoiceRulesJson = "{\"generatePdf\":true,\"addAutomaticSalesTax\":false}",
                    AssetRulesJson = "{\"supportsMultiUnit\":true}",
                    MaintenanceRulesJson = "{\"supportInboxClassification\":true}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraJurisdictionProfile
                {
                    Id = Guid.Parse("16161616-aaaa-bbbb-cccc-383838383838"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    CountryCode = "US",
                    RegionCode = "TX",
                    SupportedLanguagesJson = "[\"en-US\",\"es-MX\"]",
                    RequiredQuestionsJson = "[\"propertyAddress\",\"billingPeriod\",\"ownerName\",\"monthlyRent\"]",
                    ValidationRulesJson = "{\"billingPeriod\":{\"required\":true},\"ownerName\":{\"required\":true}}",
                    InvoiceRulesJson = "{\"generatePdf\":true,\"addAutomaticSalesTax\":false}",
                    AssetRulesJson = "{\"supportsMultiUnit\":true}",
                    MaintenanceRulesJson = "{\"supportInboxClassification\":true}",
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            modelBuilder.Entity<RuntiraQuotaPolicy>().HasData(
                new RuntiraQuotaPolicy
                {
                    Id = Guid.Parse("56565656-aaaa-bbbb-cccc-787878787878"),
                    TenantId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    MaxAssets = 100,
                    MaxDocuments = 1000,
                    MaxMonthlyAiRequests = 5000,
                    MaxBlobStorageMb = 2048,
                    MaxActiveWorkflows = 50,
                    EnforceHardLimit = true,
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraQuotaPolicy
                {
                    Id = Guid.Parse("58585858-aaaa-bbbb-cccc-808080808080"),
                    TenantId = Guid.Parse("abababab-1111-2222-3333-bcbcbcbcbcbc"),
                    MaxAssets = 100,
                    MaxDocuments = 1000,
                    MaxMonthlyAiRequests = 5000,
                    MaxBlobStorageMb = 2048,
                    MaxActiveWorkflows = 50,
                    EnforceHardLimit = true,
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new RuntiraQuotaPolicy
                {
                    Id = Guid.Parse("60606060-aaaa-bbbb-cccc-828282828282"),
                    TenantId = Guid.Parse("acacacac-1111-2222-3333-bdbdbdbdbdbd"),
                    MaxAssets = 100,
                    MaxDocuments = 1000,
                    MaxMonthlyAiRequests = 5000,
                    MaxBlobStorageMb = 2048,
                    MaxActiveWorkflows = 50,
                    EnforceHardLimit = true,
                    CreatedUtc = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)
                });
            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedUtc = DateTime.UtcNow;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddUserSecrets<ApplicationDbContext>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = SqlConnectionStringResolver.Resolve(configuration);
            optionsBuilder.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }

    internal static class SqlConnectionStringResolver
    {
        private const string RuntiraConnectionStringName = "RuntiraDb";
        private const string PropertyConnectionStringName = "PropertyDb";
        private const string RuntiraPasswordConfigurationKey = "ConnectionStrings:RuntiraDbPassword";
        private const string PropertyPasswordConfigurationKey = "ConnectionStrings:PropertyDbPassword";

        public static string Resolve(IConfiguration configuration)
        {
            var configuredConnectionString = configuration.GetConnectionString(RuntiraConnectionStringName)
                ?? configuration.GetConnectionString(PropertyConnectionStringName);

            if (string.IsNullOrWhiteSpace(configuredConnectionString))
            {
                throw new InvalidOperationException($"Connection string '{RuntiraConnectionStringName}' or '{PropertyConnectionStringName}' is not configured.");
            }

            var builder = new SqlConnectionStringBuilder(configuredConnectionString);
            var password = configuration[RuntiraPasswordConfigurationKey] ?? configuration[PropertyPasswordConfigurationKey];

            if (!string.IsNullOrWhiteSpace(password))
            {
                builder.Password = password;
            }

            builder.Encrypt = true;
            builder.TrustServerCertificate = false;

            return builder.ConnectionString;
        }
    }

    internal sealed class ApplicationDbContextLease : IApplicationDbContextLease
    {
        private readonly ApplicationDbContext _dbContext;

        public ApplicationDbContextLease(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IApplicationDbContext DbContext => _dbContext;

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }

    internal sealed class ApplicationDbContextFactoryAdapter : IApplicationDbContextFactory
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;

        public ApplicationDbContextFactoryAdapter(IDbContextFactory<ApplicationDbContext> factory)
        {
            _factory = factory;
        }

        public IApplicationDbContextLease CreateDbContext()
            => new ApplicationDbContextLease(_factory.CreateDbContext());
    }
}

namespace Runtira.Infrastructure.Services
{
    using System.Globalization;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text.Json.Serialization;
    using Runtira.Application.Common;
    using Runtira.Application.Features;
    using Runtira.Infrastructure.Data;
    using Runtira.Infrastructure.Options;

    internal sealed class ResendEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly ResendOptions _options;
        private readonly ILogger<ResendEmailService> _logger;

        public ResendEmailService(HttpClient httpClient, Microsoft.Extensions.Options.IOptions<ResendOptions> options, ILogger<ResendEmailService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string html, string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("Resend API key is not configured. Email to {Recipient} was skipped.", to);
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
            {
                Content = JsonContent.Create(new ResendEmailRequest
                {
                    From = string.IsNullOrWhiteSpace(_options.FromName) ? _options.FromEmail : $"{_options.FromName} <{_options.FromEmail}>",
                    To = new[] { to },
                    Subject = subject,
                    Html = html,
                    Text = text
                })
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Resend email failed with status {StatusCode}: {Body}", response.StatusCode, body);
            }
        }


        private sealed class ResendEmailRequest
        {
            [JsonPropertyName("from")]
            public string From { get; set; } = string.Empty;

            [JsonPropertyName("to")]
            public string[] To { get; set; } = Array.Empty<string>();

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("html")]
            public string Html { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }
    }

    public sealed class StripePlanPriceDto
    {
        public string Plan { get; set; } = string.Empty;
        public string PriceId { get; set; } = string.Empty;
        public string Currency { get; set; } = "usd";
        public decimal UnitAmount { get; set; }
        public string Interval { get; set; } = "month";
        public string DisplayPrice { get; set; } = string.Empty;
    }

    public sealed class RuntiraBillingPlanDefinition
    {
        public string Plan { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public long UnitAmount { get; set; }
        public string Currency { get; set; } = "usd";
        public string Interval { get; set; } = "month";
    }

    public sealed class StripeBillingService
    {
        private readonly StripeOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<StripeBillingService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly IReadOnlyList<RuntiraBillingPlanDefinition> Plans = new[]
        {
            new RuntiraBillingPlanDefinition { Plan = "Starter", ProductName = "Runtira Starter", ProductDescription = "Solo AI-first workspace", UnitAmount = 4900, Currency = "usd", Interval = "month" },
            new RuntiraBillingPlanDefinition { Plan = "Growth", ProductName = "Runtira Growth", ProductDescription = "Growing multi-tenant workspace", UnitAmount = 12900, Currency = "usd", Interval = "month" },
            new RuntiraBillingPlanDefinition { Plan = "Pro", ProductName = "Runtira Pro", ProductDescription = "Advanced operations and admin coverage", UnitAmount = 24900, Currency = "usd", Interval = "month" }
        };

        public StripeBillingService(HttpClient httpClient, StripeOptions options, ApplicationDbContext db, ILogger<StripeBillingService> logger)
        {
            _httpClient = httpClient;
            _options = options;
            _db = db;
            _logger = logger;
        }

        public async Task<List<StripePlanPriceDto>> GetPlanPricesAsync()
        {
            var results = new List<StripePlanPriceDto>();
            foreach (var plan in Plans)
            {
                var priceId = await EnsurePriceAsync(plan);
                results.Add(new StripePlanPriceDto
                {
                    Plan = plan.Plan,
                    PriceId = priceId,
                    Currency = plan.Currency,
                    UnitAmount = plan.UnitAmount / 100m,
                    Interval = plan.Interval,
                    DisplayPrice = FormatDisplayPrice(plan.UnitAmount, plan.Currency, plan.Interval)
                });
            }

            return results;
        }

        public async Task<string> CreateCheckoutSessionAsync(Runtira.Domain.Entities.RuntiraOrganization organization, string plan, string successUrl, string cancelUrl)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return cancelUrl;
            }

            var planDefinition = Plans.FirstOrDefault(x => string.Equals(x.Plan, plan, StringComparison.OrdinalIgnoreCase)) ?? Plans[1];
            var priceId = await EnsurePriceAsync(planDefinition);
            if (string.IsNullOrWhiteSpace(priceId))
            {
                return cancelUrl;
            }

            using var request = CreateRequest(HttpMethod.Post, "checkout/sessions");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["mode"] = "subscription",
                ["success_url"] = successUrl,
                ["cancel_url"] = cancelUrl,
                ["client_reference_id"] = organization.Id.ToString(),
                ["customer_email"] = organization.OwnerEmail,
                ["metadata[tenantId]"] = organization.Id.ToString(),
                ["metadata[plan]"] = planDefinition.Plan,
                ["line_items[0][price]"] = priceId,
                ["line_items[0][quantity]"] = "1"
            });

            using var response = await _httpClient.SendAsync(request);
            var session = await ReadAsync<StripeCheckoutSessionResponse>(response);
            return string.IsNullOrWhiteSpace(session.Url) ? cancelUrl : session.Url;
        }

        public async Task<string> CreateBillingPortalAsync(string customerId, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey) || string.IsNullOrWhiteSpace(customerId))
            {
                return returnUrl;
            }

            using var request = CreateRequest(HttpMethod.Post, "billing_portal/sessions");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["customer"] = customerId,
                ["return_url"] = returnUrl
            });

            using var response = await _httpClient.SendAsync(request);
            var session = await ReadAsync<StripePortalSessionResponse>(response);
            return string.IsNullOrWhiteSpace(session.Url) ? returnUrl : session.Url;
        }

        public async Task HandleWebhookAsync(string json, string? stripeSignature)
        {
            _logger.LogInformation("Received Runtira Stripe webhook. Signature present: {HasSignature}", !string.IsNullOrWhiteSpace(stripeSignature));
            var stripeEvent = JsonSerializer.Deserialize<StripeWebhookEvent>(json, JsonOptions);
            if (stripeEvent is null)
            {
                return;
            }

            var organization = await ResolveOrganizationForEventAsync(stripeEvent);
            if (organization is null)
            {
                return;
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    if (stripeEvent.Data?.Object?.Deserialize<StripeCheckoutSessionResponse>() is { } checkoutSession)
                    {
                        organization.StripeCustomerId = checkoutSession.Customer ?? organization.StripeCustomerId;
                        organization.StripeSubscriptionId = checkoutSession.Subscription ?? organization.StripeSubscriptionId;
                        organization.BillingPlan = checkoutSession.Metadata?.TryGetValue("plan", out var checkoutPlan) == true ? checkoutPlan : organization.BillingPlan;
                    }
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                    if (stripeEvent.Data?.Object?.Deserialize<StripeSubscriptionResponse>() is { } subscription)
                    {
                        organization.StripeCustomerId = subscription.Customer ?? organization.StripeCustomerId;
                        organization.StripeSubscriptionId = subscription.Id ?? organization.StripeSubscriptionId;
                        organization.BillingPlan = ResolvePlanFromPriceId(subscription.Items?.Data?.FirstOrDefault()?.Price?.Id);
                        organization.IsActive = subscription.Status is "active" or "trialing" or "past_due";
                    }
                    break;

                case "customer.subscription.deleted":
                    organization.StripeSubscriptionId = string.Empty;
                    organization.BillingPlan = "Trial";
                    break;
            }

            await _db.SaveChangesAsync();
        }

        private async Task<string> EnsurePriceAsync(RuntiraBillingPlanDefinition plan)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return string.Empty;
            }

            var configuredPriceId = GetConfiguredPriceId(plan.Plan);
            if (!string.IsNullOrWhiteSpace(configuredPriceId))
            {
                return configuredPriceId;
            }

            var productId = await EnsureProductAsync(plan);
            if (string.IsNullOrWhiteSpace(productId))
            {
                return string.Empty;
            }

            using var request = CreateRequest(HttpMethod.Post, "prices");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["currency"] = plan.Currency,
                ["unit_amount"] = plan.UnitAmount.ToString(CultureInfo.InvariantCulture),
                ["product"] = productId,
                ["recurring[interval]"] = plan.Interval,
                ["metadata[plan]"] = plan.Plan,
                ["nickname"] = $"{plan.Plan} monthly"
            });

            using var response = await _httpClient.SendAsync(request);
            var price = await ReadAsync<StripePriceResponse>(response);
            return price.Id ?? string.Empty;
        }

        private async Task<string> EnsureProductAsync(RuntiraBillingPlanDefinition plan)
        {
            using var request = CreateRequest(HttpMethod.Post, "products");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["name"] = plan.ProductName,
                ["description"] = plan.ProductDescription,
                ["metadata[plan]"] = plan.Plan
            });

            using var response = await _httpClient.SendAsync(request);
            var product = await ReadAsync<StripeProductResponse>(response);
            return product.Id ?? string.Empty;
        }

        private string GetConfiguredPriceId(string plan)
            => plan.ToLowerInvariant() switch
            {
                "starter" => _options.StarterPriceId,
                "growth" => _options.GrowthPriceId,
                "pro" => _options.ProPriceId,
                _ => string.Empty
            };

        private async Task<Runtira.Domain.Entities.RuntiraOrganization?> ResolveOrganizationForEventAsync(StripeWebhookEvent stripeEvent)
        {
            return stripeEvent.Type switch
            {
                "checkout.session.completed" when stripeEvent.Data?.Object?.Deserialize<StripeCheckoutSessionResponse>() is { } checkoutSession
                    => await ResolveOrganizationAsync(checkoutSession.Customer, checkoutSession.Subscription, checkoutSession.ClientReferenceId),
                "customer.subscription.created" or "customer.subscription.updated" or "customer.subscription.deleted" when stripeEvent.Data?.Object?.Deserialize<StripeSubscriptionResponse>() is { } subscription
                    => await ResolveOrganizationAsync(subscription.Customer, subscription.Id, null),
                _ => null
            };
        }

        private async Task<Runtira.Domain.Entities.RuntiraOrganization?> ResolveOrganizationAsync(string? customerId, string? subscriptionId, string? tenantReference)
        {
            if (Guid.TryParse(tenantReference, out var tenantId))
            {
                var byTenant = await _db.RuntiraOrganizations.FirstOrDefaultAsync(x => x.Id == tenantId);
                if (byTenant is not null)
                {
                    return byTenant;
                }
            }

            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                var bySubscription = await _db.RuntiraOrganizations.FirstOrDefaultAsync(x => x.StripeSubscriptionId == subscriptionId);
                if (bySubscription is not null)
                {
                    return bySubscription;
                }
            }

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                return await _db.RuntiraOrganizations.FirstOrDefaultAsync(x => x.StripeCustomerId == customerId);
            }

            return null;
        }

        private static string ResolvePlanFromPriceId(string? priceId)
        {
            if (string.IsNullOrWhiteSpace(priceId))
            {
                return "Trial";
            }

            return Plans.FirstOrDefault(x => string.Equals(x.Plan, priceId, StringComparison.OrdinalIgnoreCase))?.Plan ?? "Paid";
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
            return request;
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response) where T : class, new()
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, JsonOptions) ?? new T();
        }

        private static string FormatDisplayPrice(long unitAmount, string currency, string interval)
            => $"{(unitAmount / 100m).ToString("C", CultureInfo.GetCultureInfo("en-US"))}/{interval}";

        private sealed class StripeCheckoutSessionResponse
        {
            public string? Url { get; set; }
            public string? Customer { get; set; }
            public string? Subscription { get; set; }
            public string? ClientReferenceId { get; set; }
            public Dictionary<string, string>? Metadata { get; set; }
        }

        private sealed class StripePortalSessionResponse
        {
            public string? Url { get; set; }
        }

        private sealed class StripePriceResponse
        {
            public string? Id { get; set; }
        }

        private sealed class StripeProductResponse
        {
            public string? Id { get; set; }
        }

        private sealed class StripeWebhookEvent
        {
            public string? Type { get; set; }
            public StripeEventData? Data { get; set; }
        }

        private sealed class StripeEventData
        {
            public JsonElement? Object { get; set; }
        }

        private sealed class StripeSubscriptionResponse
        {
            public string? Id { get; set; }
            public string? Customer { get; set; }
            public string? Status { get; set; }
            public StripeSubscriptionItems? Items { get; set; }
        }

        private sealed class StripeSubscriptionItems
        {
            public List<StripeSubscriptionItem> Data { get; set; } = new();
        }

        private sealed class StripeSubscriptionItem
        {
            public StripeSubscriptionPrice? Price { get; set; }
        }

        private sealed class StripeSubscriptionPrice
        {
            public string? Id { get; set; }
        }
    }

    public sealed class JsonLegislationCatalog : ILegislationCatalog
    {
        private readonly IReadOnlyDictionary<string, Runtira.Application.Features.RuntiraLegislationProfileDto> _profiles;

        public JsonLegislationCatalog(IConfiguration configuration)
        {
            var rootPath = configuration["Legislation:RootPath"];
            var basePath = AppContext.BaseDirectory;
            var resolvedRoot = string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(basePath, "Legislation")
                : Path.IsPathRooted(rootPath) ? rootPath : Path.GetFullPath(Path.Combine(basePath, rootPath));

            var profiles = new Dictionary<string, Runtira.Application.Features.RuntiraLegislationProfileDto>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(resolvedRoot))
            {
                foreach (var file in Directory.EnumerateFiles(resolvedRoot, "*.json", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<Runtira.Application.Features.RuntiraLegislationProfileDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (profile is null || string.IsNullOrWhiteSpace(profile.CountryCode) || string.IsNullOrWhiteSpace(profile.RegionCode))
                    {
                        continue;
                    }

                    profiles[$"{profile.CountryCode}:{profile.RegionCode}"] = profile;
                }
            }

            _profiles = profiles;
        }

        public Runtira.Application.Features.RuntiraLegislationProfileDto? GetProfile(string countryCode, string regionCode)
            => _profiles.TryGetValue($"{countryCode}:{regionCode}", out var profile) ? profile : null;
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRuntiraInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var clerkOptions = new ClerkOptions();
            configuration.GetSection("Clerk").Bind(clerkOptions);
            services.AddSingleton(clerkOptions);

            var stripeOptions = new StripeOptions();
            configuration.GetSection("Stripe").Bind(stripeOptions);
            services.AddSingleton(stripeOptions);

            var blobOptions = new AzureBlobOptions();
            configuration.GetSection("AzureBlob").Bind(blobOptions);
            services.AddSingleton(blobOptions);

            var microsoft365Options = new Microsoft365Options();
            configuration.GetSection("Microsoft365").Bind(microsoft365Options);
            services.AddSingleton(microsoft365Options);

            var aiOptions = new AiOptions();
            configuration.GetSection("AI").Bind(aiOptions);
            services.AddSingleton(aiOptions);

            services.Configure<ResendOptions>(configuration.GetSection("Resend"));
            services.AddHttpClient<IEmailService, ResendEmailService>(client => client.BaseAddress = new Uri("https://api.resend.com/"));

            var connectionString = SqlConnectionStringResolver.Resolve(configuration);
            services.AddDbContextFactory<ApplicationDbContext>((provider, options) =>
            {
                options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
            }, ServiceLifetime.Scoped);
            services.AddDbContext<ApplicationDbContext>((provider, options) =>
            {
                options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
            });
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IApplicationDbContextFactory, ApplicationDbContextFactoryAdapter>();
            services.AddSingleton<ILegislationCatalog, JsonLegislationCatalog>();
            services.AddHttpClient<StripeBillingService>(client => client.BaseAddress = new Uri("https://api.stripe.com/v1/"));

            return services;
        }
    }
}
