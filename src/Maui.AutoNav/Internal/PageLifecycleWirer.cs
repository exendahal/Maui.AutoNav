#if MAUI_AUTONAV_PLATFORM
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace Maui.AutoNav.Internal;

/// <summary>
/// Sets a page's <see cref="BindableObject.BindingContext"/> exactly once and wires
/// <see cref="Page.Appearing"/> / <see cref="Page.Disappearing"/> through to
/// <see cref="IAppearingAware"/> / <see cref="IDisappearingAware"/>. This is the single
/// choke point every code path (Shell route factory, classic push, ambient safety net)
/// funnels through, so a page never gets wired twice.
/// </summary>
internal static class PageLifecycleWirer
{
    private static readonly ConditionalWeakTable<Page, object> _Wired = new();

    public static bool IsWired(Page page) => _Wired.TryGetValue(page, out _);

    public static void Wire(Page page, object viewModel)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(viewModel);

        if (_Wired.TryGetValue(page, out _))
        {
            return;
        }

        _Wired.Add(page, viewModel);

        if (!ReferenceEquals(page.BindingContext, viewModel))
        {
            page.BindingContext = viewModel;
        }

        page.Appearing += async (_, _) =>
        {
            if (viewModel is IAppearingAware appearingAware)
            {
                await appearingAware.OnAppearingAsync().ConfigureAwait(true);
            }
        };

        page.Disappearing += async (_, _) =>
        {
            if (viewModel is IDisappearingAware disappearingAware)
            {
                await disappearingAware.OnDisappearingAsync().ConfigureAwait(true);
            }
        };
    }

    public static Task InitializeAsync(object viewModel, object? parameter)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        if (parameter is not null)
        {
            var initializeAsyncMethod = FindTypedInitializeMethod(viewModel, parameter.GetType());
            if (initializeAsyncMethod is not null)
            {
                return (Task)initializeAsyncMethod.Invoke(viewModel, new[] { parameter })!;
            }
        }

        return viewModel is IInitializeAsync parameterless
            ? parameterless.InitializeAsync()
            : Task.CompletedTask;
    }

    private static System.Reflection.MethodInfo? FindTypedInitializeMethod(object viewModel, Type parameterType)
    {
        foreach (var candidateInterface in viewModel.GetType().GetInterfaces())
        {
            if (candidateInterface.IsGenericType &&
                candidateInterface.GetGenericTypeDefinition() == typeof(IInitializeAsync<>) &&
                candidateInterface.GenericTypeArguments[0].IsAssignableFrom(parameterType))
            {
                return candidateInterface.GetMethod(nameof(IInitializeAsync<object>.InitializeAsync));
            }
        }

        return null;
    }
}
#endif
