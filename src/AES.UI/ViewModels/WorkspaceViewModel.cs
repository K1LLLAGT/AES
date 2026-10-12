using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AES.Contracts;
using AES.Contracts.Models;
using AES.Modules.Workspace;
using ReactiveUI;

namespace AES.UI.ViewModels;

public sealed class WorkspaceViewModel : ReactiveObject
{
    private readonly ICoreEngine?    _engine;
    private readonly ProjectManager? _pm;

    private Project? _selectedProject;
    private string   _newProjectName = "";
    private string   _sessionNotes   = "";
    private string   _statusMessage  = "Create or open a project to begin.";

    public ObservableCollection<Project>  Projects  { get; } = new();
    public ObservableCollection<Artifact> Artifacts { get; } = new();

    public Project? SelectedProject
    {
        get => _selectedProject;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedProject, value);
            LoadArtifacts();
        }
    }

    public string NewProjectName
    {
        get => _newProjectName;
        set => this.RaiseAndSetIfChanged(ref _newProjectName, value);
    }

    public string SessionNotes
    {
        get => _sessionNotes;
        set => this.RaiseAndSetIfChanged(ref _sessionNotes, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Unit, Unit> CreateProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand       { get; }
    public ReactiveCommand<Unit, Unit> DeleteProjectCommand { get; }

    public WorkspaceViewModel(ICoreEngine? engine = null)
    {
        _engine = engine;
        _pm     = engine?.GetModule<WorkspaceModule>()?.ProjectManager;

        LoadProjects();

        var canCreate = this.WhenAnyValue(x => x.NewProjectName)
                            .Select(n => !string.IsNullOrWhiteSpace(n));

        var hasProject = this.WhenAnyValue(x => x.SelectedProject)
                             .Select(p => p is not null);

        CreateProjectCommand = ReactiveCommand.CreateFromTask(CreateProjectAsync, canCreate);
        RefreshCommand       = ReactiveCommand.Create(LoadProjects);
        DeleteProjectCommand = ReactiveCommand.CreateFromTask(DeleteProjectAsync, hasProject);
    }

    private void LoadProjects()
    {
        Projects.Clear();
        if (_pm is null) return;
        foreach (var p in _pm.Projects) Projects.Add(p);
        StatusMessage = Projects.Count == 0
            ? "No projects yet. Create one above."
            : $"{Projects.Count} project(s). Select one to view artifacts.";
    }

    private async Task CreateProjectAsync()
    {
        if (_pm is null || string.IsNullOrWhiteSpace(NewProjectName)) return;
        var project = await _pm.CreateProjectAsync(NewProjectName.Trim());
        Projects.Add(project);
        SelectedProject = project;
        NewProjectName  = "";
        StatusMessage   = $"Project '{project.Name}' created at {project.RootPath}";
    }

    private async Task DeleteProjectAsync()
    {
        if (_pm is null || SelectedProject is null) return;
        await _pm.DeleteProjectAsync(SelectedProject.Id);
        Projects.Remove(SelectedProject);
        SelectedProject = null;
        Artifacts.Clear();
        StatusMessage = "Project removed.";
    }

    private void LoadArtifacts()
    {
        Artifacts.Clear();
        if (_selectedProject is null || _engine is null) return;
        foreach (var a in _engine.Artifacts.GetProjectArtifacts(_selectedProject.Id))
            Artifacts.Add(a);
    }
}