namespace Maui.AutoNav.Tests.Fixtures.Views;

public class LoginPage;

public class SettingsPage;

public class OrphanPage;

[ViewModel(typeof(Maui.AutoNav.Tests.Fixtures.ViewModels.CustomViewModel))]
public class ProfilePage;

// No DashboardViewModel candidate exists - only the PageModel-suffixed one, exercising the
// fallback naming convention.
public class DashboardPage;
