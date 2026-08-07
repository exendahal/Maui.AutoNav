using System.Reflection;

namespace Maui.AutoNav.Internal;

/// <summary>
/// Scans a set of assemblies for page/view-model pairs. The caller supplies the predicates
/// that decide what counts as a "page" and a "view model" so this class stays free of any
/// MAUI dependency and is unit-testable on plain net10.0.
/// </summary>
internal static class AssemblyPageScanner
{
    public static PageScanResult BuildPageViewModelMap(
        IEnumerable<Assembly> assemblies,
        Func<Type, bool> isCandidatePage,
        Func<Type, bool> isCandidateViewModel)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(isCandidatePage);
        ArgumentNullException.ThrowIfNull(isCandidateViewModel);

        var types = assemblies
            .SelectMany(GetLoadableTypes)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Distinct()
            .ToArray();

        var pages = types.Where(isCandidatePage).ToArray();
        var viewModels = types.Where(isCandidateViewModel).ToArray();

        var map = new Dictionary<Type, Type>();
        foreach (var page in pages)
        {
            var viewModel = ViewModelTypeResolver.Resolve(page, viewModels);
            if (viewModel is not null)
            {
                map[page] = viewModel;
            }
        }

        return new PageScanResult(map, viewModels);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).Select(t => t!);
        }
    }
}

/// <summary>
/// The page/view-model map <see cref="AssemblyPageScanner.BuildPageViewModelMap"/> pairs up,
/// plus every type that passed the view-model candidate predicate - even ones that never
/// matched a page. <see cref="PageRouteMap"/> keeps the latter around so
/// <c>ServiceProviderExtensions.ResolveViewModelFor</c> can run the same naming-convention
/// resolution later for a view that was never part of the page scan at all (a popup, most
/// commonly).
/// </summary>
internal sealed record PageScanResult(
    IReadOnlyDictionary<Type, Type> PageViewModelMap,
    IReadOnlyCollection<Type> ViewModelCandidates);
