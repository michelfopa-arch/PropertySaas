using PropertySaaS.Domain.Entities;

namespace PropertySaaS.Application.Abstractions
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string html, string text, CancellationToken cancellationToken = default);
    }

    public interface INotificationService
    {
        Task SendMaintenanceRequestCreatedAsync(MaintenanceRequest request, string propertyName, string? unitNumber, CancellationToken cancellationToken = default);
        Task SendComplianceDueSoonDigestAsync(CancellationToken cancellationToken = default);
        Task SendSupportAlertAsync(string subject, string message, CancellationToken cancellationToken = default);
    }
}
