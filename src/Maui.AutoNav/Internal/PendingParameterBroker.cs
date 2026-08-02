#if MAUI_AUTONAV_PLATFORM
namespace Maui.AutoNav.Internal;

/// <summary>
/// Shell builds pages through a synchronous <c>RouteFactory</c>, so a typed navigation
/// parameter can't be handed to it directly - <see cref="AutoNavigationService"/> stashes
/// it here just long enough for the route factory to know "don't fire the parameterless
/// <see cref="IInitializeAsync"/> for this page, a typed delivery is coming right after
/// GoToAsync returns." MAUI navigation is confined to the UI thread and effectively
/// sequential, so a single mutable slot is sufficient.
/// </summary>
internal static class PendingParameterBroker
{
    private static object? _Pending;

    public static void SetPending(object parameter) => _Pending = parameter;

    /// <summary>Clears and returns the pending parameter, if any.</summary>
    public static object? ConsumePending()
    {
        var value = _Pending;
        _Pending = null;
        return value;
    }
}
#endif
