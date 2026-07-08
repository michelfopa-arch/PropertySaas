using System.Text.Json;
using System.Text.Json.Serialization;

namespace Runtira.Infrastructure.Services.Billing
{
    public sealed class StripePlanPriceDto
    {
        public string Plan { get; set; } = string.Empty;
        public string PriceId { get; set; } = string.Empty;
        public string Currency { get; set; } = "usd";
        public decimal UnitAmount { get; set; }
        public string Interval { get; set; } = "month";
        public string DisplayPrice { get; set; } = string.Empty;
    }

    internal sealed class RuntiraBillingPlanDefinition
    {
        public string Plan { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public long UnitAmount { get; set; }
        public string Currency { get; set; } = "usd";
        public string Interval { get; set; } = "month";
    }

    internal sealed class StripeCheckoutSessionResponse
    {
        public string? Url { get; set; }
        public string? Customer { get; set; }
        public string? Subscription { get; set; }
        public string? ClientReferenceId { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    internal sealed class StripePortalSessionResponse
    {
        public string? Url { get; set; }
    }

    internal sealed class StripePriceResponse
    {
        public string? Id { get; set; }
    }

    internal sealed class StripeProductResponse
    {
        public string? Id { get; set; }
    }

    internal sealed class StripeWebhookEvent
    {
        public string? Type { get; set; }
        public StripeEventData? Data { get; set; }
    }

    internal sealed class StripeEventData
    {
        public JsonElement? Object { get; set; }
    }

    internal sealed class StripeSubscriptionResponse
    {
        public string? Id { get; set; }
        public string? Customer { get; set; }
        public string? Status { get; set; }
        public StripeSubscriptionItems? Items { get; set; }
    }

    internal sealed class StripeSubscriptionItems
    {
        public List<StripeSubscriptionItem> Data { get; set; } = new();
    }

    internal sealed class StripeSubscriptionItem
    {
        public StripeSubscriptionPrice? Price { get; set; }
    }

    internal sealed class StripeSubscriptionPrice
    {
        public string? Id { get; set; }
    }
}
