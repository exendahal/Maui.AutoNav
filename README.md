# Maui.AutoNav

[![CI](https://github.com/exendahal/Maui.AutoNav/actions/workflows/ci.yml/badge.svg)](https://github.com/exendahal/Maui.AutoNav/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Maui.AutoNav.svg)](https://www.nuget.org/packages/Maui.AutoNav)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A lightweight, dependency-light MVVM navigation package for **.NET MAUI**. It auto-binds
pages to view models, exposes async lifecycle hooks, supports strongly-typed navigation
parameters, and gives you a single `INavigationService` that behaves identically whether
your app uses Shell or a classic `NavigationPage`.

No DI container of its own, no region manager, no modules, no custom XAML markup
extensions - just `Microsoft.Extensions.DependencyInjection`, which every MAUI app already
has.

## Why

.NET MAUI ships no first-class MVVM navigation story. Most projects end up either wiring
`BindingContext = new XViewModel()` by hand in every page's code-behind, or pulling in a
full framework (Prism, etc.) that brings a DI container, regions, and modules a
small-to-mid app doesn't need. Shell makes it worse: `IQueryAttributable` only passes
string dictionaries, and Shell never calls a view model lifecycle method on navigation -
every project reinvents this bridge. Maui.AutoNav is that bridge, and nothing else.

## Features

| Feature | How |
|---|---|
| Convention-based binding | `LoginPage` → `LoginViewModel`, resolved via DI |
| Explicit override | `[ViewModel(typeof(CustomViewModel))]` on the page |
| Zero code-behind wiring | `BindingContext` is set for you after the page is constructed |
| One-line registration | `services.AddAutoNavigation(Assembly.GetExecutingAssembly())` |
| Async lifecycle | `IInitializeAsync<T>`, `IAppearingAware`, `IDisappearingAware`, `IConfirmNavigationAsync`, `IDestructible` |
| Typed parameters | `NavigateToAsync<TViewModel, TParam>(param)` - no query-string (de)serialization |
| Back-navigation with a result | `NavigateForResultAsync<TViewModel, TResult>()` / `GoBackAsync<TResult>(result)` |
| Modal presentation | `NavigateToModalAsync<TViewModel>()` / `GoBackModalAsync()`, with their own result-returning overloads |
| Animation control | Every push/pop/modal method takes an optional `animate` flag (defaults to `true`) |
| Shell + classic parity | One `INavigationService`, works unmodified on either navigation style |
| Trim / NativeAOT friendly | Pure reflection kept to startup-time scanning; no runtime `Activator` calls on the hot navigation path |

## Install

```
dotnet add package Maui.AutoNav
```

## Quick start

**1. Name your pages and view models by convention** (or use `[ViewModel]` to override):

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

**2. Register everything in `MauiProgram.cs`:**

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiAppBuilder.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts => { /* ... */ });

    builder.Services.AddAutoNavigation(Assembly.GetExecutingAssembly());

    var app = builder.Build();
    Maui.AutoNav.AutoNavigation.Initialize(app.Services);
    return app;
}
```

**3. Wire the window once, in `App.xaml.cs`** (this is also where `AutoNavigation.Initialize`
happens automatically if you'd rather not call it in `MauiProgram.cs`):

```csharp
public partial class App : Application
{
    private readonly IServiceProvider _Services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _Services = services;
    }

    // Shell apps - AppShell takes IServiceProvider too, see below:
    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new AppShell(_Services)).UseAutoNavigation(_Services);

    // Classic NavigationPage apps - resolve the one root page explicitly,
    // every navigation after that goes through INavigationService:
    // protected override Window CreateWindow(IActivationState? activationState)
    // {
    //     var root = _Services.ResolveRootPage<LoginViewModel>();
    //     return new Window(new NavigationPage(root)).UseAutoNavigation(_Services);
    // }
}
```

For a Shell app, build the root `ShellContent` in `AppShell`'s constructor with the same
`ResolveRootPage<TViewModel>()` helper, instead of declaring it in XAML with
`ContentTemplate="{DataTemplate views:LoginPage}"`. That markup builds the page with MAUI's
own `DataTemplate` machinery and a parameterless constructor, which never gets a
`BindingContext` at all - and unlike a page reached via `GoToAsync`, there's no reliable event
to catch afterwards and fix it up, since it's the Shell's *implicit* initial navigation:

```csharp
public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        Items.Add(new ShellContent { Title = "Login", Content = services.ResolveRootPage<LoginViewModel>() });
    }
}
```

Any *other* page reached the ambient way - an extra Tab or FlyoutItem still declared with
`ContentTemplate="{DataTemplate ...}"`, or a page pushed directly via `Navigation.PushAsync`
- is still picked up on a best-effort basis. But for the one page every app can't do without,
use `ResolveRootPage` - it's a direct assignment, not something to catch after the fact.

**4. Navigate from a view model - no `Page` references, no query strings:**

```csharp
public class LoginViewModel(INavigationService navigation) : ObservableObject
{
    [RelayCommand]
    private Task SignInAsync() => navigation.NavigateToAsync<DashboardViewModel, UserSession>(session);
}
```

That's the whole integration surface. From install to a navigated page is two files
(`MauiProgram.cs`, `App.xaml.cs`) and normal page/view-model naming.

## Convention resolution

- `LoginPage` in namespace `MyApp.Views` resolves to `LoginViewModel` in `MyApp.ViewModels`
  (`.Views`/`.Pages` are mapped to `.ViewModels`; same-namespace matches are preferred over
  a same-name match in an unrelated namespace).
- Any class whose name doesn't end in `Page` is matched by appending `ViewModel` to the
  full name (`Login` → `LoginViewModel`).
- `[ViewModel(typeof(CustomViewModel))]` on the page always wins over the convention.
- Pages and view models discovered this way are registered as **transient** services, and
  a Shell route (the page's name, minus `Page`, lowercased - `LoginPage` → `"login"`) is
  registered for each one automatically. You never call `Routing.RegisterRoute` yourself.

## Lifecycle interfaces

Implement whichever of these your view model needs - none are required:

```csharp
public interface IInitializeAsync<in T>       // fires once, with a typed navigation parameter
{
    Task InitializeAsync(T parameter);
}

