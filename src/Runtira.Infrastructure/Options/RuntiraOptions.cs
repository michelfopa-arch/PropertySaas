namespace Runtira.Infrastructure.Options
{
    public sealed class ClerkOptions
    {
        public string Authority { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string SignInUrl { get; set; } = string.Empty;
        public string SignUpUrl { get; set; } = string.Empty;
        public string UnauthorizedSignInUrl { get; set; } = string.Empty;
        public string UserProfileUrl { get; set; } = string.Empty;
    }

    public sealed class StripeOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string StarterPriceId { get; set; } = string.Empty;
        public string GrowthPriceId { get; set; } = string.Empty;
        public string ProPriceId { get; set; } = string.Empty;
    }

    public sealed class AzureBlobOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string TenantArchiveContainer { get; set; } = "tenant-archive";
    }

    public sealed class Microsoft365Options
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SupportMailbox { get; set; } = "support@runtira.com";
    }

    public sealed class AiOptions
    {
        public string Provider { get; set; } = "MicrosoftAgentFramework";
        public string ModelFast { get; set; } = "gpt-4.1-mini";
        public string ModelReasoning { get; set; } = "gpt-4.1";
    }

    public sealed class CosmosOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "runtiradb";
        public bool Enabled { get; set; }
        public bool MockModeEnabled { get; set; } = true;
        public int SharedAutoscaleMaxThroughput { get; set; } = 4000;
        public int TenantCoreAutoscaleMaxThroughput { get; set; } = 4000;
        public int MessagesAutoscaleMaxThroughput { get; set; } = 4000;
        public int InboxAutoscaleMaxThroughput { get; set; } = 2000;
        public int BillingAutoscaleMaxThroughput { get; set; } = 2000;
        public int OrganizationsAutoscaleMaxThroughput { get; set; } = 1000;
        public int UsersAutoscaleMaxThroughput { get; set; } = 1000;
        public int ConversationsAutoscaleMaxThroughput { get; set; } = 1000;
        public int BlobArchivesAutoscaleMaxThroughput { get; set; } = 1000;
    }

    public sealed class ResendOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "onboarding@resend.dev";
        public string FromName { get; set; } = "PropertySaaS";
        public string SupportEmail { get; set; } = "michelfopa@gmail.com";
    }
}
