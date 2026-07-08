namespace Runtira.Infrastructure.Data
{
    using Runtira.Infrastructure.Options;

    public sealed record CosmosContainerDefinition(string Name, string PartitionKeyPath);

    public static class CosmosSchemaDefinition
    {
        public static IReadOnlyList<CosmosContainerDefinition> Build(CosmosOptions options)
            =>
            [
                new("Organizations", "/id"),
                new("Users", "/id"),
                new("TenantCore", "/tenantId"),
                new("Conversations", "/tenantId"),
                new("Messages", "/tenantId"),
                new("Inbox", "/tenantId"),
                new("BlobArchives", "/tenantId"),
                new("Billing", "/tenantId")
            ];
    }
}
