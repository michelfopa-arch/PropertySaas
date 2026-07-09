using System.Text.Json;

namespace Runtira.Application.Features
{
    public sealed partial class RuntiraWorkspaceService
    {
        public async Task<RuntiraImportValidationContextDto?> GetImportValidationContextAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            return new RuntiraImportValidationContextDto
            {
                SourceName = "prospects-q3.xlsx",
                JurisdictionDisplayName = $"{countryCode}-{regionCode}",
                Fields =
                [
                    new RuntiraLeadFormFieldDto { Key = "fullName", Required = true, SuggestedValue = "Taylor Morgan" },
                    new RuntiraLeadFormFieldDto { Key = "email", Required = true, SuggestedValue = "taylor@example.com" },
                    new RuntiraLeadFormFieldDto { Key = "preferredLanguage", Required = false, SuggestedValue = _currentOrganization.PreferredLanguage },
                    new RuntiraLeadFormFieldDto { Key = "summary", Required = false, SuggestedValue = "Validated from mock AI import pipeline." }
                ]
            };
        }

        public async Task<RuntiraImportApprovalResultDto> ApproveImportAsync(RuntiraImportApprovalRequestDto request, CancellationToken cancellationToken = default)
        {
            foreach (var field in new[] { "fullName", "email" })
            {
                if (!request.DynamicFields.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    return new RuntiraImportApprovalResultDto
                    {
                        ResultCode = "MissingRequiredField",
                        FieldKey = field
                    };
                }
            }

            var createRequest = new RuntiraCreateLeadRequestDto
            {
                AssetId = request.DynamicFields.TryGetValue("targetAsset", out var assetIdValue) && Guid.TryParse(assetIdValue, out var assetId) ? assetId : null,
                FullName = request.DynamicFields.TryGetValue("fullName", out var fullName) ? fullName : string.Empty,
                Email = request.DynamicFields.TryGetValue("email", out var email) ? email : string.Empty,
                PhoneNumber = request.DynamicFields.TryGetValue("phoneNumber", out var phoneNumber) ? phoneNumber : string.Empty,
                PreferredLanguage = request.DynamicFields.TryGetValue("preferredLanguage", out var preferredLanguage) ? preferredLanguage : _currentOrganization.PreferredLanguage,
                Summary = request.DynamicFields.TryGetValue("summary", out var summary) ? summary : $"Validated from {request.SourceName}.",
                DynamicFields = new Dictionary<string, string>(request.DynamicFields, StringComparer.OrdinalIgnoreCase)
            };

            var createResult = await CreateLeadAsync(createRequest, cancellationToken);
            return new RuntiraImportApprovalResultDto
            {
                Success = createResult.Success,
                ResultCode = createResult.Success ? "Approved" : createResult.ResultCode,
                LeadName = createResult.LeadName,
                FieldKey = createResult.FieldKey
            };
        }

