using AES.Contracts;
using AES.Contracts.Models;

namespace AES.Modules.ApkLab;

public sealed class ApkLabApi
{
    private readonly ICoreEngine _engine;

    public ApkLabApi(ICoreEngine engine) => _engine = engine;

    public Guid DecompileApk(string apkPath, Guid projectId,
                              bool includeResources = true,
                              bool includeJavaSource = true)
    {
        if (!File.Exists(apkPath))
            throw new FileNotFoundException("APK not found", apkPath);

        return _engine.Jobs.SubmitJob(new JobRequest(
            Type:       "apklab.decompile",
            ModuleId:   "APKLab",
            Parameters: new Dictionary<string, object>
            {
                ["apkPath"]           = apkPath,
                ["projectId"]         = projectId,
                ["includeResources"]  = includeResources,
                ["includeJavaSource"] = includeJavaSource
            }
        ));
    }

    public Guid RebuildApk(Guid projectId, string outputPath) =>
        _engine.Jobs.SubmitJob(new JobRequest(
            Type:       "apklab.rebuild",
            ModuleId:   "APKLab",
            Parameters: new Dictionary<string, object>
            {
                ["projectId"]  = projectId,
                ["outputPath"] = outputPath
            }
        ));

    public Guid SignApk(string apkPath, string keystorePath,
                        string keystorePassword, string keyAlias,
                        string outputPath) =>
        _engine.Jobs.SubmitJob(new JobRequest(
            Type:       "apklab.sign",
            ModuleId:   "APKLab",
            Parameters: new Dictionary<string, object>
            {
                ["apkPath"]          = apkPath,
                ["keystorePath"]     = keystorePath,
                ["keystorePassword"] = keystorePassword,
                ["keyAlias"]         = keyAlias,
                ["outputPath"]       = outputPath
            }
        ));
}