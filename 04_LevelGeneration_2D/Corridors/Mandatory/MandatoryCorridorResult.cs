using System.Collections.Generic;

public sealed class MandatoryCorridorResult
{
    public List<CorridorData> Corridors = new List<CorridorData>();
    public bool Success;
    public List<string> FailureReasons = new List<string>();
}