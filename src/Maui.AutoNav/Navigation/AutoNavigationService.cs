#if MAUI_AUTONAV_PLATFORM
using Maui.AutoNav.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Maui.AutoNav;

/// <summary>
/// Default <see cref="INavigationService"/>. Detects whether the app's root page is a
/// <see cref="Shell"/> or a classic <see cref="NavigationPage"/> at call time and drives
/// the matching MAUI navigation API, so callers write the same code either way. Modal
/// presentation (<see cref="NavigateToModalAsync{TViewModel}(bool)"/> and friends) uses
/// <see cref="INavigation.PushModalAsync(Page)"/> / <see cref="INavigation.PopModalAsync()"/>
/// directly, which work the same regardless of Shell vs. classic - modal presentation is a
/// window-level concept, not a Shell-route one.
/// </summary>
internal sealed class AutoNavigationService : INavigationService
{
    private readonly IServiceProvider _Services;
    private readonly IPageRouteMap _Routes;
    private readonly Stack<object> _PendingResults = new();

    public AutoNavigationService(IServiceProvider services, IPageRouteMap routes)
    {
        _Services = services;
        _Routes = routes;
    }

    public Task NavigateToAsync<TViewModel>(bool animate = true) where TViewModel : class =>
        NavigateCoreAsync(typeof(TViewModel), null, animate, modal: false);

    public Task NavigateToAsync<TViewModel, TParam>(TParam parameter, bool animate = true) where TViewModel : class =>
        NavigateCoreAsync(typeof(TViewModel), parameter, animate, modal: false);

    public Task<TResult?> NavigateForResultAsync<TViewModel, TResult>(bool animate = true) where TViewModel : class =>
        NavigateForResultCoreAsync<TResult>(typeof(TViewModel), null, animate, modal: false);

    public Task<TResult?> NavigateForResultAsync<TViewModel, TParam, TResult>(TParam parameter, bool animate = true) where TViewModel : class =>
        NavigateForResultCoreAsync<TResult>(typeof(TViewModel), parameter, animate, modal: false);

    public Task NavigateToModalAsync<TViewModel>(bool animate = true) where TViewModel : class =>
        NavigateCoreAsync(typeof(TViewModel), null, animate, modal: true);

    public Task NavigateToModalAsync<TViewModel, TParam>(TParam parameter, bool animate = true) where TViewModel : class =>
        NavigateCoreAsync(typeof(TViewModel), parameter, animate, modal: true);

    public Task<TResult?> NavigateForModalResultAsync<TViewModel, TResult>(bool animate = true) where TViewModel : class =>
        NavigateForResultCoreAsync<TResult>(typeof(TViewModel), null, animate, modal: true);

    public Task<TResult?> NavigateForModalResultAsync<TViewModel, TParam, TResult>(TParam parameter, bool animate = true) where TViewModel : class =>
        NavigateForResultCoreAsync<TResult>(typeof(TViewModel), parameter, animate, modal: true);

    public async Task GoBackAsync(bool animate = true)
    {
        var currentPage = GetCurrentPage();

        if (currentPage?.BindingContext is IConfirmNavigationAsync confirm &&
            !await confirm.CanNavigateFromAsync().ConfigureAwait(true))
        {
            return;
        }

        if (TryGetShell(out var shell))
        {
            await shell.GoToAsync("..", animate).ConfigureAwait(true);
        }
        else if (currentPage is not null)
        {
            await currentPage.Navigation.PopAsync(animate).ConfigureAwait(true);
        }

        if (currentPage?.BindingContext is IDestructible destructible)
        {
            destructible.Destroy();
        }
    }

    public async Task GoBackAsync<TResult>(TResult result, bool animate = true)
    {
        CompletePendingResult(result);
        await GoBackAsync(animate).ConfigureAwait(true);
    }

    public async Task GoBackModalAsync(bool animate = true)
    {
        var currentModalPage = GetCurrentModalPage();

        if (currentModalPage?.BindingContext is IConfirmNavigationAsync confirm &&
            !await confirm.CanNavigateFromAsync().ConfigureAwait(true))
        {
            return;
        }

        var navigation = GetCurrentPage()?.Navigation;
        if (navigation is not null)
        {
            await navigation.PopModalAsync(animate).ConfigureAwait(true);
        }

        if (currentModalPage?.BindingContext is IDestructible destructible)
        {
            destructible.Destroy();
        }
    }

