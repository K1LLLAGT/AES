using System;
using System.Reactive;
using System.Threading.Tasks;
using AES.Contracts;
using AES.Modules.ApkLab;
using ReactiveUI;

namespace AES.UI.ViewModels;

public sealed class ApkLabViewModel : ReactiveObject
{
    private readonly ICoreEngine? _engine;
    private readonly ApkLabApi?   _api;

    private bool    _isDecompiling;
    private double  _decompileProgress;
    private string? _statusMessage = "Open an APK to begin.";
    private string? _loadedApkPath;

    public bool IsDecompiling
    {
        get => _isDecompiling;
        set => this.RaiseAndSetIfChanged(ref _isDecompiling, value);
    }

    public double DecompileProgress
    {
        get => _decompileProgress;
        set => this.RaiseAndSetIfChanged(ref _decompileProgress, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string? LoadedApkPath
    {
        get => _loadedApkPath;
        set => this.RaiseAndSetIfChanged(ref _loadedApkPath, value);
    }

    public ReactiveCommand<Unit, Unit> OpenApkCommand { get; }

    public ApkLabViewModel(ICoreEngine? engine = null)
    {
        _engine = engine;
        if (engine is not null)
            _api = new ApkLabApi(engine);

        OpenApkCommand = ReactiveCommand.CreateFromTask(OpenApkAsync);
    }

    private Task OpenApkAsync()
    {
        // TODO: wire Avalonia StorageProvider file picker here
        StatusMessage = "File picker not yet wired. Drop an APK path into LoadedApkPath for now.";
        return Task.CompletedTask;
    }
}