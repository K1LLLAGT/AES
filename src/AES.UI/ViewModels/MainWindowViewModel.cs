using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using AES.Contracts;
using AES.Contracts.Models;
using ReactiveUI;

namespace AES.UI.ViewModels;

public sealed class MainWindowViewModel : ReactiveObject
{
    private int _activeTabIndex;

    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set => this.RaiseAndSetIfChanged(ref _activeTabIndex, value);
    }

    public WorkspaceViewModel    Workspace    { get; }
    public ApkLabViewModel       ApkLab       { get; }
    public FirmwareLabViewModel  FirmwareLab  { get; }
    public AnalyzerViewModel     Analyzer     { get; }
    public DeviceBridgeViewModel DeviceBridge { get; }

    public ObservableCollection<LogLineViewModel> LogEntries { get; } = new();

    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public MainWindowViewModel(ICoreEngine? engine = null)
    {
        Workspace    = new WorkspaceViewModel(engine);
        ApkLab       = new ApkLabViewModel(engine);
        FirmwareLab  = new FirmwareLabViewModel(engine);
        Analyzer     = new AnalyzerViewModel(engine);
        DeviceBridge = new DeviceBridgeViewModel(engine);

        ExitCommand = ReactiveCommand.Create(() =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lt)
                lt.Shutdown();
        });

        engine?.Log.LogStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(entry =>
            {
                LogEntries.Add(new LogLineViewModel(
                    $"{entry.Timestamp:HH:mm:ss} [{entry.Level,-7}] {entry.ModuleId}: {entry.Message}"));
                if (LogEntries.Count > 500)
                    LogEntries.RemoveAt(0);
            });
    }
}