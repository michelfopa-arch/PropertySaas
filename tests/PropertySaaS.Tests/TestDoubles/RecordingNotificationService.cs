using PropertySaaS.Application.Abstractions;
using PropertySaaS.Domain.Entities;

namespace PropertySaaS.Tests.TestDoubles;

internal sealed class RecordingNotificationService : INotificationService
{
    public List<(string To, string Subject, string Html, string Text)> InvoiceEmails { get; } = new();

    public Task SendMaintenanceRequestCreatedAsync(MaintenanceRequest request, string propertyName, string? unitNumber, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendComplianceDueSoonDigestAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendSupportAlertAsync(string subject, string message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendOrganizationInvitationAsync(string to, string organizationName, string invitedBy, string role, string invitationUrl, DateTime expiresUtc, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendInvoiceEmailAsync(string to, string subject, string html, string text, CancellationToken cancellationToken = default)
    {
        InvoiceEmails.Add((to, subject, html, text));
        return Task.CompletedTask;
    }
}
