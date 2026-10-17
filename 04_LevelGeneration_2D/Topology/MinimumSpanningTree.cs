using System.Collections.Generic;

public static class MinimumSpanningTree
{
    // Prim 알고리즘 기반 MST. 룸 규모(8~18개)에서는 단순 구현으로 충분
    public static List<RoomGraphEdge> Build(List<RoomGraphEdge> edges, int nodeCount, int startNode = 0)
    {
        var mst = new List<RoomGraphEdge>();
        var visited = new HashSet<int> { startNode };

        var adjacency = BuildAdjacency(edges, nodeCount);

        while (visited.Count < nodeCount)
        {
            RoomGraphEdge? best = null;

            foreach (var visitedNode in visited)
            {
                foreach (var edge in adjacency[visitedNode])
                {
                    int other = edge.A == visitedNode ? edge.B : edge.A;
                    if (visited.Contains(other)) continue;

                    if (best == null || edge.Distance < best.Value.Distance)
                        best = edge;
                }
            }

            if (best == null)
            {
                // 그래프가 끊겨 있어 도달 불가능한 노드가 남음 (8단계 검증에서 실패로 잡혀야 함)
                break;
            }

            mst.Add(best.Value);
            visited.Add(best.Value.A);
            visited.Add(best.Value.B);
        }

        return mst;
    }

    private static List<RoomGraphEdge>[] BuildAdjacency(List<RoomGraphEdge> edges, int nodeCount)
    {
        var adjacency = new List<RoomGraphEdge>[nodeCount];
        for (int i = 0; i < nodeCount; i++)
            adjacency[i] = new List<RoomGraphEdge>();

        foreach (var edge in edges)
        {
            adjacency[edge.A].Add(edge);
            adjacency[edge.B].Add(edge);
        }

        return adjacency;
    }
}