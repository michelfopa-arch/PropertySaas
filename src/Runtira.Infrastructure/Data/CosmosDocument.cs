namespace Runtira.Infrastructure.Data
{
    internal sealed class CosmosDocument
    {
        public string id { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string? tenantId { get; set; }
        public Dictionary<string, object?> data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
