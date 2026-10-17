using System.Collections.Generic;

public static class RoomTopologyBuilder
{
    public static RoomTopologyResult Build(List<RoomData> rooms, RoomTopologyConfig config, IRandomSource random)
    {
        var result = new RoomTopologyResult();

        if (rooms.Count < 2)
        {
            result.Success = rooms.Count == 1; // 룸이 1개면 연결할 게 없으므로 그대로 성공 처리
            result.Nodes = BuildNodes(rooms);
            result.SelectedEdges = new HashSet<RoomGraphEdge>();
            result.MstEdges = new List<RoomGraphEdge>();
            if (rooms.Count == 0)
            {
                result.Success = false;
                result.FailureReason = "No rooms to build topology from.";
            }
            return result;
        }

        var nodes = BuildNodes(rooms);

        List<RoomGraphEdge> allEdges = config.UseKNearest
            ? RoomDistanceGraphBuilder.BuildKNearestGraph(nodes, config.KNearestK)
            : RoomDistanceGraphBuilder.BuildAllPairsGraph(nodes);

        var mst = MinimumSpanningTree.Build(allEdges, nodes.Count);

        var selectedEdges = new HashSet<RoomGraphEdge>(mst);

        if (selectedEdges.Count < nodes.Count - 1)
        {
            result.Nodes = nodes;
            result.MstEdges = mst;
            result.SelectedEdges = selectedEdges;
            result.Success = false;
            result.FailureReason =
                $"MST only connected {selectedEdges.Count + 1} of {nodes.Count} rooms. Graph may be disconnected.";
            return result;
        }

        var remainingEdges = new HashSet<RoomGraphEdge>(allEdges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges)
        {
            if (random.Chance(config.ExtraEdgeChance))
            {
                selectedEdges.Add(edge);
            }
        }

        // 확정 조건: 최종 그래프에는 최소 1개 이상의 순환 경로가 있어야 한다 (기획서 7장)
        bool hasCycle = selectedEdges.Count > mst.Count;
        if (!hasCycle && nodes.Count > 2)
        {
            // Extra Edge가 하나도 안 뽑혔으면 가장 짧은 후보를 강제로 1개 추가
            if (TryGetShortest(remainingEdges, out var shortest))
            {
                selectedEdges.Add(shortest);
            }
        }

        result.Nodes = nodes;
        result.MstEdges = mst;
        result.SelectedEdges = selectedEdges;
        result.Success = true;
        return result;
    }

    private static List<RoomGraphNode> BuildNodes(List<RoomData> rooms)
    {
        var nodes = new List<RoomGraphNode>();
        for (int i = 0; i < rooms.Count; i++)
            nodes.Add(new RoomGraphNode(i, rooms[i]));
        return nodes;
    }

    private static bool TryGetShortest(HashSet<RoomGraphEdge> edges, out RoomGraphEdge shortest)
    {
        shortest = default;
        bool found = false;
        float best = float.MaxValue;

        foreach (var edge in edges)
        {
            if (edge.Distance < best)
            {
                best = edge.Distance;
                shortest = edge;
                found = true;
            }
        }

        return found;
    }
}