using System.Collections.Generic;

public static class RoomAdjacencyGraph
{
    // 3단계 토폴로지의 SelectedEdges(필수 연결 기준)로 룸-룸 인접 리스트 구성
    public static Dictionary<int, List<int>> Build(RoomTopologyResult topology, int roomCount)
    {
        var adjacency = new Dictionary<int, List<int>>();
        for (int i = 0; i < roomCount; i++)
            adjacency[i] = new List<int>();

        foreach (var edge in topology.SelectedEdges)
        {
            adjacency[edge.A].Add(edge.B);
            adjacency[edge.B].Add(edge.A);
        }

        return adjacency;
    }

    public static Dictionary<int, int> BfsDistances(Dictionary<int, List<int>> adjacency, int startRoom)
    {
        var distances = new Dictionary<int, int> { [startRoom] = 0 };
        var queue = new Queue<int>();
        queue.Enqueue(startRoom);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (var neighbor in adjacency[current])
            {
                if (!distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = distances[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distances;
    }
}