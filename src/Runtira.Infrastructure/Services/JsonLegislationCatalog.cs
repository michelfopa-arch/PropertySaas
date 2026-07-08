using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Runtira.Application.Abstractions;
using Runtira.Application.Features;

namespace Runtira.Infrastructure.Services
{
    public sealed class JsonLegislationCatalog : ILegislationCatalog
    {
        private readonly IReadOnlyDictionary<string, RuntiraLegislationProfileDto> _profiles;

        public JsonLegislationCatalog(IConfiguration configuration)
        {
            var rootPath = configuration["Legislation:RootPath"];
            var basePath = AppContext.BaseDirectory;
            var resolvedRoot = string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(basePath, "Legislation")
                : Path.IsPathRooted(rootPath) ? rootPath : Path.GetFullPath(Path.Combine(basePath, rootPath));

            var profiles = new Dictionary<string, RuntiraLegislationProfileDto>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(resolvedRoot))
            {
                foreach (var file in Directory.EnumerateFiles(resolvedRoot, "*.json", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<RuntiraLegislationProfileDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (profile is null || string.IsNullOrWhiteSpace(profile.CountryCode) || string.IsNullOrWhiteSpace(profile.RegionCode))
                    {
                        continue;
                    }

                    profiles[$"{profile.CountryCode}:{profile.RegionCode}"] = profile;
                }
            }

            _profiles = profiles;
        }

        public RuntiraLegislationProfileDto? GetProfile(string countryCode, string regionCode)
            => _profiles.TryGetValue($"{countryCode}:{regionCode}", out var profile) ? profile : null;
    }
}
