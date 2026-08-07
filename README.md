# Maui.AutoNav

[![CI](https://github.com/exendahal/Maui.AutoNav/actions/workflows/ci.yml/badge.svg)](https://github.com/exendahal/Maui.AutoNav/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AutoNav.Maui.svg)](https://www.nuget.org/packages/AutoNav.Maui)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

MVVM navigation for **.NET MAUI**, with no setup beyond naming your files sensibly.

Name a page `LoginPage` and a view model `LoginViewModel` (or `LoginPageModel`), and
Maui.AutoNav wires them together automatically: the view model gets created via DI, its
`BindingContext` gets set, and its lifecycle methods fire on navigation. No DI container of
its own, no XAML markup extensions - just `Microsoft.Extensions.DependencyInjection`, which
every MAUI app already has.

## Install

```
dotnet add package AutoNav.Maui
```

## Quick start

**1. Name your files by convention** - `Page` → `ViewModel` (or `PageModel`), same folder
structure either way:

```csharp
// Views/LoginPage.xaml.cs
public partial class LoginPage : ContentPage
{
    public LoginPage() => InitializeComponent();
}

// ViewModels/LoginViewModel.cs
public class LoginViewModel : ObservableObject, IInitializeAsync<string>
{
    public Task InitializeAsync(string welcomeMessage) { ... }
}
```

Prefer an explicit link instead? `[ViewModel(typeof(LoginViewModel))]` on the page always
wins over the naming convention.

**2. Register everything in `MauiProgram.cs`:**

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>();

    builder.Services.AddAutoNavigation(Assembly.GetExecutingAssembly());

    return builder.Build();
}
```

**3. Wire the window once, in `App.xaml.cs`:**

```csharp
public partial class App : Application
{
    private readonly IServiceProvider _Services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _Services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new AppShell()).UseAutoNavigation(_Services);
}
```

**4. Declare pages in `AppShell.xaml` the normal way** - including the first one:

```xml
<ShellContent Title="Login" ContentTemplate="{DataTemplate views:LoginPage}" />
```

No special-casing needed for the root page: `UseAutoNavigation` (step 3) attaches an
observer that catches it and wires its view model from the same convention map, same as
every other page. See [Root page](#root-page) below if you'd rather skip that and wire it
explicitly.

**5. Navigate from a view model** - no `Page` references, no query strings:

```csharp
public class LoginViewModel(INavigationService navigation) : ObservableObject
{
    [RelayCommand]
    private Task SignInAsync() => navigation.NavigateToAsync<DashboardViewModel, UserSession>(session);
}
```

That's it - two files (`MauiProgram.cs`, `App.xaml.cs`) plus normal naming, and every other
page just works.

## Naming convention

- `LoginPage` in `MyApp.Views` (or `.Pages`) resolves to `LoginViewModel` in
  `MyApp.ViewModels`.
- No `LoginViewModel`? `LoginPageModel` is tried next - the naming convention used by the
  .NET MAUI Community Toolkit's own sample apps. Works with the same folder layout, no extra
  setup.
- A class that doesn't end in `Page` is matched by appending `ViewModel` to its full name
  (`Login` → `LoginViewModel`).
- `[ViewModel(typeof(CustomViewModel))]` on the page always overrides the convention.

Everything discovered this way is registered as a transient service, with a Shell route
(the page name minus `Page`, lowercased) registered automatically - you never call
`Routing.RegisterRoute` yourself.

## Lifecycle interfaces

Implement whichever your view model needs - none are required:

```csharp
public interface IInitializeAsync<in T>   // fires once, with a typed navigation parameter
{
    Task InitializeAsync(T parameter);
}

public interface IInitializeAsync         // fires once, no parameter
{
    Task InitializeAsync();
}

public interface IAppearingAware          // fires on every OnAppearing
{
    Task OnAppearingAsync();
}

public interface IDisappearingAware       // fires on every OnDisappearing
{
    Task OnDisappearingAsync();
}

public interface IConfirmNavigationAsync  // checked by GoBackAsync() before popping
{
    Task<bool> CanNavigateFromAsync();
}

public interface IDestructible            // called by GoBackAsync() after popping
{
    void Destroy();
}
```

## Navigating

Inject `INavigationService` into any view model. It works the same way whether the app uses
Shell or a classic `NavigationPage`:

```csharp
// Push and wait for a picker result:
var picked = await navigation.NavigateForResultAsync<ItemPickerViewModel, Item?>();

// From the picker page, once the user taps an item:
await navigation.GoBackAsync(selectedItem);

// Present something modally, with no transition animation:
await navigation.NavigateToModalAsync<FiltersViewModel>(animate: false);

// From the filters page, dismiss without animating either:
await navigation.GoBackModalAsync(animate: false);
```

Every method takes an optional `animate` parameter (default `true`). Full interface:

```csharp
public interface INavigationService
{
    Task NavigateToAsync<TViewModel>(bool animate = true) where TViewModel : class;
    Task NavigateToAsync<TViewModel, TParam>(TParam parameter, bool animate = true) where TViewModel : class;
    Task<TResult?> NavigateForResultAsync<TViewModel, TResult>(bool animate = true) where TViewModel : class;
    Task<TResult?> NavigateForResultAsync<TViewModel, TParam, TResult>(TParam parameter, bool animate = true) where TViewModel : class;
    Task GoBackAsync(bool animate = true);
    Task GoBackAsync<TResult>(TResult result, bool animate = true);

