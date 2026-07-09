namespace Runtira.Application.Abstractions
{
    public interface ITenantContextAccessor
    {
        Guid? TenantId { get; }
        bool BypassTenantFilter { get; }
    }
}
