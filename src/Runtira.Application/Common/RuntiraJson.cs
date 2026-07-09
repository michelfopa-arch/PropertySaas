using System.Text.Json;

namespace Runtira.Application.Common
{
    public static class RuntiraJson
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static string Serialize<T>(T value)
            => JsonSerializer.Serialize(value, Options);

        public static T? Deserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, Options);
        }

        public static Dictionary<string, string> ReadStringDictionary(string? json)
            => Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
