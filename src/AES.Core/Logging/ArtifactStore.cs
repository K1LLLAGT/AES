using AES.Contracts;
using AES.Contracts.Models;

namespace AES.Core;

public sealed class ArtifactStore : IArtifactStore
{
    private readonly string _rootPath;
    private readonly List<Artifact> _artifacts = new();

    public ArtifactStore(string rootPath, string dbPath)
    {
        _rootPath = rootPath;
        Directory.CreateDirectory(rootPath);
    }

    public async Task<Artifact> StoreFileAsync(string sourcePath, string name,
                                                string type, Guid projectId,
                                                Guid? jobId = null)
    {
        var hash    = await ComputeHashAsync(sourcePath);
        var destDir = Path.Combine(_rootPath, hash[..2]);
        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, hash);
        if (!File.Exists(destPath))
            File.Copy(sourcePath, destPath);

        var artifact = new Artifact(
            Guid.NewGuid(), hash, name, type,
            new FileInfo(sourcePath).Length,
            projectId, jobId, DateTime.UtcNow);

        lock (_artifacts) _artifacts.Add(artifact);
        return artifact;
    }

    public string GetArtifactPath(Artifact artifact) =>
        Path.Combine(_rootPath, artifact.ContentHash[..2], artifact.ContentHash);

    public IEnumerable<Artifact> GetProjectArtifacts(Guid projectId)
    {
        lock (_artifacts) return _artifacts.Where(a => a.ProjectId == projectId).ToList();
    }

    private static async Task<string> ComputeHashAsync(string path)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        await using var stream = File.OpenRead(path);
        var bytes = await sha.ComputeHashAsync(stream);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}