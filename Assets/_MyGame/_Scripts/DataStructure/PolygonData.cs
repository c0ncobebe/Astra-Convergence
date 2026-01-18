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
        centerPosition = Vector2.zero; // Sẽ được set từ ngoài hoặc tính sau
        
        // Tự động tạo edges từ pointIds với HashSet để tránh duplicate
        edges = new List<Edge>(sideCount);
        var edgeSet = new HashSet<Edge>(); // Để detect và prevent duplicate
        
        for (int i = 0; i < points.Count; i++)
        {
            int nextIndex = (i + 1) % points.Count;
            var edge = new Edge(points[i], points[nextIndex]);
            
            // CRITICAL: Chỉ thêm edge nếu chưa tồn tại
            if (!edgeSet.Contains(edge))
            {
                edgeSet.Add(edge);
                edges.Add(edge);
            }
            else
            {
                Debug.LogError($"[PolygonData] PREVENTED DUPLICATE EDGE in polygon {id}: ({edge.point1}, {edge.point2})");
            }
        }
        
        Debug.Log($"[PolygonData] Created polygon {id}, points={points.Count}, unique edges={edges.Count}");
    }
    
    /// <summary>
    /// Tính centerPosition từ vị trí các điểm (gọi sau khi có point positions)
    /// </summary>
    public void CalculateCenter(List<PointData> allPoints)
    {
        if (pointIds == null || pointIds.Count == 0)
        {
            centerPosition = Vector2.zero;
            return;
        }
        
        Vector2 sum = Vector2.zero;
        int validPoints = 0;
        
        foreach (var pid in pointIds)
        {
            var point = allPoints.Find(p => p.pointId == pid);
            if (point != null)
            {
                sum += point.position;
                validPoints++;
            }
        }
        
        if (validPoints > 0)
        {
            centerPosition = sum / validPoints;
        }
        else
        {
            centerPosition = Vector2.zero;
        }
    }
}