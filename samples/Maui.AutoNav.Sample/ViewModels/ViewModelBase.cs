using CommunityToolkit.Mvvm.ComponentModel;

namespace Maui.AutoNav.Sample.ViewModels;

/// <summary>
/// Common base for this sample's view models. Inherits <see cref="ObservableObject"/> once,
/// and gives a virtual no-op default for every Maui.AutoNav lifecycle hook that doesn't need
/// a typed parameter, so a concrete view model only overrides the ones it actually uses
/// instead of re-declaring the full interface list on every class.
/// <para>
/// <see cref="IInitializeAsync{T}"/> deliberately isn't here - its parameter type is
/// different per page, so a view model that needs it (see <c>ContactDetailViewModel</c> and
/// <c>EditContactViewModel</c>) implements it directly, alongside this base.
/// </para>
/// </summary>
public abstract class ViewModelBase : ObservableObject, IInitializeAsync, IAppearingAware, IDisappearingAware, IConfirmNavigationAsync, IDestructible
{
    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task OnAppearingAsync() => Task.CompletedTask;

    public virtual Task OnDisappearingAsync() => Task.CompletedTask;

    public virtual Task<bool> CanNavigateFromAsync() => Task.FromResult(true);

    public virtual void Destroy()
    {
    }
}
