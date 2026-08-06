---
name: scaffold-page
description: Scaffolds a new .NET MAUI Page + ViewModel pair for a project that uses the Maui.AutoNav package, following its "XPage" -> "XViewModel" naming convention (or the [ViewModel] attribute override), with the right async lifecycle interfaces (IInitializeAsync<T>, IAppearingAware, IDisappearingAware, IConfirmNavigationAsync, IDestructible) stubbed in. Use this whenever the user asks to add, create, or scaffold a new page, screen, view, or tab in a MAUI project that references Maui.AutoNav (check for a PackageReference to Maui.AutoNav, a call to AddAutoNavigation, or files following the XPage/XViewModel pattern) - even if they just say "add a settings page" or "create a screen for editing a profile" without naming the package. Do not use it for MAUI projects that don't use Maui.AutoNav, or for non-page classes (services, models, converters).
---

# Scaffold a Maui.AutoNav page

Maui.AutoNav auto-binds a page's `BindingContext` to a view model that's discovered either
by naming convention or an explicit attribute - it never needs the page or view model
registered by hand, and it never needs `BindingContext = new XViewModel()` written in
code-behind. Getting the naming and namespace right is what makes that automatic discovery
work, so that's the part worth being careful about here.

## 1. Confirm the project actually uses Maui.AutoNav

Before scaffolding, check for one of:
- A `PackageReference Include="AutoNav.Maui"` in a `.csproj`.
- A call to `services.AddAutoNavigation(...)` in `MauiProgram.cs`.
- Existing pages/view models that already follow the `XPage` / `XViewModel` pattern.

If none of these are present, this isn't a Maui.AutoNav project - either say so, or offer to
run the `setup-maui-autonav` skill first if the user wants to add the package.

## 2. Work out the name, folder, and namespace

Ask (or infer from the request) the page's base name - e.g. a request for "a settings page"
means base name `Settings`, producing `SettingsPage` and `SettingsViewModel`.

Look at the existing project layout before picking a folder:
- If there's a `Views/` folder next to a `ViewModels/` folder, put the new page in `Views/`
  and the view model in `ViewModels/`. This is the layout Maui.AutoNav's namespace inference
  expects: a page in `MyApp.Views` resolves to a view model in `MyApp.ViewModels`, because the
  resolver rewrites `.Views` (or `.Pages`) to `.ViewModels` when it looks for a same-namespace
  match.
- If there's a `Pages/` folder instead, use that the same way (also rewritten to `.ViewModels`).
- If there's no existing convention, default to `Views/` + `ViewModels/`.
- Match the namespace to the folder path, the same way the rest of the project does (check
  an existing page for the exact pattern - `.csproj` root namespace + folder path is typical).

Getting the namespace pairing right matters more than the folder name itself: the resolver
prefers a same-namespace match, but falls back to a same-name match anywhere in the scanned
assemblies, so a wrong namespace won't break the build - it just means the fallback (or
`[ViewModel]`, see step 4) is doing the work instead of the convention.

## 3. Decide which lifecycle interfaces the view model needs

None are required - a view model can implement zero of these. Ask only what's relevant to
the feature being built:

| Need | Interface | Signature |
|---|---|---|
| Receive a typed value the first time this page is navigated to | `IInitializeAsync<T>` | `Task InitializeAsync(T parameter)` |
| Run async setup once, with no parameter | `IInitializeAsync` | `Task InitializeAsync()` |
| React every time the page appears (not just the first time) | `IAppearingAware` | `Task OnAppearingAsync()` |
| React every time the page disappears | `IDisappearingAware` | `Task OnDisappearingAsync()` |
| Block back-navigation (e.g. unsaved changes) | `IConfirmNavigationAsync` | `Task<bool> CanNavigateFromAsync()` |
| Clean up (unsubscribe, dispose) when popped via `INavigationService.GoBackAsync()` | `IDestructible` | `void Destroy()` |

