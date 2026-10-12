using System;
using Avalonia;
using Avalonia.ReactiveUI;
using AES.Core;
using AES.Modules.ApkLab;
using AES.Modules.FirmwareLab;
using AES.Modules.DeviceBridge;
using AES.Modules.Analyzer;
using AES.Modules.Workspace;

namespace AES.UI;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var dataRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AES");

        var engine = new CoreEngine(dataRoot);

        engine.RegisterModule(new WorkspaceModule());
        engine.RegisterModule(new ApkLabModule());
        engine.RegisterModule(new FirmwareLabModule());
        engine.RegisterModule(new DeviceBridgeModule());
        engine.RegisterModule(new AnalyzerModule());

        engine.InitializeAsync().GetAwaiter().GetResult();

        BuildAvaloniaApp(engine).StartWithClassicDesktopLifetime(args);

        engine.ShutdownAsync().GetAwaiter().GetResult();
    }

    public static AppBuilder BuildAvaloniaApp(CoreEngine? engine = null) =>
        AppBuilder.Configure(() => new App(engine))
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();
}