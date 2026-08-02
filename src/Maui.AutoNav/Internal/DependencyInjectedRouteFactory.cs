#if MAUI_AUTONAV_PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Maui.AutoNav.Internal;

/// <summary>
/// Shell's built-in <c>Routing.RegisterRoute(string, Type)</c> creates pages with
/// <c>Activator.CreateInstance</c>, bypassing DI entirely. Registering a
/// <see cref="RouteFactory"/> instead is the documented escape hatch: this one resolves
/// the page (and its view model, if one is registered) from the app's DI container, and
/// wires the binding context + lifecycle before Shell ever displays the page.
/// </summary>
internal sealed class DependencyInjectedRouteFactory : RouteFactory
{
    private readonly Type _PageType;
    private readonly Type? _ViewModelType;

    public DependencyInjectedRouteFactory(Type pageType, Type? viewModelType)
    {
        _PageType = pageType;
        _ViewModelType = viewModelType;
    }

    public override Element GetOrCreate() => Build(AutoNavigation.Services);

    public override Element GetOrCreate(IServiceProvider services) => Build(services ?? AutoNavigation.Services);

    private Element Build(IServiceProvider services)
    {
        var page = (Page)services.GetRequiredService(_PageType);

        if (_ViewModelType is not null)
        {
            var viewModel = services.GetRequiredService(_ViewModelType);
            PageLifecycleWirer.Wire(page, viewModel);

            // If a typed parameter is pending, AutoNavigationService delivers it itself
            // (awaited) right after GoToAsync returns - skip the parameterless hook here
            // so it doesn't fire twice.
            if (PendingParameterBroker.ConsumePending() is null)
            {
                _ = PageLifecycleWirer.InitializeAsync(viewModel, parameter: null);
            }
        }

        return page;
    }
}
#endif
