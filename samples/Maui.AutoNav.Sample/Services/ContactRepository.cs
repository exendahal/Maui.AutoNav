using Maui.AutoNav.Sample.Models;

namespace Maui.AutoNav.Sample.Services;

/// <summary>
/// An in-memory stand-in for a real data source, registered as a singleton in
/// MauiProgram.cs. It has nothing to do with Maui.AutoNav - it's here to show that the
/// library's <c>INavigationService</c> composes with whatever other services a real app
/// already has, rather than requiring everything to go through it.
/// </summary>
public sealed class ContactRepository
{
    private readonly List<ContactEntry> _Contacts =
    [
        new ContactEntry(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", "555-0100"),
        new ContactEntry(Guid.NewGuid(), "Grace Hopper", "grace@example.com", "555-0101"),
        new ContactEntry(Guid.NewGuid(), "Alan Turing", "alan@example.com", "555-0102"),
    ];

    public IReadOnlyList<ContactEntry> GetAll() => _Contacts;

    public void Add(ContactEntry contact) => _Contacts.Add(contact);

    public void Update(ContactEntry contact)
    {
        var index = _Contacts.FindIndex(c => c.Id == contact.Id);
        if (index >= 0)
        {
            _Contacts[index] = contact;
        }
    }
}
