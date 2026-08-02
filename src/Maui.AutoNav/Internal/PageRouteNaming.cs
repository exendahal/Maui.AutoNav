namespace Maui.AutoNav.Internal;

/// <summary>Derives a Shell route segment from a page's type name.</summary>
internal static class PageRouteNaming
{
    private const string PageSuffix = "Page";

    public static string RouteFor(Type pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        var name = pageType.Name;
        if (name.EndsWith(PageSuffix, StringComparison.Ordinal) && name.Length > PageSuffix.Length)
        {
            name = name[..^PageSuffix.Length];
        }

        return name.ToLowerInvariant();
    }
}
