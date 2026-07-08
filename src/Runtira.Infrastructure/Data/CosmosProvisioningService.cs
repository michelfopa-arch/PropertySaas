using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Runtira.Infrastructure.Options;

namespace Runtira.Infrastructure.Data
{
    internal sealed class CosmosProvisioningService : IHostedService
    {
        private readonly CosmosClient? _cosmosClient;
        private readonly CosmosOptions _options;
        private readonly ILogger<CosmosProvisioningService> _logger;

        public CosmosProvisioningService(CosmosClient? cosmosClient, CosmosOptions options, ILogger<CosmosProvisioningService> logger)
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

            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                _options.DatabaseName,
                ThroughputProperties.CreateAutoscaleThroughput(_options.SharedAutoscaleMaxThroughput),
                cancellationToken: cancellationToken);

            var database = databaseResponse.Database;

            foreach (var containerDefinition in CosmosSchemaDefinition.Build(_options))
            {
                await database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties(containerDefinition.Name, containerDefinition.PartitionKeyPath),
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Cosmos DB database {DatabaseName} and compact Runtira containers are ready.", _options.DatabaseName);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