A page that's reached with `NavigateToAsync<TViewModel, TParam>(param)` or
`NavigateToModalAsync<TViewModel, TParam>(param)` needs `IInitializeAsync<TParam>` to receive
`param` - don't add it unless the caller is actually passing something.

## 4. Decide convention vs. `[ViewModel]` override

Use the plain naming convention by default. Reach for the attribute override instead when:
- The user wants to reuse an existing view model under a different page name.
- The page's name doesn't cleanly map to `XViewModel` (e.g. a shared `ErrorPage` bound to a
  `ProblemDetailsViewModel`).

```csharp
[Maui.AutoNav.ViewModel(typeof(ProblemDetailsViewModel))]
public partial class ErrorPage : ContentPage
```

The attribute always wins over the convention, so it's also the answer whenever the
requested name and the "correct" naming-convention name genuinely conflict.

## 5. Generate the files

Check one existing page in the project to see whether it uses XAML + code-behind or a
code-only `ContentPage`, and match that style. The XAML + code-behind version:

**`Views/{Name}Page.xaml`**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="{RootNamespace}.Views.{Name}Page"
             Title="{Name}">
    <VerticalStackLayout Padding="24" Spacing="16">
        <Label Text="{Name}" FontSize="24" FontAttributes="Bold" />
    </VerticalStackLayout>
</ContentPage>
```

**`Views/{Name}Page.xaml.cs`**
```csharp
namespace {RootNamespace}.Views;

public partial class {Name}Page : ContentPage
{
    public {Name}Page()
    {
        InitializeComponent();
    }
}
```

Do **not** add a constructor parameter for the view model and do **not** set
`BindingContext` here - Maui.AutoNav sets it after the page is constructed, whether the page
was reached via Shell, `INavigationService`, or the app's root-page setup. A constructor
that expects a view model parameter would only work by accident (DI resolving it) and would
fight the library's own wiring.

**`ViewModels/{Name}ViewModel.cs`** - check whether `CommunityToolkit.Mvvm` is referenced
(most Maui.AutoNav-using projects pair with it for `[ObservableProperty]`/`[RelayCommand]`);
if so:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.AutoNav;

namespace {RootNamespace}.ViewModels;

public partial class {Name}ViewModel : ObservableObject, IInitializeAsync<{ParamType}>
{
    public Task InitializeAsync({ParamType} parameter)
    {
        // ... use parameter to load state
        return Task.CompletedTask;
    }
}
```

Only implement the interfaces actually decided on in step 3 - a view model with none of them
is just a plain `ObservableObject` (or plain `INotifyPropertyChanged` class, if
CommunityToolkit.Mvvm isn't referenced). If the page needs to navigate onward, constructor-
inject `INavigationService`:

```csharp
public partial class {Name}ViewModel(INavigationService navigation) : ObservableObject
{
    [RelayCommand]
    private Task SaveAsync() => navigation.GoBackAsync();
}
```

## 6. What NOT to do

- Don't write `services.AddTransient<{Name}Page>()` or `services.AddTransient<{Name}ViewModel>()`
  anywhere. `AddAutoNavigation`'s assembly scan (called once, in `MauiProgram.cs`) picks up
  every page/view-model pair automatically at startup - registering them again is redundant
  and won't be needed even for this new page.
- Don't call `Routing.RegisterRoute` for a Shell app - that's also handled by the scan. The
  route it registers is the page's name minus `Page`, lowercased (e.g. `SettingsPage` ->
  `"settings"`) - useful to know if the page also needs to be reachable by a literal
  `Shell.Current.GoToAsync("settings")` call or a deep link, but not something to set up by hand.
- Don't manually set `BindingContext` in the page's constructor or `OnAppearing` override.

## 7. Tell the user what happens next

Once the files exist, nothing else is required - the next time the app builds, the new page
and view model are picked up by the existing `AddAutoNavigation(...)` scan. Mention this
explicitly so they don't go looking for a registration step that doesn't exist for this
package.
