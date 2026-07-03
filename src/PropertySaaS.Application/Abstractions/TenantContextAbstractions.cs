namespace PropertySaaS.Application.Abstractions
{
    public interface ITenantContextAccessor
    {
        Guid? TenantId { get; }
        bool BypassTenantFilter { get; }
    }
}

namespace PropertySaaS.Application.Common
{
    public sealed class TenantContext : PropertySaaS.Application.Abstractions.ITenantContextAccessor
    {
        public Guid? TenantId { get; init; }
        public bool BypassTenantFilter { get; init; }
        public string TenantSlug { get; init; } = string.Empty;
        public string UserEmail { get; init; } = string.Empty;
    }
}
