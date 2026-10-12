using AES.Contracts.Models;
using AES.Modules.DeviceBridge;
using Xunit;

namespace AES.Tests;

public class DeviceBridgeTests
{
    // AdbController resolves adb.exe via AppContext.BaseDirectory —
    // in tests it won't exist, so IsAvailable must be false.
    [Fact]
    public void AdbController_IsAvailable_ReturnsFalse_WhenExeMissing()
    {
        var adb = new AdbController();
        Assert.False(adb.IsAvailable);
    }

    // DeviceMonitor wraps an AdbController — starting and stopping
    // should not throw even when adb.exe is absent.
    [Fact]
    public void DeviceMonitor_StartStop_DoesNotThrow()
    {
        var adb     = new AdbController();
        var monitor = new DeviceMonitor(adb);

        var ex = Record.Exception(() =>
        {
            monitor.Start();
            monitor.Stop();
            monitor.Dispose();
        });

        Assert.Null(ex);
    }

    // DeviceMonitor exposes an observable — confirm it is non-null.
    [Fact]
    public void DeviceMonitor_DeviceUpdates_IsNotNull()
    {
        var monitor = new DeviceMonitor(new AdbController());
        Assert.NotNull(monitor.DeviceUpdates);
    }

    // Device model must round-trip its serial correctly.
    [Fact]
    public void Device_Serial_RoundTrips()
    {
        var device = new Device("emulator-5554", "Pixel_7", null, null,
                                DeviceState.Online, false, false);
        Assert.Equal("emulator-5554", device.Serial);
        Assert.Equal(DeviceState.Online, device.State);
    }
}
