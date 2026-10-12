using AES.Contracts;

namespace AES.Modules.Analyzer;

public sealed class AnalyzerModule : IModule
{
    public string  ModuleId      => "Analyzer";
    public string  DisplayName   => "AES Analyzer";
    public Version ModuleVersion => new(1, 0, 0);

    public DexAnalyzer Analyzer { get; } = new();

    public Task InitializeAsync(ICoreEngine engine, CancellationToken ct = default)
    {
        engine.Log.Info(ModuleId, "Analyzer initialized.");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync() => Task.CompletedTask;
}