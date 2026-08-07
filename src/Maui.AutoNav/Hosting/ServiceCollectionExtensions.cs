#if MAUI_AUTONAV_PLATFORM
using System.Reflection;
using Maui.AutoNav.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Controls;

namespace Maui.AutoNav;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans <paramref name="assemblies"/> (or the calling assembly, if none are given) for
    /// page types matched to view model types by naming convention or <see cref="ViewModelAttribute"/>.
    /// Registers every page and view model as transient, registers a Shell route per page,
    /// and registers <see cref="INavigationService"/>.
    /// </summary>
    public static IServiceCollection AddAutoNavigation(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        var scanAssemblies = assemblies is { Length: > 0 } ? assemblies : new[] { Assembly.GetCallingAssembly() };

        var pageViewModelMap = AssemblyPageScanner.BuildPageViewModelMap(
            scanAssemblies,
            isCandidatePage: IsCandidatePage,
            isCandidateViewModel: IsCandidateViewModel);

        var routes = new PageRouteMap();

        foreach (var (pageType, viewModelType) in pageViewModelMap)
        {
            services.TryAddTransient(pageType);
            services.TryAddTransient(viewModelType);

            var route = PageRouteNaming.RouteFor(pageType);
            Routing.RegisterRoute(route, new DependencyInjectedRouteFactory(pageType, viewModelType));
            routes.Register(pageType, viewModelType, route);
        }

        services.AddSingleton<IPageRouteMap>(routes);
        services.AddSingleton<INavigationService, AutoNavigationService>();

        return services;
    }

    private static bool IsCandidatePage(Type type) =>
        typeof(Page).IsAssignableFrom(type) &&
        !typeof(Shell).IsAssignableFrom(type) &&
        !typeof(NavigationPage).IsAssignableFrom(type) &&
        !typeof(TabbedPage).IsAssignableFrom(type) &&
        !typeof(FlyoutPage).IsAssignableFrom(type);

    private static bool IsCandidateViewModel(Type type) =>
        type.Name.EndsWith("ViewModel", StringComparison.Ordinal) ||
        type.Name.EndsWith("PageModel", StringComparison.Ordinal);
}
#endif
