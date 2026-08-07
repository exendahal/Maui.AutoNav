using Maui.AutoNav.Internal;
using Maui.AutoNav.Tests.Fixtures.Views;
using Xunit;
using ViewModels = Maui.AutoNav.Tests.Fixtures.ViewModels;

namespace Maui.AutoNav.Tests;

public class AssemblyPageScannerTests
{
    [Fact]
    public void BuildPageViewModelMap_pairs_pages_with_their_view_models()
    {
        var result = AssemblyPageScanner.BuildPageViewModelMap(
            new[] { typeof(AssemblyPageScannerTests).Assembly },
            isCandidatePage: t => t.Namespace == typeof(LoginPage).Namespace,
            isCandidateViewModel: t => t.Name.EndsWith("ViewModel", StringComparison.Ordinal));

        Assert.Equal(typeof(ViewModels.LoginViewModel), result.PageViewModelMap[typeof(LoginPage)]);
        Assert.Equal(typeof(ViewModels.SettingsViewModel), result.PageViewModelMap[typeof(SettingsPage)]);
        Assert.False(result.PageViewModelMap.ContainsKey(typeof(OrphanPage)));
    }

    [Fact]
    public void BuildPageViewModelMap_ignores_types_that_fail_neither_predicate()
    {
        var result = AssemblyPageScanner.BuildPageViewModelMap(
            new[] { typeof(AssemblyPageScannerTests).Assembly },
            isCandidatePage: _ => false,
            isCandidateViewModel: _ => false);

        Assert.Empty(result.PageViewModelMap);
        Assert.Empty(result.ViewModelCandidates);
    }

    [Fact]
    public void BuildPageViewModelMap_exposes_view_model_candidates_that_matched_no_page()
    {
        var result = AssemblyPageScanner.BuildPageViewModelMap(
            new[] { typeof(AssemblyPageScannerTests).Assembly },
            isCandidatePage: t => t.Namespace == typeof(LoginPage).Namespace,
            isCandidateViewModel: t => t.Name.EndsWith("ViewModel", StringComparison.Ordinal));

        // CustomViewModel only matches ProfilePage via [ViewModel], never via the naming
        // convention - it's still a candidate, since ResolveViewModelFor needs the full set,
        // not just the ones a page happened to pair with.
        Assert.Contains(typeof(ViewModels.CustomViewModel), result.ViewModelCandidates);
    }
}
