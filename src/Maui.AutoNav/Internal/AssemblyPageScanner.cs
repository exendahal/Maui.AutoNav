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
