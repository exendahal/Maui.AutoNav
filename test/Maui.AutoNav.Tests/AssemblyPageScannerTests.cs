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
        var map = AssemblyPageScanner.BuildPageViewModelMap(
            new[] { typeof(AssemblyPageScannerTests).Assembly },
            isCandidatePage: t => t.Namespace == typeof(LoginPage).Namespace,
            isCandidateViewModel: t => t.Name.EndsWith("ViewModel", StringComparison.Ordinal));

        Assert.Equal(typeof(ViewModels.LoginViewModel), map[typeof(LoginPage)]);
        Assert.Equal(typeof(ViewModels.SettingsViewModel), map[typeof(SettingsPage)]);
        Assert.False(map.ContainsKey(typeof(OrphanPage)));
    }

    [Fact]
    public void BuildPageViewModelMap_ignores_types_that_fail_neither_predicate()
    {
        var map = AssemblyPageScanner.BuildPageViewModelMap(
            new[] { typeof(AssemblyPageScannerTests).Assembly },
            isCandidatePage: _ => false,
            isCandidateViewModel: _ => false);

        Assert.Empty(map);
    }
}