public interface IInitializeAsync             // fires once, no parameter
{
    Task InitializeAsync();
}

public interface IAppearingAware               // fires on every OnAppearing
{
    Task OnAppearingAsync();
}

public interface IDisappearingAware             // fires on every OnDisappearing
{
    Task OnDisappearingAsync();
}

public interface IConfirmNavigationAsync        // checked by GoBackAsync() before popping
{
    Task<bool> CanNavigateFromAsync();
}

public interface IDestructible                  // called by GoBackAsync() after popping
{
    void Destroy();
}
```

## Navigation service

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

Resolve it from DI like any other service - constructor-inject it into your view models.
It looks at the app window's current page at call time (`Shell` vs. `NavigationPage`) and
drives the matching MAUI navigation API, so the same call site works in both app styles.

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

Every method takes an optional `animate` parameter (default `true`) that flows straight
into the underlying MAUI call - `Shell.GoToAsync(state, animate)` or
`INavigation.PushAsync(page, animate)` / `PopAsync(animate)` for push/pop, and
`INavigation.PushModalAsync(page, animate)` / `PopModalAsync(animate)` for modal
presentation. Modal presentation itself is handled the same way for Shell and classic apps -
it's a window-level concept, not a Shell route, so no extra registration is needed beyond
the usual `AddAutoNavigation` scan.

## Shell vs. classic NavigationPage - what's automatic and what isn't

Both styles get: automatic `BindingContext` assignment, `IAppearingAware` /
`IDisappearingAware`, and typed-parameter delivery through `INavigationService`.

| Behavior | Shell | Classic `NavigationPage` |
|---|---|---|
| Route registration | Automatic (`Routing.RegisterRoute` called for you) | N/A - pages are pushed directly |
| Page creation goes through DI | Yes, via a custom `RouteFactory` (Shell's default route registration does **not** use DI - this is the standard workaround) | Yes, `INavigationService` resolves pages from the container |
| Root/initial page | Same helper - build the `ShellContent` in `AppShell`'s constructor with `services.ResolveRootPage<TViewModel>()` | Call `services.ResolveRootPage<TViewModel>()` once in `App.CreateWindow` |
| `IConfirmNavigationAsync` / `IDestructible` | Enforced when navigating back via `INavigationService.GoBackAsync()` | Same |
| Hardware back button / swipe-back gesture | **Not yet intercepted** - out of MVP scope, see Roadmap | **Not yet intercepted** |

## Roadmap

Matches the PRD's explicitly-scoped v2/backlog items:

- Roslyn source generator to replace the reflection-based Page↔ViewModel scan (the PRD's
  own risk mitigation: ship the reflection MVP first, swap the engine once the API is
  stable - this is that MVP).
- Android hardware back-button interception hook, and the equivalent for
  `IConfirmNavigationAsync` / `IDestructible` on gesture/hardware-triggered pops.
- Navigation logging/diagnostics/telemetry hook.
- Nested/tabbed Shell navigation edge cases.
- A classic `NavigationPage` sample app to go with the Shell one below, and a docs site.

## Sample app

[`samples/Maui.AutoNav.Sample`](samples/Maui.AutoNav.Sample) is a small Shell-based contacts
app that exercises every feature in this README across four pages - convention binding on
the app's own root page, typed parameters, every lifecycle interface, modal navigation with
a result, and the `animate` flag. It references the library from source, so it always tracks
this repo's code. See its own [README](samples/Maui.AutoNav.Sample/README.md) for what each
page demonstrates and how to run it.

## Claude Code skills

This repo ships two [Claude Code skills](https://code.claude.com/docs) under [`.skills/`](.skills)
for consumers of the package, not just its own development:

- **`scaffold-page`** - scaffolds a new Page + ViewModel pair that follows the naming
  convention above (or the `[ViewModel]` override), with the right lifecycle interfaces
  stubbed in.
- **`setup-maui-autonav`** - wires a MAUI app to use Maui.AutoNav for the first time:
  the `AddAutoNavigation` call in `MauiProgram.cs` and the `App.xaml.cs` window wiring, for
  either Shell or classic `NavigationPage` apps.

They trigger automatically in Claude Code when you ask for things like "add a settings
page" or "wire up navigation" in a project that references this package.

## Building from source

Requires the .NET 10 SDK and the MAUI workload:

```
dotnet workload install maui
dotnet test test/Maui.AutoNav.Tests/Maui.AutoNav.Tests.csproj   # fast, no workload needed
dotnet pack src/Maui.AutoNav/Maui.AutoNav.csproj -c Release      # full multi-target pack
```

The naming-convention engine lives in a separate project, `src/Maui.AutoNav.Core`
(plain `net10.0`, no MAUI reference at all), compiling the same source files as `src/Maui.AutoNav`
via `<Compile Include>` links rather than a project-to-project dependency. That split exists
specifically so `dotnet test` never needs to touch a TargetFramework whose workload might be
missing: MSBuild's workload resolution evaluates *every* `TargetFramework` a project declares
before it will build even one of them, so a single project mixing `net10.0` with
`net10.0-ios`/`net10.0-maccatalyst` would need those workloads installed just to run unit
tests - and on Linux, those two can't be installed at all. `src/Maui.AutoNav` itself only
declares the real platform TargetFrameworks (`net10.0-android`, `net10.0-ios`,
`net10.0-maccatalyst`, `net10.0-windows10.0.19041.0`) - the ones package consumers get.

Every push to `main` and every pull request runs `.github/workflows/ci.yml`: unit tests on
Ubuntu (no MAUI workload needed), plus a full multi-target build and pack on Windows (with
the MAUI workload installed) to catch anything platform-specific.

## Contributing

Issues and PRs are welcome. Please open an issue describing the change before sending a
large PR, and keep the "dependency-light" constraint in mind - the only intended runtime
dependency is `Microsoft.Extensions.DependencyInjection.Abstractions`.

## License

[MIT](LICENSE)
