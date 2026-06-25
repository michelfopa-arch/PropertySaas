namespace PropertySaaS.Web.Localization;

public static class DemoModeRouteHelper
{
    public static bool IsClean(string? demoMode)
        => string.Equals(demoMode, "clean", StringComparison.OrdinalIgnoreCase);

    public static bool IsCleanUri(string? uri)
        => !string.IsNullOrWhiteSpace(uri)
            && uri.Contains("demo=clean", StringComparison.OrdinalIgnoreCase);

    public static string Apply(string route, bool isDemoCleanView)
    {
        if (!isDemoCleanView || string.IsNullOrWhiteSpace(route) || route.Contains("demo=", StringComparison.OrdinalIgnoreCase))
        {
            return route;
        }

        var anchorIndex = route.IndexOf('#');
        var anchor = anchorIndex >= 0 ? route[anchorIndex..] : string.Empty;
        var baseRoute = anchorIndex >= 0 ? route[..anchorIndex] : route;
        var separator = baseRoute.Contains('?') ? "&" : "?";
        return $"{baseRoute}{separator}demo=clean{anchor}";
    }
}
