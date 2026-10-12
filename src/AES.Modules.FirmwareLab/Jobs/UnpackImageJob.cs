using AES.Contracts;
using AES.Contracts.Models;
using AES.Core;
using CliWrap;
using CliWrap.Buffered;

namespace AES.Modules.FirmwareLab.Jobs;

public enum ImageType { SystemImg, VendorImg, BootImg, Apex, Generic }

public sealed class UnpackImageJobHandler : IJobHandler
{
    public string JobType => "firmwarelab.unpack";

    private readonly ICoreEngine _engine;

    public UnpackImageJobHandler(ICoreEngine engine) => _engine = engine;

    public async Task<JobResult> ExecuteAsync(JobRequest request,
                                               Action<float> reportProgress,
                                               CancellationToken ct)
    {
        var imagePath = (string)request.Parameters["imagePath"];
        var projectId = (Guid)request.Parameters["projectId"];
        var typeStr   = (string)request.Parameters["imageType"];
        var imageType = Enum.Parse<ImageType>(typeStr, ignoreCase: true);

        var outputDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AES", "Projects", projectId.ToString(),
            "firmware", Path.GetFileNameWithoutExtension(imagePath));
        Directory.CreateDirectory(outputDir);

        _engine.Log.Info("FirmwareLab",
            $"Unpacking [{imageType}]: {Path.GetFileName(imagePath)}");
        reportProgress(0.1f);

        switch (imageType)
        {
            case ImageType.BootImg:
                await UnpackBootAsync(imagePath, outputDir, ct);
                break;
            case ImageType.Apex:
                await UnpackZipBasedAsync(imagePath, outputDir, ct);
                break;
            default:
                await UnpackZipBasedAsync(imagePath, outputDir, ct);
                break;
        }

        reportProgress(0.85f);

        var manifestPath = Path.Combine(outputDir, ".aes-fw-manifest.json");
        await File.WriteAllTextAsync(manifestPath,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                imagePath, outputDir, imageType = typeStr,
                unpacked = DateTimeOffset.UtcNow
            }), ct);

        var artifact = await _engine.Artifacts.StoreFileAsync(
            manifestPath, Path.GetFileName(imagePath), "firmware-unpack", projectId);

        reportProgress(1.0f);
        _engine.Log.Info("FirmwareLab", $"Unpack complete → {outputDir}");
        return new JobResult(artifact);
    }

    private async Task UnpackBootAsync(string imagePath, string outputDir, CancellationToken ct)
    {
        var magiskboot = Path.Combine(AppContext.BaseDirectory,
            "tools", "magiskboot", "magiskboot.exe");

        if (File.Exists(magiskboot))
        {
            _engine.Log.Info("FirmwareLab", "Using magiskboot to unpack boot.img");
            await Cli.Wrap(magiskboot)
                .WithArguments(new[] { "unpack", imagePath })
                .WithWorkingDirectory(outputDir)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(ct);
            return;
        }

        _engine.Log.Warn("FirmwareLab",
            "magiskboot.exe not found in tools/magiskboot/. Falling back to raw extraction.");
        await UnpackZipBasedAsync(imagePath, outputDir, ct);
    }

    private async Task UnpackZipBasedAsync(string imagePath, string outputDir,
                                            CancellationToken ct)
    {
        // Try bundled 7z, then system 7z, then .NET ZipFile fallback
        var sevenZipPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "tools", "7zip", "7z.exe"),
            @"C:\Program Files\7-Zip\7z.exe",
            @"C:\Program Files (x86)\7-Zip\7z.exe",
            "7z.exe" // PATH
        };

        foreach (var z in sevenZipPaths)
        {
            if (!File.Exists(z) && z != "7z.exe") continue;

            try
            {
                var result = await Cli.Wrap(z)
                    .WithArguments(new[] { "x", imagePath, $"-o{outputDir}", "-y" })
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(ct);

                if (result.ExitCode == 0)
                {
                    _engine.Log.Info("FirmwareLab", $"7z extraction complete using: {z}");
                    return;
                }
            }
            catch { /* try next */ }
        }

        // .NET zip fallback for APEX/zip-based images
        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(imagePath, outputDir, true);
            _engine.Log.Info("FirmwareLab", "Extracted using .NET ZipFile (zip-based image).");
        }
        catch (Exception ex)
        {
            _engine.Log.Warn("FirmwareLab",
                $"All extraction methods failed: {ex.Message}. " +
                "Install 7-Zip or drop 7z.exe into tools/7zip/");
        }
    }
}