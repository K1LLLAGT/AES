namespace AES.Modules.Analyzer;

public sealed class AnalysisResult
{
    public string DecompiledDir     { get; set; } = "";
    public string PackageName       { get; set; } = "";
    public string VersionName       { get; set; } = "";
    public string VersionCode       { get; set; } = "";
    public string MinSdk            { get; set; } = "";
    public string TargetSdk         { get; set; } = "";

    public List<string> Permissions       { get; set; } = new();
    public List<string> Activities        { get; set; } = new();
    public List<string> Services          { get; set; } = new();
    public List<string> Receivers         { get; set; } = new();
    public List<string> Classes           { get; set; } = new();
    public List<string> InterestingStrings { get; set; } = new();
}