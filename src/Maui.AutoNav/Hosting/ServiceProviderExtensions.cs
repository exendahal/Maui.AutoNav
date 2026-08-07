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

    /// <summary>
    /// Finds the view model that matches <typeparamref name="TView"/> by the same naming
    /// convention (or <see cref="ViewModelAttribute"/> override) <c>AddAutoNavigation</c> uses
    /// for pages, and resolves it from DI. For a view that never reaches the screen through
    /// <see cref="INavigationService"/>, the Shell route factory, or the ambient safety net -
    /// most commonly a popup (CommunityToolkit.Maui's <c>Popup</c>, or MAUI's own
    /// <c>ShowPopupAsync</c>) - so there's nothing to auto-wire it. The caller is responsible
    /// for assigning <see cref="BindableObject.BindingContext"/> and invoking any lifecycle
    /// interface it wants; none of them fire automatically for a view this method resolves.
    /// </summary>
    /// <example>
    /// <code>
    /// var viewModel = services.ResolveViewModelFor&lt;FilterPopup&gt;();
    /// var popup = new FilterPopup { BindingContext = viewModel };
    /// await (viewModel as IInitializeAsync)?.InitializeAsync() ?? Task.CompletedTask;
    /// await this.ShowPopupAsync(popup);
    /// </code>
    /// </example>
    public static object ResolveViewModelFor<TView>(this IServiceProvider services) =>
        services.ResolveViewModelFor(typeof(TView));

    /// <inheritdoc cref="ResolveViewModelFor{TView}(IServiceProvider)"/>
    public static object ResolveViewModelFor(this IServiceProvider services, Type viewType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(viewType);

        var routes = services.GetRequiredService<IPageRouteMap>();
        var viewModelType = routes.ResolveViewModelType(viewType)
            ?? throw new InvalidOperationException(
                $"No view model matches '{viewType.FullName}' by naming convention or " +
                $"[ViewModel] override - expected a '...ViewModel' or '...PageModel' class " +
                $"matching '{viewType.Name}', or a [ViewModel(typeof(...))] attribute on it.");

        return services.GetRequiredService(viewModelType);
    }
}
#endif
