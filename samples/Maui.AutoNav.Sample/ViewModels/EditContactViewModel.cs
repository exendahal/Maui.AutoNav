using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.AutoNav.Sample.Models;
using Maui.AutoNav.Sample.Services;

namespace Maui.AutoNav.Sample.ViewModels;

/// <summary>
/// Reached from ContactDetailViewModel via <c>NavigateToAsync&lt;EditContactViewModel, ContactEntry&gt;</c>.
/// Demonstrates <see cref="IConfirmNavigationAsync"/>: editing any field marks the page dirty,
/// and both the explicit "Back" command and (per Maui.AutoNav's current scope) any future
/// programmatic <c>GoBackAsync()</c> call ask for confirmation before discarding changes.
/// "Save" clears the dirty flag first, since there's nothing left to confirm once it's saved.
/// </summary>
public partial class EditContactViewModel(INavigationService navigation, ContactRepository repository)
    : ViewModelBase, IInitializeAsync<ContactEntry>
{
    private ContactEntry _Original = ContactEntry.Empty;
    private bool _IsDirty;

    [ObservableProperty]
    private string _Name = string.Empty;

    [ObservableProperty]
    private string _Email = string.Empty;

    [ObservableProperty]
    private string _Phone = string.Empty;

    public Task InitializeAsync(ContactEntry parameter)
    {
        _Original = parameter;
        Name = parameter.Name;
        Email = parameter.Email;
        Phone = parameter.Phone;
        _IsDirty = false;
        return Task.CompletedTask;
    }

    partial void OnNameChanged(string value) => _IsDirty = true;

    partial void OnEmailChanged(string value) => _IsDirty = true;

    partial void OnPhoneChanged(string value) => _IsDirty = true;

    public override Task<bool> CanNavigateFromAsync()
    {
        if (!_IsDirty)
        {
            return Task.FromResult(true);
        }

        var page = Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page : null;
        return page?.DisplayAlert(
            "Discard changes?",
            "You have unsaved changes. Leave without saving?",
            "Leave", "Stay") ?? Task.FromResult(true);
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        _IsDirty = false;
        repository.Update(_Original with { Name = Name, Email = Email, Phone = Phone });
        return navigation.GoBackAsync();
    }

    [RelayCommand]
    private Task CancelAsync() =>
        // No transition animation for an explicit "never mind" - and still goes through the
        // confirm-navigation check above like any other GoBackAsync() call, so an accidental
        // tap here after real edits still asks first.
        navigation.GoBackAsync(animate: false);
}
