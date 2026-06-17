using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertySaaS.Application.Abstractions;
using PropertySaaS.Domain.Common;
using PropertySaaS.Domain.Entities;
using PropertySaaS.Domain.Enums;
using Stripe;

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
        public DbSet<Property> Properties => Set<Property>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Lease> Leases => Set<Lease>();
        public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ComplianceReminder> ComplianceReminders => Set<ComplianceReminder>();
        public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organization>().HasIndex(x => x.Slug).IsUnique();
            modelBuilder.Entity<AppUser>().HasOne(x => x.Organization).WithMany(x => x.Users).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Property>().HasMany(x => x.Units).WithOne(x => x.Property).HasForeignKey(x => x.PropertyId);
            modelBuilder.Entity<Property>().Property(x => x.MonthlyRevenueTarget).HasPrecision(18, 2);
            modelBuilder.Entity<Unit>().Property(x => x.MonthlyRent).HasPrecision(18, 2);
            modelBuilder.Entity<Lease>().HasOne(x => x.Unit).WithMany(x => x.Leases).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Lease>().HasOne(x => x.Tenant).WithMany(x => x.Leases).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Lease>().Property(x => x.MonthlyRent).HasPrecision(18, 2);
            modelBuilder.Entity<MaintenanceRequest>().Property(x => x.EstimatedCost).HasPrecision(18, 2);
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

            modelBuilder.Entity<Organization>().HasData(new Organization { Id = orgId, Name = "Maple Leaf Property Group", Slug = "maple-leaf", CountryCode = "CA", Province = "ON", PreferredLanguage = "en-CA", TimeZone = "America/Toronto", SubscriptionTier = SubscriptionTier.Growth, StripeCustomerId = "cus_demo_mapleleaf", StripeSubscriptionId = "sub_demo_mapleleaf", IsActive = true, CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<AppUser>().HasData(new AppUser { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), OrganizationId = orgId, ClerkUserId = "user_demo_owner", Email = "owner@mapleleafpm.ca", FullName = "Morgan Chen", Role = "Owner", PreferredLanguage = "en-CA", IsActive = true, CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<Property>().HasData(new Property { Id = propertyId, OrganizationId = orgId, Name = "King West Lofts", PropertyType = "Urban mid-rise", AddressLine1 = "18 Stafford Street", City = "Toronto", Province = "ON", PostalCode = "M6J 2R9", YearBuilt = 2017, MonthlyRevenueTarget = 14800m, AmenitySummary = "Gym access, rooftop terrace, bike storage", NeighborhoodNotes = "Walkable King West location with strong renter demand and transit access.", LeasingNotes = "Position as design-forward downtown living for professionals and couples.", OperationalNotes = "Monitor turnover windows closely and prioritize same-week suite refreshes.", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<Unit>().HasData(new Unit { Id = unitId, OrganizationId = orgId, PropertyId = propertyId, UnitNumber = "508", Bedrooms = 1, Bathrooms = 1, MonthlyRent = 2895m, IsOccupied = true, CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<Tenant>().HasData(new Tenant { Id = tenantId, OrganizationId = orgId, FullName = "Jordan Patel", Email = "jordan.patel@example.com", PhoneNumber = "647-555-0134", CreditScore = 731, ScreeningCompleted = true, ScreeningProvider = "SingleKey", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<Lease>().HasData(new Lease { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), OrganizationId = orgId, UnitId = unitId, TenantId = tenantId, StartDate = new DateOnly(2026,1,1), EndDate = new DateOnly(2026,12,31), MonthlyRent = 2895m, Status = LeaseStatus.Active, StandardOntarioLeaseSigned = true, N1IncreaseNoticeScheduled = true, CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<MaintenanceRequest>().HasData(new MaintenanceRequest { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), OrganizationId = orgId, PropertyId = propertyId, UnitId = unitId, Title = "Annual smoke detector certification", Description = "Ontario compliance inspection and battery replacement.", Priority = MaintenancePriority.High, Status = "Open", VendorName = "SafeHome Fire Services", EstimatedCost = 180m, RequestedDate = new DateOnly(2026,6,15), CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<AuditLog>().HasData(new AuditLog { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), OrganizationId = orgId, EntityName = "Lease", Action = "Seed", PerformedBy = "system", Details = "Seeded demo Ontario lease", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<ComplianceReminder>().HasData(
                new ComplianceReminder { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), OrganizationId = orgId, Title = "N1 rent increase notice window", NoticeType = "N1", Province = "ON", DueDate = new DateOnly(2026,9,1), IsCompleted = false, Reference = "90 days notice recommended workflow", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new ComplianceReminder { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), OrganizationId = orgId, Title = "Standard Ontario Lease review", NoticeType = "SOL", Province = "ON", DueDate = new DateOnly(2026,7,15), IsCompleted = false, Reference = "Ensure latest government template is attached", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
            modelBuilder.Entity<DocumentTemplate>().HasData(
                new DocumentTemplate { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), OrganizationId = orgId, Name = "Ontario Standard Lease Package", Category = "Lease", Province = "ON", Description = "Prebuilt package with required Ontario clauses and signature checklist.", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new DocumentTemplate { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), OrganizationId = orgId, Name = "N4 Non-payment Notice Template", Category = "Notice", Province = "ON", Description = "Guided landlord workflow for arrears communication.", CreatedUtc = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) });
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

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("PropertyDb")));
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

            if (!string.IsNullOrWhiteSpace(stripeOptions.SecretKey))
            {
                StripeConfiguration.ApiKey = stripeOptions.SecretKey;
            }

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

    public class StripeBillingService
    {
        private readonly StripeOptions _options;
        private readonly ILogger<StripeBillingService> _logger;
        private readonly ApplicationDbContext _db;

        public StripeBillingService(StripeOptions options, ILogger<StripeBillingService> logger, ApplicationDbContext db)
        {
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

            var service = new Stripe.Checkout.SessionService();
            var session = await service.CreateAsync(new Stripe.Checkout.SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new() { Price = priceId, Quantity = 1 }
                }
            });

            return session.Url ?? cancelUrl;
        }

        public async Task<string> CreateBillingPortalAsync(string customerId, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey) || string.IsNullOrWhiteSpace(customerId))
            {
                return returnUrl;
            }

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            });

            return session.Url;
        }

        public async Task HandleWebhookAsync(string json, string? stripeSignature)
        {
            _logger.LogInformation("Received Stripe webhook. Signature present: {HasSignature}", !string.IsNullOrWhiteSpace(stripeSignature));

            var organization = await _db.Organizations.FirstOrDefaultAsync(x => x.Slug == "maple-leaf");
            if (organization is null)
            {
                return;
            }

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organization.Id,
                EntityName = "StripeWebhook",
                Action = "Received",
                PerformedBy = "stripe",
                Details = $"PayloadLength={json.Length}; SignaturePresent={!string.IsNullOrWhiteSpace(stripeSignature)}"
            });

            await _db.SaveChangesAsync();
        }
    }
}









