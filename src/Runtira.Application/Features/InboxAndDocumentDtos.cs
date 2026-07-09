namespace Runtira.Application.Features
{
    public sealed class RuntiraInboxMessageDto
    {
        public Guid Id { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string PreviewText { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime ReceivedUtc { get; set; }
        public bool HasAttachments { get; set; }
    }

    public sealed class RuntiraInboxActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class RuntiraDocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UploadedUtc { get; set; }
        public long SizeBytes { get; set; }
    }

    public sealed class RuntiraDocumentActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
