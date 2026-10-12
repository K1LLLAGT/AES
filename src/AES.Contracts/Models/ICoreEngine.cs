using AES.Contracts.Models;

namespace AES.Contracts;

public interface ICoreEngine
{
    IJobScheduler Jobs     { get; }
    IEventBus     Events   { get; }
    IAesLogger    Log      { get; }
    IArtifactStore Artifacts { get; }

    void RegisterModule(IModule module);
    T?   GetModule<T>() where T : class, IModule;

    Task InitializeAsync(CancellationToken ct = default);
    Task ShutdownAsync();
}

public interface IModule
{
    string  ModuleId      { get; }
    string  DisplayName   { get; }
    Version ModuleVersion { get; }

    Task InitializeAsync(ICoreEngine engine, CancellationToken ct = default);
    Task ShutdownAsync();
}

public interface IJobScheduler
{
    Guid       SubmitJob(JobRequest request);
    bool       CancelJob(Guid jobId);
    JobStatus  GetStatus(Guid jobId);
    IObservable<JobUpdate> JobUpdates { get; }
}

public interface IEventBus
{
    void        Publish<TEvent>(TEvent evt)               where TEvent : class;
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}

public interface IAesLogger
{
    void Info (string moduleId, string message, Guid? jobId = null);
    void Warn (string moduleId, string message, Guid? jobId = null);
    void Error(string moduleId, string message, Exception? ex = null, Guid? jobId = null);
    void Log  (LogLevel level,  string moduleId, string message,
               Guid? jobId = null, Exception? ex = null);
    IObservable<LogEntry> LogStream { get; }
}

public interface IArtifactStore
{
    Task<Artifact>      StoreFileAsync(string sourcePath, string name, string type,
                                       Guid projectId, Guid? jobId = null);
    string              GetArtifactPath(Artifact artifact);
    IEnumerable<Artifact> GetProjectArtifacts(Guid projectId);
}