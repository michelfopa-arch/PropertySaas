using Microsoft.Azure.Cosmos;

namespace Runtira.Infrastructure.Data
{
    internal static class CosmosDocumentHelpers
    {
        public static async Task<CosmosDocument?> ReadItemAsync(Container container, Guid id, Guid tenantId, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            try
            {
                var response = await container.ReadItemAsync<CosmosDocument>(id.ToString(), new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public static async Task<CosmosDocument?> QuerySingleAsync(Container container, Guid tenantId, string queryText, CancellationToken cancellationToken, params (string Name, object Value)[] extraParameters)
            => (await QueryManyAsync(container, tenantId, queryText, cancellationToken, extraParameters)).FirstOrDefault();

        public static async Task<List<CosmosDocument>> QueryManyAsync(Container container, Guid tenantId, string queryText, CancellationToken cancellationToken, params (string Name, object Value)[] extraParameters)
        {
            var queryDefinition = new QueryDefinition(queryText).WithParameter("@tenantId", tenantId.ToString());
            foreach (var parameter in extraParameters)
            {
                queryDefinition = queryDefinition.WithParameter(parameter.Name, parameter.Value);
            }

            var iterator = container.GetItemQueryIterator<CosmosDocument>(queryDefinition, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId.ToString())
            });

            var results = new List<CosmosDocument>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }

            return results;
        }

        public static Guid ParseGuid(string? value)
            => Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;

        public static Guid GetGuid(CosmosDocument document, string fieldName)
            => Guid.TryParse(GetString(document, fieldName), out var parsed) ? parsed : Guid.Empty;

        public static string GetString(CosmosDocument document, string fieldName, string defaultValue = "")
            => document.data.TryGetValue(fieldName, out var value) && value is not null
                ? value.ToString() ?? defaultValue
                : defaultValue;

        public static int GetInt(CosmosDocument document, string fieldName)
            => document.data.TryGetValue(fieldName, out var value) && value is not null && int.TryParse(value.ToString(), out var parsed) ? parsed : 0;

        public static decimal GetDecimal(CosmosDocument document, string fieldName)
            => document.data.TryGetValue(fieldName, out var value) && value is not null && decimal.TryParse(value.ToString(), out var parsed) ? parsed : 0m;

        public static DateTime GetDateTime(CosmosDocument document, string fieldName)
            => document.data.TryGetValue(fieldName, out var value) && value is not null && DateTime.TryParse(value.ToString(), out var parsed) ? parsed : DateTime.MinValue;

        public static DateTime? GetDateTimeOrNull(CosmosDocument document, string fieldName)
            => document.data.TryGetValue(fieldName, out var value) && value is not null && DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null;

        public static void SetValue(CosmosDocument document, string fieldName, object? value)
            => document.data[fieldName] = value;

        public static CosmosDocument CreateOrganizationDocument(string id, string name, string slug, string ownerEmail, string defaultLocale, string countryCode, string regionCode, string timeZone, string legalProfileJson)
            => new()
            {
                id = id,
                type = "organization",
                data = new Dictionary<string, object?>
                {
                    ["name"] = name,
                    ["slug"] = slug,
                    ["ownerEmail"] = ownerEmail,
                    ["defaultLocale"] = defaultLocale,
                    ["countryCode"] = countryCode,
                    ["regionCode"] = regionCode,
                    ["timeZone"] = timeZone,
                    ["legalProfileJson"] = legalProfileJson,
                    ["additionalSettingsJson"] = "{\"tenantMode\":\"path\",\"archive\":\"blob\"}",
                    ["stripeCustomerId"] = string.Empty,
                    ["stripeSubscriptionId"] = string.Empty,
                    ["billingPlan"] = "Trial",
                    ["isActive"] = true,
                    ["createdUtc"] = "2026-07-03T00:00:00Z",
                    ["modifiedUtc"] = null
                }
            };

        public static CosmosDocument CreateUserDocument(string id, string clerkUserId, string email, string fullName, string preferredLanguage, bool isSuperAdmin)
            => new()
            {
                id = id,
                type = "user",
                data = new Dictionary<string, object?>
                {
                    ["clerkUserId"] = clerkUserId,
                    ["email"] = email,
                    ["fullName"] = fullName,
                    ["preferredLanguage"] = preferredLanguage,
                    ["isSuperAdmin"] = isSuperAdmin,
                    ["isActive"] = true,
                    ["createdUtc"] = "2026-07-03T00:00:00Z",
                    ["modifiedUtc"] = null
                }
            };

        public static CosmosDocument CreateTenantDocument(string type, string id, string tenantId, Dictionary<string, object?> data)
            => new()
            {
                id = id,
                type = type,
                tenantId = tenantId,
                data = data
            };

        public static List<string> ParseList(string? json)
            => Runtira.Application.Common.RuntiraJson.Deserialize<List<string>>(json) ?? new List<string>();

        public static (IReadOnlyList<string> VisibleFields, HashSet<string> RequiredFields) ParseContextFormDefinition(string? json, string formKey, IReadOnlyList<string> defaultVisibleFields, IReadOnlyList<string> defaultRequiredFields)
        {
            var payload = Runtira.Application.Common.RuntiraJson.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(json);
            if (payload is not null
                && payload.TryGetValue(formKey, out var formDefinition)
                && formDefinition.TryGetValue("visibleFields", out var visibleFields)
                && visibleFields.Count > 0)
            {
                var required = formDefinition.TryGetValue("requiredFields", out var requiredFields)
                    ? requiredFields
                    : defaultRequiredFields.ToList();
                return (visibleFields, new HashSet<string>(required, StringComparer.OrdinalIgnoreCase));
            }

            return (defaultVisibleFields, new HashSet<string>(defaultRequiredFields, StringComparer.OrdinalIgnoreCase));
        }
    }
}
