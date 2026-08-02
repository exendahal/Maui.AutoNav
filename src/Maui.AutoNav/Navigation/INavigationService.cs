namespace Maui.AutoNav;

/// <summary>
/// A single navigation abstraction that behaves identically whether the app's root page
/// is a <c>Shell</c> or a classic <c>NavigationPage</c>. Resolve it from DI - do not
/// construct implementations directly.
/// </summary>
public interface INavigationService
{
    /// <summary>Navigates to the page registered for <typeparamref name="TViewModel"/>.</summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's push transition.</param>
    Task NavigateToAsync<TViewModel>(bool animate = true) where TViewModel : class;

    /// <summary>
    /// Navigates to the page registered for <typeparamref name="TViewModel"/>, delivering
    /// <paramref name="parameter"/> to the view model via <see cref="IInitializeAsync{T}"/>.
    /// </summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's push transition.</param>
    Task NavigateToAsync<TViewModel, TParam>(TParam parameter, bool animate = true) where TViewModel : class;

    /// <summary>
    /// Navigates to the page registered for <typeparamref name="TViewModel"/> and awaits a
    /// result produced by a later call to <see cref="GoBackAsync{TResult}(TResult, bool)"/>.
    /// </summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's push transition.</param>
    Task<TResult?> NavigateForResultAsync<TViewModel, TResult>(bool animate = true) where TViewModel : class;

    /// <summary>
    /// Combines <see cref="NavigateToAsync{TViewModel, TParam}(TParam, bool)"/> and
    /// <see cref="NavigateForResultAsync{TViewModel, TResult}(bool)"/>.
    /// </summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's push transition.</param>
    Task<TResult?> NavigateForResultAsync<TViewModel, TParam, TResult>(TParam parameter, bool animate = true) where TViewModel : class;

    /// <summary>Pops the current page, honoring <see cref="IConfirmNavigationAsync"/> and <see cref="IDestructible"/>.</summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's pop transition.</param>
    Task GoBackAsync(bool animate = true);

    /// <summary>Pops the current page, first completing any pending <see cref="NavigateForResultAsync{TViewModel, TResult}(bool)"/> caller with <paramref name="result"/>.</summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's pop transition.</param>
    Task GoBackAsync<TResult>(TResult result, bool animate = true);

    /// <summary>Presents the page registered for <typeparamref name="TViewModel"/> modally, on top of whatever is currently shown.</summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's modal-presentation transition.</param>
    Task NavigateToModalAsync<TViewModel>(bool animate = true) where TViewModel : class;

    /// <summary>
    /// Presents the page registered for <typeparamref name="TViewModel"/> modally, delivering
    /// <paramref name="parameter"/> to the view model via <see cref="IInitializeAsync{T}"/>.
    /// </summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's modal-presentation transition.</param>
    Task NavigateToModalAsync<TViewModel, TParam>(TParam parameter, bool animate = true) where TViewModel : class;

    /// <summary>
    /// Presents the page registered for <typeparamref name="TViewModel"/> modally and awaits a
    /// result produced by a later call to <see cref="GoBackModalAsync{TResult}(TResult, bool)"/>.
    /// </summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's modal-presentation transition.</param>
    Task<TResult?> NavigateForModalResultAsync<TViewModel, TResult>(bool animate = true) where TViewModel : class;

    /// <summary>
    /// Combines <see cref="NavigateToModalAsync{TViewModel, TParam}(TParam, bool)"/> and
    /// <see cref="NavigateForModalResultAsync{TViewModel, TResult}(bool)"/>.
    /// </summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's modal-presentation transition.</param>
    Task<TResult?> NavigateForModalResultAsync<TViewModel, TParam, TResult>(TParam parameter, bool animate = true) where TViewModel : class;

    /// <summary>Dismisses the topmost modal page, honoring <see cref="IConfirmNavigationAsync"/> and <see cref="IDestructible"/>.</summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's dismiss transition.</param>
    Task GoBackModalAsync(bool animate = true);

    /// <summary>Dismisses the topmost modal page, first completing any pending <see cref="NavigateForModalResultAsync{TViewModel, TResult}(bool)"/> caller with <paramref name="result"/>.</summary>
    /// <param name="animate">Pass <c>false</c> to skip the platform's dismiss transition.</param>
    Task GoBackModalAsync<TResult>(TResult result, bool animate = true);
}
