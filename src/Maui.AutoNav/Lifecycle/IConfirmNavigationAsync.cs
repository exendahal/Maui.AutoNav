namespace Maui.AutoNav;

/// <summary>
/// Implemented by a view model that wants a chance to block back-navigation away from
/// its page (e.g. unsaved changes). Checked by <see cref="INavigationService.GoBackAsync()"/>
/// before the pop is performed.
/// </summary>
public interface IConfirmNavigationAsync
{
    /// <returns><c>true</c> to allow navigating away, <c>false</c> to cancel it.</returns>
    Task<bool> CanNavigateFromAsync();
}
