namespace Runtira.Application.Common
{
    public static class RuntiraSlug
    {
        public static string Slugify(string? name)
            => string.Join('-', (name ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Split(new[] { ' ', '·', ',', '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
