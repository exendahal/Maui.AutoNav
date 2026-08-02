#if MAUI_AUTONAV_PLATFORM
using Maui.AutoNav.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Maui.AutoNav;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Resolves and wires the page registered for <typeparamref name="TViewModel"/>, for any
    /// page that can't reach the screen through <see cref="INavigationService"/> or the Shell
    /// route factory because nothing is navigating to it - it's <em>assigned</em> as content
    /// instead. That covers two cases: a classic <c>NavigationPage</c>'s root page, and a
    /// Shell app's root <c>ShellContent</c> (and any Tab/FlyoutItem) - assign the result
    /// directly to <see cref="ShellContent.Content"/> rather than using
    /// <c>ContentTemplate="{DataTemplate ...}"</c> in XAML, which builds the page with a bare
    /// parameterless constructor and leaves it with no view model at all.
    /// </summary>
    /// <example>
    /// <code>
    /// // Classic NavigationPage:
    /// protected override Window CreateWindow(IActivationState? activationState)
    /// {
    ///     var root = _Services.ResolveRootPage&lt;LoginViewModel&gt;();
    ///     return new Window(new NavigationPage(root)).UseAutoNavigation(_Services);
    /// }
    ///
    /// // Shell - in AppShell's constructor:
    /// public AppShell(IServiceProvider services)
    /// {
    ///     InitializeComponent();
    ///     Items.Add(new ShellContent { Title = "Home", Content = services.ResolveRootPage&lt;HomeViewModel&gt;() });
    /// }
    /// </code>
    /// </example>
    public static Page ResolveRootPage<TViewModel>(this IServiceProvider services) where TViewModel : class
    {
        ArgumentNullException.ThrowIfNull(services);

        var routes = services.GetRequiredService<IPageRouteMap>();
        var pageType = routes.GetPageType(typeof(TViewModel));

        var page = (Page)services.GetRequiredService(pageType);
        var viewModel = services.GetRequiredService<TViewModel>();
        PageLifecycleWirer.Wire(page, viewModel);
        _ = PageLifecycleWirer.InitializeAsync(viewModel, parameter: null);

        return page;
    }
}
#endif
