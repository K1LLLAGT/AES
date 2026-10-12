using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AES.Contracts;
using AES.Contracts.Models;
using ReactiveUI;

namespace AES.UI.ViewModels;

public sealed class FirmwareLabViewModel : ReactiveObject
{
    private readonly ICoreEngine? _engine;

    private string? _imagePath;
    private string  _selectedType  = "SystemImg";
    private bool    _isUnpacking;
    private double  _progress;
    private string  _statusMessage = "Select a firmware image file to unpack.";
    private string? _outputDir;

    public string? ImagePath
    {
        get => _imagePath;
        set => this.RaiseAndSetIfChanged(ref _imagePath, value);
    }

    public string SelectedType
    {
        get => _selectedType;
        set => this.RaiseAndSetIfChanged(ref _selectedType, value);
    }

    public bool IsUnpacking
    {
        get => _isUnpacking;
        set => this.RaiseAndSetIfChanged(ref _isUnpacking, value);
    }

    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string? OutputDir
    {
        get => _outputDir;
        set => this.RaiseAndSetIfChanged(ref _outputDir, value);
    }

    public ObservableCollection<string> ImageTypes { get; } = new()
    {
        "SystemImg", "VendorImg", "BootImg", "Apex", "Generic"
    };

    public ReactiveCommand<Unit, Unit> UnpackCommand { get; }

    public FirmwareLabViewModel(ICoreEngine? engine = null)
    {
        _engine = engine;

        var canUnpack = this.WhenAnyValue(
            x => x.ImagePath, x => x.IsUnpacking,
            (path, busy) => !string.IsNullOrEmpty(path) && !busy);

        UnpackCommand = ReactiveCommand.Create(SubmitUnpack, canUnpack);
    }

    private void SubmitUnpack()
    {
        if (_engine is null || string.IsNullOrEmpty(ImagePath)) return;

        var projectId = Guid.NewGuid();

        var jobId = _engine.Jobs.SubmitJob(new JobRequest(
            Type:       "firmwarelab.unpack",
            ModuleId:   "FirmwareLab",
            Parameters: new Dictionary<string, object>
            {
                ["imagePath"]  = ImagePath,
                ["projectId"]  = projectId,
                ["imageType"]  = SelectedType
            }
        ));

        IsUnpacking   = true;
        StatusMessage = $"Unpacking {System.IO.Path.GetFileName(ImagePath)}...";

        _engine.Jobs.JobUpdates
            .Where(u => u.JobId == jobId)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(u =>
            {
                Progress = u.Progress;

                if (u.Status == JobStatus.Completed)
                {
                    IsUnpacking   = false;
                    StatusMessage = "Unpack complete.";
                    OutputDir     = u.ProducedArtifact?.Name;
                }
                else if (u.Status == JobStatus.Failed)
                {
                    IsUnpacking   = false;
                    StatusMessage = $"Failed: {u.Message}";
                }
                else if (u.Message is not null)
                {
                    StatusMessage = u.Message;
                }
            });
    }
}