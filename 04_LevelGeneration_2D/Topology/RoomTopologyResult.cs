using System.Collections.Generic;

public sealed class RoomTopologyResult
{
    public List<RoomGraphNode> Nodes;
    public HashSet<RoomGraphEdge> SelectedEdges; // MST + 복원된 Extra Edge
    public List<RoomGraphEdge> MstEdges;
    public bool Success;
    public string FailureReason;
}