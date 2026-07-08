using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Runtira.Application.Abstractions;
using Runtira.Infrastructure.Options;

namespace Runtira.Infrastructure.Services
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
                _logger.LogWarning("Resend API key is not configured. Email to {Recipient} was skipped.", to);
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
            {
                Content = JsonContent.Create(new ResendEmailRequest
                {
                    From = string.IsNullOrWhiteSpace(_options.FromName) ? _options.FromEmail : $"{_options.FromName} <{_options.FromEmail}>",
                    To = new[] { to },
                    Subject = subject,
                    Html = html,
                    Text = text
                })
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
}
