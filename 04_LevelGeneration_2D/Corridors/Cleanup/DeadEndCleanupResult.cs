using System.Collections.Generic;

public sealed class DeadEndCleanupResult
{
    public List<OptionalCorridorData> KeptDeadEnds = new List<OptionalCorridorData>();
    public List<OptionalCorridorData> RemovedDeadEnds = new List<OptionalCorridorData>();
    public List<string> RemovalReasons = new List<string>();
    public float FinalDeadEndAreaRatio;
}