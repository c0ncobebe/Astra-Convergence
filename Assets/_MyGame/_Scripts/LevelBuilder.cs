using System.Collections.Generic;
using UnityEngine;

public static class LevelBuilder
{
    public static LevelData CreateLevel(int levelId)
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.levelId = levelId;
        return level;
    }
    
    public static void AddPoint(LevelData level, int pointId, Vector2 position)
    {
        level.points.Add(new PointData(pointId, position));
    }
    
    public static void AddPolygon(LevelData level, int polygonId, List<int> pointIds, Vector2 centerPos)
    {
        var polygon = new PolygonData(polygonId, pointIds);
        polygon.centerPosition = centerPos;
        level.polygons.Add(polygon);
    }
    
    // Example: Triangle và Square share 2 points
    public static LevelData CreateExampleLevel()
    {
        var level = CreateLevel(1);
        
        // 5 điểm
        AddPoint(level, 0, new Vector2(0, 0));     // Bottom left
        AddPoint(level, 1, new Vector2(2, 0));     // Bottom right
        AddPoint(level, 2, new Vector2(2, 2));     // Top right
        AddPoint(level, 3, new Vector2(0, 2));     // Top left
        AddPoint(level, 4, new Vector2(1, 3));     // Top center
        
        // Square: 0-1-2-3
        AddPolygon(level, 0, new List<int> { 0, 1, 2, 3 }, new Vector2(1, 1));
        
        // Triangle: 3-4-2 (share điểm 3 và 2 với square)
        AddPolygon(level, 1, new List<int> { 3, 4, 2 }, new Vector2(1.3f, 2.3f));
        
        level.Initialize();
        return level;
    }
}