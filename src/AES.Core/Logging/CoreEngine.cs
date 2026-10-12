using AES.Contracts;
using AES.Contracts.Models;
using AES.Core.Logging;

namespace AES.Core;

public sealed class CoreEngine : ICoreEngine
{
    private readonly Dictionary<string, IModule> _modules = new();

    public IJobScheduler  Jobs      { get; }
    public IEventBus      Events    { get; }
    public IAesLogger     Log       { get; }
    public IArtifactStore Artifacts { get; }

    public CoreEngine(string dataRoot)
    {
        Directory.CreateDirectory(dataRoot);

        var logPath      = Path.Combine(dataRoot, "Logs");
        var artifactRoot = Path.Combine(dataRoot, "Artifacts");
        var dbPath       = Path.Combine(dataRoot, "aes.db");

        Log       = new AesLogger(logPath);
        Events    = new EventBus();
        Jobs      = new JobScheduler(Log, Events, maxConcurrency: 4);
        Artifacts = new ArtifactStore(artifactRoot, dbPath);
    }

    public void RegisterModule(IModule module)
    {
        if (_modules.ContainsKey(module.ModuleId))
            throw new InvalidOperationException(
                $"Module '{module.ModuleId}' is already registered.");

        _modules[module.ModuleId] = module;
        Log.Info("CoreEngine", $"Registered module: {module.DisplayName} v{module.ModuleVersion}");
    }

    public T? GetModule<T>() where T : class, IModule =>
        _modules.Values.OfType<T>().FirstOrDefault();

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        Log.Info("CoreEngine", "Initializing AES Core Engine...");
        LoadPlugins();

        foreach (var module in _modules.Values)
        {
            Log.Info("CoreEngine", $"Initializing module: {module.ModuleId}");
            await module.InitializeAsync(this, ct);
        }

        Log.Info("CoreEngine", $"Engine ready. {_modules.Count} module(s) loaded.");
    }

    public async Task ShutdownAsync()
    {
        Log.Info("CoreEngine", "Shutting down...");
        foreach (var module in _modules.Values.Reverse())
            await module.ShutdownAsync();
    }

    private void LoadPlugins()
    {
        var pluginsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AES", "Plugins");

        Directory.CreateDirectory(pluginsDir);

        foreach (var dll in Directory.GetFiles(pluginsDir, "*.dll"))
        {
            try
            {
                var asm = System.Runtime.Loader.AssemblyLoadContext.Default
                    .LoadFromAssemblyPath(dll);

                foreach (var type in asm.GetExportedTypes())
                {
                    if (typeof(IModule).IsAssignableFrom(type)
                        && !type.IsAbstract
                        && !type.IsInterface)
                    {
                        var m = (IModule)Activator.CreateInstance(type)!;
                        RegisterModule(m);
                        Log.Info("CoreEngine", $"Plugin loaded: {m.DisplayName} from {Path.GetFileName(dll)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("CoreEngine", $"Failed to load plugin: {Path.GetFileName(dll)}", ex);
            }
        }
    }
}