using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LevelEditorWindow : EditorWindow
{
    private LevelData levelData;
    private Vector2 scrollPos;
    private int selectedPointIndex = -1;
    private int selectedPolygonIndex = -1;
    
    private bool showPoints = true;
    private bool showPolygons = true;
    private float pointSize = 0.3f;
    private Color pointColor = Color.cyan;
    private Color selectedColor = Color.yellow;
    
    private Vector2 previewOffset = Vector2.zero;
    private float previewZoom = 50f;
    private Rect previewRect;
    
    private bool isCreatingPoint = false;
    private bool isCreatingPolygon = false;
    private List<int> selectedPointsForPolygon = new List<int>();
    private int hoveredPointIndex = -1;
    
    [MenuItem("Tools/Level Editor/Open Level Editor")]
    static void ShowWindow()
    {
        var window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(800, 600);
    }
    
    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        
        DrawLeftPanel();
        DrawPreviewPanel();
        DrawRightPanel();
        
        EditorGUILayout.EndHorizontal();
        
        if (Event.current.type == EventType.MouseDown)
            Repaint();
    }
    
    void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        
        EditorGUILayout.LabelField("Level Data", EditorStyles.boldLabel);
        
        LevelData newData = EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelData), false) as LevelData;
        if (newData != levelData)
        {
            levelData = newData;
            if (levelData != null)
                levelData.Initialize();
            selectedPointIndex = -1;
            selectedPolygonIndex = -1;
        }
        
        EditorGUILayout.Space(10);
        
        if (levelData == null)
        {
            EditorGUILayout.HelpBox("Select a LevelData asset to start editing", MessageType.Info);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create New Level", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create New LevelData"))
            {
                CreateNewLevelData();
            }
        }
        else
        {
            EditorGUILayout.LabelField("Display Options", EditorStyles.boldLabel);
            showPoints = EditorGUILayout.Toggle("Show Points", showPoints);
            showPolygons = EditorGUILayout.Toggle("Show Polygons", showPolygons);
            pointSize = EditorGUILayout.Slider("Point Size", pointSize, 0.1f, 1f);
            pointColor = EditorGUILayout.ColorField("Point Color", pointColor);
            selectedColor = EditorGUILayout.ColorField("Selected Color", selectedColor);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField($"Points: {levelData.points.Count}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Polygons: {levelData.polygons.Count}", EditorStyles.miniLabel);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Reset View"))
            {
                previewOffset = Vector2.zero;
                previewZoom = 50f;
            }
            
            if (GUILayout.Button("Refresh Data"))
            {
                levelData.Initialize();
                Repaint();
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Import/Export", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Import Points from List"))
            {
                ImportPointsWindow.ShowWindow(levelData, () => Repaint());
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create New", EditorStyles.boldLabel);
            
            GUI.backgroundColor = isCreatingPoint ? Color.green : Color.white;
            if (GUILayout.Button(isCreatingPoint ? "Creating Point (Click in preview)" : "Create New Point"))
            {
                isCreatingPoint = !isCreatingPoint;
                isCreatingPolygon = false;
                selectedPointsForPolygon.Clear();
            }
            GUI.backgroundColor = Color.white;
            
            GUI.backgroundColor = isCreatingPolygon ? Color.green : Color.white;
            if (GUILayout.Button(isCreatingPolygon ? "Creating Polygon (Select points)" : "Create New Polygon"))
            {
                isCreatingPolygon = !isCreatingPolygon;
                isCreatingPoint = false;
                selectedPointsForPolygon.Clear();
            }
            GUI.backgroundColor = Color.white;
            
            if (isCreatingPolygon && selectedPointsForPolygon.Count > 0)
            {
                EditorGUILayout.HelpBox($"Selected {selectedPointsForPolygon.Count} points. Need at least 3 to create polygon.", MessageType.Info);
                
                if (selectedPointsForPolygon.Count >= 3)
                {
                    if (GUILayout.Button("Finish Polygon"))
                    {
                        CreatePolygonFromSelectedPoints();
                    }
                }
                
                if (GUILayout.Button("Cancel"))
                {
                    isCreatingPolygon = false;
                    selectedPointsForPolygon.Clear();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawPreviewPanel()
    {
        EditorGUILayout.BeginVertical();
        
        GUILayout.Label("Preview", EditorStyles.boldLabel);
        
        previewRect = GUILayoutUtility.GetRect(300, 10000, 400, 10000);
        GUI.Box(previewRect, "", "box");
        
        if (levelData != null)
        {
            DrawLevelPreview();
            HandlePreviewInput();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        if (levelData != null)
        {
            if (selectedPointIndex >= 0 && selectedPointIndex < levelData.points.Count)
            {
                DrawPointEditor();
            }
            else if (selectedPolygonIndex >= 0 && selectedPolygonIndex < levelData.polygons.Count)
            {
                DrawPolygonEditor();
            }
            else
            {
                EditorGUILayout.HelpBox("Click on a point or polygon in the preview to edit", MessageType.Info);
            }
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    
    void DrawLevelPreview()
    {
        Handles.BeginGUI();
        
        Matrix4x4 oldMatrix = Handles.matrix;
        Handles.matrix = GetPreviewMatrix();
        
        if (showPolygons)
        {
            for (int i = 0; i < levelData.polygons.Count; i++)
            {
                DrawPolygon(levelData.polygons[i], i == selectedPolygonIndex);
            }
        }
        
        if (showPoints)
        {
            for (int i = 0; i < levelData.points.Count; i++)
            {
                bool isHovered = (i == hoveredPointIndex && !isCreatingPoint);
                DrawPoint(levelData.points[i], i, i == selectedPointIndex, isHovered);
            }
        }
        
        Handles.matrix = oldMatrix;
        Handles.EndGUI();
    }
    
    void DrawPoint(PointData point, int index, bool isSelected, bool isHovered = false)
    {
        Vector3 worldPos = new Vector3(point.position.x, point.position.y, 0);
        
        bool isSelectedForPolygon = selectedPointsForPolygon.Contains(point.pointId);
        Color color = isSelected ? selectedColor : (isSelectedForPolygon ? Color.green : pointColor);
        
        if (isHovered && !isSelected && !isSelectedForPolygon)
        {
            color = Color.yellow;
        }
        
        float size = isHovered ? pointSize * 1.2f : pointSize;
        
        Handles.color = color;
        Handles.DrawSolidDisc(worldPos, Vector3.forward, size);
        
        Handles.color = Color.black;
        Handles.DrawWireDisc(worldPos, Vector3.forward, pointSize);
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 10;
        style.fontStyle = FontStyle.Bold;
        
        Vector3 screenPos = Handles.matrix.MultiplyPoint(worldPos);
        Vector2 labelPos = new Vector2(screenPos.x + 10, screenPos.y - 5);
        GUI.Label(new Rect(labelPos.x, labelPos.y, 100, 20), $"P{point.pointId}", style);
    }
    
    void DrawPolygon(PolygonData polygon, bool isSelected)
    {
        if (polygon.pointIds.Count < 3) return;
        
        List<Vector3> vertices = new List<Vector3>();
        foreach (int pointId in polygon.pointIds)
        {
            PointData point = levelData.points.Find(p => p.pointId == pointId);
            if (point != null)
            {
                vertices.Add(new Vector3(point.position.x, point.position.y, 0));
            }
        }
        
        if (vertices.Count < 3) return;
        
        Color fillColor = isSelected ? new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.3f) : 
                                       new Color(polygon.color.r, polygon.color.g, polygon.color.b, 0.2f);
        Handles.color = fillColor;
        Handles.DrawAAConvexPolygon(vertices.ToArray());
        
        Color edgeColor = isSelected ? selectedColor : polygon.color;
        Handles.color = edgeColor;
        for (int i = 0; i < vertices.Count; i++)
        {
            int next = (i + 1) % vertices.Count;
            Handles.DrawLine(vertices[i], vertices[next], 2f);
        }
        
        Vector3 center = Vector3.zero;
        foreach (var v in vertices)
            center += v;
        center /= vertices.Count;
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        Vector3 screenPos = Handles.matrix.MultiplyPoint(center);
        GUI.Label(new Rect(screenPos.x - 30, screenPos.y - 10, 60, 20), $"Poly {polygon.polygonId}", style);
    }
    
    void HandlePreviewInput()
    {
        Event e = Event.current;
        
        if (previewRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseMove)
            {
                Vector2 worldPos = ScreenToWorld(e.mousePosition);
                hoveredPointIndex = FindPointAtPosition(worldPos);
                Repaint();
            }
            
            if (e.type == EventType.ScrollWheel)
            {
                float delta = -e.delta.y;
                previewZoom += delta * 5f;
                previewZoom = Mathf.Clamp(previewZoom, 10f, 200f);
                e.Use();
                Repaint();
            }
            
            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                previewOffset += e.delta / previewZoom;
                e.Use();
                Repaint();
            }
            
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 mousePos = e.mousePosition;
                Vector2 worldPos = ScreenToWorld(mousePos);
                
                if (isCreatingPoint)
                {
                    CreatePointAtPosition(worldPos);
                    e.Use();
                    Repaint();
                }
                else if (isCreatingPolygon)
                {
                    int clickedPoint = FindPointAtPosition(worldPos);
                    if (clickedPoint >= 0)
                    {
                        int pointId = levelData.points[clickedPoint].pointId;
                        if (selectedPointsForPolygon.Contains(pointId))
                        {
                            selectedPointsForPolygon.Remove(pointId);
                        }
                        else
                        {
                            selectedPointsForPolygon.Add(pointId);
                        }
                        e.Use();
                        Repaint();
                    }
                }
                else
                {
                    int clickedPoint = FindPointAtPosition(worldPos);
                    int clickedPolygon = FindPolygonAtPosition(worldPos);
                    
                    if (clickedPoint >= 0)
                    {
                        selectedPointIndex = clickedPoint;
                        selectedPolygonIndex = -1;
                        e.Use();
                        Repaint();
                    }
                    else if (clickedPolygon >= 0)
                    {
                        selectedPolygonIndex = clickedPolygon;
                        selectedPointIndex = -1;
                        e.Use();
                        Repaint();
                    }
                }
            }
        }
    }
    
    int FindPointAtPosition(Vector2 worldPos)
    {
        float threshold = Mathf.Max(0.5f, 20f / previewZoom);
        
        int closestIndex = -1;
        float closestDist = float.MaxValue;
        
        for (int i = 0; i < levelData.points.Count; i++)
        {
            Vector2 pointPos = levelData.points[i].position;
            float dist = Vector2.Distance(pointPos, worldPos);
            
            if (dist < threshold && dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }
    
    int FindPolygonAtPosition(Vector2 worldPos)
    {
        for (int i = 0; i < levelData.polygons.Count; i++)
        {
            PolygonData polygon = levelData.polygons[i];
            if (IsPointInPolygon(worldPos, polygon))
            {
                return i;
            }
        }
        
        return -1;
    }
    
    bool IsPointInPolygon(Vector2 point, PolygonData polygon)
    {
        List<Vector2> vertices = new List<Vector2>();
        foreach (int pointId in polygon.pointIds)
        {
            PointData p = levelData.points.Find(pt => pt.pointId == pointId);
            if (p != null)
                vertices.Add(p.position);
        }
        
        if (vertices.Count < 3) return false;
        
        int intersections = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector2 v1 = vertices[i];
            Vector2 v2 = vertices[(i + 1) % vertices.Count];
            
            if ((v1.y > point.y) != (v2.y > point.y))
            {
                float xIntersect = (v2.x - v1.x) * (point.y - v1.y) / (v2.y - v1.y) + v1.x;
                if (point.x < xIntersect)
                    intersections++;
            }
        }
        
        return (intersections % 2) == 1;
    }
    
    Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Vector2 localPos = screenPos - new Vector2(previewRect.x, previewRect.y);
        Vector2 center = new Vector2(previewRect.width / 2, previewRect.height / 2);
        Vector2 offset = (localPos - center) / previewZoom;
        return new Vector2(offset.x, -offset.y) - previewOffset;
    }
    
    Matrix4x4 GetPreviewMatrix()
    {
        Vector2 center = new Vector2(previewRect.width / 2, previewRect.height / 2);
        Vector2 translation = new Vector2(previewRect.x, previewRect.y) + center;
        
        Matrix4x4 matrix = Matrix4x4.identity;
        matrix = Matrix4x4.Translate(new Vector3(translation.x, translation.y, 0));
        matrix *= Matrix4x4.Scale(new Vector3(previewZoom, -previewZoom, 1));
        matrix *= Matrix4x4.Translate(new Vector3(previewOffset.x, previewOffset.y, 0));
        
        return matrix;
    }
    
    void DrawPointEditor()
    {
        PointData point = levelData.points[selectedPointIndex];
        
        EditorGUILayout.LabelField("Point Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.LabelField($"Point ID: {point.pointId}");
        
        Vector2 newPos = EditorGUILayout.Vector2Field("Position", point.position);
        if (newPos != point.position)
        {
            Undo.RecordObject(levelData, "Change Point Position");
            point.position = newPos;
            EditorUtility.SetDirty(levelData);
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Belongs to Polygons:", EditorStyles.boldLabel);
        
        if (point.belongToPolygons.Count == 0)
        {
            EditorGUILayout.HelpBox("This point doesn't belong to any polygon", MessageType.Warning);
        }
        
        List<int> polygonsToRemove = new List<int>();
        foreach (int polyId in point.belongToPolygons)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Polygon {polyId}");
            
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                selectedPolygonIndex = levelData.polygons.FindIndex(p => p.polygonId == polyId);
                selectedPointIndex = -1;
                Repaint();
            }
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                polygonsToRemove.Add(polyId);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        if (polygonsToRemove.Count > 0)
        {
            Undo.RecordObject(levelData, "Remove Point from Polygon");
            foreach (int polyId in polygonsToRemove)
            {
                PolygonData poly = levelData.polygons.Find(p => p.polygonId == polyId);
                if (poly != null)
                {
                    poly.pointIds.Remove(point.pointId);
                    point.belongToPolygons.Remove(polyId);
                }
            }
            EditorUtility.SetDirty(levelData);
            levelData.Initialize();
        }
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Add to Polygon"))
        {
            GenericMenu menu = new GenericMenu();
            foreach (var poly in levelData.polygons)
            {
                if (!point.belongToPolygons.Contains(poly.polygonId))
                {
                    menu.AddItem(new GUIContent($"Polygon {poly.polygonId}"), false, () => {
                        Undo.RecordObject(levelData, "Add Point to Polygon");
                        poly.pointIds.Add(point.pointId);
                        point.belongToPolygons.Add(poly.polygonId);
                        EditorUtility.SetDirty(levelData);
                        levelData.Initialize();
                    });
                }
            }
            
            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No available polygons"));
            }
            
            menu.ShowAsContext();
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }
    }
    
    void DrawPolygonEditor()
    {
        PolygonData polygon = levelData.polygons[selectedPolygonIndex];
        
        EditorGUILayout.LabelField("Polygon Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.LabelField($"Polygon ID: {polygon.polygonId}");
        
        Color newColor = EditorGUILayout.ColorField("Color", polygon.color);
        if (newColor != polygon.color)
        {
            Undo.RecordObject(levelData, "Change Polygon Color");
            polygon.color = newColor;
            EditorUtility.SetDirty(levelData);
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Points in Polygon: {polygon.pointIds.Count}", EditorStyles.boldLabel);
        
        List<int> pointsToRemove = new List<int>();
        for (int i = 0; i < polygon.pointIds.Count; i++)
        {
            int pointId = polygon.pointIds[i];
            PointData point = levelData.points.Find(p => p.pointId == pointId);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Point {pointId}" + (point != null ? $" ({point.position.x:F1}, {point.position.y:F1})" : " (NOT FOUND)"));
            
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                selectedPointIndex = levelData.points.FindIndex(p => p.pointId == pointId);
                selectedPolygonIndex = -1;
                Repaint();
            }
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                pointsToRemove.Add(pointId);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        if (pointsToRemove.Count > 0)
        {
            Undo.RecordObject(levelData, "Remove Point from Polygon");
            foreach (int pointId in pointsToRemove)
            {
                polygon.pointIds.Remove(pointId);
                PointData point = levelData.points.Find(p => p.pointId == pointId);
                if (point != null)
                {
                    point.belongToPolygons.Remove(polygon.polygonId);
                }
            }
            EditorUtility.SetDirty(levelData);
            levelData.Initialize();
        }
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Add Point to Polygon"))
        {
            GenericMenu menu = new GenericMenu();
            foreach (var point in levelData.points)
            {
                if (!polygon.pointIds.Contains(point.pointId))
                {
                    menu.AddItem(new GUIContent($"Point {point.pointId} ({point.position.x:F1}, {point.position.y:F1})"), false, () => {
                        Undo.RecordObject(levelData, "Add Point to Polygon");
                        polygon.pointIds.Add(point.pointId);
                        point.belongToPolygons.Add(polygon.polygonId);
                        EditorUtility.SetDirty(levelData);
                        levelData.Initialize();
                    });
                }
            }
            
            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No available points"));
            }
            
            menu.ShowAsContext();
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }
    }
    
    void CreateNewLevelData()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Level Data",
            "NewLevel",
            "asset",
            "Choose location for new level data"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
            newLevel.levelId = System.DateTime.Now.GetHashCode();
            newLevel.points = new List<PointData>();
            newLevel.polygons = new List<PolygonData>();
            
            AssetDatabase.CreateAsset(newLevel, path);
            AssetDatabase.SaveAssets();
            
            levelData = newLevel;
            levelData.Initialize();
            
            EditorUtility.DisplayDialog("Success", "New LevelData created successfully!", "OK");
        }
    }
    
    void CreatePointAtPosition(Vector2 worldPos)
    {
        Undo.RecordObject(levelData, "Create Point");
        
        int newPointId = 0;
        if (levelData.points.Count > 0)
        {
            newPointId = levelData.points[levelData.points.Count - 1].pointId + 1;
        }
        
        PointData newPoint = new PointData(newPointId, worldPos);
        levelData.points.Add(newPoint);
        
        EditorUtility.SetDirty(levelData);
        AssetDatabase.SaveAssets();
        
        selectedPointIndex = levelData.points.Count - 1;
        isCreatingPoint = false;
    }
    
    void CreatePolygonFromSelectedPoints()
    {
        if (selectedPointsForPolygon.Count < 3)
        {
            EditorUtility.DisplayDialog("Error", "Need at least 3 points to create a polygon", "OK");
            return;
        }
        
        Undo.RecordObject(levelData, "Create Polygon");
        
        int newPolygonId = 0;
        if (levelData.polygons.Count > 0)
        {
            newPolygonId = levelData.polygons[levelData.polygons.Count - 1].polygonId + 1;
        }
        
        List<int> orderedPoints = new List<int>(selectedPointsForPolygon);
        OrderPointsClockwise(orderedPoints);
        
        PolygonData newPolygon = new PolygonData(newPolygonId, orderedPoints);
        newPolygon.color = new Color(
            UnityEngine.Random.Range(0.3f, 1f),
            UnityEngine.Random.Range(0.3f, 1f),
            UnityEngine.Random.Range(0.3f, 1f),
            1f
        );
        
        levelData.polygons.Add(newPolygon);
        levelData.Initialize();
        
        EditorUtility.SetDirty(levelData);
        AssetDatabase.SaveAssets();
        
        selectedPolygonIndex = levelData.polygons.Count - 1;
        isCreatingPolygon = false;
        selectedPointsForPolygon.Clear();
    }
    
    void OrderPointsClockwise(List<int> pointIds)
    {
        if (pointIds.Count < 3) return;
        
        Vector2 center = Vector2.zero;
        foreach (int id in pointIds)
        {
            PointData p = levelData.points.Find(pt => pt.pointId == id);
            if (p != null)
                center += p.position;
        }
        center /= pointIds.Count;
        
        pointIds.Sort((id1, id2) => {
            PointData p1 = levelData.points.Find(p => p.pointId == id1);
            PointData p2 = levelData.points.Find(p => p.pointId == id2);
            
            if (p1 == null || p2 == null) return 0;
            
            float angle1 = Mathf.Atan2(p1.position.y - center.y, p1.position.x - center.x);
            float angle2 = Mathf.Atan2(p2.position.y - center.y, p2.position.x - center.x);
            
            return angle1.CompareTo(angle2);
        });
    }
}

