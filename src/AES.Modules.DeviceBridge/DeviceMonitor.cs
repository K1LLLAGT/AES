using System.Reactive.Subjects;
using AES.Contracts.Models;

namespace AES.Modules.DeviceBridge;

public sealed class DeviceMonitor : IDisposable
{
    private readonly AdbController _adb;
    private readonly Subject<List<Device>> _updates = new();
    private CancellationTokenSource? _cts;

    public IObservable<List<Device>> DeviceUpdates => _updates;
    public List<Device> CurrentDevices { get; private set; } = new();

    public DeviceMonitor(AdbController adb) => _adb = adb;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = PollAsync(_cts.Token);
    }

    public void Stop() => _cts?.Cancel();

    private async Task PollAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var devices = await _adb.GetDevicesAsync(ct);
                CurrentDevices = devices;
                _updates.OnNext(devices);
            }
            catch (OperationCanceledException) { break; }
            catch { /* swallow transient ADB errors */ }

            try { await Task.Delay(2000, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _updates.Dispose();
    }
}