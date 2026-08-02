using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.AutoNav.Sample.Models;

namespace Maui.AutoNav.Sample.ViewModels;

/// <summary>
/// Presented modally from ContactsViewModel via
/// <c>NavigateForModalResultAsync&lt;AddContactViewModel, ContactEntry&gt;()</c>. Overrides the
/// base's parameterless <see cref="ViewModelBase.InitializeAsync()"/> (the one lifecycle hook
/// the other three pages in this sample don't need) purely to reset its fields on open - each
/// navigation already resolves a fresh transient instance, so this is mostly here to show the
/// hook exists.
/// </summary>
public partial class AddContactViewModel(INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private string _Name = string.Empty;

    [ObservableProperty]
    private string _Email = string.Empty;

    [ObservableProperty]
    private string _Phone = string.Empty;

    public override Task InitializeAsync()
    {
        Name = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        var contact = new ContactEntry(Guid.NewGuid(), Name, Email, Phone);
        return navigation.GoBackModalAsync(contact);
    }

    [RelayCommand]
    private Task CancelAsync() => navigation.GoBackModalAsync<ContactEntry?>(null);
}
