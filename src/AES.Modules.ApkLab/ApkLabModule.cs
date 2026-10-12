using AES.Contracts;
using AES.Core;
using AES.Modules.ApkLab.Jobs;

namespace AES.Modules.ApkLab;

public sealed class ApkLabModule : IModule
{
    public string  ModuleId      => "APKLab";
    public string  DisplayName   => "AES APK Lab";
    public Version ModuleVersion => new(1, 0, 0);

    private ICoreEngine _engine = null!;
    public  ApkLabApi   Api     { get; private set; } = null!;

    public Task InitializeAsync(ICoreEngine engine, CancellationToken ct = default)
    {
        _engine = engine;
        Api     = new ApkLabApi(engine);

        // Register job handlers
        var scheduler = (JobScheduler)engine.Jobs;
        scheduler.RegisterHandler(new DecompileJobHandler(engine));

        engine.Log.Info(ModuleId, "APK Lab initialized.");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _engine.Log.Info(ModuleId, "APK Lab shutting down.");
        return Task.CompletedTask;
    }
}