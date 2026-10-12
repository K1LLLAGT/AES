using System.Text.Json;
using System.Text.Json.Serialization;
using AES.Contracts.Models;

namespace AES.Modules.Workspace;

public sealed class ProjectManager
{
    private readonly string _projectsRoot;
    private readonly string _indexPath;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };
    private List<Project> _projects = new();

    public IReadOnlyList<Project> Projects => _projects;

    public ProjectManager(string dataRoot)
    {
        _projectsRoot = Path.Combine(dataRoot, "Projects");
        _indexPath    = Path.Combine(dataRoot, "projects.json");
        Directory.CreateDirectory(_projectsRoot);
        LoadIndex();
    }

    public async Task<Project> CreateProjectAsync(string name)
    {
        var id      = Guid.NewGuid();
        var root    = Path.Combine(_projectsRoot, id.ToString());
        Directory.CreateDirectory(root);

        var project = new Project(id, name, "", root, DateTime.UtcNow, DateTime.UtcNow);
        _projects.Add(project);

        var projectFile = Path.Combine(root, ".aes-project.json");
        await File.WriteAllTextAsync(projectFile,
            JsonSerializer.Serialize(project, _json));

        await SaveIndexAsync();
        return project;
    }

    public async Task<Project?> OpenProjectAsync(string projectFilePath)
    {
        if (!File.Exists(projectFilePath)) return null;

        try
        {
            var json    = await File.ReadAllTextAsync(projectFilePath);
            var project = JsonSerializer.Deserialize<Project>(json, _json);
            if (project is null) return null;

            if (!_projects.Any(p => p.Id == project.Id))
            {
                _projects.Add(project);
                await SaveIndexAsync();
            }
            return project;
        }
        catch { return null; }
    }

    public async Task DeleteProjectAsync(Guid projectId)
    {
        _projects.RemoveAll(p => p.Id == projectId);
        await SaveIndexAsync();
    }

    private void LoadIndex()
    {
        if (!File.Exists(_indexPath)) return;
        try
        {
            var json = File.ReadAllText(_indexPath);
            _projects = JsonSerializer.Deserialize<List<Project>>(json, _json) ?? new();
            // Remove entries whose root directories no longer exist
            _projects = _projects.Where(p => Directory.Exists(p.RootPath)).ToList();
        }
        catch { _projects = new(); }
    }

    private async Task SaveIndexAsync() =>
        await File.WriteAllTextAsync(_indexPath,
            JsonSerializer.Serialize(_projects, _json));
}