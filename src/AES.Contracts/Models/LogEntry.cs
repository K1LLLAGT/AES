namespace AES.Contracts.Models;

public record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string ModuleId,
    string Message,
    Guid? JobId,
    string? ExceptionText
);

public enum LogLevel { Verbose, Debug, Info, Warning, Error, Fatal }