    Task NavigateToModalAsync<TViewModel>(bool animate = true) where TViewModel : class;
    Task NavigateToModalAsync<TViewModel, TParam>(TParam parameter, bool animate = true) where TViewModel : class;
    Task<TResult?> NavigateForModalResultAsync<TViewModel, TResult>(bool animate = true) where TViewModel : class;
    Task<TResult?> NavigateForModalResultAsync<TViewModel, TParam, TResult>(TParam parameter, bool animate = true) where TViewModel : class;
    Task GoBackModalAsync(bool animate = true);
    Task GoBackModalAsync<TResult>(TResult result, bool animate = true);
}
```

## Root page

A page declared with `ContentTemplate="{DataTemplate views:LoginPage}"` gets built by
MAUI's own `DataTemplate` machinery with a bare parameterless constructor - no
`BindingContext` from that alone. For every other page this doesn't matter, because
`INavigationService`/the Shell route factory resolve and wire it before showing it. The
first page is different: it's the Shell's own *implicit* initial navigation, not something
your app navigated to.

`UseAutoNavigation` (step 3) attaches exactly for this - it catches the first page once
Shell has it current and wires its view model from the same convention map, no different
from any other page. That's what [the sample app](samples/Maui.AutoNav.Sample) does. Nothing
else to do for a Shell app: keep declaring the root page in XAML like every other page.

A classic `NavigationPage` app has no Shell to catch this, so its root page needs one
explicit call - `services.ResolveRootPage<TViewModel>()` - in `App.CreateWindow`:

```csharp
protected override Window CreateWindow(IActivationState? activationState)
{
    var root = _Services.ResolveRootPage<LoginViewModel>();
    return new Window(new NavigationPage(root)).UseAutoNavigation(_Services);
}
```

Every page after that goes through `INavigationService` as usual.

## Popups (and other views outside the page scan)

The naming convention and `[ViewModel]` override only cover `Page`s - a popup
(CommunityToolkit.Maui's `Popup`, or MAUI's own `ShowPopupAsync`) never reaches the screen
through Shell, `INavigationService`, or anything else this library hooks into, so there's no
event to auto-wire it from. `ResolveViewModelFor` runs the same convention lookup on demand
instead, so you're not hand-writing `GetRequiredService<FilterViewModel>()` yourself:

```csharp
var viewModel = services.ResolveViewModelFor<FilterPopup>();   // FilterPopup -> FilterViewModel
var popup = new FilterPopup { BindingContext = viewModel };
await (viewModel as IInitializeAsync)?.InitializeAsync() ?? Task.CompletedTask;
await this.ShowPopupAsync(popup);
```

`Popup` is stripped the same way `Page` is (`FilterPopup` → `FilterViewModel`, or
`FilterPageModel` if you use that suffix instead), and a `Popups` folder maps to
`ViewModels` the same way `Views`/`Pages` do. You're responsible for assigning
`BindingContext` and calling any lifecycle interface yourself, as above - nothing fires
automatically for a view resolved this way. The view model needs to be registered in DI too;
`AddAutoNavigation` only registers view models it paired with an actual page, so add yours
alongside it: `services.AddTransient<FilterViewModel>()`.

## Not yet supported

- Hardware back button / swipe-back gesture interception (so `IConfirmNavigationAsync` /
  `IDestructible` aren't enforced on a hardware/gesture-triggered pop, only on
  `INavigationService.GoBackAsync()`).
- Nested/tabbed Shell navigation edge cases.
- A Roslyn source generator to replace the reflection-based scan (planned once the API
  settles).

## Sample app

[`samples/Maui.AutoNav.Sample`](samples/Maui.AutoNav.Sample) is a small Shell-based contacts
app exercising everything above - convention binding, typed parameters, every lifecycle
interface, modal navigation with a result, and the `animate` flag. It references the library
from source, so it always tracks this repo's code.

## Claude Code skills

This repo ships two [Claude Code skills](https://code.claude.com/docs) under [`.skills/`](.skills)
for consumers of the package:

- **`scaffold-page`** - scaffolds a new Page + ViewModel pair following the naming
  convention above (or a `[ViewModel]` override), with the right lifecycle interfaces stubbed in.
- **`setup-maui-autonav`** - wires a MAUI app to use Maui.AutoNav for the first time.

They trigger automatically in Claude Code when you ask for things like "add a settings
page" or "wire up navigation" in a project that references this package.

## Building from source

Requires the .NET 10 SDK and the MAUI workload:

```
dotnet workload install maui
dotnet test test/Maui.AutoNav.Tests/Maui.AutoNav.Tests.csproj   # fast, no workload needed
dotnet pack src/Maui.AutoNav/Maui.AutoNav.csproj -c Release      # full multi-target pack
```

(The test project builds against `src/Maui.AutoNav.Core`, a MAUI-free `net10.0` project that
compiles the same source files - that's what keeps `dotnet test` workload-free. See its
`.csproj` comments if you're touching that split.)

## Contributing

Issues and PRs are welcome. Please open an issue describing the change before sending a
large PR, and keep the "dependency-light" constraint in mind - the only intended runtime
dependency is `Microsoft.Extensions.DependencyInjection.Abstractions`.

## License

[MIT](LICENSE)
