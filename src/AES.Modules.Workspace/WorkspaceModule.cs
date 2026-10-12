using AES.Contracts;

namespace AES.Modules.Workspace;

public sealed class WorkspaceModule : IModule
{
    public string  ModuleId      => "Workspace";
    public string  DisplayName   => "AES Workspace";
    public Version ModuleVersion => new(1, 0, 0);

    public ProjectManager? ProjectManager { get; private set; }

    public Task InitializeAsync(ICoreEngine engine, CancellationToken ct = default)
    {
        var dataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AES");
        ProjectManager = new ProjectManager(dataRoot);
        engine.Log.Info(ModuleId, $"Workspace initialized. {ProjectManager.Projects.Count} project(s) found.");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync() => Task.CompletedTask;
}