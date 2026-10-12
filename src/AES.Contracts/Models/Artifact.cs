namespace AES.Contracts.Models;

public record Artifact(
    Guid Id,
    string ContentHash,
    string Name,
    string Type,
    long SizeBytes,
    Guid ProjectId,
    Guid? ProducingJobId,
    DateTime CreatedAt
);