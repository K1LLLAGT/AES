using AES.Contracts;

namespace AES.Modules.DeviceBridge;

public sealed class DeviceBridgeModule : IModule
{
    public string  ModuleId      => "DeviceBridge";
    public string  DisplayName   => "AES Device Bridge";
    public Version ModuleVersion => new(1, 0, 0);

    public AdbController? Adb     { get; private set; }
    public DeviceMonitor? Monitor { get; private set; }

    public Task InitializeAsync(ICoreEngine engine, CancellationToken ct = default)
    {
        Adb     = new AdbController();
        Monitor = new DeviceMonitor(Adb);
        Monitor.Start();
        engine.Log.Info(ModuleId, "Device Bridge initialized. ADB polling started.");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        Monitor?.Stop();
        Monitor?.Dispose();
        return Task.CompletedTask;
    }
}