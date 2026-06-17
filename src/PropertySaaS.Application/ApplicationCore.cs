using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertySaaS.Domain.Entities;
using PropertySaaS.Domain.Enums;

namespace PropertySaaS.Application.Abstractions
{
    public interface IApplicationDbContext
    {
        DbSet<Organization> Organizations { get; }
        DbSet<AppUser> Users { get; }
        DbSet<OrganizationMembership> OrganizationMemberships { get; }
        DbSet<OrganizationInvitation> OrganizationInvitations { get; }
        DbSet<Property> Properties { get; }
        DbSet<Unit> Units { get; }
        DbSet<Tenant> Tenants { get; }
        DbSet<Lease> Leases { get; }
        DbSet<MaintenanceRequest> MaintenanceRequests { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<ComplianceReminder> ComplianceReminders { get; }
        DbSet<DocumentTemplate> DocumentTemplates { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

namespace PropertySaaS.Application.Common
{
    public class JurisdictionProfile
    {
        public string CountryCode { get; init; } = "CA";
        public string ProvinceCode { get; init; } = "ON";
        public string ProvinceDisplayName { get; init; } = "Ontario";
        public string DefaultLanguage { get; init; } = "en-CA";
        public IReadOnlyList<string> SupportedLanguages { get; init; } = new[] { "en-CA" };
        public IReadOnlyList<string> NoticeTypes { get; init; } = Array.Empty<string>();
        public string LeasePackageLabel { get; init; } = "Lease package";
        public IReadOnlyDictionary<string, string> DocumentTemplates { get; init; } = new Dictionary<string, string>();
        public IReadOnlyList<string> ComplianceChecklist { get; init; } = Array.Empty<string>();
    }

    public static class JurisdictionCatalog
    {
        private static readonly Dictionary<string, JurisdictionProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ON"] = new JurisdictionProfile
            {
                ProvinceCode = "ON",
                ProvinceDisplayName = "Ontario",
                DefaultLanguage = "en-CA",
                SupportedLanguages = new[] { "en-CA", "fr-CA" },
                NoticeTypes = new[] { "N1", "N4", "SOL" },
                LeasePackageLabel = "Ontario Standard Lease Package",
                DocumentTemplates = new Dictionary<string, string>
                {
                    ["lease-package"] = "Ontario Standard Lease Package",
                    ["non-payment-notice"] = "N4 Non-payment Notice",
                    ["rent-increase-notice"] = "N1 Rent Increase Notice"
                },
                ComplianceChecklist = new[]
                {
                    "Review 90-day rent increase workflows",
                    "Track the latest Ontario lease form version",
                    "Retain audit trail for notices and service dates"
                }
            },
            ["QC"] = new JurisdictionProfile
            {
                ProvinceCode = "QC",
                ProvinceDisplayName = "Québec",
                DefaultLanguage = "fr-CA",
                SupportedLanguages = new[] { "fr-CA", "en-CA" },
                NoticeTypes = new[] { "TAL", "RentReview", "LeaseRenewal" },
                LeasePackageLabel = "Québec Residential Lease Package",
                DocumentTemplates = new Dictionary<string, string>
                {
                    ["lease-package"] = "Québec Residential Lease Package",
                    ["non-payment-notice"] = "Notice to pay rent or begin TAL file",
                    ["rent-increase-notice"] = "Lease renewal and rent adjustment notice"
                },
                ComplianceChecklist = new[]
                {
                    "Track Tribunal administratif du logement timelines",
                    "Prepare lease wording and notices in the appropriate language",
                    "Retain delivery proof and resident communication history"
                }
            },
            ["AB"] = new JurisdictionProfile
            {
                ProvinceCode = "AB",
                ProvinceDisplayName = "Alberta",
                DefaultLanguage = "en-CA",
                SupportedLanguages = new[] { "en-CA", "fr-CA" },
                NoticeTypes = new[] { "RentIncrease", "Termination", "Inspection" },
                LeasePackageLabel = "Alberta Residential Tenancy Package",
                DocumentTemplates = new Dictionary<string, string>
                {
                    ["lease-package"] = "Alberta Residential Tenancy Package",
                    ["non-payment-notice"] = "Alberta non-payment notice workflow",
                    ["rent-increase-notice"] = "Alberta rent increase notice"
                },
                ComplianceChecklist = new[]
                {
                    "Track notice windows for rent changes and terminations",
                    "Review inspection and entry documentation requirements",
                    "Keep service evidence and lease package history organized"
                }
            }
        };

        public static JurisdictionProfile GetProfile(string? province)
            => Profiles.TryGetValue(province ?? string.Empty, out var profile)
                ? profile
                : Profiles["ON"];

        public static IReadOnlyList<string> SupportedCultureNames => Profiles.Values
            .SelectMany(x => x.SupportedLanguages)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    public class CurrentOrganization
    {
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public int AccessibleOrganizationCount { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Owner";
        public string SystemRole { get; set; } = "User";
        public string Province { get; set; } = "ON";
        public string CountryCode { get; set; } = "CA";
        public string PreferredLanguage { get; set; } = "en-CA";
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserEmail);
        public bool HasOrganizationAccess => OrganizationId != Guid.Empty;
        public bool RequiresOrganizationSelection => !HasOrganizationAccess && AccessibleOrganizationCount > 1;
        public bool CanManageData => Role is "Owner" or "Manager";
        public bool IsSuperAdmin => string.Equals(SystemRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        public JurisdictionProfile Jurisdiction => JurisdictionCatalog.GetProfile(Province);
    }

    public sealed class MemberSummaryDto
    {
        public Guid MembershipId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class OrganizationAccessOptionDto
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class InvitationSummaryDto
    {
        public Guid InvitationId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
    }

    public sealed class OrganizationInviteResult
    {
        public Guid InvitationId { get; set; }
        public string InvitationUrl { get; set; } = string.Empty;
    }

    public sealed class SubscriptionEntitlementsDto
    {
        public string PlanName { get; set; } = string.Empty;
        public int MaxUnits { get; set; }
        public int MaxUsers { get; set; }
        public bool IncludesCompliance { get; set; }
        public bool IncludesAuditLogs { get; set; }
        public bool IncludesPrioritySupport { get; set; }
        public bool IncludesAdvancedExports { get; set; }
        public string TrialBanner { get; set; } = string.Empty;
    }

    public sealed class SupportSessionDto
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
    }

    public sealed class SuperAdminOrganizationDto
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string SubscriptionTier { get; set; } = string.Empty;
        public int Units { get; set; }
        public int Users { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int Properties { get; set; }
        public int Units { get; set; }
        public int OccupiedUnits { get; set; }
        public int VacantUnits => Math.Max(Units - OccupiedUnits, 0);
        public int Tenants { get; set; }
        public int ActiveLeases { get; set; }
        public int OpenMaintenance { get; set; }
        public int ComplianceDueSoon { get; set; }
        public int JurisdictionNoticesInWorkflow { get; set; }
        public int ExportFeedsReady { get; set; } = 3;
        public decimal MonthlyRentRoll { get; set; }
        public string SubscriptionTier { get; set; } = "Growth";
        public decimal OccupancyRate => Units == 0 ? 0 : Math.Round((decimal)OccupiedUnits / Units * 100, 2);
    }
}

namespace PropertySaaS.Application.Features
{
    using PropertySaaS.Application.Abstractions;
    using PropertySaaS.Application.Common;

    public class SaasDataService
    {
        private readonly IApplicationDbContext _db;
        private readonly CurrentOrganization _current;
        private readonly INotificationService _notifications;
        private const string SupportRole = "SupportViewer";

        public SaasDataService(IApplicationDbContext db, CurrentOrganization current, INotificationService notifications)
        {
            _db = db;
            _current = current;
            _notifications = notifications;
        }

        private void EnsureCanManageData()
        {
            if (!_current.CanManageData)
            {
                throw new InvalidOperationException("Current role cannot modify portfolio data.");
            }
        }

        private async Task<SubscriptionEntitlementsDto> GetEntitlementsInternalAsync(CancellationToken cancellationToken = default)
        {
            var organization = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == _current.OrganizationId, cancellationToken);
            var tier = organization?.SubscriptionTier ?? SubscriptionTier.Trial;

            return tier switch
            {
                SubscriptionTier.Starter => new SubscriptionEntitlementsDto
                {
                    PlanName = "Starter",
                    MaxUnits = 25,
                    MaxUsers = 2,
                    IncludesCompliance = true,
                    IncludesAuditLogs = false,
                    IncludesPrioritySupport = false,
                    IncludesAdvancedExports = false,
                    TrialBanner = string.Empty
                },
                SubscriptionTier.Growth => new SubscriptionEntitlementsDto
                {
                    PlanName = "Growth",
                    MaxUnits = 150,
                    MaxUsers = 8,
                    IncludesCompliance = true,
                    IncludesAuditLogs = true,
                    IncludesPrioritySupport = false,
                    IncludesAdvancedExports = true,
                    TrialBanner = string.Empty
                },
                SubscriptionTier.Pro => new SubscriptionEntitlementsDto
                {
                    PlanName = "Pro",
                    MaxUnits = int.MaxValue,
                    MaxUsers = 25,
                    IncludesCompliance = true,
                    IncludesAuditLogs = true,
                    IncludesPrioritySupport = true,
                    IncludesAdvancedExports = true,
                    TrialBanner = string.Empty
                },
                _ => new SubscriptionEntitlementsDto
                {
                    PlanName = "Trial",
                    MaxUnits = 10,
                    MaxUsers = 3,
                    IncludesCompliance = true,
                    IncludesAuditLogs = true,
                    IncludesPrioritySupport = false,
                    IncludesAdvancedExports = false,
                    TrialBanner = "14-day trial active"
                }
            };
        }

        private async Task EnsureUserLimitAsync(CancellationToken cancellationToken = default)
        {
            var entitlements = await GetEntitlementsInternalAsync(cancellationToken);
            var activeUsers = await _db.OrganizationMemberships.CountAsync(x => x.OrganizationId == _current.OrganizationId && x.Status == "Active", cancellationToken);
            if (entitlements.MaxUsers != int.MaxValue && activeUsers >= entitlements.MaxUsers)
            {
                throw new InvalidOperationException($"The current {entitlements.PlanName} plan allows up to {entitlements.MaxUsers} active users.");
            }
        }

        private async Task EnsureUnitLimitAsync(int additionalUnits = 1, CancellationToken cancellationToken = default)
        {
            var entitlements = await GetEntitlementsInternalAsync(cancellationToken);
            var unitCount = await _db.Units.CountAsync(x => x.OrganizationId == _current.OrganizationId, cancellationToken);
            if (entitlements.MaxUnits != int.MaxValue && unitCount + additionalUnits > entitlements.MaxUnits)
            {
                throw new InvalidOperationException($"The current {entitlements.PlanName} plan allows up to {entitlements.MaxUnits} units.");
            }
        }

        private async Task EnsureFeatureEnabledAsync(Func<SubscriptionEntitlementsDto, bool> predicate, string featureName, CancellationToken cancellationToken = default)
        {
            var entitlements = await GetEntitlementsInternalAsync(cancellationToken);
            if (!predicate(entitlements))
            {
                throw new InvalidOperationException($"{featureName} is not included in the current {entitlements.PlanName} plan.");
            }
        }

        public async Task<DashboardSummaryDto> GetDashboardAsync()
        {
            var id = _current.OrganizationId;
            var units = await _db.Units.Where(x => x.OrganizationId == id).ToListAsync();
            var leases = await _db.Leases.Where(x => x.OrganizationId == id).ToListAsync();
            var organization = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            var jurisdictionProfile = JurisdictionCatalog.GetProfile(_current.Province);
            return new DashboardSummaryDto
            {
                Properties = await _db.Properties.CountAsync(x => x.OrganizationId == id),
                Units = units.Count,
                OccupiedUnits = units.Count(x => x.IsOccupied),
                Tenants = await _db.Tenants.CountAsync(x => x.OrganizationId == id),
                ActiveLeases = leases.Count(x => x.Status == LeaseStatus.Active || x.Status == LeaseStatus.EndingSoon),
                OpenMaintenance = await _db.MaintenanceRequests.CountAsync(x => x.OrganizationId == id && x.Status != "Closed"),
                ComplianceDueSoon = await _db.ComplianceReminders.CountAsync(x => x.OrganizationId == id && !x.IsCompleted && x.DueDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(45))),
                JurisdictionNoticesInWorkflow = leases.Count(x => x.N1IncreaseNoticeScheduled) + await _db.ComplianceReminders.CountAsync(x => x.OrganizationId == id && !x.IsCompleted && jurisdictionProfile.NoticeTypes.Contains(x.NoticeType)),
                MonthlyRentRoll = units.Sum(x => x.MonthlyRent),
                SubscriptionTier = organization?.SubscriptionTier.ToString() ?? "Growth"
            };
        }

        public async Task<SubscriptionEntitlementsDto> GetSubscriptionEntitlementsAsync()
            => await GetEntitlementsInternalAsync();

        public Task<List<Property>> GetPropertiesAsync() => _db.Properties.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.Name).ToListAsync();
        public Task<List<Unit>> GetUnitsAsync() => _db.Units.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.UnitNumber).ToListAsync();
        public Task<List<Tenant>> GetTenantsAsync() => _db.Tenants.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.FullName).ToListAsync();
        public Task<List<Lease>> GetLeasesAsync() => _db.Leases.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.StartDate).ToListAsync();
        public Task<List<MaintenanceRequest>> GetMaintenanceAsync() => _db.MaintenanceRequests.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.RequestedDate).ToListAsync();
        public Task<List<ComplianceReminder>> GetComplianceAsync() => _db.ComplianceReminders.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.DueDate).ToListAsync();
        public Task<List<DocumentTemplate>> GetTemplatesAsync() => _db.DocumentTemplates.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.Name).ToListAsync();
        public Task<List<AuditLog>> GetAuditLogsAsync() => _db.AuditLogs.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.CreatedUtc).ToListAsync();

        public async Task AddPropertyAsync(Property property)
        {
            EnsureCanManageData();
            property.Id = Guid.NewGuid();
            property.OrganizationId = _current.OrganizationId;
            property.CreatedUtc = DateTime.UtcNow;
            _db.Properties.Add(property);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Property), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created property {property.Name}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePropertyAsync(Property property)
        {
            EnsureCanManageData();
            var entity = await _db.Properties.FirstOrDefaultAsync(x => x.Id == property.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.Name = property.Name;
            entity.PropertyType = property.PropertyType;
            entity.AddressLine1 = property.AddressLine1;
            entity.City = property.City;
            entity.Province = property.Province;
            entity.PostalCode = property.PostalCode;
            entity.YearBuilt = property.YearBuilt;
            entity.MonthlyRevenueTarget = property.MonthlyRevenueTarget;
            entity.AmenitySummary = property.AmenitySummary;
            entity.NeighborhoodNotes = property.NeighborhoodNotes;
            entity.LeasingNotes = property.LeasingNotes;
            entity.OperationalNotes = property.OperationalNotes;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Property), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated property playbook for {property.Name}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeletePropertyAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Properties.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Properties.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Property), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted property {entity.Name}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddTenantAsync(Tenant tenant)
        {
            EnsureCanManageData();
            tenant.Id = Guid.NewGuid();
            tenant.OrganizationId = _current.OrganizationId;
            tenant.CreatedUtc = DateTime.UtcNow;
            _db.Tenants.Add(tenant);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Tenant), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created tenant {tenant.FullName}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteTenantAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Tenants.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Tenant), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted tenant {entity.FullName}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateTenantAsync(Tenant tenant)
        {
            EnsureCanManageData();
            var entity = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == tenant.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.FullName = tenant.FullName;
            entity.Email = tenant.Email;
            entity.PhoneNumber = tenant.PhoneNumber;
            entity.CreditScore = tenant.CreditScore;
            entity.ScreeningCompleted = tenant.ScreeningCompleted;
            entity.ScreeningProvider = tenant.ScreeningProvider;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Tenant), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated tenant {tenant.FullName}" });
            await _db.SaveChangesAsync();
        }

        public async Task<AppUser?> GetCurrentUserProfileAsync()
            => await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == _current.OrganizationId && x.Email == _current.UserEmail);

        public async Task<List<MemberSummaryDto>> GetMembersAsync()
            => await _db.OrganizationMemberships
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId)
                .OrderBy(x => x.User!.FullName)
                .Select(x => new MemberSummaryDto
                {
                    MembershipId = x.Id,
                    UserId = x.UserId,
                    FullName = x.User != null && !string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.FullName : x.User!.Email,
                    Email = x.User!.Email,
                    Role = x.Role,
                    Status = x.Status
                })
                .ToListAsync();

        public async Task<Organization?> CreateOrganizationAsync(string name, string province, CancellationToken cancellationToken = default)
        {
            if (!_current.IsAuthenticated || _current.UserId == Guid.Empty)
            {
                throw new InvalidOperationException("You must be authenticated to create an organization.");
            }

            var normalizedName = string.IsNullOrWhiteSpace(name)
                ? throw new InvalidOperationException("Organization name is required.")
                : name.Trim();

            var slug = BuildSlug(normalizedName);
            var existingSlugCount = await _db.Organizations.CountAsync(x => x.Slug.StartsWith(slug), cancellationToken);
            if (existingSlugCount > 0)
            {
                slug = $"{slug}-{existingSlugCount + 1}";
            }

            var selectedProvince = string.IsNullOrWhiteSpace(province) ? "ON" : province.Trim().ToUpperInvariant();
            var profile = JurisdictionCatalog.GetProfile(selectedProvince);

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = normalizedName,
                Slug = slug,
                CountryCode = profile.CountryCode,
                Province = profile.ProvinceCode,
                PreferredLanguage = profile.DefaultLanguage,
                TimeZone = "America/Toronto",
                SubscriptionTier = SubscriptionTier.Trial,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow
            };

            _db.Organizations.Add(organization);

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == _current.UserId, cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException("Current user profile was not found.");
            }

            user.OrganizationId = organization.Id;
            user.Role = "Owner";
            user.PreferredLanguage = profile.DefaultLanguage;

            _db.OrganizationMemberships.Add(new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UserId = user.Id,
                Role = "Owner",
                Status = "Active",
                CreatedUtc = DateTime.UtcNow
            });

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organization.Id,
                EntityName = nameof(Organization),
                Action = "Create",
                PerformedBy = _current.UserEmail,
                Details = $"Created organization {organization.Name} from onboarding"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return organization;
        }

        public async Task<List<OrganizationAccessOptionDto>> GetAccessibleOrganizationsAsync()
            => await _db.OrganizationMemberships
                .AsNoTracking()
                .Where(x => x.UserId == _current.UserId && x.Status == "Active")
                .OrderBy(x => x.Organization!.Name)
                .Select(x => new OrganizationAccessOptionDto
                {
                    OrganizationId = x.OrganizationId,
                    OrganizationName = x.Organization!.Name,
                    Role = x.Role,
                    Status = x.Status
                })
                .ToListAsync();

        public async Task<List<InvitationSummaryDto>> GetInvitationsAsync()
            => await _db.OrganizationInvitations
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId)
                .OrderByDescending(x => x.CreatedUtc)
                .Select(x => new InvitationSummaryDto
                {
                    InvitationId = x.Id,
                    Email = x.Email,
                    Role = x.Role,
                    Status = x.Status,
                    ExpiresUtc = x.ExpiresUtc
                })
                .ToListAsync();

        public async Task<OrganizationInviteResult> InviteUserAsync(string email, string role, string invitationBaseUrl, CancellationToken cancellationToken = default)
        {
            EnsureCanManageData();
            await EnsureUserLimitAsync(cancellationToken);

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var normalizedRole = NormalizeInviteRole(role);
            var existingMembership = await _db.OrganizationMemberships
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.OrganizationId == _current.OrganizationId && x.User != null && x.User.Email == normalizedEmail && x.Status == "Active", cancellationToken);

            if (existingMembership is not null)
            {
                throw new InvalidOperationException("This user already belongs to the organization.");
            }

            var existingInvite = await _db.OrganizationInvitations
                .FirstOrDefaultAsync(x => x.OrganizationId == _current.OrganizationId && x.Email == normalizedEmail && x.Status == "Pending", cancellationToken);

            var token = Guid.NewGuid().ToString("N");
            var invite = existingInvite ?? new OrganizationInvitation
            {
                Id = Guid.NewGuid(),
                OrganizationId = _current.OrganizationId,
                Email = normalizedEmail,
                InvitedBy = _current.UserEmail,
                CreatedUtc = DateTime.UtcNow
            };

            invite.Role = normalizedRole;
            invite.Token = token;
            invite.Status = "Pending";
            invite.ExpiresUtc = DateTime.UtcNow.AddDays(7);
            invite.AcceptedUtc = null;
            invite.RevokedUtc = null;

            if (existingInvite is null)
            {
                _db.OrganizationInvitations.Add(invite);
            }

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = _current.OrganizationId,
                EntityName = nameof(OrganizationInvitation),
                Action = existingInvite is null ? "Create" : "Refresh",
                PerformedBy = _current.UserEmail,
                Details = $"Invitation for {normalizedEmail} with role {normalizedRole}"
            });

            await _db.SaveChangesAsync(cancellationToken);

            var invitationUrl = $"{invitationBaseUrl.TrimEnd('/')}/invitations/accept?token={Uri.EscapeDataString(token)}";
            await _notifications.SendOrganizationInvitationAsync(normalizedEmail, _current.OrganizationName, _current.UserEmail, normalizedRole, invitationUrl, cancellationToken);

            return new OrganizationInviteResult
            {
                InvitationId = invite.Id,
                InvitationUrl = invitationUrl
            };
        }

        public async Task AcceptInvitationAsync(string token, string email, string clerkUserId, string fullName, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var invitation = await _db.OrganizationInvitations
                .FirstOrDefaultAsync(x => x.Token == token && x.Email == normalizedEmail && x.Status == "Pending", cancellationToken);

            if (invitation is null || invitation.ExpiresUtc < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Invitation is invalid or expired.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
            if (user is null)
            {
                user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = invitation.OrganizationId,
                    ClerkUserId = clerkUserId,
                    Email = normalizedEmail,
                    FullName = fullName,
                    Role = invitation.Role,
                    PreferredLanguage = "en-CA",
                    IsActive = true,
                    CreatedUtc = DateTime.UtcNow
                };
                _db.Users.Add(user);
            }
            else
            {
                user.ClerkUserId = string.IsNullOrWhiteSpace(user.ClerkUserId) ? clerkUserId : user.ClerkUserId;
                user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? fullName : user.FullName;
            }

            var membership = await _db.OrganizationMemberships
                .FirstOrDefaultAsync(x => x.OrganizationId == invitation.OrganizationId && x.UserId == user.Id, cancellationToken);

            if (membership is null)
            {
                membership = new OrganizationMembership
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = invitation.OrganizationId,
                    UserId = user.Id,
                    Role = invitation.Role,
                    Status = "Active",
                    CreatedUtc = DateTime.UtcNow
                };
                _db.OrganizationMemberships.Add(membership);
            }
            else
            {
                membership.Role = invitation.Role;
                membership.Status = "Active";
            }

            user.OrganizationId = invitation.OrganizationId;
            user.Role = invitation.Role;
            invitation.Status = "Accepted";
            invitation.AcceptedUtc = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = invitation.OrganizationId,
                EntityName = nameof(OrganizationInvitation),
                Action = "Accept",
                PerformedBy = normalizedEmail,
                Details = $"Accepted invitation for {normalizedEmail}"
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RevokeInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default)
        {
            EnsureCanManageData();
            var invitation = await _db.OrganizationInvitations.FirstOrDefaultAsync(x => x.Id == invitationId && x.OrganizationId == _current.OrganizationId, cancellationToken);
            if (invitation is null)
            {
                return;
            }

            invitation.Status = "Revoked";
            invitation.RevokedUtc = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = _current.OrganizationId,
                EntityName = nameof(OrganizationInvitation),
                Action = "Revoke",
                PerformedBy = _current.UserEmail,
                Details = $"Revoked invitation for {invitation.Email}"
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateCurrentUserPreferredLanguageAsync(string preferredLanguage)
        {
            var entity = await _db.Users.FirstOrDefaultAsync(x => x.OrganizationId == _current.OrganizationId && x.Email == _current.UserEmail);
            if (entity is null) return;

            var normalizedLanguage = string.IsNullOrWhiteSpace(preferredLanguage)
                ? _current.Jurisdiction.DefaultLanguage
                : preferredLanguage.Trim();

            if (!_current.Jurisdiction.SupportedLanguages.Contains(normalizedLanguage, StringComparer.OrdinalIgnoreCase))
            {
                normalizedLanguage = _current.Jurisdiction.DefaultLanguage;
            }

            entity.PreferredLanguage = normalizedLanguage;
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(AppUser), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated preferred language to {normalizedLanguage}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddUnitAsync(Unit unit)
        {
            EnsureCanManageData();
            await EnsureUnitLimitAsync();
            unit.Id = Guid.NewGuid();
            unit.OrganizationId = _current.OrganizationId;
            unit.CreatedUtc = DateTime.UtcNow;
            _db.Units.Add(unit);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Unit), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created unit {unit.UnitNumber}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteUnitAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Units.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Units.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Unit), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted unit {entity.UnitNumber}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateUnitAsync(Unit unit)
        {
            EnsureCanManageData();
            var entity = await _db.Units.FirstOrDefaultAsync(x => x.Id == unit.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.PropertyId = unit.PropertyId;
            entity.UnitNumber = unit.UnitNumber;
            entity.Bedrooms = unit.Bedrooms;
            entity.Bathrooms = unit.Bathrooms;
            entity.MonthlyRent = unit.MonthlyRent;
            entity.IsOccupied = unit.IsOccupied;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Unit), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated unit {unit.UnitNumber}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddLeaseAsync(Lease lease)
        {
            EnsureCanManageData();
            lease.Id = Guid.NewGuid();
            lease.OrganizationId = _current.OrganizationId;
            lease.CreatedUtc = DateTime.UtcNow;
            _db.Leases.Add(lease);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Lease), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created lease for unit {lease.UnitId}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteLeaseAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Leases.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Leases.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Lease), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted lease {entity.Id}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateLeaseAsync(Lease lease)
        {
            EnsureCanManageData();
            var entity = await _db.Leases.FirstOrDefaultAsync(x => x.Id == lease.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.UnitId = lease.UnitId;
            entity.TenantId = lease.TenantId;
            entity.StartDate = lease.StartDate;
            entity.EndDate = lease.EndDate;
            entity.MonthlyRent = lease.MonthlyRent;
            entity.Status = lease.Status;
            entity.StandardOntarioLeaseSigned = lease.StandardOntarioLeaseSigned;
            entity.N1IncreaseNoticeScheduled = lease.N1IncreaseNoticeScheduled;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Lease), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated lease {lease.Id}" });
            await _db.SaveChangesAsync();
        }
        public async Task AddMaintenanceAsync(MaintenanceRequest request)
        {
            EnsureCanManageData();
            request.Id = Guid.NewGuid();
            request.OrganizationId = _current.OrganizationId;
            request.CreatedUtc = DateTime.UtcNow;
            if (request.RequestedDate == default) request.RequestedDate = DateOnly.FromDateTime(DateTime.Today);
            _db.MaintenanceRequests.Add(request);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(MaintenanceRequest), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created maintenance request {request.Title}" });
            await _db.SaveChangesAsync();

            var propertyName = await _db.Properties
                .Where(x => x.Id == request.PropertyId && x.OrganizationId == _current.OrganizationId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? "Unknown property";

            var unitNumber = request.UnitId is null
                ? null
                : await _db.Units
                    .Where(x => x.Id == request.UnitId.Value && x.OrganizationId == _current.OrganizationId)
                    .Select(x => x.UnitNumber)
                    .FirstOrDefaultAsync();

            await _notifications.SendMaintenanceRequestCreatedAsync(request, propertyName, unitNumber);
        }

        public async Task DeleteMaintenanceAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.MaintenanceRequests.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.MaintenanceRequests.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(MaintenanceRequest), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted maintenance request {entity.Title}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateMaintenanceAsync(MaintenanceRequest request)
        {
            EnsureCanManageData();
            var entity = await _db.MaintenanceRequests.FirstOrDefaultAsync(x => x.Id == request.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.PropertyId = request.PropertyId;
            entity.UnitId = request.UnitId;
            entity.Title = request.Title;
            entity.Description = request.Description;
            entity.Priority = request.Priority;
            entity.Status = request.Status;
            entity.VendorName = request.VendorName;
            entity.EstimatedCost = request.EstimatedCost;
            entity.RequestedDate = request.RequestedDate;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(MaintenanceRequest), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated maintenance request {request.Title}" });
            await _db.SaveChangesAsync();
        }

        public async Task ImportSamplePortfolioAsync()
        {
            if (!_current.CanManageData) return;
            if (await _db.Properties.AnyAsync(x => x.OrganizationId == _current.OrganizationId)) return;
            await EnsureUnitLimitAsync();

            var property = new Property { OrganizationId = _current.OrganizationId, Name = "Lakeshore Residences", PropertyType = "Waterfront condo", AddressLine1 = "25 Queens Quay W", City = "Toronto", PostalCode = "M5J 2N6", YearBuilt = 2018, MonthlyRevenueTarget = 12500m, AmenitySummary = "Concierge, fitness room, parking waitlist", NeighborhoodNotes = "Strong waterfront demand with transit, trail and downtown access.", LeasingNotes = "Best suited for professionals seeking downtown access with premium views.", OperationalNotes = "Protect turnover speed during peak spring leasing cycle and keep concierge communication tight." };
            var unit = new Unit { OrganizationId = _current.OrganizationId, Property = property, UnitNumber = "1204", Bedrooms = 2, Bathrooms = 2, MonthlyRent = 3150m, IsOccupied = true };
            var tenant = new Tenant { OrganizationId = _current.OrganizationId, FullName = "Avery Martin", Email = "avery@example.com", PhoneNumber = "416-555-0198", CreditScore = 742, ScreeningCompleted = true, ScreeningProvider = "OpenRoom / FrontLobby" };

            _db.Properties.Add(property);
            _db.Units.Add(unit);
            _db.Tenants.Add(tenant);
            _db.Leases.Add(new Lease { OrganizationId = _current.OrganizationId, Unit = unit, Tenant = tenant, StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)), MonthlyRent = 3150m, Status = LeaseStatus.Active, StandardOntarioLeaseSigned = true, N1IncreaseNoticeScheduled = false });
            _db.MaintenanceRequests.Add(new MaintenanceRequest { OrganizationId = _current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, Title = "HVAC spring inspection", Description = "Preventive maintenance before cooling season.", Priority = MaintenancePriority.Medium, Status = "Scheduled", VendorName = "Toronto HVAC Collective", EstimatedCost = 240m, RequestedDate = DateOnly.FromDateTime(DateTime.Today) });
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Property), Action = "Import", PerformedBy = _current.UserEmail, Details = "Imported starter portfolio data" });

            await _db.SaveChangesAsync();
        }

        public async Task<List<SuperAdminOrganizationDto>> GetSuperAdminOrganizationsAsync(CancellationToken cancellationToken = default)
        {
            if (!_current.IsSuperAdmin)
            {
                throw new InvalidOperationException("Current user is not a super admin.");
            }

            return await _db.Organizations
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new SuperAdminOrganizationDto
                {
                    OrganizationId = x.Id,
                    OrganizationName = x.Name,
                    SubscriptionTier = x.SubscriptionTier.ToString(),
                    Units = _db.Units.Count(unit => unit.OrganizationId == x.Id),
                    Users = _db.OrganizationMemberships.Count(membership => membership.OrganizationId == x.Id && membership.Status == "Active")
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<SupportSessionDto> GrantSupportAccessAsync(Guid organizationId, string reason, CancellationToken cancellationToken = default)
        {
            if (!_current.IsSuperAdmin)
            {
                throw new InvalidOperationException("Current user is not a super admin.");
            }

            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Support review" : reason.Trim();
            var organization = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken)
                ?? throw new InvalidOperationException("Organization not found.");
            var adminUser = await _db.Users.FirstOrDefaultAsync(x => x.Id == _current.UserId, cancellationToken)
                ?? throw new InvalidOperationException("Current user profile was not found.");

            var membership = await _db.OrganizationMemberships.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == adminUser.Id, cancellationToken);
            if (membership is null)
            {
                membership = new OrganizationMembership
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = adminUser.Id,
                    Role = SupportRole,
                    Status = "Active",
                    CreatedUtc = DateTime.UtcNow
                };
                _db.OrganizationMemberships.Add(membership);
            }
            else
            {
                membership.Role = SupportRole;
                membership.Status = "Active";
            }

            adminUser.OrganizationId = organizationId;
            adminUser.Role = SupportRole;

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                EntityName = "SupportAccess",
                Action = "Grant",
                PerformedBy = _current.UserEmail,
                Details = normalizedReason
            });

            await _db.SaveChangesAsync(cancellationToken);

            return new SupportSessionDto
            {
                OrganizationId = organizationId,
                OrganizationName = organization.Name,
                UserEmail = _current.UserEmail,
                Reason = normalizedReason,
                ExpiresUtc = DateTime.UtcNow.AddHours(2)
            };
        }

        private static string NormalizeInviteRole(string role)
            => role?.Trim() switch
            {
                "Manager" => "Manager",
                "Agent" => "Agent",
                "Viewer" => "Viewer",
                _ => "Viewer"
            };

        private static string BuildSlug(string value)
        {
            var chars = value
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray();

            var slug = new string(chars);
            while (slug.Contains("--", StringComparison.Ordinal))
            {
                slug = slug.Replace("--", "-", StringComparison.Ordinal);
            }

            return slug.Trim('-');
        }
    }
}

namespace PropertySaaS.Application.Dashboard
{
    using PropertySaaS.Application.Common;
    using PropertySaaS.Application.Features;

    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<SaasDataService>();
            services.AddScoped(_ => new CurrentOrganization
            {
                UserId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                OrganizationName = "Maple Leaf Property Group",
                UserEmail = "owner@mapleleafpm.ca",
                Role = "Owner",
                Province = "ON",
                CountryCode = "CA",
                PreferredLanguage = "en-CA"
            });
            return services;
        }
    }
}












