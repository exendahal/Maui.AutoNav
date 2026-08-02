namespace Maui.AutoNav;

/// <summary>
/// Implemented by a view model that needs to react every time its page's
/// <c>OnAppearing</c> fires (not just on first navigation).
/// </summary>
public interface IAppearingAware
{
    Task OnAppearingAsync();
}
