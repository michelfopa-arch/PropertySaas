using PropertySaaS.Domain.Common;

namespace PropertySaaS.Domain.Entities;

public class ComplianceReminder : TenantScopedEntity
{
    public string Title { get; set; } = string.Empty;
    public string NoticeType { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public string Reference { get; set; } = string.Empty;
}
