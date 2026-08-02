namespace Maui.AutoNav;

/// <summary>
/// Implemented by a view model that needs deterministic cleanup (unsubscribe, dispose)
/// when its page is popped from the navigation stack via <see cref="INavigationService"/>.
/// </summary>
public interface IDestructible
{
    void Destroy();
}
