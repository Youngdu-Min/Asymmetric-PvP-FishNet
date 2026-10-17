using UnityEngine;

public readonly struct RoomGraphEdge
{
    public readonly int A;
    public readonly int B;
    public readonly float Distance;

    public RoomGraphEdge(int a, int b, float distance)
    {
        // 무방향 그래프이므로 (a,b)와 (b,a)를 같은 간선으로 취급하기 위해 정렬
        A = Mathf.Min(a, b);
        B = Mathf.Max(a, b);
        Distance = distance;
    }

    public override bool Equals(object obj)
        => obj is RoomGraphEdge other && other.A == A && other.B == B;

    public override int GetHashCode()
        => A * 397 ^ B;
}