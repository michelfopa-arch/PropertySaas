using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Runtira.Application.Abstractions;
using Runtira.Infrastructure.Data;
using Runtira.Infrastructure.Options;
using Runtira.Infrastructure.Services;
using Runtira.Infrastructure.Services.Billing;

namespace Runtira.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRuntiraInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var clerkOptions = new ClerkOptions();
            configuration.GetSection("Clerk").Bind(clerkOptions);
            services.AddSingleton(clerkOptions);

            var stripeOptions = new StripeOptions();
            configuration.GetSection("Stripe").Bind(stripeOptions);
            services.AddSingleton(stripeOptions);

            var blobOptions = new AzureBlobOptions();
            configuration.GetSection("AzureBlob").Bind(blobOptions);
            services.AddSingleton(blobOptions);

            var microsoft365Options = new Microsoft365Options();
            configuration.GetSection("Microsoft365").Bind(microsoft365Options);
            services.AddSingleton(microsoft365Options);

            var aiOptions = new AiOptions();
            configuration.GetSection("AI").Bind(aiOptions);
            services.AddSingleton(aiOptions);

            var cosmosOptions = new CosmosOptions();
            configuration.GetSection("Cosmos").Bind(cosmosOptions);
            services.AddSingleton(cosmosOptions);

            if (cosmosOptions.Enabled
                && !string.IsNullOrWhiteSpace(cosmosOptions.Endpoint)
                && !string.IsNullOrWhiteSpace(cosmosOptions.Key))
            {
                services.AddSingleton(_ => new CosmosClient(cosmosOptions.Endpoint, cosmosOptions.Key, new CosmosClientOptions
                {
                    ApplicationName = "Runtira"
                }));
                services.AddHostedService<CosmosProvisioningService>();
                services.AddHostedService<CosmosSeedService>();
                services.AddSingleton<IRuntiraAssetWorkspaceStore, CosmosAssetWorkspaceStore>();
                services.AddSingleton<IRuntiraLeadWorkspaceStore, CosmosLeadWorkspaceStore>();
                services.AddSingleton<IRuntiraReadModelStore, CosmosReadModelStore>();
            }

            services.Configure<ResendOptions>(configuration.GetSection("Resend"));
            services.AddHttpClient<IEmailService, ResendEmailService>(client => client.BaseAddress = new Uri("https://api.resend.com/"));

            services.AddSingleton<ILegislationCatalog, JsonLegislationCatalog>();
            services.AddSingleton<IRentInvoicePdfRenderer, AlbertaMinimalRentInvoicePdfRenderer>();
            services.AddSingleton<IRentInvoiceArchiveStore, LocalJsonRentInvoiceArchiveStore>();
            services.AddHttpClient<StripeBillingService>(client => client.BaseAddress = new Uri("https://api.stripe.com/v1/"));

            return services;
        }
    }
}
