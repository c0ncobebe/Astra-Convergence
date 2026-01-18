using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tool để validate và fix LevelData assets
/// Menu: Tools/Level Validator
/// </summary>
public class LevelDataValidator : EditorWindow
{
    private LevelData targetLevel;
    private Vector2 scrollPos;
    private List<string> validationIssues = new List<string>();
    
    [MenuItem("Tools/Level Data Validator")]
    static void ShowWindow()
    {
        GetWindow<LevelDataValidator>("Level Validator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Level Data Validator & Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        targetLevel = (LevelData)EditorGUILayout.ObjectField("Level Data", targetLevel, typeof(LevelData), false);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Validate Level", GUILayout.Height(40)))
        {
            if (targetLevel != null)
                ValidateLevel();
            else
                EditorUtility.DisplayDialog("Error", "Please select a Level Data asset", "OK");
        }
        
        if (GUILayout.Button("Fix Duplicates", GUILayout.Height(40)))
        {
            if (targetLevel != null)
                FixDuplicates();
            else
                EditorUtility.DisplayDialog("Error", "Please select a Level Data asset", "OK");
        }
        
        if (GUILayout.Button("Validate All Levels in Project", GUILayout.Height(40)))
        {
            ValidateAllLevels();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Validation Results:", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var issue in validationIssues)
        {
            EditorGUILayout.HelpBox(issue, MessageType.Warning);
        }
        EditorGUILayout.EndScrollView();
    }
    
    void ValidateLevel()
    {
        validationIssues.Clear();
        
        Debug.Log($"[Validator] Validating level {targetLevel.levelId}...");
        
        // Check 1: Duplicate point IDs
        var pointIds = targetLevel.points.Select(p => p.pointId).ToList();
        var duplicatePoints = pointIds.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicatePoints.Count > 0)
        {
            validationIssues.Add($"Found duplicate point IDs: {string.Join(", ", duplicatePoints)}");
        }
        
        // Check 2: Duplicate polygon IDs
        var polygonIds = targetLevel.polygons.Select(p => p.polygonId).ToList();
        var duplicatePolygons = polygonIds.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicatePolygons.Count > 0)
        {
            validationIssues.Add($"Found duplicate polygon IDs: {string.Join(", ", duplicatePolygons)}");
        }
        
        // Check 3: Duplicate belongToPolygons
        foreach (var point in targetLevel.points)
        {
            var uniquePolygons = point.belongToPolygons.Distinct().ToList();
            if (uniquePolygons.Count != point.belongToPolygons.Count)
            {
                validationIssues.Add($"Point {point.pointId} has duplicate belongToPolygons entries. Has {point.belongToPolygons.Count}, should have {uniquePolygons.Count}");
            }
        }
        
        // Check 4: Duplicate edges in polygons
        foreach (var polygon in targetLevel.polygons)
        {
            if (polygon.edges != null && polygon.edges.Count > 0)
            {
                var edgeSet = new HashSet<Edge>();
                var duplicateEdges = new List<Edge>();
                
                foreach (var edge in polygon.edges)
                {
                    if (edgeSet.Contains(edge))
                    {
                        duplicateEdges.Add(edge);
                    }
                    else
                    {
                        edgeSet.Add(edge);
                    }
                }
                
                if (duplicateEdges.Count > 0)
                {
                    validationIssues.Add($"Polygon {polygon.polygonId} has {duplicateEdges.Count} duplicate edges");
                }
            }
        }
        
        // Check 5: Edge count mismatch
        foreach (var polygon in targetLevel.polygons)
        {
            if (polygon.edges != null && polygon.edges.Count != polygon.pointIds.Count)
            {
                validationIssues.Add($"Polygon {polygon.polygonId} edge count mismatch: {polygon.edges.Count} edges, {polygon.pointIds.Count} points");
            }
        }
        
        if (validationIssues.Count == 0)
        {
            validationIssues.Add("✓ No issues found! Level data is valid.");
        }
        
        Debug.Log($"[Validator] Validation complete. Found {validationIssues.Count} issues.");
    }
    
    void FixDuplicates()
    {
        Undo.RecordObject(targetLevel, "Fix Level Data Duplicates");
        
        int fixCount = 0;
        
        // Fix 1: Remove duplicate belongToPolygons
        foreach (var point in targetLevel.points)
        {
            var before = point.belongToPolygons.Count;
            point.belongToPolygons = point.belongToPolygons.Distinct().ToList();
            var after = point.belongToPolygons.Count;
            
            if (before != after)
            {
                Debug.Log($"[Validator] Fixed Point {point.pointId}: removed {before - after} duplicate polygon references");
                fixCount++;
            }
        }
        
        // Fix 2: Remove duplicate edges in polygons
        foreach (var polygon in targetLevel.polygons)
        {
            if (polygon.edges != null && polygon.edges.Count > 0)
            {
                var before = polygon.edges.Count;
                var uniqueEdges = new HashSet<Edge>();
                var fixedEdges = new List<Edge>();
                
                foreach (var edge in polygon.edges)
                {
                    if (!uniqueEdges.Contains(edge))
                    {
                        uniqueEdges.Add(edge);
                        fixedEdges.Add(edge);
                    }
                }
                
                polygon.edges = fixedEdges;
                var after = polygon.edges.Count;
                
                if (before != after)
                {
                    Debug.Log($"[Validator] Fixed Polygon {polygon.polygonId}: removed {before - after} duplicate edges");
                    fixCount++;
                }
            }
        }
        
        EditorUtility.SetDirty(targetLevel);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Fix Complete", 
            $"Fixed {fixCount} duplicate issues.\n\nPlease validate again to confirm.", 
            "OK");
        
        // Re-validate
        ValidateLevel();
    }
    
    void ValidateAllLevels()
    {
        validationIssues.Clear();
        
        string[] guids = AssetDatabase.FindAssets("t:LevelData");
        int totalLevels = guids.Length;
        int issueCount = 0;
        
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            
            if (level != null)
            {
                // Quick validation
                bool hasIssues = false;
                
                foreach (var point in level.points)
                {
                    if (point.belongToPolygons.Count != point.belongToPolygons.Distinct().Count())
                    {
                        validationIssues.Add($"❌ {level.name}: Point {point.pointId} has duplicates");
                        hasIssues = true;
                    }
                }
                
                foreach (var polygon in level.polygons)
                {
                    if (polygon.edges != null && polygon.edges.Count != polygon.edges.Distinct().Count())
                    {
                        validationIssues.Add($"❌ {level.name}: Polygon {polygon.polygonId} has duplicate edges");
                        hasIssues = true;
                    }
                }
                
                if (!hasIssues)
                {
                    validationIssues.Add($"✓ {level.name}: OK");
                }
                else
                {
                    issueCount++;
                }
            }
        }
        
        validationIssues.Insert(0, $"Scanned {totalLevels} levels. Found issues in {issueCount} level(s).");
    }
}
