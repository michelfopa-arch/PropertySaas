using PropertySaaS.Application.Abstractions;
using PropertySaaS.Domain.Entities;

namespace PropertySaaS.Tests.TestDoubles;

internal sealed class NullNotificationService : INotificationService
{
    public Task SendMaintenanceRequestCreatedAsync(MaintenanceRequest request, string propertyName, string? unitNumber, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendComplianceDueSoonDigestAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendSupportAlertAsync(string subject, string message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
