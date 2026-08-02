using System.Reflection;
using Maui.AutoNav;
using Maui.AutoNav.Sample.Services;
using Microsoft.Extensions.Logging;

namespace Maui.AutoNav.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddSingleton<ContactRepository>();

        // The one call that discovers every Page/ViewModel pair in this assembly (by naming
        // convention or [ViewModel] override), registers them for DI, and registers a Shell
        // route for each one. See MauiProgram.cs / App.xaml.cs in the main package's README
        // for the full two-file integration story this sample follows.
        builder.Services.AddAutoNavigation(Assembly.GetExecutingAssembly());

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
