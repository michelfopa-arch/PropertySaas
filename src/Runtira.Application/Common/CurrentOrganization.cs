namespace Runtira.Application.Common
{
    public class CurrentOrganization
    {
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public int AccessibleOrganizationCount { get; set; }
        public bool HasSuperAdminOrganizationSelection { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationSlug { get; set; } = string.Empty;
        public string PropertySlug { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public bool IsDemo { get; set; }
        public DateTime? DemoExpiresUtc { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Owner";
        public string SystemRole { get; set; } = "User";
        public string Province { get; set; } = "ON";
        public string CountryCode { get; set; } = "CA";
        public string PreferredLanguage { get; set; } = "en-CA";
        public bool SubscriptionIsActive { get; set; } = true;
        public bool TrialExpired { get; set; }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserEmail);
        public bool HasOrganizationAccess => OrganizationId != Guid.Empty;
        public bool CanAccessWorkspace => HasOrganizationAccess && (SubscriptionIsActive || !TrialExpired);
        public bool RequiresOrganizationSelection => !HasOrganizationAccess && (AccessibleOrganizationCount > 1 || HasSuperAdminOrganizationSelection);
        public bool IsSupervisor => string.Equals(SystemRole, "Supervisor", StringComparison.OrdinalIgnoreCase);
        public bool CanManageData => Role is "Owner" or "Manager" or "SuperAdmin" or "Supervisor" || IsSupervisor;
        public bool IsSuperAdmin => string.Equals(SystemRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        public IReadOnlyList<OrganizationAccessOptionDto> OrganizationOptions { get; set; } = Array.Empty<OrganizationAccessOptionDto>();
        public JurisdictionProfile Jurisdiction => JurisdictionCatalog.GetProfile(Province);
    }
}
