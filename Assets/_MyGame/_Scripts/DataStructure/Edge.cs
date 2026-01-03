using System;

/// <summary>
/// Struct đại diện cho một cạnh
/// </summary>
[Serializable]
public struct Edge : IEquatable<Edge>
{
    public int point1;
    public int point2;
    
    public Edge(int p1, int p2)
    {
        // Luôn lưu điểm nhỏ hơn trước để dễ so sánh
        if (p1 < p2)
        {
            point1 = p1;
            point2 = p2;
        }
        else
        {
            point1 = p2;
            point2 = p1;
        }
    }
    
    public bool Equals(Edge other)
    {
        return point1 == other.point1 && point2 == other.point2;
    }

    public override bool Equals(object obj)
    {
        if (obj is Edge other)
            return Equals(other);
        return false;
    }

    public static bool operator ==(Edge a, Edge b) => a.Equals(b);
    public static bool operator !=(Edge a, Edge b) => !a.Equals(b);
    
    public override int GetHashCode()
    {
        // Better hash distribution to avoid collisions when point ids can be large.
        // Use unchecked arithmetic and a prime multiplier (common pattern).
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + point1;
            hash = hash * 31 + point2;
            return hash;
        }
    }
    
    public bool Contains(int pointId)
    {
        return point1 == pointId || point2 == pointId;
    }
    
    public int GetOtherPoint(int pointId)
    {
        if (point1 == pointId) return point2;
        if (point2 == pointId) return point1;
        return -1;
    }
}