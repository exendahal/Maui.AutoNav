using Maui.AutoNav.Internal;
using Maui.AutoNav.Tests.Fixtures.Views;
using Xunit;
using ViewModels = Maui.AutoNav.Tests.Fixtures.ViewModels;

namespace Maui.AutoNav.Tests;

public class PageRouteMapTests
{
    [Fact]
    public void Register_then_GetRoute_and_GetPageType_round_trip()
    {
        var map = new PageRouteMap();
        map.Register(typeof(LoginPage), typeof(ViewModels.LoginViewModel), "login");

        Assert.Equal("login", map.GetRoute(typeof(ViewModels.LoginViewModel)));
        Assert.Equal(typeof(LoginPage), map.GetPageType(typeof(ViewModels.LoginViewModel)));
    }

    [Fact]
    public void TryGetViewModelType_finds_the_view_model_for_a_registered_page()
    {
        var map = new PageRouteMap();
        map.Register(typeof(LoginPage), typeof(ViewModels.LoginViewModel), "login");

        var found = map.TryGetViewModelType(typeof(LoginPage), out var viewModelType);

        Assert.True(found);
        Assert.Equal(typeof(ViewModels.LoginViewModel), viewModelType);
    }

    [Fact]
    public void TryGetViewModelType_returns_false_for_unregistered_page()
    {
        var map = new PageRouteMap();

        var found = map.TryGetViewModelType(typeof(LoginPage), out var viewModelType);

        Assert.False(found);
        Assert.Null(viewModelType);
    }

    [Fact]
    public void TryGetPageType_returns_false_for_unregistered_view_model()
    {
        var map = new PageRouteMap();

        var found = map.TryGetPageType(typeof(ViewModels.LoginViewModel), out var pageType);

        Assert.False(found);
        Assert.Null(pageType);
    }

    [Fact]
    public void GetRoute_throws_descriptive_error_for_unregistered_view_model()
    {
        var map = new PageRouteMap();

        var ex = Assert.Throws<InvalidOperationException>(() => map.GetRoute(typeof(ViewModels.LoginViewModel)));
        Assert.Contains("AddAutoNavigation", ex.Message);
    }

    [Fact]
    public void ResolveViewModelType_matches_a_view_never_seen_by_the_page_scan()
    {
        // DashboardPage/DashboardPageModel were never Register()-ed as a page/route pair -
        // only passed in as view-model candidates, exactly like a popup's view model would be.
        var map = new PageRouteMap(new[] { typeof(ViewModels.LoginViewModel), typeof(ViewModels.DashboardPageModel) });

        var result = map.ResolveViewModelType(typeof(DashboardPage));

        Assert.Equal(typeof(ViewModels.DashboardPageModel), result);
    }

    [Fact]
    public void ResolveViewModelType_returns_null_when_no_candidate_matches()
    {
        var map = new PageRouteMap(new[] { typeof(ViewModels.LoginViewModel) });

        var result = map.ResolveViewModelType(typeof(OrphanPage));

        Assert.Null(result);
    }

    [Fact]
    public void ResolveViewModelType_returns_null_with_no_candidates_supplied()
    {
        var map = new PageRouteMap();

        var result = map.ResolveViewModelType(typeof(LoginPage));

        Assert.Null(result);
    }
}
