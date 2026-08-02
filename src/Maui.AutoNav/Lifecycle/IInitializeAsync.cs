namespace Maui.AutoNav;

/// <summary>
/// Implemented by a view model that needs a strongly-typed parameter the first time
/// its page is navigated to. Fired by <see cref="INavigationService"/> for parameterized
/// navigations, and skipped (in favor of <see cref="IInitializeAsync"/>) otherwise.
/// </summary>
/// <typeparam name="T">The parameter type passed via NavigateToAsync&lt;TViewModel, T&gt;.</typeparam>
public interface IInitializeAsync<in T>
{
    Task InitializeAsync(T parameter);
}

/// <summary>
/// Implemented by a view model that needs to run async setup the first time its page
/// is navigated to, without requiring a parameter.
/// </summary>
public interface IInitializeAsync
{
    Task InitializeAsync();
}
