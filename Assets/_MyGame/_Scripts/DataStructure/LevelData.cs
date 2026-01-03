using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dữ liệu của một level
/// </summary>
[CreateAssetMenu(fileName = "Level", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int levelId;
    public List<PointData> points = new List<PointData>();
    public List<PolygonData> polygons = new List<PolygonData>();
    
    // Cache cho performance
    [NonSerialized] private HashSet<Edge> allEdgesCache;
    [NonSerialized] private Dictionary<Edge, List<int>> edgeToPolygonsCache;
    
    public void Initialize()
    {
        // Link points to polygons
        // Build a dictionary for fast pointId -> PointData lookup to avoid repeated List.Find calls.
        var pointLookup = new Dictionary<int, PointData>(points.Count);
        for (int p = 0; p < points.Count; p++)
        {
            pointLookup[points[p].pointId] = points[p];
        }

        for (int i = 0; i < polygons.Count; i++)
        {
            var polygon = polygons[i];
            for (int j = 0; j < polygon.pointIds.Count; j++)
            {
                var pointId = polygon.pointIds[j];
                if (pointLookup.TryGetValue(pointId, out var point))
                {
                    if (!point.belongToPolygons.Contains(polygon.polygonId))
                    {
                        point.belongToPolygons.Add(polygon.polygonId);
                    }
                }
            }
        }
        
        BuildEdgeCache();
    }
    
    private void BuildEdgeCache()
    {
        allEdgesCache = new HashSet<Edge>();
        edgeToPolygonsCache = new Dictionary<Edge, List<int>>();
        
        Debug.Log($"[BuildEdgeCache] Starting... {polygons.Count} polygons");
        
        for (int i = 0; i < polygons.Count; i++)
        {
            var polygon = polygons[i];
            Debug.Log($"[BuildEdgeCache] Polygon {polygon.polygonId}, edges={polygon.edges.Count}");
            
            for (int j = 0; j < polygon.edges.Count; j++)
            {
                var edge = polygon.edges[j];
                allEdgesCache.Add(edge);
                
                if (!edgeToPolygonsCache.ContainsKey(edge))
                {
                    edgeToPolygonsCache[edge] = new List<int>();
                    Debug.Log($"[BuildEdgeCache] NEW Edge ({edge.point1}, {edge.point2}), create new list");
                }
                
                edgeToPolygonsCache[edge].Add(polygon.polygonId);
                Debug.Log($"[BuildEdgeCache] Edge ({edge.point1}, {edge.point2}) -> Add polygon {polygon.polygonId}, total={edgeToPolygonsCache[edge].Count}");
            }
        }
        
        Debug.Log($"[BuildEdgeCache] Complete! Total edges: {edgeToPolygonsCache.Count}");
    }
    
    public bool IsValidEdge(int point1, int point2, out List<int> polygonIds)
    {
        var edge = new Edge(point1, point2);
        bool found = edgeToPolygonsCache.TryGetValue(edge, out polygonIds);
        
        if (found && polygonIds != null)
        {
            string polygonStr = "[";
            for (int i = 0; i < polygonIds.Count; i++)
            {
                polygonStr += polygonIds[i];
                if (i < polygonIds.Count - 1) polygonStr += ", ";
            }
            polygonStr += "]";
            
            Debug.Log($"[IsValidEdge] Edge ({edge.point1}, {edge.point2}), polygonIds={polygonStr}, listHashCode={polygonIds.GetHashCode()}");
        }
        else
        {
            Debug.Log($"[IsValidEdge] Edge ({point1}, {point2}) NOT FOUND in cache!");
        }
        
        return found;
    }
    
    public HashSet<Edge> GetAllEdges()
    {
        return allEdgesCache;
    }
}