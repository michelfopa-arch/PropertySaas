namespace Runtira.Application.Common
{
    public class JurisdictionProfile
    {
        public string CountryCode { get; init; } = "CA";
        public string ProvinceCode { get; init; } = "ON";
        public string ProvinceDisplayName { get; init; } = "Ontario";
        public string DefaultLanguage { get; init; } = "en-CA";
        public IReadOnlyList<string> SupportedLanguages { get; init; } = new[] { "en-CA" };
    }

    public static class JurisdictionCatalog
    {
        private static readonly Dictionary<string, JurisdictionProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ON"] = new() { ProvinceCode = "ON", ProvinceDisplayName = "Ontario", DefaultLanguage = "en-CA", SupportedLanguages = new[] { "en-CA", "fr-CA", "es-MX" } },
            ["QC"] = new() { ProvinceCode = "QC", ProvinceDisplayName = "Québec", DefaultLanguage = "fr-CA", SupportedLanguages = new[] { "fr-CA", "en-CA", "es-MX" } },
            ["AB"] = new() { ProvinceCode = "AB", ProvinceDisplayName = "Alberta", DefaultLanguage = "en-CA", SupportedLanguages = new[] { "en-CA", "fr-CA", "es-MX" } }
        };

        public static JurisdictionProfile GetProfile(string? province)
            => Profiles.TryGetValue(province ?? string.Empty, out var profile) ? profile : Profiles["ON"];
    }
}
