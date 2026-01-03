using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool để tạo LevelData dễ dàng
/// Menu: Tools/Create Level Data
/// </summary>
public class LevelDataCreator : EditorWindow
{
    private int levelId = 1;
    private string levelName = "TestLevel_01";
    
    [MenuItem("Tools/Create Level Data")]
    static void ShowWindow()
    {
        GetWindow<LevelDataCreator>("Level Creator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Level Data Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        levelId = EditorGUILayout.IntField("Level ID", levelId);
        levelName = EditorGUILayout.TextField("Level Name", levelName);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Create Simple Test Level", GUILayout.Height(40)))
        {
            CreateSimpleLevel();
        }
        
        if (GUILayout.Button("Create Medium Test Level", GUILayout.Height(40)))
        {
            CreateMediumLevel();
        }
        
        if (GUILayout.Button("Create Complex Test Level", GUILayout.Height(40)))
        {
            CreateComplexLevel();
        }
    }
    
    void CreateSimpleLevel()
    {
        // 1 tam giác đơn giản
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.levelId = levelId;
        level.name = levelName;
        
        // 3 điểm tạo tam giác
        level.points.Add(new PointData(0, new Vector2(-1, -1)));  // Bottom left
        level.points.Add(new PointData(1, new Vector2(1, -1)));   // Bottom right
        level.points.Add(new PointData(2, new Vector2(0, 1)));    // Top
        
        // 1 đa giác tam giác
        level.polygons.Add(new PolygonData(0, new List<int> { 0, 1, 2 }));
        level.polygons[0].centerPosition = new Vector2(0, -0.3f);
        
        level.Initialize();
        SaveAsset(level);
    }
    
    void CreateMediumLevel()
    {
        // 1 hình vuông + 1 tam giác share 2 điểm
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.levelId = levelId;
        level.name = levelName;
        
        // 5 điểm
        level.points.Add(new PointData(0, new Vector2(-1, -1)));  // 0: Bottom left
        level.points.Add(new PointData(1, new Vector2(1, -1)));   // 1: Bottom right
        level.points.Add(new PointData(2, new Vector2(1, 1)));    // 2: Top right
        level.points.Add(new PointData(3, new Vector2(-1, 1)));   // 3: Top left
        level.points.Add(new PointData(4, new Vector2(0, 2)));    // 4: Peak
        
        // Polygon 0: Square (0-1-2-3)
        level.polygons.Add(new PolygonData(0, new List<int> { 0, 1, 2, 3 }));
        level.polygons[0].centerPosition = new Vector2(0, 0);
        
        // Polygon 1: Triangle (3-4-2) - share điểm 3 và 2 với square
        level.polygons.Add(new PolygonData(1, new List<int> { 3, 4, 2 }));
        level.polygons[1].centerPosition = new Vector2(0, 1.3f);
        
        level.Initialize();
        SaveAsset(level);
    }
    
    void CreateComplexLevel()
    {
        // Multiple shapes với nhiều shared points
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.levelId = levelId;
        level.name = levelName;
        
        // 7 điểm tạo layout phức tạp
        level.points.Add(new PointData(0, new Vector2(-2, 0)));    // Left
        level.points.Add(new PointData(1, new Vector2(-1, -1)));   // Bottom left
        level.points.Add(new PointData(2, new Vector2(0, 0)));     // Center
        level.points.Add(new PointData(3, new Vector2(1, -1)));    // Bottom right
        level.points.Add(new PointData(4, new Vector2(2, 0)));     // Right
        level.points.Add(new PointData(5, new Vector2(1, 1)));     // Top right
        level.points.Add(new PointData(6, new Vector2(-1, 1)));    // Top left
        
        // Polygon 0: Triangle left (0-1-2)
        level.polygons.Add(new PolygonData(0, new List<int> { 0, 1, 2 }));
        level.polygons[0].centerPosition = new Vector2(-1, -0.3f);
        
        // Polygon 1: Triangle bottom (1-3-2)
        level.polygons.Add(new PolygonData(1, new List<int> { 1, 3, 2 }));
        level.polygons[1].centerPosition = new Vector2(0, -0.7f);
        
        // Polygon 2: Triangle right (2-3-4)
        level.polygons.Add(new PolygonData(2, new List<int> { 2, 3, 4 }));
        level.polygons[2].centerPosition = new Vector2(1, -0.3f);
        
        // Polygon 3: Square center (0-2-5-6) - note: không liên tiếp trong array
        level.polygons.Add(new PolygonData(3, new List<int> { 0, 2, 5, 6 }));
        level.polygons[3].centerPosition = new Vector2(-0.5f, 0.5f);
        
        // Polygon 4: Square right (2-4-5-2) - wait, this is wrong
        // Let me fix: (2-4-5) triangle top right
        level.polygons.Add(new PolygonData(4, new List<int> { 2, 4, 5 }));
        level.polygons[4].centerPosition = new Vector2(1, 0.3f);
        
        level.Initialize();
        SaveAsset(level);
    }
    
    void SaveAsset(LevelData level)
    {
        string path = $"Assets/Levels/{levelName}.asset";
        
        // Tạo folder nếu chưa có
        if (!AssetDatabase.IsValidFolder("Assets/Levels"))
        {
            AssetDatabase.CreateFolder("Assets", "Levels");
        }
        
        AssetDatabase.CreateAsset(level, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = level;
        
        Debug.Log($"✅ Created level: {path}");
        Debug.Log($"📊 Points: {level.points.Count}, Polygons: {level.polygons.Count}");
        
        // Log edges for debugging
        var edges = level.GetAllEdges();
        Debug.Log($"🔗 Total edges: {edges.Count}");
        foreach (var edge in edges)
        {
            Debug.Log($"   Edge: {edge.point1} ↔ {edge.point2}");
        }
    }
}