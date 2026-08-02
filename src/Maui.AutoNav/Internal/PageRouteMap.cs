namespace Maui.AutoNav.Internal;

/// <summary>Read side of the page/view-model/route table built by <c>AddAutoNavigation</c>.</summary>
internal interface IPageRouteMap
{
    string GetRoute(Type viewModelType);

    Type GetPageType(Type viewModelType);

    bool TryGetPageType(Type viewModelType, out Type pageType);

    /// <summary>
    /// Reverse lookup used to wire pages that reach the screen without going through
    /// <see cref="INavigationService"/> or the Shell route factory - e.g. a ShellContent
    /// declared with a plain <c>{DataTemplate ...}</c> in AppShell.xaml, which constructs
    /// the page with its parameterless constructor and never assigns a view model.
    /// </summary>
    bool TryGetViewModelType(Type pageType, out Type viewModelType);
}

internal sealed class PageRouteMap : IPageRouteMap
{
    private readonly Dictionary<Type, (Type PageType, string Route)> _ByViewModel = new();
    private readonly Dictionary<Type, Type> _ViewModelByPage = new();

    public void Register(Type pageType, Type viewModelType, string route)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        ArgumentNullException.ThrowIfNull(viewModelType);
        ArgumentNullException.ThrowIfNull(route);

        _ByViewModel[viewModelType] = (pageType, route);
        _ViewModelByPage[pageType] = viewModelType;
    }

    public string GetRoute(Type viewModelType) =>
        _ByViewModel.TryGetValue(viewModelType, out var entry)
            ? entry.Route
            : throw NotRegistered(viewModelType);

    public Type GetPageType(Type viewModelType) =>
        _ByViewModel.TryGetValue(viewModelType, out var entry)
            ? entry.PageType
            : throw NotRegistered(viewModelType);

    public bool TryGetPageType(Type viewModelType, out Type pageType)
    {
        if (_ByViewModel.TryGetValue(viewModelType, out var entry))
        {
            pageType = entry.PageType;
            return true;
        }

        pageType = null!;
        return false;
    }

    public bool TryGetViewModelType(Type pageType, out Type viewModelType) =>
        _ViewModelByPage.TryGetValue(pageType, out viewModelType!);

    private static InvalidOperationException NotRegistered(Type viewModelType) =>
        new($"No page is registered for view model '{viewModelType.FullName}'. " +
            "Did you forget to call services.AddAutoNavigation(...), or is the page missing " +
            "its naming-convention match / [ViewModel] override?");
}
