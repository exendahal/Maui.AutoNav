using System.Reflection;

namespace Maui.AutoNav.Internal;

/// <summary>
/// Resolves the view model type for a view type, either via an explicit
/// <see cref="ViewModelAttribute"/> override or the "XPage"/"XPopup" → "XViewModel" /
/// "XPageModel" naming convention. Used both for the page scan and for
/// <c>ServiceProviderExtensions.ResolveViewModelFor</c> (views, like popups, that never go
/// through the page scan at all). Pure reflection over <see cref="Type"/> - no MAUI
/// dependency, so it is unit-testable on plain net10.0.
/// </summary>
internal static class ViewModelTypeResolver
{
    private const string PageSuffix = "Page";
    private const string ViewModelSuffix = "ViewModel";

    // "PageModel" is the .NET MAUI Community Toolkit sample/template convention -
    // LoginPage -> LoginPageModel, same folder layout as the ViewModel convention. Tried
    // second, so an app mixing both still prefers XViewModel where both exist.
    private const string PageModelSuffix = "PageModel";

    // "Popup" is stripped the same way "Page" is - FilterPopup -> FilterViewModel - for
    // views that never go through Shell/NavigationPage at all (CommunityToolkit.Maui's
    // Popup, MAUI's own ShowPopupAsync) and are resolved through ResolveViewModelFor
    // instead of the page scan.
    private static readonly string[] StrippableSuffixes = [PageSuffix, "Popup"];

    /// <summary>
    /// Returns the view model type that should be bound to <paramref name="viewType"/>,
    /// or <c>null</c> when no override attribute is present and no candidate matches the
    /// naming convention.
    /// </summary>
    public static Type? Resolve(Type viewType, IReadOnlyCollection<Type> candidateViewModels)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        ArgumentNullException.ThrowIfNull(candidateViewModels);

        var overrideAttribute = viewType.GetCustomAttribute<ViewModelAttribute>();
        if (overrideAttribute is not null)
        {
            return overrideAttribute.ViewModelType;
        }

        var expectedNamespace = InferViewModelNamespace(viewType.Namespace);

        foreach (var expectedName in ExpectedViewModelNames(viewType.Name))
        {
            var match = candidateViewModels.FirstOrDefault(vm =>
                    string.Equals(vm.Name, expectedName, StringComparison.Ordinal) &&
                    string.Equals(vm.Namespace, expectedNamespace, StringComparison.Ordinal))
                ?? candidateViewModels.FirstOrDefault(vm =>
                    string.Equals(vm.Name, expectedName, StringComparison.Ordinal));

            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    internal static string ExpectedViewModelName(string pageTypeName) =>
        pageTypeName.EndsWith(PageSuffix, StringComparison.Ordinal)
            ? pageTypeName[..^PageSuffix.Length] + ViewModelSuffix
            : pageTypeName + ViewModelSuffix;

    internal static IEnumerable<string> ExpectedViewModelNames(string viewTypeName)
    {
        var stem = StripKnownSuffix(viewTypeName);

        yield return stem + ViewModelSuffix;
        yield return stem + PageModelSuffix;
    }

    private static string StripKnownSuffix(string viewTypeName)
    {
        foreach (var suffix in StrippableSuffixes)
        {
            if (viewTypeName.EndsWith(suffix, StringComparison.Ordinal))
            {
                return viewTypeName[..^suffix.Length];
            }
        }

        return viewTypeName;
    }

    internal static string? InferViewModelNamespace(string? pageNamespace)
    {
        if (string.IsNullOrEmpty(pageNamespace))
        {
            return pageNamespace;
        }

        // Segment-by-segment, not a chain of string.Replace calls: ".ViewModels" (the
        // replacement for ".Views") itself contains ".View" as a substring, so chaining a
        // later Replace(".View", ...) over the already-replaced string corrupts it into
        // ".ViewModelsModels". Matching whole dot-separated segments avoids that entirely.
        var segments = pageNamespace.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i] is "Views" or "Pages" or "Popups" or "View" or "Page" or "Popup")
            {
                segments[i] = "ViewModels";
            }
        }

        return string.Join('.', segments);
    }
}
