# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- Optional `animate` parameter (default `true`) on every push/pop method of `INavigationService`, flowing through to `Shell.GoToAsync(state, animate)` and `INavigation.PushAsync`/`PopAsync`.
- Modal navigation: `NavigateToModalAsync<TViewModel>()` / `NavigateToModalAsync<TViewModel, TParam>()`, `NavigateForModalResultAsync` variants, and `GoBackModalAsync()` / `GoBackModalAsync<TResult>()`, all with the same `animate` flag. Modal presentation is handled uniformly for Shell and classic `NavigationPage` apps via `INavigation.PushModalAsync`/`PopModalAsync`.
- A runnable sample app under [`samples/Maui.AutoNav.Sample`](samples/Maui.AutoNav.Sample) (Shell-based) exercising every feature above across four pages.

### Fixed
- A Shell app's root page - declared the normal way in `AppShell.xaml` via `ContentTemplate="{DataTemplate views:SomePage}"` - never actually got its `BindingContext` set, and its `IAppearingAware`/etc. hooks never fired. That markup builds the page with MAUI's own `DataTemplate` machinery (a parameterless constructor), which bypasses the DI-aware `RouteFactory` entirely - that factory only intercepts routes reached dynamically via `GoToAsync`, not a Shell's own implicit initial navigation. An earlier attempt at this fix had `AmbientNavigationObserver` catch the page via `Shell.Navigated` and resolve its view model from the same convention/`[ViewModel]` map `AddAutoNavigation` already built - but `Navigated` turns out not to fire reliably for that implicit initial navigation, so it still didn't work in practice. The real fix: use `IServiceProvider.ResolveRootPage<TViewModel>()` - already used for a classic `NavigationPage`'s root page - for Shell's root content too, building the `ShellContent` in `AppShell`'s constructor and assigning the already-resolved-and-wired page directly to `ShellContent.Content`, which needs no event to catch at all. `AmbientNavigationObserver` is now a best-effort fallback (also listening for `Shell.PropertyChanged` on `CurrentPage`, not just `Navigated`) for anything else reached the ambient way - extra Tabs/FlyoutItems, or a page pushed directly via `Navigation.PushAsync` - but is no longer relied on for the root page.
- The sample app's `.csproj` declared `Sdk="Microsoft.NET.Sdk.Maui"`, which isn't a real SDK - .NET MAUI apps use the plain `Microsoft.NET.Sdk` with `<UseMaui>true</UseMaui>` (exactly like this repo's own library project already does). Fixed to `Sdk="Microsoft.NET.Sdk"`.
- The sample's model type was named `Contact`, which collides with `Microsoft.Maui.ApplicationModel.Communication.Contact` (the device-contacts picker API) - one of the namespaces the MAUI SDK adds as an implicit global `using`. Any unqualified reference to `Contact` was therefore ambiguous. Renamed to `ContactEntry`.
- **`ViewModelTypeResolver.InferViewModelNamespace` computed the wrong expected namespace for the exact convention this package recommends** (`XProject.Views` → `XProject.ViewModels`), found by actually running the test suite for the first time in an environment with a working .NET SDK. It chained four `string.Replace` calls - `.Replace(".Views", ".ViewModels")` then `.Replace(".View", ".ViewModels")`, etc. - but `.ViewModels` itself contains `.View` as a substring, so the later replacement matched inside the earlier replacement's own output, turning `MyApp.Views` into `MyApp.ViewModelsModels` instead of `MyApp.ViewModels`. In practice this meant the resolver's same-namespace check almost never matched, silently falling back to a same-name-anywhere match - harmless when there's only one `XViewModel` in the scanned assemblies, but capable of picking the *wrong* one whenever two candidates shared a name in different namespaces. Rewrote to match whole dot-separated namespace segments instead of chaining substring replacements, which can't cascade this way. Covered by `ViewModelTypeResolverTests` and `AssemblyPageScannerTests`, both of which were failing before this fix.
- `src/Maui.AutoNav/Maui.AutoNav.csproj` was missing an explicit `PackageReference` to `Microsoft.Maui.Controls` (build warning MA002) - starting with .NET 8, `UseMaui=true` no longer adds it implicitly.
- `dotnet test`/`dotnet restore` on the test project failed outright without the MAUI workload installed - contradicting the "fast, no workload needed" claim this README and CI's `test` job both made. Cause: MSBuild's workload resolution evaluates *every* `TargetFramework` a project declares before building any single one of them, so `src/Maui.AutoNav`'s `net10.0` slice sitting alongside its `net10.0-ios`/`net10.0-maccatalyst` TargetFrameworks meant even referencing just the `net10.0` slice required the iOS/macCatalyst workloads too - which aren't even installable on Linux, making the "test" CI job (on `ubuntu-latest`, no workload) broken in practice, not just unverified. Fixed by moving the MAUI-free naming-convention engine into its own project, `src/Maui.AutoNav.Core` (plain `net10.0`, no other TargetFrameworks at all), which the test project now references instead. Verified for real this time: `dotnet test` passes with only the .NET SDK installed, no workload.

## [0.1.0] - 2026-07-27

Initial reflection-based MVP (PRD milestones M1-M3).

### Added
- Convention-based Page → ViewModel binding (`XPage` → `XViewModel`), with `[ViewModel(typeof(...))]` for explicit override.
- `services.AddAutoNavigation(assemblies...)` - one call to register every discovered page/view model pair and their Shell routes.
- Async lifecycle interfaces: `IInitializeAsync` / `IInitializeAsync<T>`, `IAppearingAware`, `IDisappearingAware`, `IConfirmNavigationAsync`, `IDestructible`.
- `INavigationService` with strongly-typed parameters (`NavigateToAsync<TViewModel, TParam>`) and back-navigation-with-result (`NavigateForResultAsync` / `GoBackAsync<TResult>`).
- Shell and classic `NavigationPage` parity through a single navigation service implementation.
- `Window.UseAutoNavigation(services)` and `IServiceProvider.ResolveRootPage<TViewModel>()` startup helpers.

### Known limitations
- Convention/DI scanning is reflection-based (see [README - Roadmap](README.md#roadmap) for the planned source-generator swap, M4 in the PRD).
- `IDestructible.Destroy()` and `IConfirmNavigationAsync` are only guaranteed when navigating back through `INavigationService.GoBackAsync()` - not yet for platform back-gesture/hardware-button pops (tracked as backlog, matching the PRD's V2 scope).
