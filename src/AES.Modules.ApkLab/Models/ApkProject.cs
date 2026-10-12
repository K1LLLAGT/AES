namespace AES.Modules.ApkLab.Models;

public record ApkProject(
    Guid Id,
    string Name,
    string SourceApkPath,
    string DecompileOutputDir,
    string? JavaSourceDir,
    Guid ProjectId,
    DateTime DecompiledAt
);

public record ApkBuildArtifact(
    Guid Id,
    Guid ApkProjectId,
    string BuiltApkPath,
    string? SignedApkPath,
    bool IsSigned,
    DateTime BuiltAt
);