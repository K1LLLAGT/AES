using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AES.Core;
using AES.UI.ViewModels;

namespace AES.UI;

public partial class App : Application
{
    private readonly CoreEngine? _engine;

    // Parameterless constructor required by Avalonia XAML loader
    public App() { }

    public App(CoreEngine? engine)
    {
        _engine = engine;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(_engine)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}