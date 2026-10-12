using AES.Contracts;
using AES.Core;
using AES.Modules.FirmwareLab.Jobs;

namespace AES.Modules.FirmwareLab;

public sealed class FirmwareLabModule : IModule
{
    public string  ModuleId      => "FirmwareLab";
    public string  DisplayName   => "AES Firmware Lab";
    public Version ModuleVersion => new(1, 0, 0);

    public Task InitializeAsync(ICoreEngine engine, CancellationToken ct = default)
    {
        var scheduler = (JobScheduler)engine.Jobs;
        scheduler.RegisterHandler(new UnpackImageJobHandler(engine));
        engine.Log.Info(ModuleId, "Firmware Lab initialized.");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync() => Task.CompletedTask;
}