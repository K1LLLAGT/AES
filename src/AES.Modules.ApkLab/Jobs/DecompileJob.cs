using AES.Contracts;
using AES.Contracts.Models;
using AES.Core;
using CliWrap;
using CliWrap.Buffered;

namespace AES.Modules.ApkLab.Jobs;

public sealed class DecompileJobHandler : IJobHandler
{
    public string JobType => "apklab.decompile";

    private readonly ICoreEngine _engine;

    private static string ApktoolJar =>
        Path.Combine(AppContext.BaseDirectory, "tools", "apktool", "apktool.jar");

    private static string JavaExe =>
        Path.Combine(AppContext.BaseDirectory, "tools", "jdk17", "bin", "java.exe");

    public DecompileJobHandler(ICoreEngine engine) => _engine = engine;

    public async Task<JobResult> ExecuteAsync(JobRequest request,
                                               Action<float> reportProgress,
                                               CancellationToken ct)
    {
        var apkPath   = (string)request.Parameters["apkPath"];
        var projectId = (Guid)request.Parameters["projectId"];
        var incRes    = (bool)request.Parameters["includeResources"];
        var incJava   = (bool)request.Parameters["includeJavaSource"];

        var outputDir = GetOutputDir(projectId);
        _engine.Log.Info("APKLab", $"Decompiling: {Path.GetFileName(apkPath)}", request.Parameters.ContainsKey("jobId") ? (Guid?)request.Parameters["jobId"] : null);
        reportProgress(0.1f);

        // Verify tools exist before running
        if (!File.Exists(JavaExe))
            throw new FileNotFoundException($"JDK not found at: {JavaExe}. Drop JDK 17 into tools/jdk17/");
        if (!File.Exists(ApktoolJar))
            throw new FileNotFoundException($"apktool.jar not found at: {ApktoolJar}. Drop apktool.jar into tools/apktool/");

        var args = new List<string> { "-jar", ApktoolJar, "d", apkPath, "-o", outputDir, "--force" };
        if (!incRes) args.Add("--no-res");

        reportProgress(0.2f);

        var result = await Cli.Wrap(JavaExe)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"apktool exited {result.ExitCode}:\n{result.StandardError}");

        reportProgress(0.7f);
        _engine.Log.Info("APKLab", $"apktool complete. Output: {outputDir}");

        if (incJava)
        {
            reportProgress(0.75f);
            await RunJadxAsync(apkPath, outputDir, ct);
            reportProgress(0.9f);
        }

        // Write manifest and store as artifact
        var manifestPath = Path.Combine(outputDir, ".aes-manifest.json");
        await File.WriteAllTextAsync(manifestPath,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                apkPath, outputDir,
                projectId = projectId.ToString(),
                decompiled = DateTimeOffset.UtcNow
            }), ct);

        var artifact = await _engine.Artifacts.StoreFileAsync(
            manifestPath, Path.GetFileName(apkPath), "apk-decompile", projectId);

        reportProgress(1.0f);
        return new JobResult(artifact);
    }

    private async Task RunJadxAsync(string apkPath, string outputDir, CancellationToken ct)
    {
        var jadxBat = Path.Combine(AppContext.BaseDirectory, "tools", "jadx", "bin", "jadx.bat");
        if (!File.Exists(jadxBat))
        {
            _engine.Log.Warn("APKLab", "JADX not found, skipping Java source decompile.");
            return;
        }

        var jadxOut = Path.Combine(outputDir, "jadx-src");
        await Cli.Wrap(jadxBat)
            .WithArguments(new[] { "-d", jadxOut, apkPath })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        _engine.Log.Info("APKLab", $"JADX output: {jadxOut}");
    }

    private static string GetOutputDir(Guid projectId)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AES", "Projects", projectId.ToString(), "apktool-output");
        Directory.CreateDirectory(root);
        return root;
    }
}