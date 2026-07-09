using Microsoft.Extensions.DependencyInjection;
using Runtira.Application.Abstractions;
using Runtira.Application.Common;

namespace Runtira.Application.Features
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddRuntiraApplication(this IServiceCollection services)
        {
            services.AddScoped<RuntiraWorkspaceService>();
            services.AddScoped<ITenantContextAccessor>(_ => new TenantContext { TenantId = null, BypassTenantFilter = false });
            services.AddScoped<CurrentOrganization>();
            return services;
        }
    }
}
