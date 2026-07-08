using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Runtira.Infrastructure.Data;
using Runtira.Infrastructure.Options;

namespace Runtira.Infrastructure.Services.Billing
{
    public sealed class StripeBillingService
    {
        private readonly StripeOptions _options;
        private readonly HttpClient _httpClient;
        private readonly CosmosClient? _cosmosClient;
        private readonly CosmosOptions _cosmosOptions;
        private readonly ILogger<StripeBillingService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly IReadOnlyList<RuntiraBillingPlanDefinition> Plans = new[]
        {
            new RuntiraBillingPlanDefinition { Plan = "Starter", ProductName = "Runtira Starter", ProductDescription = "Solo AI-first workspace", UnitAmount = 4900, Currency = "usd", Interval = "month" },
            new RuntiraBillingPlanDefinition { Plan = "Growth", ProductName = "Runtira Growth", ProductDescription = "Growing multi-tenant workspace", UnitAmount = 12900, Currency = "usd", Interval = "month" },
            new RuntiraBillingPlanDefinition { Plan = "Pro", ProductName = "Runtira Pro", ProductDescription = "Advanced operations and admin coverage", UnitAmount = 24900, Currency = "usd", Interval = "month" }
        };

        public StripeBillingService(HttpClient httpClient, StripeOptions options, CosmosClient? cosmosClient, CosmosOptions cosmosOptions, ILogger<StripeBillingService> logger)
        {
            _httpClient = httpClient;
            _options = options;
            _cosmosClient = cosmosClient;
            _cosmosOptions = cosmosOptions;
            _logger = logger;
        }

        public async Task<List<StripePlanPriceDto>> GetPlanPricesAsync()
        {
            var results = new List<StripePlanPriceDto>();
            foreach (var plan in Plans)
            {
                var priceId = await EnsurePriceAsync(plan);
                results.Add(new StripePlanPriceDto
                {
                    Plan = plan.Plan,
                    PriceId = priceId,
                    Currency = plan.Currency,
                    UnitAmount = plan.UnitAmount / 100m,
                    Interval = plan.Interval,
                    DisplayPrice = FormatDisplayPrice(plan.UnitAmount, plan.Currency, plan.Interval)
                });
            }

            return results;
        }

        public async Task<string> CreateCheckoutSessionAsync(Guid organizationId, string ownerEmail, string plan, string successUrl, string cancelUrl)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return cancelUrl;
            }

            var planDefinition = Plans.FirstOrDefault(x => string.Equals(x.Plan, plan, StringComparison.OrdinalIgnoreCase)) ?? Plans[1];
            var priceId = await EnsurePriceAsync(planDefinition);
            if (string.IsNullOrWhiteSpace(priceId))
            {
                return cancelUrl;
            }

