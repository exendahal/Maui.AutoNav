---
name: setup-maui-autonav
description: Wires a .NET MAUI app to use the Maui.AutoNav navigation package for the first time - adding the package reference, the services.AddAutoNavigation(...) call in MauiProgram.cs, and the App.xaml.cs CreateWindow wiring via Window.UseAutoNavigation(...) for Shell apps or IServiceProvider.ResolveRootPage<TViewModel>() for classic NavigationPage apps. Use this whenever the user asks to install, set up, integrate, bootstrap, or configure Maui.AutoNav in a MAUI project, or says things like "wire up navigation", "add MVVM navigation to this app", or "set up the AutoNav package" - even if they don't spell out every step. Do not use it for MAUI project scaffolding unrelated to Maui.AutoNav, or for non-MAUI apps.
---

# Set up Maui.AutoNav in a MAUI app

Maui.AutoNav's entire integration surface is two files: `MauiProgram.cs` (register) and
`App.xaml.cs` (wire the window once). Everything after that - binding pages to view models,
registering Shell routes, resolving pages through DI - happens automatically. This skill
gets those two files right the first time, for whichever navigation style the app uses.

## 1. Add the package

```
dotnet add package AutoNav.Maui
```

Skip this if a `PackageReference Include="AutoNav.Maui"` already exists.

## 2. Work out whether the app is Shell-based or classic `NavigationPage`-based

Check, in order:
1. Does the project have an `AppShell.xaml` / a class deriving from `Shell`? → Shell-based.
2. Does `App.xaml.cs` (or wherever `MainPage`/`CreateWindow` is set) construct a
   `NavigationPage` directly? → Classic.
3. If neither is obviously true, ask the user rather than guessing - the two setups produce
   different `App.CreateWindow` code, and picking the wrong one means real navigation calls
   fail at runtime instead of failing to compile.

## 3. Register in `MauiProgram.cs`

Add the scan call before `builder.Build()`. It needs to know which assembly to scan - the
app's own assembly, not the library's:

```csharp
using System.Reflection;
using Maui.AutoNav;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiAppBuilder.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { /* existing font config */ });

        // ... the app's other builder.Services.Add... calls ...

        builder.Services.AddAutoNavigation(Assembly.GetExecutingAssembly());

        return builder.Build();
    }
}
```

Don't also add an explicit `Maui.AutoNav.AutoNavigation.Initialize(app.Services)` call here -
`Window.UseAutoNavigation(...)` in step 4 already does that internally. Adding both isn't
wrong (the second call is harmless), but it's redundant, and README examples showing it as a
`MauiProgram.cs` alternative are only there for apps that don't want to touch `App.xaml.cs`'s
`CreateWindow` for some reason - the two-file version below is the normal path.

`AddAutoNavigation` scans for every page/view-model pair that matches the `XPage`/`XViewModel`
naming convention (or carries a `[ViewModel(typeof(...))]` override), registers both as
transient DI services, and - for Shell apps - registers a Shell route for each page through a
DI-aware route factory (Shell's own `Routing.RegisterRoute(string, Type)` bypasses DI
entirely, which is why this extra step exists; without it, pages resolved by Shell wouldn't
get their view model or any constructor-injected dependencies).

## 4. Wire `App.xaml.cs`

The app's `App` class needs to receive `IServiceProvider` through DI - `UseMauiApp<App>()`
already resolves `App` from the container, so a constructor parameter is enough; no extra
registration is required.

**Shell apps:**

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
        new Window(new AppShell(_Services)).UseAutoNavigation(_Services);
}
```

`AppShell` needs a small change too - **do not** declare its root `ShellContent` in XAML with
`ContentTemplate="{DataTemplate views:LoginPage}"`. That markup builds the page with MAUI's
own `DataTemplate` machinery and a parameterless constructor, so it never gets a
`BindingContext` - and because it's the Shell's own *implicit* initial navigation (not a
`GoToAsync` call), there's no reliable event afterwards to catch and fix it up. Build it in
the constructor instead, with the same helper the classic-`NavigationPage` case uses below:

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

Ask the user which view model should back the root page (commonly a login or splash/landing
view model) if it isn't obvious. Any *other* Tab/FlyoutItem still declared with
`ContentTemplate="{DataTemplate ...}"` is picked up on a best-effort basis by an ambient
observer, but don't rely on that for the root page - use `ResolveRootPage` there.

**Classic `NavigationPage` apps:** there's one page that can't reach the screen through
`INavigationService` - the root page, since there's no "current page" to navigate from yet.
Ask the user which view model should back that root page (commonly a login or splash/landing
view model), then resolve it explicitly with `ResolveRootPage<TViewModel>()`:

```csharp
public partial class App : Application
{
    private readonly IServiceProvider _Services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _Services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var root = _Services.ResolveRootPage<LoginViewModel>();
        return new Window(new NavigationPage(root)).UseAutoNavigation(_Services);
    }
}
```

Every navigation after that root page goes through `INavigationService` as usual -
`ResolveRootPage` is only needed once, for the page that has no predecessor to navigate from.

## 5. Confirm `INavigationService` is ready to use

`AddAutoNavigation` already registers `INavigationService` as a singleton - nothing else to
register. Any view model that needs to navigate just constructor-injects it:

```csharp
public partial class LoginViewModel(INavigationService navigation) : ObservableObject
{
    [RelayCommand]
    private Task SignInAsync() => navigation.NavigateToAsync<DashboardViewModel, UserSession>(session);
}
```

## 6. Sanity-check before calling it done

- Run the app and confirm the root page's `BindingContext` is set (a bound `{Binding ...}`
  in the root page's XAML should show real data, not blank/default values).
- For a Shell app, navigate to at least one other registered page via
  `INavigationService.NavigateToAsync<TViewModel>()` and confirm it appears with its view
  model already bound - that exercises the DI-aware route factory from step 3.
- Don't add any per-page registration, `Routing.RegisterRoute` call, or manual
  `BindingContext` assignment anywhere - if any of those seem necessary to make a page work,
  something upstream (usually the page/view-model naming, see the `scaffold-page` skill) is
  off, not the wiring done here.
