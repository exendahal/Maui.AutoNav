using Xunit;

namespace Maui.AutoNav.Tests;

// AutoNavigation holds process-wide static state, so these run against Initialize/Services
// directly rather than relying on ordering with other test classes.
public class AutoNavigationTests
{
    [Fact]
    public void Initialize_then_Services_returns_the_same_provider()
    {
        var provider = new FakeServiceProvider();

        AutoNavigation.Initialize(provider);

        Assert.Same(provider, AutoNavigation.Services);
        Assert.True(AutoNavigation.IsInitialized);
    }

    [Fact]
    public void Initialize_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => AutoNavigation.Initialize(null!));
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
