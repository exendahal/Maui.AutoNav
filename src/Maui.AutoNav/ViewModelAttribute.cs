namespace Maui.AutoNav;

/// <summary>
/// Applied to a page to override the default naming-convention lookup
/// (e.g. <c>LoginPage</c> → <c>LoginViewModel</c>) with an explicit view model type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ViewModelAttribute : Attribute
{
    public ViewModelAttribute(Type viewModelType)
    {
        ViewModelType = viewModelType ?? throw new ArgumentNullException(nameof(viewModelType));
    }

    public Type ViewModelType { get; }
}
