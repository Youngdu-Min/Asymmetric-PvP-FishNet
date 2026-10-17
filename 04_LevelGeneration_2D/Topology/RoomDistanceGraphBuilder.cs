using System.Collections.Generic;
using UnityEngine;

public static class RoomDistanceGraphBuilder
{
    // 룸 수가 적당히 적을 때(8~18개 권장 범위) 전체 쌍 그래프가 Delaunay보다 단순하고 충분함
    public static List<RoomGraphEdge> BuildAllPairsGraph(List<RoomGraphNode> nodes)
    {
        var edges = new List<RoomGraphEdge>();

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                float dist = Vector2.Distance(nodes[i].Center, nodes[j].Center);
                edges.Add(new RoomGraphEdge(i, j, dist));
            }
        }

        return edges;
    }

    // 룸 수가 많아질 경우를 대비한 대안: K-Nearest Neighbor 그래프
    public static List<RoomGraphEdge> BuildKNearestGraph(List<RoomGraphNode> nodes, int k)
    {
        var edgeSet = new HashSet<RoomGraphEdge>();

        for (int i = 0; i < nodes.Count; i++)
        {
            var distances = new List<(int index, float dist)>();

            for (int j = 0; j < nodes.Count; j++)
            {
                if (i == j) continue;
                distances.Add((j, Vector2.Distance(nodes[i].Center, nodes[j].Center)));
            }

            distances.Sort((x, y) => x.dist.CompareTo(y.dist));

            int neighborCount = Mathf.Min(k, distances.Count);
            for (int n = 0; n < neighborCount; n++)
            {
                edgeSet.Add(new RoomGraphEdge(i, distances[n].index, distances[n].dist));
            }
        }

        return new List<RoomGraphEdge>(edgeSet);
    }
}