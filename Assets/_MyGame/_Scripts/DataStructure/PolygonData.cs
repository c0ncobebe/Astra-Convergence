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
    public Color color = Color.white;
    
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
    /// Sử dụng polygon centroid để hỗ trợ đa giác lõm
    /// </summary>
    public void CalculateCenter(List<PointData> allPoints)
    {
        if (pointIds == null || pointIds.Count < 3)
        {
            centerPosition = Vector2.zero;
            return;
        }
        
        // Lấy vị trí các điểm
        List<Vector2> positions = new List<Vector2>();
        foreach (var pid in pointIds)
        {
            var point = allPoints.Find(p => p.pointId == pid);
            if (point != null)
            {
                positions.Add(point.position);
            }
        }
        
        if (positions.Count < 3)
        {
            centerPosition = Vector2.zero;
            return;
        }
        
        // Tính polygon centroid (trọng tâm theo diện tích)
        float area = 0f;
        Vector2 centroid = Vector2.zero;
        
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2 p1 = positions[i];
            Vector2 p2 = positions[(i + 1) % positions.Count];
            
            float cross = p1.x * p2.y - p2.x * p1.y;
            area += cross;
            centroid.x += (p1.x + p2.x) * cross;
            centroid.y += (p1.y + p2.y) * cross;
        }
        
        area *= 0.5f;
        
        if (Mathf.Abs(area) > 0.0001f)
        {
            centroid /= (6f * area);
            centerPosition = centroid;
        }
        else
        {
            // Fallback về trung bình nếu area = 0 (degerate polygon)
            Vector2 sum = Vector2.zero;
            foreach (var pos in positions)
            {
                sum += pos;
            }
            centerPosition = sum / positions.Count;
        }
    }
}