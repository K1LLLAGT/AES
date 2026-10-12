using System.Xml.Linq;

namespace AES.Modules.Analyzer;

public sealed class DexAnalyzer
{
    private static readonly string[] InterestingPatterns =
    {
        "http://", "https://", "jdbc:", "sqlite",
        "password", "passwd", "secret", "api_key", "apikey",
        "bearer ", "token", "firebase", "amazonaws",
        "/data/data", "/system/", "\"su\"", "root",
        "frida", "xposed", "magisk"
    };

    public async Task<AnalysisResult> AnalyzeAsync(string decompiledDir,
                                                    CancellationToken ct = default)
    {
        var result = new AnalysisResult { DecompiledDir = decompiledDir };

        var manifestPath = Path.Combine(decompiledDir, "AndroidManifest.xml");
        if (File.Exists(manifestPath))
            await ParseManifestAsync(manifestPath, result, ct);

        var smaliDir = Path.Combine(decompiledDir, "smali");
        if (Directory.Exists(smaliDir))
        {
            EnumerateClasses(smaliDir, result);
            await ScanStringsAsync(smaliDir, result, ct);
        }

        // Also check smali_classes2, smali_classes3 (multidex)
        foreach (var extraSmali in Directory.EnumerateDirectories(
            decompiledDir, "smali_classes*"))
        {
            EnumerateClasses(extraSmali, result);
            await ScanStringsAsync(extraSmali, result, ct);
        }

        return result;
    }

    private static async Task ParseManifestAsync(string path, AnalysisResult r,
                                                  CancellationToken ct)
    {
        try
        {
            var content = await File.ReadAllTextAsync(path, ct);
            var doc     = XDocument.Parse(content);
            var ns      = "http://schemas.android.com/apk/res/android";
            var root    = doc.Root;

            r.PackageName = root?.Attribute("package")?.Value ?? "";
            r.VersionName = root?.Attribute(XName.Get("versionName", ns))?.Value ?? "";
            r.VersionCode = root?.Attribute(XName.Get("versionCode", ns))?.Value ?? "";

            var sdk = root?.Element("uses-sdk");
            r.MinSdk    = sdk?.Attribute(XName.Get("minSdkVersion",    ns))?.Value ?? "";
            r.TargetSdk = sdk?.Attribute(XName.Get("targetSdkVersion", ns))?.Value ?? "";

            r.Permissions = doc.Descendants("uses-permission")
                .Select(e => e.Attribute(XName.Get("name", ns))?.Value ?? "")
                .Where(p => !string.IsNullOrEmpty(p))
                .OrderBy(p => p)
                .ToList();

            r.Activities = doc.Descendants("activity")
                .Select(e => e.Attribute(XName.Get("name", ns))?.Value ?? "")
                .Where(a => !string.IsNullOrEmpty(a)).ToList();

            r.Services = doc.Descendants("service")
                .Select(e => e.Attribute(XName.Get("name", ns))?.Value ?? "")
                .Where(s => !string.IsNullOrEmpty(s)).ToList();

            r.Receivers = doc.Descendants("receiver")
                .Select(e => e.Attribute(XName.Get("name", ns))?.Value ?? "")
                .Where(rv => !string.IsNullOrEmpty(rv)).ToList();
        }
        catch { /* malformed or binary manifest — skip */ }
    }

    private static void EnumerateClasses(string smaliDir, AnalysisResult r)
    {
        foreach (var file in Directory.EnumerateFiles(
            smaliDir, "*.smali", SearchOption.AllDirectories))
        {
            var className = file
                .Replace(smaliDir, "")
                .TrimStart(Path.DirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(".smali", "");
            r.Classes.Add(className);
        }
    }

    private static async Task ScanStringsAsync(string smaliDir, AnalysisResult r,
                                                CancellationToken ct)
    {
        var files = Directory.EnumerateFiles(
            smaliDir, "*.smali", SearchOption.AllDirectories).Take(1000);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            var lines = await File.ReadAllLinesAsync(file, ct);
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (!trimmed.StartsWith("const-string")) continue;

                foreach (var pattern in InterestingPatterns)
                {
                    if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase)
                        && !r.InterestingStrings.Contains(trimmed))
                    {
                        r.InterestingStrings.Add(trimmed);
                        break;
                    }
                }
            }
        }
    }
}