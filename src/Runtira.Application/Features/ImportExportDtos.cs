namespace Runtira.Application.Features
{
    public sealed class RuntiraImportValidationContextDto
    {
        public string SourceName { get; set; } = string.Empty;
        public string JurisdictionDisplayName { get; set; } = string.Empty;
        public IReadOnlyList<RuntiraLeadFormFieldDto> Fields { get; set; } = Array.Empty<RuntiraLeadFormFieldDto>();
    }

    public sealed class RuntiraImportApprovalRequestDto
    {
        public string SourceName { get; set; } = string.Empty;
        public Dictionary<string, string> DynamicFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RuntiraImportApprovalResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
    }

    public sealed class RuntiraImportSourceDto
    {
        public string SourceName { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int SuggestedRecordCount { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    public sealed class RuntiraImportFieldSuggestionDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string SuggestedValue { get; set; } = string.Empty;
        public int ConfidenceScore { get; set; }
    }

    public sealed class RuntiraImportWorkspaceDto
    {
        public string ActiveRegion { get; set; } = string.Empty;
        public string ActiveLanguage { get; set; } = string.Empty;
        public IReadOnlyList<string> SupportedFormats { get; set; } = Array.Empty<string>();
        public IReadOnlyList<RuntiraImportSourceDto> Sources { get; set; } = Array.Empty<RuntiraImportSourceDto>();
        public IReadOnlyList<RuntiraImportFieldSuggestionDto> SuggestedFields { get; set; } = Array.Empty<RuntiraImportFieldSuggestionDto>();
        public IReadOnlyList<RuntiraUploadJobDto> UploadJobs { get; set; } = Array.Empty<RuntiraUploadJobDto>();
    }

    public sealed class RuntiraUploadJobDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
        public string BlobPath { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public bool RequiresSuperAdminReview { get; set; }
    }

    public sealed class RuntiraExportOptionDto
    {
        public string Format { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }

    public sealed class RuntiraExportWorkspaceDto
    {
        public string ActiveRegion { get; set; } = string.Empty;
        public string ActiveLanguage { get; set; } = string.Empty;
        public IReadOnlyList<RuntiraExportOptionDto> Options { get; set; } = Array.Empty<RuntiraExportOptionDto>();
        public IReadOnlyList<string> SupportedDestinations { get; set; } = Array.Empty<string>();
    }

    public sealed class RuntiraExportFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/csv";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