        public async Task<RuntiraImportWorkspaceDto> GetImportWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.GetImportWorkspaceAsync(tenantId.Value, _currentOrganization.PreferredLanguage, cancellationToken);
            }

            return CreateFallbackImportWorkspace();
        }

        public async Task<RuntiraExportWorkspaceDto> GetExportWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            var summary = await GetWorkspaceSummaryAsync(cancellationToken);

            var options = new List<RuntiraExportOptionDto>
            {
                new()
                {
                    Format = "Excel",
                    Audience = "Operations",
                    Status = "Ready",
                    Summary = "Exporter les leads, résidents et actifs en tableur pour traitement métier."
                },
                new()
                {
                    Format = "PDF",
                    Audience = "Client",
                    Status = "Ready",
                    Summary = "Produire des documents envoyables comme factures, résumés locatifs et dossiers de validation."
                },
                new()
                {
                    Format = "Word",
                    Audience = "Legal",
                    Status = "Planned",
                    Summary = "Préparer des documents narratifs et modèles éditables pour contrats et communications."
                },
                new()
                {
                    Format = "CSV",
                    Audience = "Integrations",
                    Status = "Ready",
                    Summary = "Partager des extractions simples pour imports externes, BI ou scripts internes."
                }
            };

            return new RuntiraExportWorkspaceDto
            {
                ActiveRegion = summary is null ? $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}" : $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                ActiveLanguage = _currentOrganization.PreferredLanguage,
                Options = options,
                SupportedDestinations = ["Download", "EmailDraft", "BlobArchive", "ExternalShare"]
            };
        }

        public async Task<RuntiraExportFileDto?> ExportLeadsCsvAsync(CancellationToken cancellationToken = default)
        {
            var leads = await GetLeadsAsync(cancellationToken);
            if (leads.Count == 0)
            {
                return null;
            }

            var rows = new List<string>
            {
                "FullName,Email,Status,Source,AssetName,PreferredLanguage,QualificationScore,Summary"
            };

            rows.AddRange(leads.Select(lead => string.Join(',',
                EscapeCsv(lead.FullName),
                EscapeCsv(lead.Email),
                EscapeCsv(lead.Status),
                EscapeCsv(lead.Source),
                EscapeCsv(lead.AssetName),
                EscapeCsv(lead.PreferredLanguage),
                lead.QualificationScore.ToString(System.Globalization.CultureInfo.InvariantCulture),
                EscapeCsv(lead.Summary))));

            var content = string.Join(Environment.NewLine, rows);
            var slug = string.IsNullOrWhiteSpace(_currentOrganization.OrganizationSlug) ? "workspace" : _currentOrganization.OrganizationSlug;

            return new RuntiraExportFileDto
            {
                FileName = $"{slug}-leads-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
                ContentType = "text/csv; charset=utf-8",
                Content = System.Text.Encoding.UTF8.GetBytes(content)
            };
        }

        private RuntiraImportWorkspaceDto CreateFallbackImportWorkspace()
            => new()
            {
                ActiveRegion = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                ActiveLanguage = _currentOrganization.PreferredLanguage,
                SupportedFormats = ["Excel", "CSV", "Text", "PDF"],
                Sources =
                [
                    new()
                    {
                        SourceName = "prospects-q3.xlsx",
                        SourceType = "Excel",
                        Status = "MockReady",
                        SuggestedRecordCount = 12,
                        Summary = "Extraction simulée prête pour validation utilisateur."
                    }
                ],
                SuggestedFields =
                [
                    new()
                    {
                        FieldName = "PreferredLanguage",
                        SuggestedValue = _currentOrganization.PreferredLanguage,
                        ConfidenceScore = 98
                    }
                ],
                UploadJobs =
                [
                    new()
                    {
                        Id = Guid.Parse("91919191-aaaa-bbbb-cccc-101010101010"),
                        FileName = "owner-ledger-july.xlsx",
                        OrganizationName = "Runtira Demo Alberta",
                        PropertyName = "1180 17 Ave SW · Atlas 50",
                        QueueName = "manual-import-review",
                        BlobPath = "demo-alberta/uploads/owner-ledger-july.xlsx",
                        Status = "Queued",
                        CreatedUtc = DateTime.UtcNow.AddMinutes(-32),
                        RequiresSuperAdminReview = true
                    },
                    new()
                    {
                        Id = Guid.Parse("92929292-aaaa-bbbb-cccc-111111111111"),
                        FileName = "new-rent-roll.xlsx",
                        OrganizationName = "Runtira Demo Ontario",
                        PropertyName = "25 Carlton Street",
                        QueueName = "manual-import-review",
                        BlobPath = "demo-ontario/uploads/new-rent-roll.xlsx",
                        Status = "Reviewing",
                        CreatedUtc = DateTime.UtcNow.AddHours(-2),
                        RequiresSuperAdminReview = true
                    }
                ]
            };

        private static FormDefinition ParseContextFormDefinition(string? assetRulesJson, string sectionName, IReadOnlyList<string> defaultVisibleFields, IReadOnlyList<string> defaultRequiredFields)
        {
            if (string.IsNullOrWhiteSpace(assetRulesJson))
            {
                return new FormDefinition(defaultVisibleFields, defaultRequiredFields);
            }

            using var document = JsonDocument.Parse(assetRulesJson);
            if (!document.RootElement.TryGetProperty(sectionName, out var leadFormElement) || leadFormElement.ValueKind != JsonValueKind.Object)
            {
                return new FormDefinition(defaultVisibleFields, defaultRequiredFields);
            }

            var visibleFields = ReadStringArray(leadFormElement, "visibleFields", defaultVisibleFields);
            var requiredFields = ReadStringArray(leadFormElement, "requiredFields", defaultRequiredFields);
            return new FormDefinition(visibleFields, requiredFields);
        }

        private static IReadOnlyList<string> ReadStringArray(JsonElement parent, string propertyName, IReadOnlyList<string> fallback)
        {
            if (!parent.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            {
                return fallback;
            }

            var values = property
                .EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToList();

            return values.Count == 0 ? fallback : values;
        }

        private static string EscapeCsv(string? value)
        {
            var normalized = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            return $"\"{normalized.Replace("\"", "\"\"")}\"";
        }

        private sealed class FormDefinition
        {
            public FormDefinition(IReadOnlyList<string> visibleFields, IReadOnlyList<string> requiredFields)
            {
                VisibleFields = visibleFields;
                RequiredFields = new HashSet<string>(requiredFields, StringComparer.OrdinalIgnoreCase);
            }

            public IReadOnlyList<string> VisibleFields { get; }
            public HashSet<string> RequiredFields { get; }
        }
    }
}