public class ImportPointsWindow : EditorWindow
{
    [SerializeField]
    private List<Vector2> vectorList = new List<Vector2>();
    private LevelData targetLevel;
    private System.Action onComplete;
    private Vector2 scrollPos;
    private SerializedObject serializedObject;
    private SerializedProperty vectorListProperty;
    
    public static void ShowWindow(LevelData level, System.Action callback)
    {
        var window = GetWindow<ImportPointsWindow>("Import Points");
        window.minSize = new Vector2(400, 300);
        window.targetLevel = level;
        window.onComplete = callback;
        window.vectorList = new List<Vector2>();
    }
    
    void OnEnable()
    {
        serializedObject = new SerializedObject(this);
        vectorListProperty = serializedObject.FindProperty("vectorList");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Import Points from Vector2 List", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("Copy List<Vector2> from Inspector and paste here using Ctrl+V", MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (serializedObject == null || vectorListProperty == null)
        {
            serializedObject = new SerializedObject(this);
            vectorListProperty = serializedObject.FindProperty("vectorList");
        }
        
        serializedObject.Update();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
        EditorGUILayout.PropertyField(vectorListProperty, true);
        EditorGUILayout.EndScrollView();
        
        serializedObject.ApplyModifiedProperties();
        
        EditorGUILayout.Space();
        
        if (vectorList != null && vectorList.Count > 0)
        {
            EditorGUILayout.HelpBox($"Ready to import {vectorList.Count} points", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Import", GUILayout.Height(30)))
        {
            ImportPoints();
        }
        
        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            Close();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    void ImportPoints()
    {
        if (targetLevel == null)
        {
            EditorUtility.DisplayDialog("Error", "No target level data!", "OK");
            return;
        }
        
        if (vectorList == null || vectorList.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No points to import!", "OK");
            return;
        }
        
        Undo.RecordObject(targetLevel, "Import Points");
        
        int startId = 0;
        if (targetLevel.points.Count > 0)
        {
            startId = targetLevel.points[targetLevel.points.Count - 1].pointId + 1;
        }
        
        for (int i = 0; i < vectorList.Count; i++)
        {
            PointData newPoint = new PointData(startId + i, vectorList[i]);
            targetLevel.points.Add(newPoint);
        }
        
        EditorUtility.SetDirty(targetLevel);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Success", $"Imported {vectorList.Count} points successfully!", "OK");
        
        if (onComplete != null)
            onComplete();
        
        Close();
    }
}
