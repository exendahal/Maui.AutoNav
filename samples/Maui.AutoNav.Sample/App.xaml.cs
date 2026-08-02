namespace Maui.AutoNav.Sample;

public partial class App : Application
{
    private readonly IServiceProvider _Services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _Services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new AppShell()).UseAutoNavigation(_Services);
}
