using System.Collections.Generic;

public sealed class OptionalCorridorResult
{
    public List<OptionalCorridorData> Corridors = new List<OptionalCorridorData>();
    public Dictionary<CorridorTerminationType, int> TypeCounts = new Dictionary<CorridorTerminationType, int>();
    public int RejectedAttempts;
}