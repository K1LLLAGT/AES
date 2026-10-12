namespace AES.Contracts.Models;

public record JobRequest(
    string Type,
    string ModuleId,
    IReadOnlyDictionary<string, object> Parameters,
    JobPriority Priority = JobPriority.Normal
);

public record JobUpdate(
    Guid JobId,
    JobStatus Status,
    float Progress,
    string? Message,
    Artifact? ProducedArtifact
);

public enum JobStatus   { Queued, Running, Completed, Failed, Cancelled }
public enum JobPriority { Low = 0, Normal = 1, High = 2 }