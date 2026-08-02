#if MAUI_AUTONAV_PLATFORM
using Maui.AutoNav.Internal;
using Microsoft.Maui.Controls;

namespace Maui.AutoNav;

public static class WindowExtensions
{
    /// <summary>
    /// One-line integration point for <c>App.CreateWindow</c>. Stores <paramref name="services"/>
    /// for the Shell route factory to use, and attaches the ambient safety net that wires
    /// pages reached without going through <see cref="INavigationService"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override Window CreateWindow(IActivationState? activationState) =>
    ///     new Window(new AppShell()).UseAutoNavigation(_Services);
    /// </code>
    /// </example>
    public static Window UseAutoNavigation(this Window window, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(services);

        AutoNavigation.Initialize(services);
        AmbientNavigationObserver.Attach(window, services);

        return window;
    }
}
#endif
