using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dữ liệu định nghĩa một đa giác trong level
/// </summary>
[Serializable]
public class PolygonData
{
    public int polygonId;
    public List<int> pointIds; // Danh sách ID các điểm theo thứ tự nối
    public List<Edge> edges; // Các cạnh của đa giác
    public int sideCount;
    public Vector2 centerPosition;
    
    public PolygonData(int id, List<int> points)
    {
        polygonId = id;
        pointIds = new List<int>(points);
        sideCount = points.Count;
        
        // Tự động tạo edges từ pointIds
        edges = new List<Edge>(sideCount);
        var edgeSet = new HashSet<Edge>(); // Để detect duplicate
        
        for (int i = 0; i < points.Count; i++)
        {
            int nextIndex = (i + 1) % points.Count;
            var edge = new Edge(points[i], points[nextIndex]);
            
            if (edgeSet.Contains(edge))
            {
                Debug.LogWarning($"[PolygonData] DUPLICATE EDGE detected in polygon {id}: ({edge.point1}, {edge.point2})");
            }
            else
            {
                edgeSet.Add(edge);
                edges.Add(edge);
            }
        }
        
        Debug.Log($"[PolygonData] Created polygon {id}, points={points.Count}, unique edges={edges.Count}");
    }
}