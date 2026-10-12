using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AES.Contracts;
using AES.Modules.Analyzer;
using ReactiveUI;

namespace AES.UI.ViewModels;

public sealed class AnalyzerViewModel : ReactiveObject
{
    private readonly DexAnalyzer _analyzer;
    private CancellationTokenSource? _cts;

    private string? _decompiledDir;
    private bool    _isAnalyzing;
    private string  _statusMessage = "Paste the path to a decompiled APK directory and click Analyze.";
    private string  _packageInfo   = "";
    private int     _activeSubTab;

    public string? DecompiledDir
    {
        get => _decompiledDir;
        set => this.RaiseAndSetIfChanged(ref _decompiledDir, value);
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set => this.RaiseAndSetIfChanged(ref _isAnalyzing, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string PackageInfo
    {
        get => _packageInfo;
        set => this.RaiseAndSetIfChanged(ref _packageInfo, value);
    }

    public int ActiveSubTab
    {
        get => _activeSubTab;
        set => this.RaiseAndSetIfChanged(ref _activeSubTab, value);
    }

    public ObservableCollection<string> Permissions        { get; } = new();
    public ObservableCollection<string> Classes            { get; } = new();
    public ObservableCollection<string> InterestingStrings { get; } = new();
    public ObservableCollection<string> Components         { get; } = new();

    public ReactiveCommand<Unit, Unit> AnalyzeCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand  { get; }

    public AnalyzerViewModel(ICoreEngine? engine = null)
    {
        _analyzer = engine?.GetModule<AnalyzerModule>()?.Analyzer ?? new DexAnalyzer();

        var canAnalyze = this.WhenAnyValue(
                x => x.DecompiledDir,
                x => x.IsAnalyzing,
                (dir, busy) => !string.IsNullOrWhiteSpace(dir) && !busy);

        var canCancel = this.WhenAnyValue(x => x.IsAnalyzing)
                            .Select(v => v);

        AnalyzeCommand = ReactiveCommand.CreateFromTask(AnalyzeAsync, canAnalyze);
        CancelCommand  = ReactiveCommand.Create(() => _cts?.Cancel(), canCancel);
    }

    private async Task AnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(DecompiledDir) || !Directory.Exists(DecompiledDir))
        {
            StatusMessage = "Directory not found. Check the path and try again.";
            return;
        }

        IsAnalyzing = true;
        StatusMessage = "Analyzing...";
        Permissions.Clear();
        Classes.Clear();
        InterestingStrings.Clear();
        Components.Clear();

        _cts = new CancellationTokenSource();

        try
        {
            var result = await _analyzer.AnalyzeAsync(DecompiledDir!, _cts.Token);

            PackageInfo =
                $"Package:     {result.PackageName}\n" +
                $"Version:     {result.VersionName} (code {result.VersionCode})\n" +
                $"SDK:         min={result.MinSdk}  target={result.TargetSdk}\n" +
                $"Classes:     {result.Classes.Count}\n" +
                $"Permissions: {result.Permissions.Count}\n" +
                $"Components:  {result.Activities.Count} activities, " +
                $"{result.Services.Count} services, {result.Receivers.Count} receivers";

            foreach (var p  in result.Permissions)        Permissions.Add(p);
            foreach (var c  in result.Classes)            Classes.Add(c);
            foreach (var s  in result.InterestingStrings) InterestingStrings.Add(s);
            foreach (var a  in result.Activities)         Components.Add($"[Activity]  {a}");
            foreach (var s  in result.Services)           Components.Add($"[Service]   {s}");
            foreach (var rv in result.Receivers)          Components.Add($"[Receiver]  {rv}");

            StatusMessage =
                $"Done. {result.Classes.Count} classes · " +
                $"{result.Permissions.Count} permissions · " +
                $"{result.InterestingStrings.Count} flagged strings.";
        }
        catch (OperationCanceledException) { StatusMessage = "Analysis cancelled."; }
        catch (Exception ex)              { StatusMessage = $"Error: {ex.Message}"; }
        finally                           { IsAnalyzing = false; }
    }
}