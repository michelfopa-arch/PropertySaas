using Runtira.Application.Abstractions;

namespace Runtira.Application.Common
{
    public sealed class TenantContext : ITenantContextAccessor
    {
        public Guid? TenantId { get; init; }
        public bool BypassTenantFilter { get; init; }
        public string TenantSlug { get; init; } = string.Empty;
        public string UserEmail { get; init; } = string.Empty;
    }
}
