using System;
using System.Collections.Generic;
using UnityEngine;

// Vazgriz DungeonPathfinder2D 골격 기반, 비용 함수를 외부에서 주입받는 구조로 정리
public sealed class DungeonPathfinder2D
{
    public struct PathCost
    {
        public bool Traversable;
        public float Cost;
    }

    private class Node
    {
        public Vector2Int Position;
        public Node Previous;
        public float Cost;
        public bool Visited;
    }

    private static readonly Vector2Int[] Neighbors4 = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private readonly Vector2Int _size;
    private readonly Dictionary<Vector2Int, Node> _nodes = new Dictionary<Vector2Int, Node>();

    public DungeonPathfinder2D(Vector2Int size)
    {
        _size = size;
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, Func<Vector2Int, Vector2Int, PathCost> costFunction)
    {
        _nodes.Clear();

        var startNode = GetNode(start);
        startNode.Cost = 0;

        var open = new List<Node> { startNode };

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.Cost.CompareTo(b.Cost));
            var current = open[0];
            open.RemoveAt(0);

            if (current.Visited) continue;
            current.Visited = true;

            if (current.Position == end)
                return ReconstructPath(current);

            foreach (var dir in Neighbors4)
            {
                var nextPos = current.Position + dir;
                if (!InBounds(nextPos)) continue;

                var cost = costFunction(current.Position, nextPos);
                if (!cost.Traversable) continue;

                var nextNode = GetNode(nextPos);
                if (nextNode.Visited) continue;

                float newCost = current.Cost + cost.Cost;
                if (newCost < nextNode.Cost || nextNode.Previous == null)
                {
                    nextNode.Cost = newCost;
                    nextNode.Previous = current;
                    open.Add(nextNode);
                }
            }
        }

        return null; // 경로 없음
    }

    private Node GetNode(Vector2Int pos)
    {
        if (!_nodes.TryGetValue(pos, out var node))
        {
            node = new Node { Position = pos, Cost = float.MaxValue };
            _nodes[pos] = node;
        }
        return node;
    }

    private bool InBounds(Vector2Int pos)
        => pos.x >= 0 && pos.x < _size.x && pos.y >= 0 && pos.y < _size.y;

    private List<Vector2Int> ReconstructPath(Node end)
    {
        var path = new List<Vector2Int>();
        var current = end;
        while (current != null)
        {
            path.Add(current.Position);
            current = current.Previous;
        }
        path.Reverse();
        return path;
    }
}