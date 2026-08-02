# Maui.AutoNav.Sample

A small Shell-based contacts app that exercises every feature of the
[Maui.AutoNav](../../README.md) package across four pages, using the library from source
(via a `ProjectReference` to `src/Maui.AutoNav`) rather than from NuGet.

## What it demonstrates

| Page | ViewModel | Feature shown |
|---|---|---|
| `ContactsPage` (Shell's root - built in `AppShell`'s constructor via `ResolveRootPage<ContactsViewModel>()`) | `ContactsViewModel` | Overrides `OnAppearingAsync`; navigating on selection via a two-way `SelectedItem` binding (no `SelectionChanged` code-behind); `NavigateForModalResultAsync` |
| `ContactDetailPage` | `ContactDetailViewModel` | `IInitializeAsync<ContactEntry>` (implemented directly - see `ViewModelBase` below); overrides `OnAppearingAsync` + `OnDisappearingAsync` + `Destroy` |
| `EditContactPage` | `EditContactViewModel` | `IInitializeAsync<ContactEntry>`; overrides `CanNavigateFromAsync` (dirty-tracking, with a real confirmation `DisplayAlert`); `GoBackAsync(animate: false)` |
| `AddContactPage` (presented modally) | `AddContactViewModel` | Overrides the base's parameterless `InitializeAsync()`; `NavigateToModalAsync`/`GoBackModalAsync<TResult>` result round-trip, including the "cancelled" (`null`) case |

All four inherit **`ViewModelBase`** (`ViewModels/ViewModelBase.cs`), which inherits
`ObservableObject` once and implements every lifecycle interface *except*
`IInitializeAsync<T>` with a virtual no-op default - `Task.CompletedTask`,
`Task.FromResult(true)`, or nothing, depending on the hook. A concrete view model only
overrides what it actually uses instead of re-declaring `: ObservableObject, IAppearingAware,
IDisappearingAware, ...` on every class. `IInitializeAsync<T>` stays out of the base because
its parameter type is different per page (`ContactEntry` here, but it wouldn't be in a bigger
app) - `ContactDetailViewModel` and `EditContactViewModel` implement it directly, alongside
`ViewModelBase`.

The flow: **Contacts** list → tap a contact → **Contact detail** → **Edit** → save or cancel
back to detail → back to the list (which refreshes via `IAppearingAware`). "Add contact" on
the list opens **Add contact** modally and, on save, hands the new `ContactEntry` straight back to
`ContactsViewModel` with no query strings or serialization involved.

## Running it

Requires the .NET 10 SDK and the MAUI workload (`dotnet workload install maui`) - see the
main [README's "Building from source"](../../README.md#building-from-source) section.

From this folder, target whichever platform you have set up (Android emulator, Windows,
etc.):

```
dotnet build -t:Run -f net10.0-android
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

Or open `Maui.AutoNav.sln` at the repo root in Visual Studio / Visual Studio Code with the
MAUI workload installed, set `Maui.AutoNav.Sample` as the startup project, and run.

## What to look at first

- **`MauiProgram.cs`** and **`App.xaml.cs`** - the entire integration surface: one
  `AddAutoNavigation(...)` call and one `.UseAutoNavigation(_Services)` call. Nothing else in
  this app registers a page or a view model by hand.
- **`AppShell.xaml.cs`** - builds its `ShellContent` in the constructor with
  `services.ResolveRootPage<ContactsViewModel>()` instead of declaring it in XAML with
  `ContentTemplate="{DataTemplate views:ContactsPage}"`. That markup would construct the page
  with a bare parameterless constructor and never give it a `BindingContext` - and since it's
  Shell's own *implicit* initial navigation rather than a `GoToAsync` call, there's no
  reliable event afterwards to catch and fix it up. `ResolveRootPage` sidesteps that
  entirely: it resolves and wires the view model *before* the page is ever shown.
- **`ViewModels/*.cs`** - none of them touch `Page`, `BindingContext`, or DI registration.
  They only ever depend on `INavigationService` (and, for the contacts list/detail/edit flow,
  the sample's own `ContactRepository` - showing that Maui.AutoNav composes with whatever
  other services a real app already has).
- **`ViewModels/ViewModelBase.cs`** - a small pattern worth copying into a real app: a shared
  base with virtual no-op defaults for the lifecycle interfaces, so a view model only
  overrides the ones it needs instead of listing every interface it implements.
- **`Models/ContactEntry.cs`** - named `ContactEntry`, not the more obvious `Contact`. MAUI
  adds `Microsoft.Maui.ApplicationModel.Communication` (the device-contacts picker API) as an
  implicit global `using`, and it has its own `Contact` type - the two collide the moment
  either is referenced unqualified. Worth remembering for any model whose name might also be
  a common MAUI/Essentials/BCL type (`Contact`, `Location`, `Color`, `Result`, ...).
- **Every file under `Views/`** - every `.xaml.cs` in this sample is just a constructor
  calling `InitializeComponent()`. `ContactsViewModel.SelectedContact` is bound two-way to
  the `CollectionView`'s `SelectedItem` in `ContactsPage.xaml`, and a
  `[ObservableProperty]`-generated partial method (`OnSelectedContactChanged`) reacts to it -
  navigation-on-selection is entirely a binding, not a `SelectionChanged` event handler.
