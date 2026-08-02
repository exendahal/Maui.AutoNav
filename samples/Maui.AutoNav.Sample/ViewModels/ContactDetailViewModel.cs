using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.AutoNav.Sample.Models;

namespace Maui.AutoNav.Sample.ViewModels;

/// <summary>
/// Reached from ContactsViewModel via <c>NavigateToAsync&lt;ContactDetailViewModel, ContactEntry&gt;</c>.
/// Demonstrates the typed-parameter delivery (<see cref="IInitializeAsync{T}"/>) plus every
/// other non-modal lifecycle hook: appearing/disappearing on every visit, and cleanup once
/// popped via <see cref="INavigationService.GoBackAsync(bool)"/>.
/// </summary>
public partial class ContactDetailViewModel(INavigationService navigation) : ViewModelBase, IInitializeAsync<ContactEntry>
{
    [ObservableProperty]
    private ContactEntry _Contact = ContactEntry.Empty;

    public Task InitializeAsync(ContactEntry parameter)
    {
        Contact = parameter;
        return Task.CompletedTask;
    }

    public override Task OnAppearingAsync()
    {
        Debug.WriteLine($"[ContactDetail] appearing for {Contact.Name}");
        return Task.CompletedTask;
    }

    public override Task OnDisappearingAsync()
    {
        Debug.WriteLine($"[ContactDetail] disappearing for {Contact.Name}");
        return Task.CompletedTask;
    }

    public override void Destroy()
    {
        // Runs once, after INavigationService.GoBackAsync() pops this page - the place to
        // unsubscribe from events or dispose anything this view model picked up.
        Debug.WriteLine($"[ContactDetail] destroyed for {Contact.Name}");
    }

    [RelayCommand]
    private Task EditAsync() => navigation.NavigateToAsync<EditContactViewModel, ContactEntry>(Contact);

    [RelayCommand]
    private Task GoBackAsync() => navigation.GoBackAsync();
}
