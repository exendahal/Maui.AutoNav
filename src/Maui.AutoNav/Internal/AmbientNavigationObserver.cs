#if MAUI_AUTONAV_PLATFORM
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Maui.AutoNav.Internal;

/// <summary>
/// Best-effort fallback for pages that reach the screen without going through
/// <see cref="INavigationService"/> or the Shell route factory - most commonly an
/// AppShell.xaml <c>ShellContent</c>/<c>Tab</c>/<c>FlyoutItem</c> declared with
/// <c>ContentTemplate="{DataTemplate views:SomePage}"</c>, which MAUI's own
/// <see cref="DataTemplate"/> machinery builds with a parameterless constructor, bypassing
/// the DI-aware <see cref="DependencyInjectedRouteFactory"/> entirely.
/// </summary>
/// <remarks>
/// This is a fallback, not a guarantee: it depends on catching a page after Shell has already
/// created it, via whichever change-notification actually fires for that page - which isn't
/// consistent enough across MAUI versions and scenarios to rely on for the one page an app
/// can't do without: its <em>root</em> page. Use <see cref="ServiceProviderExtensions.ResolveRootPage{TViewModel}(IServiceProvider)"/>
/// for that one instead if this fallback ever misbehaves for it.
/// This observer still helps with everything else reached the same ambient way (extra
/// Tabs/FlyoutItems, or a page pushed directly via <c>Navigation.PushAsync</c>).
/// </remarks>
internal static class AmbientNavigationObserver
{
    public static void Attach(Window window, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(services);

        var routes = services.GetRequiredService<IPageRouteMap>();

        if (window.Page is Shell shell)
        {
            shell.Navigated += (_, _) => TryWire(shell.CurrentPage, services, routes);

            // Navigated doesn't reliably fire for the Shell's own implicit initial
            // navigation, so also watch the CurrentPage property itself change.
            shell.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Shell.CurrentPage))
                {
                    TryWire(shell.CurrentPage, services, routes);
                }
            };

            TryWire(shell.CurrentPage, services, routes);
        }
        else if (window.Page is NavigationPage navigationPage)
        {
            navigationPage.Pushed += (_, e) => TryWire(e.Page, services, routes);
            TryWire(navigationPage.CurrentPage, services, routes);
        }
    }

    private static void TryWire(Page? page, IServiceProvider services, IPageRouteMap routes)
    {
        if (page is null || PageLifecycleWirer.IsWired(page))
        {
            return;
        }

        var viewModel = page.BindingContext;

        if (viewModel is null)
        {
            // No BindingContext yet - resolve it from the same convention/[ViewModel] map
            // AddAutoNavigation already built, exactly like the DI-aware route factory does
            // for dynamically-routed pages. If this page type was never discovered by that
            // scan, there's nothing to bind it to - leave it alone rather than guessing.
            if (!routes.TryGetViewModelType(page.GetType(), out var viewModelType))
            {
                return;
            }

            viewModel = services.GetRequiredService(viewModelType);
        }

        PageLifecycleWirer.Wire(page, viewModel);

        if (viewModel is IInitializeAsync parameterless)
        {
            _ = parameterless.InitializeAsync();
        }

        // By the time this fallback ever gets a chance to run, Shell/NavigationPage has
        // already made this page current (that's the only way it ends up as
        // shell.CurrentPage / navigationPage.CurrentPage / e.Page here) - which means its
        // real Page.Appearing has already fired and was missed, since Wire() above is what
        // subscribes to it. Invoke the same hook once now as a catch-up; any later Appearing
        // (navigating away and back) still fires normally through that subscription.
        if (viewModel is IAppearingAware appearingAware)
        {
            _ = appearingAware.OnAppearingAsync();
        }
    }
}
#endif