    public async Task GoBackModalAsync<TResult>(TResult result, bool animate = true)
    {
        CompletePendingResult(result);
        await GoBackModalAsync(animate).ConfigureAwait(true);
    }

    private void CompletePendingResult<TResult>(TResult result)
    {
        if (_PendingResults.Count > 0)
        {
            var tcs = (TaskCompletionSource<object?>)_PendingResults.Pop();
            tcs.TrySetResult(result);
        }
    }

    private async Task<TResult?> NavigateForResultCoreAsync<TResult>(Type viewModelType, object? parameter, bool animate, bool modal)
    {
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _PendingResults.Push(tcs);
        await NavigateCoreAsync(viewModelType, parameter, animate, modal).ConfigureAwait(true);
        var result = await tcs.Task.ConfigureAwait(true);
        return (TResult?)result;
    }

    private async Task NavigateCoreAsync(Type viewModelType, object? parameter, bool animate, bool modal)
    {
        if (modal)
        {
            await NavigateModalCoreAsync(viewModelType, parameter, animate).ConfigureAwait(true);
            return;
        }

        var route = _Routes.GetRoute(viewModelType);

        if (TryGetShell(out var shell))
        {
            if (parameter is not null)
            {
                PendingParameterBroker.SetPending(parameter);
            }

            await shell.GoToAsync(route, animate).ConfigureAwait(true);

            var boundViewModel = shell.CurrentPage?.BindingContext;
            if (parameter is not null && boundViewModel is not null)
            {
                await PageLifecycleWirer.InitializeAsync(boundViewModel, parameter).ConfigureAwait(true);
            }

            return;
        }

        var (page, viewModel) = ResolvePageAndViewModel(viewModelType);

        if (parameter is not null)
        {
            await PageLifecycleWirer.InitializeAsync(viewModel, parameter).ConfigureAwait(true);
        }

        var navigation = GetCurrentPage()?.Navigation
            ?? throw new InvalidOperationException(
                "No active NavigationPage was found. Ensure the app window's page is a Shell or a NavigationPage.");

        await navigation.PushAsync(page, animate).ConfigureAwait(true);
    }

    private async Task NavigateModalCoreAsync(Type viewModelType, object? parameter, bool animate)
    {
        var (page, viewModel) = ResolvePageAndViewModel(viewModelType);

        if (parameter is not null)
        {
            await PageLifecycleWirer.InitializeAsync(viewModel, parameter).ConfigureAwait(true);
        }

        var navigation = GetCurrentPage()?.Navigation
            ?? throw new InvalidOperationException(
                "No active page was found to present the modal page from.");

        await navigation.PushModalAsync(page, animate).ConfigureAwait(true);
    }

    private (Page Page, object ViewModel) ResolvePageAndViewModel(Type viewModelType)
    {
        var pageType = _Routes.GetPageType(viewModelType);
        var page = (Page)_Services.GetRequiredService(pageType);
        var viewModel = _Services.GetRequiredService(viewModelType);
        PageLifecycleWirer.Wire(page, viewModel);
        return (page, viewModel);
    }

    private static Page? GetCurrentPage()
    {
        var root = GetRootPage();
        return root switch
        {
            Shell shell => shell.CurrentPage,
            NavigationPage navigationPage => navigationPage.CurrentPage,
            _ => root
        };
    }

    private static Page? GetCurrentModalPage()
    {
        var navigation = GetCurrentPage()?.Navigation;
        return navigation is not null && navigation.ModalStack.Count > 0
            ? navigation.ModalStack[^1]
            : null;
    }

    private static bool TryGetShell(out Shell shell)
    {
        if (GetRootPage() is Shell root)
        {
            shell = root;
            return true;
        }

        shell = null!;
        return false;
    }

    private static Page? GetRootPage()
    {
        var app = Application.Current;
        if (app is null)
        {
            return null;
        }

        return app.Windows.Count > 0 ? app.Windows[0].Page : app.MainPage;
    }
}
#endif
