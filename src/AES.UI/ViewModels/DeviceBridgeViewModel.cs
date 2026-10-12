using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AES.Contracts;
using AES.Contracts.Models;
using AES.Modules.DeviceBridge;
using ReactiveUI;

namespace AES.UI.ViewModels;

public sealed class DeviceBridgeViewModel : ReactiveObject
{
    private readonly AdbController? _adb;
    private CancellationTokenSource? _logcatCts;

    private Device? _selectedDevice;
    private string  _commandInput  = string.Empty;
    private string  _commandOutput = string.Empty;
    private bool    _isLogcatRunning;
    private string  _statusMessage = "Waiting for devices...";

    public ObservableCollection<Device> Devices     { get; } = new();
    public ObservableCollection<string> LogcatLines { get; } = new();

    public Device? SelectedDevice
    {
        get => _selectedDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
    }

    public string CommandInput
    {
        get => _commandInput;
        set => this.RaiseAndSetIfChanged(ref _commandInput, value);
    }

    public string CommandOutput
    {
        get => _commandOutput;
        set => this.RaiseAndSetIfChanged(ref _commandOutput, value);
    }

    public bool IsLogcatRunning
    {
        get => _isLogcatRunning;
        set => this.RaiseAndSetIfChanged(ref _isLogcatRunning, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Unit, Unit> RefreshCommand     { get; }
    public ReactiveCommand<Unit, Unit> RunCommandCommand  { get; }
    public ReactiveCommand<Unit, Unit> StartLogcatCommand { get; }
    public ReactiveCommand<Unit, Unit> StopLogcatCommand  { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand       { get; }

    public DeviceBridgeViewModel(ICoreEngine? engine = null)
    {
        _adb = engine?.GetModule<DeviceBridgeModule>()?.Adb;

        engine?.GetModule<DeviceBridgeModule>()?.Monitor?.DeviceUpdates
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateDeviceList);

        var hasDevice    = this.WhenAnyValue(x => x.SelectedDevice)
                               .Select(d => d is not null);
        var logcatActive = this.WhenAnyValue(x => x.IsLogcatRunning)
                               .Select(v => v);

        RefreshCommand     = ReactiveCommand.CreateFromTask(RefreshAsync);
        RunCommandCommand  = ReactiveCommand.CreateFromTask(RunCommandAsync, hasDevice);
        StartLogcatCommand = ReactiveCommand.CreateFromTask(StartLogcatAsync, hasDevice);
        StopLogcatCommand  = ReactiveCommand.Create(StopLogcat, logcatActive);
        ClearCommand       = ReactiveCommand.Create(() =>
        {
            CommandOutput = string.Empty;
            LogcatLines.Clear();
        });
    }

    private void UpdateDeviceList(List<Device> devices)
    {
        Devices.Clear();
        foreach (var d in devices) Devices.Add(d);
        StatusMessage = devices.Count == 0
            ? "No devices connected. Connect a device and ensure ADB is authorized."
            : $"{devices.Count} device(s) connected.";
    }

    private async Task RefreshAsync()
    {
        if (_adb is null) { StatusMessage = "ADB not available."; return; }
        var devices = await _adb.GetDevicesAsync();
        UpdateDeviceList(devices);
    }

    private async Task RunCommandAsync()
    {
        if (_adb is null || SelectedDevice is null || string.IsNullOrWhiteSpace(CommandInput))
            return;

        var cmd    = CommandInput.Trim();
        var output = await _adb.ShellAsync(SelectedDevice.Serial, cmd);
        CommandOutput += $"\n$ {cmd}\n{output}";
        CommandInput   = string.Empty;
    }

    private async Task StartLogcatAsync()
    {
        if (_adb is null || SelectedDevice is null) return;

        StopLogcat();
        _logcatCts      = new CancellationTokenSource();
        IsLogcatRunning = true;
        LogcatLines.Clear();

        try
        {
            await foreach (var line in _adb.StreamLogcatAsync(
                SelectedDevice.Serial, _logcatCts.Token))
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LogcatLines.Add(line);
                    if (LogcatLines.Count > 2000)
                        LogcatLines.RemoveAt(0);
                });
            }
        }
        catch (OperationCanceledException) { }
        finally { IsLogcatRunning = false; }
    }

    private void StopLogcat()
    {
        _logcatCts?.Cancel();
        _logcatCts      = null;
        IsLogcatRunning = false;
    }
}