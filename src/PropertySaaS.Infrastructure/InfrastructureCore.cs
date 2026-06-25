using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertySaaS.Application.Abstractions;
using PropertySaaS.Domain.Common;
using PropertySaaS.Domain.Entities;
using PropertySaaS.Domain.Enums;

namespace PropertySaaS.Infrastructure.Options
{
    public class ClerkOptions
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

    public class StripeOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string StarterPriceId { get; set; } = string.Empty;
        public string GrowthPriceId { get; set; } = string.Empty;
        public string ProPriceId { get; set; } = string.Empty;
    }
}

namespace PropertySaaS.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
        public DbSet<OrganizationInvitation> OrganizationInvitations => Set<OrganizationInvitation>();
        public DbSet<Property> Properties => Set<Property>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Lease> Leases => Set<Lease>();
        public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ComplianceReminder> ComplianceReminders => Set<ComplianceReminder>();
        public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
        public DbSet<Vendor> Vendors => Set<Vendor>();
        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Showing> Showings => Set<Showing>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<PaymentEntry> PaymentEntries => Set<PaymentEntry>();
        public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
        public DbSet<AISuggestionLog> AISuggestionLogs => Set<AISuggestionLog>();
        public DbSet<TenantConversation> TenantConversations => Set<TenantConversation>();
        public DbSet<TenantMessage> TenantMessages => Set<TenantMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organization>().HasIndex(x => x.Slug).IsUnique();
            modelBuilder.Entity<AppUser>().HasOne(x => x.Organization).WithMany(x => x.Users).HasForeignKey(x => x.OrganizationId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OrganizationMembership>().HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
            modelBuilder.Entity<OrganizationMembership>().HasOne(x => x.Organization).WithMany(x => x.Memberships).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrganizationMembership>().HasOne(x => x.User).WithMany(x => x.Memberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrganizationInvitation>().HasIndex(x => x.Token).IsUnique();
            modelBuilder.Entity<OrganizationInvitation>().HasOne(x => x.Organization).WithMany(x => x.Invitations).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Property>().HasMany(x => x.Units).WithOne(x => x.Property).HasForeignKey(x => x.PropertyId);
            modelBuilder.Entity<Property>().Property(x => x.MonthlyRevenueTarget).HasPrecision(18, 2);
            modelBuilder.Entity<Unit>().Property(x => x.MonthlyRent).HasPrecision(18, 2);
            modelBuilder.Entity<Lease>().HasOne(x => x.Unit).WithMany(x => x.Leases).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Lease>().HasOne(x => x.Tenant).WithMany(x => x.Leases).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Lease>().Property(x => x.MonthlyRent).HasPrecision(18, 2);
            modelBuilder.Entity<TenantConversation>().HasOne(x => x.Tenant).WithMany(x => x.Conversations).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TenantConversation>().HasOne(x => x.Lease).WithMany().HasForeignKey(x => x.LeaseId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TenantConversation>().HasOne(x => x.MaintenanceRequest).WithMany().HasForeignKey(x => x.MaintenanceRequestId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TenantMessage>().HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.TenantConversationId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TenantMessage>().Property(x => x.DeliveryMethod).HasDefaultValue(string.Empty);
            modelBuilder.Entity<TenantMessage>().Property(x => x.DeliveryProof).HasDefaultValue(string.Empty);
            modelBuilder.Entity<MediaAsset>().Property(x => x.DocumentType).HasDefaultValue(string.Empty);
            modelBuilder.Entity<Vendor>().Property(x => x.DispatchStatus).HasDefaultValue("Available");
            modelBuilder.Entity<Vendor>().Property(x => x.PreferredForPriority).HasDefaultValue(string.Empty);
            modelBuilder.Entity<MaintenanceRequest>().Property(x => x.DispatchStatus).HasDefaultValue("Unassigned");
            modelBuilder.Entity<MaintenanceRequest>().Property(x => x.EstimatedCost).HasPrecision(18, 2);
            modelBuilder.Entity<Listing>().Property(x => x.AskingRent).HasPrecision(18, 2);
            modelBuilder.Entity<Lead>().Property(x => x.MonthlyIncome).HasPrecision(18, 2);
            modelBuilder.Entity<Invoice>().Property(x => x.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Invoice>().Property(x => x.Balance).HasPrecision(18, 2);
            modelBuilder.Entity<PaymentEntry>().Property(x => x.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Listing>().HasOne(x => x.Property).WithMany(x => x.Listings).HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Listing>().HasOne(x => x.Unit).WithMany(x => x.Listings).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Lead>().HasOne(x => x.Listing).WithMany(x => x.Leads).HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Showing>().HasOne(x => x.Listing).WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Showing>().HasOne(x => x.Lead).WithMany(x => x.Showings).HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>().HasOne(x => x.Lease).WithMany(x => x.Invoices).HasForeignKey(x => x.LeaseId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PaymentEntry>().HasOne(x => x.Invoice).WithMany(x => x.Payments).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MediaAsset>().HasOne(x => x.Property).WithMany(x => x.MediaAssets).HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MediaAsset>().HasOne(x => x.Unit).WithMany(x => x.MediaAssets).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MediaAsset>().HasOne(x => x.Listing).WithMany(x => x.MediaAssets).HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MediaAsset>().HasOne(x => x.Lease).WithMany(x => x.MediaAssets).HasForeignKey(x => x.LeaseId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MediaAsset>().HasOne(x => x.MaintenanceRequest).WithMany().HasForeignKey(x => x.MaintenanceRequestId).OnDelete(DeleteBehavior.Restrict);
            ApplicationDbSeeder.Seed(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Modified) entry.Entity.ModifiedUtc = DateTime.UtcNow;
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }

    public static class ApplicationDbSeeder
    {
        public static readonly Guid DemoOrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static void Seed(ModelBuilder modelBuilder)
        {
            var orgId = DemoOrganizationId;
            var propertyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var unitId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var tenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");

            modelBuilder.Entity<Organization>().HasData(new Organization { Id = orgId, Name = "Maple Leaf Property Group", Slug = "maple-leaf", CountryCode = "CA", Province = "ON", PreferredLanguage = "en-CA", TimeZone = "America/Toronto", IsDemo = false, DemoTemplate = string.Empty, DemoExpiresUtc = null, DemoResetAtUtc = null, SubscriptionTier = SubscriptionTier.Growth, TrialEndsUtc = new DateTime(2026,1,15,0,0,0,DateTimeKind.Utc), StripeCustomerId = "cus_demo_mapleleaf", StripeSubscriptionId = "sub_demo_mapleleaf", IsActive = true, CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<AppUser>().HasData(new AppUser { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), OrganizationId = orgId, ClerkUserId = "user_demo_owner", Email = "owner@mapleleafpm.ca", FullName = "Morgan Chen", Role = "Owner", PreferredLanguage = "en-CA", IsActive = true, CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<OrganizationMembership>().HasData(new OrganizationMembership { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), OrganizationId = orgId, UserId = Guid.Parse("66666666-6666-6666-6666-666666666666"), Role = "Owner", Status = "Active", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<Property>().HasData(new Property { Id = propertyId, OrganizationId = orgId, Name = "King West Lofts", PropertyType = "Urban mid-rise", AddressLine1 = "18 Stafford Street", City = "Toronto", Province = "ON", PostalCode = "M6J 2R9", YearBuilt = 2017, MonthlyRevenueTarget = 14800m, AmenitySummary = "Gym access, rooftop terrace, bike storage", NeighborhoodNotes = "Walkable King West location with strong renter demand and transit access.", LeasingNotes = "Position as design-forward downtown living for professionals and couples.", OperationalNotes = "Monitor turnover windows closely and prioritize same-week suite refreshes.", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<Unit>().HasData(new Unit { Id = unitId, OrganizationId = orgId, PropertyId = propertyId, UnitNumber = "508", Bedrooms = 1, Bathrooms = 1, MonthlyRent = 2895m, IsOccupied = true, CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<Tenant>().HasData(new Tenant { Id = tenantId, OrganizationId = orgId, FullName = "Jordan Patel", Email = "jordan.patel@example.com", PhoneNumber = "647-555-0134", CreditScore = 731, ScreeningCompleted = true, ScreeningProvider = "SingleKey", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<Lease>().HasData(new Lease { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), OrganizationId = orgId, UnitId = unitId, TenantId = tenantId, StartDate = new DateOnly(2026,1,1), EndDate = new DateOnly(2026,12,31), MonthlyRent = 2895m, Status = LeaseStatus.Active, StandardOntarioLeaseSigned = true, N1IncreaseNoticeScheduled = true, DepositReceived = true, InsuranceProofReceived = true, MoveInChecklistCompleted = true, MoveInNotes = "Demo move-in package completed and ready for resident handoff.", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<MaintenanceRequest>().HasData(new MaintenanceRequest { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), OrganizationId = orgId, PropertyId = propertyId, UnitId = unitId, Title = "Annual smoke detector certification", Description = "Ontario compliance inspection and battery replacement.", Priority = MaintenancePriority.High, Status = "Open", DispatchStatus = "Assigned", VendorName = "SafeHome Fire Services", EstimatedCost = 180m, RequestedDate = new DateOnly(2026,6,15), CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<AuditLog>().HasData(new AuditLog { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), OrganizationId = orgId, EntityName = "Lease", Action = "Seed", PerformedBy = "system", Details = "Seeded demo Ontario lease", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<ComplianceReminder>().HasData(
                new ComplianceReminder { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), OrganizationId = orgId, Title = "N1 rent increase notice window", NoticeType = "N1", Province = "ON", DueDate = new DateOnly(2026,9,1), IsCompleted = false, Reference = "90 days notice recommended workflow", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new ComplianceReminder { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), OrganizationId = orgId, Title = "Standard Ontario Lease review", NoticeType = "SOL", Province = "ON", DueDate = new DateOnly(2026,7,15), IsCompleted = false, Reference = "Ensure latest government template is attached", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<DocumentTemplate>().HasData(
                new DocumentTemplate { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), OrganizationId = orgId, Name = "Ontario Standard Lease Package", Category = "Lease", Province = "ON", Description = "Prebuilt package with required Ontario clauses and signature checklist.", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new DocumentTemplate { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), OrganizationId = orgId, Name = "N4 Non-payment Notice Template", Category = "Notice", Province = "ON", Description = "Guided landlord workflow for arrears communication.", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<MediaAsset>().HasData(
                new MediaAsset { Id = Guid.Parse("12121212-1212-1212-1212-121212121212"), OrganizationId = orgId, PropertyId = propertyId, UnitId = unitId, LeaseId = Guid.Parse("55555555-5555-5555-5555-555555555555"), FileName = "Ontario Standard Lease.pdf", BlobPath = "/demo/lease-package/ontario-standard-lease.pdf", Caption = "Signed standard lease ready for tenant welcome package.", DocumentType = "SignedLease", SortOrder = 1, IsPrimary = true, Category = MediaAssetCategory.LeaseDocument, CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new MediaAsset { Id = Guid.Parse("34343434-3434-3434-3434-343434343434"), OrganizationId = orgId, PropertyId = propertyId, UnitId = unitId, LeaseId = Guid.Parse("55555555-5555-5555-5555-555555555555"), FileName = "Insurance Certificate.pdf", BlobPath = "/demo/lease-package/insurance-certificate.pdf", Caption = "Tenant insurance proof collected before move-in.", DocumentType = "InsuranceProof", SortOrder = 2, IsPrimary = false, Category = MediaAssetCategory.LeaseDocument, CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer("Data Source=WIN-QVV1GR7G0KH\\SQLEXPRESS;Initial Catalog=PropertyDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=PropertySaaS");
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}

namespace PropertySaaS.Infrastructure.Services
{
    using PropertySaaS.Infrastructure.Data;
    using PropertySaaS.Infrastructure.Options;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var clerkOptions = new ClerkOptions();
            configuration.GetSection("Clerk").Bind(clerkOptions);
            services.AddSingleton(clerkOptions);

            var stripeOptions = new StripeOptions();
            configuration.GetSection("Stripe").Bind(stripeOptions);
            services.AddSingleton(stripeOptions);

            services.Configure<ResendOptions>(configuration.GetSection("Resend"));
            services.AddHttpClient<IEmailService, ResendEmailService>(client => client.BaseAddress = new Uri("https://api.resend.com/"));
            services.AddScoped<INotificationService, NotificationService>();
            services.AddHttpClient<StripeBillingService>(client => client.BaseAddress = new Uri("https://api.stripe.com/v1/"));

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("PropertyDb")), ServiceLifetime.Transient, ServiceLifetime.Transient);
            services.AddTransient<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

            return services;
        }
    }
}

namespace PropertySaaS.Infrastructure.Services
{
    using Microsoft.Extensions.Logging;
    using PropertySaaS.Domain.Entities;
    using PropertySaaS.Infrastructure.Data;
    using PropertySaaS.Infrastructure.Options;

    public sealed class StripePlanPriceDto
    {
        public string Plan { get; set; } = string.Empty;
        public string PriceId { get; set; } = string.Empty;
        public string Currency { get; set; } = "usd";
        public decimal UnitAmount { get; set; }
        public string Interval { get; set; } = "month";
        public string DisplayPrice { get; set; } = string.Empty;
    }

    public class StripeBillingService
    {
        private readonly StripeOptions _options;
        private readonly ILogger<StripeBillingService> _logger;
        private readonly ApplicationDbContext _db;
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public StripeBillingService(HttpClient httpClient, StripeOptions options, ILogger<StripeBillingService> logger, ApplicationDbContext db)
        {
            _httpClient = httpClient;
            _options = options;
            _logger = logger;
            _db = db;
        }

        public async Task<string> CreateCheckoutSessionAsync(string plan, string successUrl, string cancelUrl)
        {
            var priceId = plan?.ToLowerInvariant() switch
            {
                "starter" => _options.StarterPriceId,
                "growth" => _options.GrowthPriceId,
                "pro" => _options.ProPriceId,
                _ => _options.GrowthPriceId
            };

            if (string.IsNullOrWhiteSpace(_options.SecretKey) || string.IsNullOrWhiteSpace(priceId))
            {
                return cancelUrl;
            }

            using var request = CreateRequest(HttpMethod.Post, "checkout/sessions");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["mode"] = "subscription",
                ["success_url"] = successUrl,
                ["cancel_url"] = cancelUrl,
                ["line_items[0][price]"] = priceId,
                ["line_items[0][quantity]"] = "1"
            });

            using var response = await _httpClient.SendAsync(request);
            var session = await ReadAsync<StripeCheckoutSessionResponse>(response);

            return string.IsNullOrWhiteSpace(session.Url) ? cancelUrl : session.Url;
        }

        public async Task<List<StripePlanPriceDto>> GetPlanPricesAsync()
        {
            var plans = new[]
            {
                new { Plan = "Starter", PriceId = _options.StarterPriceId },
                new { Plan = "Growth", PriceId = _options.GrowthPriceId },
                new { Plan = "Pro", PriceId = _options.ProPriceId }
            };

            var results = new List<StripePlanPriceDto>();

            foreach (var plan in plans)
            {
                if (string.IsNullOrWhiteSpace(_options.SecretKey) || string.IsNullOrWhiteSpace(plan.PriceId))
                {
                    results.Add(new StripePlanPriceDto
                    {
                        Plan = plan.Plan,
                        PriceId = plan.PriceId,
                        DisplayPrice = string.Empty
                    });
                    continue;
                }

                using var request = CreateRequest(HttpMethod.Get, $"prices/{plan.PriceId}");
                using var response = await _httpClient.SendAsync(request);
                var price = await ReadAsync<StripePriceResponse>(response);

                results.Add(new StripePlanPriceDto
                {
                    Plan = plan.Plan,
                    PriceId = plan.PriceId,
                    Currency = price.Currency ?? "usd",
                    UnitAmount = (price.UnitAmount ?? 0m) / 100m,
                    Interval = price.Recurring?.Interval ?? "month",
                    DisplayPrice = FormatDisplayPrice(price.UnitAmount, price.Currency, price.Recurring?.Interval)
                });
            }

            return results;
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

            return session.Url;
        }

        public async Task HandleWebhookAsync(string json, string? stripeSignature)
        {
            _logger.LogInformation("Received Stripe webhook. Signature present: {HasSignature}", !string.IsNullOrWhiteSpace(stripeSignature));

            if (!string.IsNullOrWhiteSpace(_options.WebhookSecret) && !string.IsNullOrWhiteSpace(stripeSignature))
            {
                ValidateWebhookSignature(json, stripeSignature, _options.WebhookSecret);
            }

            var stripeEvent = JsonSerializer.Deserialize<StripeWebhookEvent>(json, JsonOptions)
                ?? throw new InvalidOperationException("Invalid Stripe webhook payload.");

            var organization = await ResolveOrganizationForEventAsync(stripeEvent);
            if (organization is null)
            {
                _logger.LogWarning("Stripe webhook {EventType} could not be matched to an organization.", stripeEvent.Type);
                return;
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    if (stripeEvent.Data?.Object?.Deserialize<StripeCheckoutSessionResponse>() is { } checkoutSession)
                    {
                        organization.StripeCustomerId = checkoutSession.Customer ?? organization.StripeCustomerId;
                        organization.StripeSubscriptionId = checkoutSession.Subscription ?? organization.StripeSubscriptionId;
                    }
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                    if (stripeEvent.Data?.Object?.Deserialize<StripeSubscriptionResponse>() is { } subscription)
                    {
                        organization.StripeCustomerId = subscription.Customer ?? organization.StripeCustomerId;
                        organization.StripeSubscriptionId = subscription.Id ?? organization.StripeSubscriptionId;
                        organization.SubscriptionTier = ResolveTierFromPriceId(subscription.Items?.Data?.FirstOrDefault()?.Price?.Id);
                        organization.IsActive = subscription.Status is "active" or "trialing" or "past_due";
                    }
                    break;

                case "customer.subscription.deleted":
                    organization.SubscriptionTier = SubscriptionTier.Trial;
                    organization.StripeSubscriptionId = string.Empty;
                    break;

                case "invoice.payment_failed":
                    organization.IsActive = true;
                    break;

                case "invoice.paid":
                    organization.IsActive = true;
                    break;
            }

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organization.Id,
                EntityName = "StripeWebhook",
                Action = stripeEvent.Type ?? "unknown",
                PerformedBy = "stripe",
                Details = $"EventId={stripeEvent.Id}; EventType={stripeEvent.Type}"
            });

            await _db.SaveChangesAsync();
        }

        private async Task<Organization?> ResolveOrganizationForEventAsync(StripeWebhookEvent stripeEvent)
        {
            return stripeEvent.Type switch
            {
                "checkout.session.completed" when stripeEvent.Data?.Object?.Deserialize<StripeCheckoutSessionResponse>() is { } checkoutSession
                    => await ResolveOrganizationAsync(checkoutSession.Customer, checkoutSession.Subscription),
                "customer.subscription.created" or "customer.subscription.updated" or "customer.subscription.deleted" when stripeEvent.Data?.Object?.Deserialize<StripeSubscriptionResponse>() is { } subscription
                    => await ResolveOrganizationAsync(subscription.Customer, subscription.Id),
                "invoice.paid" or "invoice.payment_failed" when stripeEvent.Data?.Object?.Deserialize<StripeInvoiceResponse>() is { } invoice
                    => await ResolveOrganizationAsync(invoice.Customer, invoice.Parent?.SubscriptionDetails?.Subscription),
                _ => null
            };
        }

        private async Task<Organization?> ResolveOrganizationAsync(string? customerId, string? subscriptionId)
        {
            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                var bySubscription = await _db.Organizations.FirstOrDefaultAsync(x => x.StripeSubscriptionId == subscriptionId);
                if (bySubscription is not null)
                {
                    return bySubscription;
                }
            }

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                var byCustomer = await _db.Organizations.FirstOrDefaultAsync(x => x.StripeCustomerId == customerId);
                if (byCustomer is not null)
                {
                    return byCustomer;
                }
            }

            return null;
        }

        private static string FormatDisplayPrice(decimal? unitAmountMinor, string? currency, string? interval)
        {
            if (!unitAmountMinor.HasValue)
            {
                return string.Empty;
            }

            var amount = unitAmountMinor.Value / 100m;
            var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant();
            var symbol = normalizedCurrency switch
            {
                "CAD" => "$",
                "USD" => "$",
                "EUR" => "€",
                "GBP" => "£",
                _ => normalizedCurrency + " "
            };

            var intervalLabel = string.IsNullOrWhiteSpace(interval) ? "month" : interval;
            return $"{symbol}{amount:0.##} / {intervalLabel}";
        }

        private SubscriptionTier ResolveTierFromPriceId(string? priceId)
            => priceId switch
            {
                var id when string.Equals(id, _options.StarterPriceId, StringComparison.Ordinal) => SubscriptionTier.Starter,
                var id when string.Equals(id, _options.GrowthPriceId, StringComparison.Ordinal) => SubscriptionTier.Growth,
                var id when string.Equals(id, _options.ProPriceId, StringComparison.Ordinal) => SubscriptionTier.Pro,
                _ => SubscriptionTier.Trial
            };

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
            return request;
        }

        private async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Stripe API error ({(int)response.StatusCode}): {content}");
            }

            return JsonSerializer.Deserialize<T>(content, JsonOptions)
                ?? throw new InvalidOperationException("Unable to deserialize Stripe response.");
        }

        private static void ValidateWebhookSignature(string payload, string stripeSignature, string webhookSecret)
        {
            var elements = stripeSignature.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

            if (!elements.TryGetValue("t", out var timestamp) || !elements.TryGetValue("v1", out var signature))
            {
                throw new InvalidOperationException("Stripe signature header is invalid.");
            }

            var signedPayload = $"{timestamp}.{payload}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computed), Encoding.UTF8.GetBytes(signature.ToLowerInvariant())))
            {
                throw new InvalidOperationException("Stripe signature verification failed.");
            }
        }

        private sealed class StripeCheckoutSessionResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("customer")]
            public string? Customer { get; set; }

            [JsonPropertyName("subscription")]
            public string? Subscription { get; set; }
        }

        private sealed class StripePortalSessionResponse
        {
            [JsonPropertyName("url")]
            public string Url { get; set; } = string.Empty;
        }

        private sealed class StripeWebhookEvent
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("data")]
            public StripeWebhookData? Data { get; set; }
        }

        private sealed class StripeWebhookData
        {
            [JsonPropertyName("object")]
            public JsonElement? Object { get; set; }
        }

        private sealed class StripeSubscriptionResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("customer")]
            public string? Customer { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("items")]
            public StripeSubscriptionItems? Items { get; set; }
        }

        private sealed class StripeSubscriptionItems
        {
            [JsonPropertyName("data")]
            public List<StripeSubscriptionItem>? Data { get; set; }
        }

        private sealed class StripeSubscriptionItem
        {
            [JsonPropertyName("price")]
            public StripePrice? Price { get; set; }
        }

        private sealed class StripePrice
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        private sealed class StripePriceResponse
        {
            [JsonPropertyName("currency")]
            public string? Currency { get; set; }

            [JsonPropertyName("unit_amount")]
            public decimal? UnitAmount { get; set; }

            [JsonPropertyName("recurring")]
            public StripeRecurringPrice? Recurring { get; set; }
        }

        private sealed class StripeRecurringPrice
        {
            [JsonPropertyName("interval")]
            public string? Interval { get; set; }
        }

        private sealed class StripeInvoiceResponse
        {
            [JsonPropertyName("customer")]
            public string? Customer { get; set; }

            [JsonPropertyName("parent")]
            public StripeInvoiceParent? Parent { get; set; }
        }

        private sealed class StripeInvoiceParent
        {
            [JsonPropertyName("subscription_details")]
            public StripeSubscriptionDetails? SubscriptionDetails { get; set; }
        }

        private sealed class StripeSubscriptionDetails
        {
            [JsonPropertyName("subscription")]
            public string? Subscription { get; set; }
        }
    }
}









