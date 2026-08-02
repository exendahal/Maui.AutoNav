using Maui.AutoNav.Internal;
using Maui.AutoNav.Tests.Fixtures.Views;
using Xunit;

namespace Maui.AutoNav.Tests;

public class PageRouteNamingTests
{
    [Fact]
    public void RouteFor_lowercases_and_strips_Page_suffix()
    {
        Assert.Equal("login", PageRouteNaming.RouteFor(typeof(LoginPage)));
        Assert.Equal("settings", PageRouteNaming.RouteFor(typeof(SettingsPage)));
    }

    [Fact]
    public void RouteFor_throws_for_null_type()
    {
        Assert.Throws<ArgumentNullException>(() => PageRouteNaming.RouteFor(null!));
    }
}
