using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.AutoNav.Sample.Models;
using Maui.AutoNav.Sample.Services;

namespace Maui.AutoNav.Sample.ViewModels;

/// <summary>
/// Backs the Shell app's root page (ContactsPage), declared normally in AppShell.xaml.
/// Overrides <see cref="ViewModelBase.OnAppearingAsync"/> so the list reloads every time the
/// page appears - including after returning from EditContactPage, where a contact may have
/// changed. Nothing else from the base's lifecycle hooks applies here.
/// </summary>
public partial class ContactsViewModel(INavigationService navigation, ContactRepository repository) : ViewModelBase
{
    public ObservableCollection<ContactEntry> Contacts { get; } = [];

    [ObservableProperty]
    private ContactEntry? _SelectedContact;

    public override Task OnAppearingAsync()
    {
        // Clears the CollectionView's highlight (via the two-way SelectedItem binding) on
        // every return trip, and refreshes the list in case EditContactPage changed something.
        SelectedContact = null;

        Contacts.Clear();
        foreach (var contact in repository.GetAll())
        {
            Contacts.Add(contact);
        }

        return Task.CompletedTask;
    }

    // CollectionView's SelectedItem is bound two-way straight to this property in
    // ContactsPage.xaml - no SelectionChanged handler, no code-behind at all. This partial
    // method is a CommunityToolkit.Mvvm hook that [ObservableProperty] calls automatically
    // whenever SelectedContact changes, from either side of the binding.
    partial void OnSelectedContactChanged(ContactEntry? value)
    {
        if (value is not null)
        {
            // Fire-and-forget: a property-changed hook can't be async, and navigating away
            // is the whole point of the selection changing.
            _ = navigation.NavigateToAsync<ContactDetailViewModel, ContactEntry>(value);
        }
    }

    [RelayCommand]
    private async Task AddContactAsync()
    {
        // Presented modally, and awaited for a result - AddContactPage hands the new
        // ContactEntry back via GoBackModalAsync(contact), or null via GoBackModalAsync<ContactEntry?>(null)
        // if the user cancels.
        var newContact = await navigation.NavigateForModalResultAsync<AddContactViewModel, ContactEntry>();
        if (newContact is not null)
        {
            repository.Add(newContact);
            Contacts.Add(newContact);
        }
    }
}
