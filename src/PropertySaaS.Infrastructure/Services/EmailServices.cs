using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertySaaS.Application.Abstractions;
using PropertySaaS.Application.Common;
using PropertySaaS.Domain.Entities;
using PropertySaaS.Infrastructure.Options;

namespace PropertySaaS.Infrastructure.Options
{
    public sealed class ResendOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "onboarding@resend.dev";
        public string FromName { get; set; } = "PropertySaaS";
        public string SupportEmail { get; set; } = "michelfopa@gmail.com";
    }
}

namespace PropertySaaS.Infrastructure.Services
{
    internal sealed class ResendEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly ResendOptions _options;
        private readonly ILogger<ResendEmailService> _logger;

        public ResendEmailService(HttpClient httpClient, IOptions<ResendOptions> options, ILogger<ResendEmailService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string html, string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("Resend API key is not configured. Email to {Recipient} with subject {Subject} was skipped.", to, subject);
                return;
            }

            var payload = new ResendEmailRequest
            {
                From = string.IsNullOrWhiteSpace(_options.FromName)
                    ? _options.FromEmail
                    : $"{_options.FromName} <{_options.FromEmail}>",
                To = new[] { to },
                Subject = subject,
                Html = html,
                Text = text
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Resend email failed with status {StatusCode}: {Body}", response.StatusCode, body);
            }
        }

        private sealed class ResendEmailRequest
        {
            [JsonPropertyName("from")]
            public string From { get; set; } = string.Empty;

            [JsonPropertyName("to")]
            public string[] To { get; set; } = Array.Empty<string>();

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("html")]
            public string Html { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }
    }

    internal sealed class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly IApplicationDbContext _db;
        private readonly CurrentOrganization _current;
        private readonly ResendOptions _options;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IEmailService emailService,
            IApplicationDbContext db,
            CurrentOrganization current,
            IOptions<ResendOptions> options,
            ILogger<NotificationService> logger)
        {
            _emailService = emailService;
            _db = db;
            _current = current;
            _options = options.Value;
            _logger = logger;
        }

        public Task SendMaintenanceRequestCreatedAsync(MaintenanceRequest request, string propertyName, string? unitNumber, CancellationToken cancellationToken = default)
        {
            var unitText = string.IsNullOrWhiteSpace(unitNumber) ? "Portfolio-level" : $"Unit {unitNumber}";
            var subject = $"[PropertySaaS] New maintenance request: {request.Title}";
            var html = $"""
                <h2>New maintenance request created</h2>
                <p><strong>Organization:</strong> {_current.OrganizationName}</p>
                <p><strong>Property:</strong> {propertyName}</p>
                <p><strong>Scope:</strong> {unitText}</p>
                <p><strong>Priority:</strong> {request.Priority}</p>
                <p><strong>Status:</strong> {request.Status}</p>
                <p><strong>Requested date:</strong> {request.RequestedDate}</p>
                <p><strong>Description:</strong> {request.Description}</p>
                <p><strong>Vendor:</strong> {request.VendorName}</p>
                <p><strong>Created by:</strong> {_current.UserEmail}</p>
                """;
            var text = $"New maintenance request created\nOrganization: {_current.OrganizationName}\nProperty: {propertyName}\nScope: {unitText}\nPriority: {request.Priority}\nStatus: {request.Status}\nRequested date: {request.RequestedDate}\nDescription: {request.Description}\nVendor: {request.VendorName}\nCreated by: {_current.UserEmail}";

            return SendSafeAsync(ResolveSupportEmail(), subject, html, text, cancellationToken);
        }

        public async Task SendComplianceDueSoonDigestAsync(CancellationToken cancellationToken = default)
        {
            var dueSoon = await _db.ComplianceReminders
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId && !x.IsCompleted && x.DueDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(14)))
                .OrderBy(x => x.DueDate)
                .ToListAsync(cancellationToken);

            if (dueSoon.Count == 0)
            {
                _logger.LogInformation("No compliance reminders due soon for organization {OrganizationId}.", _current.OrganizationId);
                return;
            }

            var subject = $"[PropertySaaS] Compliance due soon digest ({dueSoon.Count})";
            var itemsHtml = string.Join(string.Empty, dueSoon.Select(reminder => $"<li><strong>{reminder.Title}</strong> - {reminder.NoticeType} - due {reminder.DueDate} - {reminder.Reference}</li>"));
            var itemsText = string.Join(Environment.NewLine, dueSoon.Select(reminder => $"- {reminder.Title} | {reminder.NoticeType} | due {reminder.DueDate} | {reminder.Reference}"));
            var html = $"""
                <h2>Compliance due soon</h2>
                <p><strong>Organization:</strong> {_current.OrganizationName}</p>
                <ul>{itemsHtml}</ul>
                """;
            var text = $"Compliance due soon\nOrganization: {_current.OrganizationName}\n{itemsText}";

            await SendSafeAsync(ResolveSupportEmail(), subject, html, text, cancellationToken);
        }

        public Task SendSupportAlertAsync(string subject, string message, CancellationToken cancellationToken = default)
            => SendSafeAsync(ResolveSupportEmail(), subject, $"<pre>{System.Net.WebUtility.HtmlEncode(message)}</pre>", message, cancellationToken);

        public Task SendOrganizationInvitationAsync(string to, string organizationName, string invitedBy, string role, string invitationUrl, DateTime expiresUtc, CancellationToken cancellationToken = default)
        {
            var subject = $"[PropertySaaS] Invitation to join {organizationName}";
            var expirationText = expiresUtc.ToString("yyyy-MM-dd HH:mm 'UTC'");
            var html = $"""
                <h2>You're invited to join {organizationName}</h2>
                <p><strong>Invited by:</strong> {invitedBy}</p>
                <p><strong>Role:</strong> {role}</p>
                <p>Use the secure link below to accept your invitation:</p>
                <p><a href=\"{invitationUrl}\" style=\"display:inline-block;padding:12px 18px;background:#2563eb;color:#ffffff;text-decoration:none;border-radius:6px;\">Accept invitation</a></p>
                <p>If the button does not open, copy and paste this URL into your browser:</p>
                <p><a href=\"{invitationUrl}\">{invitationUrl}</a></p>
                <p><strong>Expires:</strong> {expirationText}</p>
                """;
            var text = $"You're invited to join {organizationName}\nInvited by: {invitedBy}\nRole: {role}\nAccept invitation: {invitationUrl}\nExpires: {expirationText}";

            return SendSafeAsync(to, subject, html, text, cancellationToken);
        }

        public Task SendInvoiceEmailAsync(string to, string subject, string html, string text, CancellationToken cancellationToken = default)
            => SendSafeAsync(to, subject, html, text, cancellationToken);

        private async Task SendSafeAsync(string recipient, string subject, string html, string text, CancellationToken cancellationToken)
        {
            try
            {
                await _emailService.SendAsync(recipient, subject, html, text, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification email with subject {Subject}.", subject);
            }
        }

        private string ResolveSupportEmail()
            => string.IsNullOrWhiteSpace(_options.SupportEmail) ? "michelfopa@gmail.com" : _options.SupportEmail;
    }
}