            using var request = CreateRequest(HttpMethod.Post, "checkout/sessions");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["mode"] = "subscription",
                ["success_url"] = successUrl,
                ["cancel_url"] = cancelUrl,
                ["client_reference_id"] = organizationId.ToString(),
                ["customer_email"] = ownerEmail,
                ["metadata[tenantId]"] = organizationId.ToString(),
                ["metadata[plan]"] = planDefinition.Plan,
                ["line_items[0][price]"] = priceId,
                ["line_items[0][quantity]"] = "1"
            });

            using var response = await _httpClient.SendAsync(request);
            var session = await ReadAsync<StripeCheckoutSessionResponse>(response);
            return string.IsNullOrWhiteSpace(session.Url) ? cancelUrl : session.Url;
        }

        public async Task<string> CreateBillingPortalAsync(string customerId, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey) || string.IsNullOrWhiteSpace(customerId))
            {
                return returnUrl;
            }

            using var request = CreateRequest(HttpMethod.Post, "billing_portal/sessions");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["customer"] = customerId,
                ["return_url"] = returnUrl
            });

            using var response = await _httpClient.SendAsync(request);
            var session = await ReadAsync<StripePortalSessionResponse>(response);
            return string.IsNullOrWhiteSpace(session.Url) ? returnUrl : session.Url;
        }

        public async Task HandleWebhookAsync(string json, string? stripeSignature)
        {
            _logger.LogInformation("Received Runtira Stripe webhook. Signature present: {HasSignature}", !string.IsNullOrWhiteSpace(stripeSignature));
            var stripeEvent = JsonSerializer.Deserialize<StripeWebhookEvent>(json, JsonOptions);
            if (stripeEvent is null)
            {
                return;
            }

            var organization = await ResolveOrganizationForEventAsync(stripeEvent);
            if (organization is null)
            {
                return;
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    if (stripeEvent.Data?.Object?.Deserialize<StripeCheckoutSessionResponse>() is { } checkoutSession)
                    {
                        organization.StripeCustomerId = checkoutSession.Customer ?? organization.StripeCustomerId;
                        organization.StripeSubscriptionId = checkoutSession.Subscription ?? organization.StripeSubscriptionId;
                        organization.BillingPlan = checkoutSession.Metadata?.TryGetValue("plan", out var checkoutPlan) == true ? checkoutPlan : organization.BillingPlan;
                    }
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                    if (stripeEvent.Data?.Object?.Deserialize<StripeSubscriptionResponse>() is { } subscription)
                    {
                        organization.StripeCustomerId = subscription.Customer ?? organization.StripeCustomerId;
                        organization.StripeSubscriptionId = subscription.Id ?? organization.StripeSubscriptionId;
                        organization.BillingPlan = ResolvePlanFromPriceId(subscription.Items?.Data?.FirstOrDefault()?.Price?.Id);
                        organization.IsActive = subscription.Status is "active" or "trialing" or "past_due";
                    }
                    break;

                case "customer.subscription.deleted":
                    organization.StripeSubscriptionId = string.Empty;
                    organization.BillingPlan = "Trial";
                    break;
            }

            await SaveOrganizationAsync(organization);
        }

        private async Task<string> EnsurePriceAsync(RuntiraBillingPlanDefinition plan)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return string.Empty;
            }

            var configuredPriceId = GetConfiguredPriceId(plan.Plan);
            if (!string.IsNullOrWhiteSpace(configuredPriceId))
            {
                return configuredPriceId;
            }

            var productId = await EnsureProductAsync(plan);
            if (string.IsNullOrWhiteSpace(productId))
            {
                return string.Empty;
            }

            using var request = CreateRequest(HttpMethod.Post, "prices");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["currency"] = plan.Currency,
                ["unit_amount"] = plan.UnitAmount.ToString(CultureInfo.InvariantCulture),
                ["product"] = productId,
                ["recurring[interval]"] = plan.Interval,
                ["metadata[plan]"] = plan.Plan,
                ["nickname"] = $"{plan.Plan} monthly"
            });

            using var response = await _httpClient.SendAsync(request);
            var price = await ReadAsync<StripePriceResponse>(response);
            return price.Id ?? string.Empty;
        }

        private async Task<string> EnsureProductAsync(RuntiraBillingPlanDefinition plan)
        {
            using var request = CreateRequest(HttpMethod.Post, "products");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["name"] = plan.ProductName,
                ["description"] = plan.ProductDescription,
                ["metadata[plan]"] = plan.Plan
            });

            using var response = await _httpClient.SendAsync(request);
            var product = await ReadAsync<StripeProductResponse>(response);
            return product.Id ?? string.Empty;
        }

        private string GetConfiguredPriceId(string plan)
            => plan.ToLowerInvariant() switch
            {
                "starter" => _options.StarterPriceId,
                "growth" => _options.GrowthPriceId,
                "pro" => _options.ProPriceId,
                _ => string.Empty
            };

        private async Task<Runtira.Domain.Entities.RuntiraOrganization?> ResolveOrganizationForEventAsync(StripeWebhookEvent stripeEvent)
        {
            return stripeEvent.Type switch
            {
                "checkout.session.completed" when stripeEvent.Data?.Object?.Deserialize<StripeCheckoutSessionResponse>() is { } checkoutSession
                    => await ResolveOrganizationAsync(checkoutSession.Customer, checkoutSession.Subscription, checkoutSession.ClientReferenceId),
                "customer.subscription.created" or "customer.subscription.updated" or "customer.subscription.deleted" when stripeEvent.Data?.Object?.Deserialize<StripeSubscriptionResponse>() is { } subscription
                    => await ResolveOrganizationAsync(subscription.Customer, subscription.Id, null),
                _ => null
            };
        }

        private async Task<Runtira.Domain.Entities.RuntiraOrganization?> ResolveOrganizationAsync(string? customerId, string? subscriptionId, string? tenantReference)
        {
            if (_cosmosClient is null || !_cosmosOptions.Enabled)
            {
                return null;
            }

            var organizations = await QueryGlobalOrganizationsAsync();
            if (Guid.TryParse(tenantReference, out var tenantId))
            {
                var byTenant = organizations.FirstOrDefault(x => x.Id == tenantId);
                if (byTenant is not null)
                {
                    return byTenant;
                }
            }

            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                var bySubscription = organizations.FirstOrDefault(x => x.StripeSubscriptionId == subscriptionId);
                if (bySubscription is not null)
                {
                    return bySubscription;
                }
            }

            return string.IsNullOrWhiteSpace(customerId)
                ? null
                : organizations.FirstOrDefault(x => x.StripeCustomerId == customerId);
        }

        private async Task<List<Runtira.Domain.Entities.RuntiraOrganization>> QueryGlobalOrganizationsAsync()
        {
            if (_cosmosClient is null)
            {
                return new List<Runtira.Domain.Entities.RuntiraOrganization>();
            }

            var container = _cosmosClient.GetDatabase(_cosmosOptions.DatabaseName).GetContainer("Organizations");
            var iterator = container.GetItemQueryIterator<CosmosDocument>(new QueryDefinition("SELECT * FROM c"));
            var results = new List<Runtira.Domain.Entities.RuntiraOrganization>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Select(x => new Runtira.Domain.Entities.RuntiraOrganization
                {
                    Id = Guid.Parse(x.id),
                    Name = x.data.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
                    Slug = x.data.TryGetValue("slug", out var slug) ? slug?.ToString() ?? string.Empty : string.Empty,
                    OwnerEmail = x.data.TryGetValue("ownerEmail", out var ownerEmail) ? ownerEmail?.ToString() ?? string.Empty : string.Empty,
                    DefaultLocale = x.data.TryGetValue("defaultLocale", out var defaultLocale) ? defaultLocale?.ToString() ?? string.Empty : string.Empty,
                    CountryCode = x.data.TryGetValue("countryCode", out var countryCode) ? countryCode?.ToString() ?? string.Empty : string.Empty,
                    RegionCode = x.data.TryGetValue("regionCode", out var regionCode) ? regionCode?.ToString() ?? string.Empty : string.Empty,
                    StripeCustomerId = x.data.TryGetValue("stripeCustomerId", out var stripeCustomerId) ? stripeCustomerId?.ToString() ?? string.Empty : string.Empty,
                    StripeSubscriptionId = x.data.TryGetValue("stripeSubscriptionId", out var stripeSubscriptionId) ? stripeSubscriptionId?.ToString() ?? string.Empty : string.Empty,
                    BillingPlan = x.data.TryGetValue("billingPlan", out var billingPlan) ? billingPlan?.ToString() ?? string.Empty : string.Empty,
                    IsActive = !x.data.TryGetValue("isActive", out var isActive) || bool.TryParse(isActive?.ToString(), out var parsedIsActive) && parsedIsActive
                }));
            }

            return results;
        }

        private async Task SaveOrganizationAsync(Runtira.Domain.Entities.RuntiraOrganization organization)
        {
            if (_cosmosClient is null)
            {
                return;
            }

            var container = _cosmosClient.GetDatabase(_cosmosOptions.DatabaseName).GetContainer("Organizations");
            var document = new CosmosDocument
            {
                id = organization.Id.ToString(),
                type = "organization",
                data = new Dictionary<string, object?>
                {
                    ["name"] = organization.Name,
                    ["slug"] = organization.Slug,
                    ["ownerEmail"] = organization.OwnerEmail,
                    ["defaultLocale"] = organization.DefaultLocale,
                    ["countryCode"] = organization.CountryCode,
                    ["regionCode"] = organization.RegionCode,
                    ["timeZone"] = organization.TimeZone,
                    ["legalProfileJson"] = organization.LegalProfileJson,
                    ["additionalSettingsJson"] = organization.AdditionalSettingsJson,
                    ["stripeCustomerId"] = organization.StripeCustomerId,
                    ["stripeSubscriptionId"] = organization.StripeSubscriptionId,
                    ["billingPlan"] = organization.BillingPlan,
                    ["isActive"] = organization.IsActive,
                    ["createdUtc"] = organization.CreatedUtc,
                    ["modifiedUtc"] = DateTime.UtcNow
                }
            };

            await container.UpsertItemAsync(document, new PartitionKey(document.id));
        }

        private static string ResolvePlanFromPriceId(string? priceId)
        {
            if (string.IsNullOrWhiteSpace(priceId))
            {
                return "Trial";
            }

            return Plans.FirstOrDefault(x => string.Equals(x.Plan, priceId, StringComparison.OrdinalIgnoreCase))?.Plan ?? "Paid";
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
            return request;
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response) where T : class, new()
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, JsonOptions) ?? new T();
        }

        private static string FormatDisplayPrice(long unitAmount, string currency, string interval)
            => $"{(unitAmount / 100m).ToString("C", CultureInfo.GetCultureInfo("en-US"))}/{interval}";
    }
}
