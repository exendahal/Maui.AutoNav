namespace Maui.AutoNav;

/// <summary>
/// Implemented by a view model that needs to react every time its page's
/// <c>OnDisappearing</c> fires.
/// </summary>
public interface IDisappearingAware
{
    Task OnDisappearingAsync();
}
