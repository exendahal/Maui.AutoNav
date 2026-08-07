namespace Maui.AutoNav.Tests.Fixtures.Views;

public class LoginPage;

public class SettingsPage;

public class OrphanPage;

[ViewModel(typeof(Maui.AutoNav.Tests.Fixtures.ViewModels.CustomViewModel))]
public class ProfilePage;

// No DashboardViewModel candidate exists - only the PageModel-suffixed one, exercising the
// fallback naming convention.
public class DashboardPage;

// A popup-shaped view, standing in for CommunityToolkit.Maui's Popup or MAUI's own
// ShowPopupAsync - never reaches the screen through Shell/NavigationPage, so it's resolved
// via ResolveViewModelFor rather than the page scan. Exercises "Popup" suffix stripping.
public class FilterPopup;
