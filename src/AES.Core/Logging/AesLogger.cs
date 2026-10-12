using System.Reactive.Subjects;
using AES.Contracts;
using AES.Contracts.Models;
using Serilog;
using Serilog.Events;

namespace AES.Core.Logging;

public sealed class AesLogger : IAesLogger
{
    private readonly ILogger _serilog;
    private readonly Subject<LogEntry> _stream = new();

    public IObservable<LogEntry> LogStream => _stream;

    public AesLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, "aes-.log");

        _serilog = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public void Log(AES.Contracts.Models.LogLevel level, string moduleId, string message,
                    Guid? jobId = null, Exception? ex = null)
    {
        var entry = new LogEntry(DateTimeOffset.Now, level, moduleId, message, jobId, ex?.ToString());
        _stream.OnNext(entry);

        var serilogLevel = level switch
        {
            AES.Contracts.Models.LogLevel.Verbose => LogEventLevel.Verbose,
            AES.Contracts.Models.LogLevel.Debug   => LogEventLevel.Debug,
            AES.Contracts.Models.LogLevel.Info    => LogEventLevel.Information,
            AES.Contracts.Models.LogLevel.Warning => LogEventLevel.Warning,
            AES.Contracts.Models.LogLevel.Error   => LogEventLevel.Error,
            AES.Contracts.Models.LogLevel.Fatal   => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };

        _serilog.Write(serilogLevel, ex,
            "[{ModuleId}] {Message} (Job={JobId})", moduleId, message, jobId);
    }

    public void Info(string moduleId, string message, Guid? jobId = null) =>
        Log(AES.Contracts.Models.LogLevel.Info, moduleId, message, jobId);

    public void Warn(string moduleId, string message, Guid? jobId = null) =>
        Log(AES.Contracts.Models.LogLevel.Warning, moduleId, message, jobId);

    public void Error(string moduleId, string message, Exception? ex = null, Guid? jobId = null) =>
        Log(AES.Contracts.Models.LogLevel.Error, moduleId, message, jobId, ex);
}