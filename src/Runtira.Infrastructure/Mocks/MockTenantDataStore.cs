using Runtira.Infrastructure.Data;

namespace Runtira.Infrastructure.Mocks
{
    /// <summary>
    /// Thread-safe in-memory clone of <see cref="RuntiraDemoSeedData"/> used when Cosmos DB
    /// mock mode is enabled (<see cref="Runtira.Infrastructure.Options.CosmosOptions.MockModeEnabled"/>).
    /// Lets the Mock*Store classes serve/mutate the same demo dataset without ever contacting
    /// Cosmos DB, so the app stays fully mocked until Cosmos is explicitly enabled.
    /// </summary>
    internal sealed class MockTenantDataStore
    {
        private readonly object _lock = new();
        private readonly List<CosmosDocument> _documents;

        public MockTenantDataStore()
        {
            _documents = new List<CosmosDocument>(RuntiraDemoSeedData.BuildSeedDocuments());
        }

        public List<CosmosDocument> QueryTenant(Guid tenantId, string type)
        {
            var tenantIdText = tenantId.ToString();
            lock (_lock)
            {
                return _documents
                    .Where(x => string.Equals(x.tenantId, tenantIdText, StringComparison.OrdinalIgnoreCase) && string.Equals(x.type, type, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        public List<CosmosDocument> QueryGlobal(string type)
        {
            lock (_lock)
            {
                return _documents.Where(x => string.Equals(x.type, type, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        public CosmosDocument? FindTenantById(Guid tenantId, string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var tenantIdText = tenantId.ToString();
            lock (_lock)
            {
                return _documents.FirstOrDefault(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase) && string.Equals(x.tenantId, tenantIdText, StringComparison.OrdinalIgnoreCase));
            }
        }

        public CosmosDocument? FindGlobalById(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            lock (_lock)
            {
                return _documents.FirstOrDefault(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void Upsert(CosmosDocument document)
        {
            lock (_lock)
            {
                var index = _documents.FindIndex(x => string.Equals(x.id, document.id, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    _documents[index] = document;
                }
                else
                {
                    _documents.Add(document);
                }
            }
        }

        public void Delete(string id)
        {
            lock (_lock)
            {
                _documents.RemoveAll(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
