using System.Reflection;

namespace Maui.AutoNav.Internal;

/// <summary>
/// Resolves the view model type for a page type, either via an explicit
/// <see cref="ViewModelAttribute"/> override or the "XPage" → "XViewModel" naming
/// convention. Pure reflection over <see cref="Type"/> - no MAUI dependency, so it is
/// unit-testable on plain net10.0.
/// </summary>
internal static class ViewModelTypeResolver
{
    private const string PageSuffix = "Page";
    private const string ViewModelSuffix = "ViewModel";

    /// <summary>
    /// Returns the view model type that should be bound to <paramref name="pageType"/>,
    /// or <c>null</c> when no override attribute is present and no candidate matches the
    /// naming convention.
    /// </summary>
    public static Type? Resolve(Type pageType, IReadOnlyCollection<Type> candidateViewModels)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        ArgumentNullException.ThrowIfNull(candidateViewModels);

        var overrideAttribute = pageType.GetCustomAttribute<ViewModelAttribute>();
        if (overrideAttribute is not null)
        {
            return overrideAttribute.ViewModelType;
        }

        var expectedName = ExpectedViewModelName(pageType.Name);
        var expectedNamespace = InferViewModelNamespace(pageType.Namespace);

        return candidateViewModels.FirstOrDefault(vm =>
                string.Equals(vm.Name, expectedName, StringComparison.Ordinal) &&
                string.Equals(vm.Namespace, expectedNamespace, StringComparison.Ordinal))
            ?? candidateViewModels.FirstOrDefault(vm =>
                string.Equals(vm.Name, expectedName, StringComparison.Ordinal));
    }

    internal static string ExpectedViewModelName(string pageTypeName) =>
        pageTypeName.EndsWith(PageSuffix, StringComparison.Ordinal)
            ? pageTypeName[..^PageSuffix.Length] + ViewModelSuffix
            : pageTypeName + ViewModelSuffix;

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
            if (segments[i] is "Views" or "Pages" or "View" or "Page")
            {
                segments[i] = "ViewModels";
            }
        }

        return string.Join('.', segments);
    }
}
