using System;

public sealed class GenerationDiagnostics
{
    public int BaseSeed { get; private set; }
    public string GeneratorVersion { get; private set; }
    public string ConfigVersion { get; private set; }
    public DateTime StartTimeUtc { get; private set; }

    private readonly System.Collections.Generic.List<string> _failureReasons
        = new System.Collections.Generic.List<string>();

    private readonly System.Collections.Generic.Dictionary<string, object> _stats
        = new System.Collections.Generic.Dictionary<string, object>();

    public void Begin(int baseSeed, string generatorVersion, string configVersion)
    {
        BaseSeed = baseSeed;
        GeneratorVersion = generatorVersion;
        ConfigVersion = configVersion;
        StartTimeUtc = DateTime.UtcNow;
        _failureReasons.Clear();
        _stats.Clear();
    }

    public void RecordStat(string key, object value)
    {
        _stats[key] = value;
    }

    public void RecordFailure(string reason)
    {
        _failureReasons.Add(reason);
    }

    public bool HasFailures => _failureReasons.Count > 0;

    public System.Collections.Generic.IReadOnlyList<string> FailureReasons => _failureReasons;

    public System.Collections.Generic.IReadOnlyDictionary<string, object> Stats => _stats;
}