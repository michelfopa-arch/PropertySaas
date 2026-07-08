using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Runtira.Infrastructure.Mocks;
using Runtira.Infrastructure.Options;

namespace Runtira.Infrastructure.Data
{
    /// <summary>
    /// Orchestrates writing the Runtira demo/mock seed documents (see <see cref="RuntiraDemoSeedData"/>)
    /// to their respective Cosmos DB containers on startup. Contains no mock data itself.
    /// </summary>
    internal sealed class CosmosSeedService : IHostedService
    {
        private readonly CosmosClient? _cosmosClient;
        private readonly CosmosOptions _options;
        private readonly ILogger<CosmosSeedService> _logger;

        public CosmosSeedService(CosmosClient? cosmosClient, CosmosOptions options, ILogger<CosmosSeedService> logger)
        {
            _cosmosClient = cosmosClient;
            _options = options;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_cosmosClient is null || !_options.Enabled)
            {
                return;
            }

            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var organizations = database.GetContainer("Organizations");
            var users = database.GetContainer("Users");
            var tenantCore = database.GetContainer("TenantCore");
            var inbox = database.GetContainer("Inbox");
            var blobArchives = database.GetContainer("BlobArchives");

            foreach (var document in RuntiraDemoSeedData.BuildSeedDocuments())
            {
                var container = document.type switch
                {
                    "organization" => organizations,
                    "user" => users,
                    "inboxMessage" => inbox,
                    "attachment" => inbox,
                    "blobArchive" => blobArchives,
                    _ => tenantCore
                };

                var partitionKey = document.type switch
                {
                    "organization" => new PartitionKey(document.id),
                    "user" => new PartitionKey(document.id),
                    _ => new PartitionKey(document.tenantId)
                };

                await container.UpsertItemAsync(document, partitionKey, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Cosmos DB seed data for Runtira demo tenants is ready.");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
