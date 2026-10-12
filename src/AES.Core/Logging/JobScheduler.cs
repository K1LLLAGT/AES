using System.Collections.Concurrent;
using System.Reactive.Subjects;
using AES.Contracts;
using AES.Contracts.Models;

namespace AES.Core;

public sealed class JobScheduler : IJobScheduler, IDisposable
{
    private readonly IAesLogger _log;
    private readonly IEventBus _events;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<Guid, JobEntry> _jobs = new();
    private readonly ConcurrentDictionary<string, IJobHandler> _handlers = new();
    private readonly Subject<JobUpdate> _updates = new();
    private readonly CancellationTokenSource _engineCts = new();

    public IObservable<JobUpdate> JobUpdates => _updates;

    public JobScheduler(IAesLogger log, IEventBus events, int maxConcurrency = 4)
    {
        _log    = log;
        _events = events;
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public void RegisterHandler(IJobHandler handler) =>
        _handlers[handler.JobType] = handler;

    public Guid SubmitJob(JobRequest request)
    {
        var id  = Guid.NewGuid();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_engineCts.Token);
        var entry = new JobEntry(id, request, cts);
        _jobs[id] = entry;

        Publish(id, JobStatus.Queued, 0f, "Queued");
        _ = RunJobAsync(entry);
        return id;
    }

    public bool CancelJob(Guid jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var entry)) return false;
        entry.Cts.Cancel();
        return true;
    }

    public JobStatus GetStatus(Guid jobId) =>
        _jobs.TryGetValue(jobId, out var e) ? e.Status : JobStatus.Cancelled;

    private async Task RunJobAsync(JobEntry entry)
    {
        var ct = entry.Cts.Token;
        await _semaphore.WaitAsync(ct);
        try
        {
            entry.Status = JobStatus.Running;
            Publish(entry.Id, JobStatus.Running, 0f, "Running");
            _log.Info(entry.Request.ModuleId,
                      $"Job {entry.Id} started: {entry.Request.Type}", entry.Id);

            if (!_handlers.TryGetValue(entry.Request.Type, out var handler))
                throw new InvalidOperationException(
                    $"No handler registered for job type '{entry.Request.Type}'");

            var result = await handler.ExecuteAsync(
                entry.Request,
                progress => Publish(entry.Id, JobStatus.Running, progress, null),
                ct);

            entry.Status = JobStatus.Completed;
            Publish(entry.Id, JobStatus.Completed, 1f, "Completed", result.ProducedArtifact);
            _log.Info(entry.Request.ModuleId, $"Job {entry.Id} completed.", entry.Id);
        }
        catch (OperationCanceledException)
        {
            entry.Status = JobStatus.Cancelled;
            Publish(entry.Id, JobStatus.Cancelled, 0f, "Cancelled");
            _log.Warn(entry.Request.ModuleId, $"Job {entry.Id} cancelled.", entry.Id);
        }
        catch (Exception ex)
        {
            entry.Status = JobStatus.Failed;
            Publish(entry.Id, JobStatus.Failed, 0f, $"Error: {ex.Message}");
            _log.Error(entry.Request.ModuleId, $"Job {entry.Id} failed.", ex, entry.Id);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void Publish(Guid jobId, JobStatus status, float progress,
                         string? msg, Artifact? artifact = null)
    {
        var update = new JobUpdate(jobId, status, progress, msg, artifact);
        _updates.OnNext(update);
        _events.Publish(update);
    }

    public void Dispose()
    {
        _engineCts.Cancel();
        _semaphore.Dispose();
        _updates.OnCompleted();
        _updates.Dispose();
        _engineCts.Dispose();
    }

    private record JobEntry(Guid Id, JobRequest Request, CancellationTokenSource Cts)
    {
        public JobStatus Status { get; set; } = JobStatus.Queued;
    }
}

public interface IJobHandler
{
    string JobType { get; }
    Task<JobResult> ExecuteAsync(JobRequest request,
                                  Action<float> reportProgress,
                                  CancellationToken ct);
}

public record JobResult(Artifact? ProducedArtifact, string? ErrorMessage = null);