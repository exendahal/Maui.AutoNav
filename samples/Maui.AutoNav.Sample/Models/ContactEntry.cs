namespace Maui.AutoNav.Sample.Models;

/// <summary>
/// The sample's only model - deliberately a plain immutable record. Editing happens on
/// copies of its fields in a view model, then a new <see cref="ContactEntry"/> replaces the
/// old one; nothing here needs to be mutable for the navigation patterns this sample shows off.
/// </summary>
/// <remarks>
/// Named <c>ContactEntry</c>, not <c>Contact</c> - <c>Microsoft.Maui.ApplicationModel.Communication</c>
/// (used by the device-contacts picker API) is one of the namespaces the MAUI SDK adds as an
/// implicit global <c>using</c>, and it already has its own <c>Contact</c> type. Any type name
/// this sample picked could collide with something MAUI, Essentials, or another package
/// brings in as an implicit/global using - worth double-checking common short names like this
/// one in a real app too.
/// </remarks>
public sealed record ContactEntry(Guid Id, string Name, string Email, string Phone)
{
    public static ContactEntry Empty { get; } = new(Guid.Empty, string.Empty, string.Empty, string.Empty);
}
