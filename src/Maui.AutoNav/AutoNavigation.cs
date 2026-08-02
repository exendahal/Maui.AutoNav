namespace Maui.AutoNav;

/// <summary>
/// Holds the app's root <see cref="IServiceProvider"/> so that MAUI-owned object graphs
/// (Shell route factories in particular) can resolve pages and view models through DI even
/// though MAUI - not this library - decides when those objects get constructed.
/// </summary>
/// <remarks>
/// Call <see cref="Initialize"/> exactly once, in <c>MauiProgram.CreateMauiApp()</c> right
/// after <c>builder.Build()</c> (or implicitly via <c>Window.UseAutoNavigation(...)</c>).
/// </remarks>
public static class AutoNavigation
{
    private static IServiceProvider? _Services;

    public static IServiceProvider Services => _Services
        ?? throw new InvalidOperationException(
            "Maui.AutoNav.AutoNavigation.Initialize(IServiceProvider) has not been called. " +
            "Call it once in MauiProgram.CreateMauiApp() after builder.Build(), or call " +
            "Window.UseAutoNavigation(services) from App.CreateWindow.");

    public static bool IsInitialized => _Services is not null;

    public static void Initialize(IServiceProvider services)
    {
        _Services = services ?? throw new ArgumentNullException(nameof(services));
    }
}
