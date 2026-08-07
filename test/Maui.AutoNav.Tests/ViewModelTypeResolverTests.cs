using Maui.AutoNav.Internal;
using Maui.AutoNav.Tests.Fixtures.Views;
using Xunit;
using ViewModels = Maui.AutoNav.Tests.Fixtures.ViewModels;
using Other = Maui.AutoNav.Tests.Fixtures.Other;

namespace Maui.AutoNav.Tests;

public class ViewModelTypeResolverTests
{
    private static readonly Type[] AllCandidates =
    {
        typeof(ViewModels.LoginViewModel),
        typeof(ViewModels.SettingsViewModel),
        typeof(ViewModels.CustomViewModel),
        typeof(ViewModels.DashboardPageModel),
        typeof(Other.SettingsViewModel),
    };

    [Fact]
    public void Resolve_matches_by_naming_convention()
    {
        var result = ViewModelTypeResolver.Resolve(typeof(LoginPage), AllCandidates);

        Assert.Equal(typeof(ViewModels.LoginViewModel), result);
    }

    [Fact]
    public void Resolve_prefers_same_namespace_match_over_name_only_match()
    {
        var result = ViewModelTypeResolver.Resolve(typeof(SettingsPage), AllCandidates);

        Assert.Equal(typeof(ViewModels.SettingsViewModel), result);
    }

    [Fact]
    public void Resolve_honors_ViewModelAttribute_override()
    {
        var result = ViewModelTypeResolver.Resolve(typeof(ProfilePage), AllCandidates);

        Assert.Equal(typeof(ViewModels.CustomViewModel), result);
    }

    [Fact]
    public void Resolve_returns_null_when_no_candidate_matches()
    {
        var result = ViewModelTypeResolver.Resolve(typeof(OrphanPage), AllCandidates);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_falls_back_to_PageModel_suffix_when_no_ViewModel_candidate_exists()
    {
        var result = ViewModelTypeResolver.Resolve(typeof(DashboardPage), AllCandidates);

        Assert.Equal(typeof(ViewModels.DashboardPageModel), result);
    }

    [Theory]
    [InlineData("LoginPage", "LoginViewModel")]
    [InlineData("SettingsPage", "SettingsViewModel")]
    [InlineData("Login", "LoginViewModel")]
    public void ExpectedViewModelName_strips_Page_suffix_before_appending_ViewModel(string pageName, string expected)
    {
        Assert.Equal(expected, ViewModelTypeResolver.ExpectedViewModelName(pageName));
    }

    [Theory]
    [InlineData("LoginPage", new[] { "LoginViewModel", "LoginPageModel" })]
    [InlineData("Login", new[] { "LoginViewModel", "LoginPageModel" })]
    public void ExpectedViewModelNames_tries_ViewModel_suffix_before_PageModel_suffix(string pageName, string[] expected)
    {
        Assert.Equal(expected, ViewModelTypeResolver.ExpectedViewModelNames(pageName));
    }

    [Theory]
    [InlineData("MyApp.Views", "MyApp.ViewModels")]
    [InlineData("MyApp.Pages", "MyApp.ViewModels")]
    [InlineData(null, null)]
    public void InferViewModelNamespace_maps_Views_and_Pages_to_ViewModels(string? pageNamespace, string? expected)
    {
        Assert.Equal(expected, ViewModelTypeResolver.InferViewModelNamespace(pageNamespace));
    }
}
