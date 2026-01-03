using System.Collections.Generic;
using UnityEngine;

public class GamePlayManager : MonoBehaviour
{
    [Header("References")]
    public LevelData currentLevel;
    public GameObject pointPrefab;
    public GameObject polygonPrefab;
    
    [Header("Settings")]
    public float swipeDetectionRadius = 0.5f;
    public bool showDebugLines = true;
    
    // Runtime data - Optimized for mobile
    private GamePoint[] pointsArray; // Array nhanh hơn Dictionary cho iteration
    private Dictionary<int, GamePoint> pointsDict;
    private Dictionary<int, GamePolygon> polygonsDict;
    
    // Selection tracking - preallocated để tránh GC
    private List<int> selectedPointIds = new (10);
    private List<GamePoint> selectedPoints = new (10);
    private HashSet<int> possiblePolygons = new ();
    
    // Edge validation cache
    private List<int> tempPolygonList = new (2); // Reuse để tránh allocation
    
    // Input tracking
    private bool isDragging = false;
    private Camera mainCamera;
    private GamePoint lastAddedPoint = null;
    
    // Visual feedback
    private LineRenderer selectionLineRenderer;
    
    void Start()
    {
        mainCamera = Camera.main;
        SetupSelectionLineRenderer();
        InitializeLevel();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    #region Level Setup
    
    void InitializeLevel()
    {
        if (currentLevel == null) return;
        
        currentLevel.Initialize();
        
        int pointCount = currentLevel.points.Count;
        pointsArray = new GamePoint[pointCount];
        pointsDict = new Dictionary<int, GamePoint>(pointCount);
        polygonsDict = new Dictionary<int, GamePolygon>(currentLevel.polygons.Count);
        
        // Spawn points
        for (int i = 0; i < pointCount; i++)
        {
            var pointData = currentLevel.points[i];
            var pointObj = Instantiate(pointPrefab);
            var gamePoint = pointObj.GetComponent<GamePoint>();
            gamePoint.Initialize(pointData);
            pointsArray[i] = gamePoint;
            pointsDict[pointData.pointId] = gamePoint;
        }
        
        // Spawn polygons
        for (int i = 0; i < currentLevel.polygons.Count; i++)
        {
            var polygonData = currentLevel.polygons[i];
            var polygonObj = Instantiate(polygonPrefab);
            var gamePolygon = polygonObj.GetComponent<GamePolygon>();
            gamePolygon.Initialize(polygonData);
            polygonsDict[polygonData.polygonId] = gamePolygon;
        }
    }
    
    void SetupSelectionLineRenderer()
    {
        var obj = new GameObject("SelectionLine");
        selectionLineRenderer = obj.AddComponent<LineRenderer>();
        selectionLineRenderer.startWidth = 0.05f;
        selectionLineRenderer.endWidth = 0.05f;
        selectionLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        selectionLineRenderer.startColor = Color.yellow;
        selectionLineRenderer.endColor = Color.yellow;
        selectionLineRenderer.positionCount = 0;
        selectionLineRenderer.sortingOrder = 5;
        Debug.Log("Connect A line");
    }
    
    #endregion
    
    #region Input Handling
    
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnTouchDown(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            OnTouchMove(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnTouchUp();
        }
    }
    
    void OnTouchDown(Vector2 screenPos)
    {
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        GamePoint point = GetPointAtPosition(worldPos);
        
        
        if (point != null && point.CanInteract())
        {
            isDragging = true;
            AddPointToSelection(point);
        }
    }
    
    void OnTouchMove(Vector2 screenPos)
    {
        if (!isDragging) return;
        
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        GamePoint point = GetPointAtPosition(worldPos);
        
        if (point != null && point.CanInteract())
        {
            // Cho phép chọn lại điểm đầu tiên NẾU đã có >= 3 điểm (đóng polygon)
            bool isClosingMove = (selectedPointIds.Count >= 3) && (point.pointId == selectedPointIds[0]);
            
            if (isClosingMove || !selectedPointIds.Contains(point.pointId))
            {
                AddPointToSelection(point);
            }
        }
    }
    
    void OnTouchUp()
    {
        isDragging = false;
        ValidateAndCompleteSelection();
    }
    
    GamePoint GetPointAtPosition(Vector2 worldPos)
    {
        float radiusSq = swipeDetectionRadius * swipeDetectionRadius;
        
        // Return nearest point within radius (avoid ambiguous selection when multiple points overlap)
        GamePoint nearest = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < pointsArray.Length; i++)
        {
            var point = pointsArray[i];
            Vector2 pointPos = point.transform.position;
            float dx = pointPos.x - worldPos.x;
            float dy = pointPos.y - worldPos.y;
            float distSq = dx * dx + dy * dy;

            if (distSq < radiusSq && distSq < bestDistSq)
            {
                nearest = point;
                bestDistSq = distSq;
            }
        }

        return nearest;
    }
    
    #endregion
    
    #region Selection Logic - OPTIMIZED WITH EDGE VALIDATION
    
    void AddPointToSelection(GamePoint point)
    {
        // ĐIỂM ĐẦU TIÊN
        if (selectedPointIds.Count == 0)
        {
            possiblePolygons.Clear();
            Debug.Log($"[AddPoint] ĐIỂM ĐẦU TIÊN: Point {point.pointId}, có {point.remainingPolygons.Count} polygons");
            for (int i = 0; i < point.remainingPolygons.Count; i++)
            {
                possiblePolygons.Add(point.remainingPolygons[i]);
                Debug.Log($"[AddPoint] Thêm polygon {point.remainingPolygons[i]} vào possiblePolygons");
            }
            
            selectedPointIds.Add(point.pointId);
            selectedPoints.Add(point);
            point.SetState(PointState.Selected);
            lastAddedPoint = point;
            
            UpdateSelectionVisual();
            return;
        }
        
        // ĐIỂM THỨ 2 TRỞ ĐI - KIỂM TRA CẠNH
        int lastPointId = lastAddedPoint.pointId;
        int newPointId = point.pointId;
        
        // KIỂM TRA không phải cùng điểm
        if (lastPointId == newPointId)
        {
            Debug.LogWarning($"[AddPoint] ❌ Không thể nối điểm {lastPointId} với chính nó!");
            return;
        }
        
        // KIỂM TRA KHÔNG CHỌN LẠI ĐIỂM ĐÃ CHỌN (trừ khi đóng polygon)
        if (selectedPointIds.Contains(newPointId))
        {
            // Chỉ cho phép chọn lại điểm đầu tiên NẾU đủ điểm để đóng polygon
            bool isClosingPolygon = (newPointId == selectedPointIds[0]) && (selectedPointIds.Count >= 3);
            
            if (!isClosingPolygon)
            {
                Debug.LogWarning($"[AddPoint] ❌ Điểm {newPointId} đã được chọn rồi! Không thể chọn lại.");
                return; // Không clear selection, chỉ ignore
            }
            
            Debug.Log($"[AddPoint] ✓ Đóng polygon: Điểm {newPointId} là điểm đầu tiên, có {selectedPointIds.Count} điểm.");
        }
        
        
        // Kiểm tra xem 2 điểm có tạo thành cạnh hợp lệ không
        // KHÔNG được Clear() tempPolygonList vì out parameter trả về reference từ cache
        bool isValidEdge = currentLevel.IsValidEdge(lastPointId, newPointId, out tempPolygonList);
        
        string edgePolygonsStr = tempPolygonList != null ? "[" + string.Join(", ", tempPolygonList) + "]" : "null";
        Debug.Log($"[AddPoint] Edge ({lastPointId} -> {newPointId}): isValid={isValidEdge}, polygonCount={tempPolygonList?.Count ?? 0}, polygonIds={edgePolygonsStr}");
        
        if (!isValidEdge)
        {
            // Không phải cạnh hợp lệ -> Clear selection
            Debug.Log("[AddPoint] Invalid edge, clearing selection");
            ClearSelection();
            return;
        }
        
        // Kiểm tra cạnh có thuộc một trong các possiblePolygons không
        bool foundCommonPolygon = false;
        
        // Log possiblePolygons trước khi kiểm tra
        string possibleStr = "possiblePolygons: [";
        foreach (var pid in possiblePolygons)
        {
            possibleStr += pid + ", ";
        }
        possibleStr += "]";
        Debug.Log($"[AddPoint] TRƯỚC IntersectWith: {possibleStr}");
        
        Debug.Log($"[AddPoint] Checking {tempPolygonList.Count} polygons against {possiblePolygons.Count} possible polygons");
        for (int i = 0; i < tempPolygonList.Count; i++)
        {
            Debug.Log($"[AddPoint] Edge polygon[{i}] = {tempPolygonList[i]}, inPossible={possiblePolygons.Contains(tempPolygonList[i])}");
            if (possiblePolygons.Contains(tempPolygonList[i]))
            {
                foundCommonPolygon = true;
            }
        }
        
        if (!foundCommonPolygon)
        {
            // Cạnh không thuộc đa giác nào đang có thể -> Clear
            Debug.LogWarning($"[AddPoint] ❌ Không tìm thấy polygon chung! Clearing selection");
            ClearSelection();
            return;
        }
        
        // Cập nhật possiblePolygons (giao với các đa giác chứa cạnh này)
        possiblePolygons.IntersectWith(tempPolygonList);
        
        // Log possiblePolygons sau IntersectWith
        possibleStr = "possiblePolygons SAU IntersectWith: [";
        foreach (var pid in possiblePolygons)
        {
            possibleStr += pid + ", ";
        }
        possibleStr += "]";
        Debug.Log($"[AddPoint] {possibleStr}, count={possiblePolygons.Count}");
        
        if (possiblePolygons.Count == 0)
        {
            Debug.LogWarning($"[AddPoint] ❌ possiblePolygons rỗng sau IntersectWith! Clearing selection");
            ClearSelection();
            return;
        }
        
        // Thêm điểm vào selection
        selectedPointIds.Add(point.pointId);
        selectedPoints.Add(point);
        point.SetState(PointState.Selected);
        lastAddedPoint = point;
        
        UpdateSelectionVisual();
    }
    
    void ClearSelection()
    {
        // Dùng for thay vì foreach
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            var point = selectedPoints[i];
            if (point.currentState == PointState.Selected)
            {
                point.SetState(PointState.Idle);
            }
        }

        Debug.Log("Clear Collection");
        selectedPointIds.Clear();
        selectedPoints.Clear();
        possiblePolygons.Clear();
        lastAddedPoint = null;
        
        UpdateSelectionVisual();
    }
    
    void ValidateAndCompleteSelection()
    {
        if (selectedPointIds.Count == 0) return;
        
        Debug.Log($"[ValidateAndCompleteSelection] Checking {selectedPointIds.Count} selected points against {possiblePolygons.Count} possible polygons");
        
        // Kiểm tra từng polygon trong possiblePolygons
        foreach (var polygonId in possiblePolygons)
        {
            if (polygonsDict.TryGetValue(polygonId, out var polygon))
            {
                Debug.Log($"[ValidateAndCompleteSelection] Checking polygon {polygonId}, isCompleted={polygon.isCompleted}");
                
                if (polygon.isCompleted) continue;
                
                // Kiểm tra selection có match với polygon không
                bool isValid = IsValidPolygonCompletion(polygon);
                Debug.Log($"[ValidateAndCompleteSelection] Polygon {polygonId} isValid={isValid}");
                
                if (isValid)
                {
                    CompletePolygon(polygon);
                    return;
                }
            }
        }
        
        // Không match -> clear
        Debug.LogWarning($"[ValidateAndCompleteSelection] ❌ Không tìm thấy polygon hợp lệ! Clearing selection");
        ClearSelection();
    }
    
    bool IsValidPolygonCompletion(GamePolygon polygon)
    {
        int selectedCount = selectedPointIds.Count;
        int polygonCount = polygon.pointIds.Count;
        
        // BẮT BUỘC phải đóng vòng (điểm cuối = điểm đầu)
        bool isClosedLoop = (selectedCount > polygonCount) && 
                            (selectedPointIds[0] == selectedPointIds[selectedCount - 1]);
        
        if (!isClosedLoop)
        {
            // Không đóng vòng -> Không cho phép hoàn thành
            Debug.Log($"[IsValidPolygonCompletion] ❌ Chưa đóng vòng! selected={selectedCount}, polygon={polygonCount}, firstPoint={selectedPointIds[0]}, lastPoint={selectedPointIds[selectedCount - 1]}");
            return false;
        }
        
        // Trường hợp A→B→C→A: selectedCount=4, polygonCount=3
        // Bỏ điểm cuối ra để so sánh
        Debug.Log($"[IsValidPolygonCompletion] ✓ Detected closed loop: selected={selectedCount}, polygon={polygonCount}");
        
        if (selectedCount - 1 != polygonCount)
        {
            Debug.Log($"[IsValidPolygonCompletion] ❌ Số điểm không khớp: selectedCount-1={selectedCount - 1}, polygonCount={polygonCount}");
            return false;
        }
        
        // Check thứ tự (bỏ điểm cuối)
        bool isValidSequence = IsValidSequence(polygon.pointIds, selectedPointIds, polygonCount) ||
                               IsValidSequenceReversed(polygon.pointIds, selectedPointIds, polygonCount);
        
        Debug.Log($"[IsValidPolygonCompletion] isValidSequence={isValidSequence}");
        return isValidSequence;
    }
    
    bool IsValidSequence(List<int> polygonPoints, List<int> selectedPoints, int compareCount)
    {
        int n = polygonPoints.Count;
        
        // Tìm vị trí điểm đầu tiên trong polygon
        int startIdx = -1;
        for (int i = 0; i < n; i++)
        {
            if (polygonPoints[i] == selectedPoints[0])
            {
                startIdx = i;
                break;
            }
        }
        
        if (startIdx == -1) return false;
        
        // Check thứ tự theo chiều thuận (chỉ so sánh compareCount điểm)
        for (int i = 0; i < compareCount; i++)
        {
            int polygonIdx = (startIdx + i) % n;
            if (polygonPoints[polygonIdx] != selectedPoints[i])
            {
                return false;
            }
        }
        
        return true;
    }
    
    bool IsValidSequenceReversed(List<int> polygonPoints, List<int> selectedPoints, int compareCount)
    {
        int n = polygonPoints.Count;
        
        int startIdx = -1;
        for (int i = 0; i < n; i++)
        {
            if (polygonPoints[i] == selectedPoints[0])
            {
                startIdx = i;
                break;
            }
        }
        
        if (startIdx == -1) return false;
        
        // Check thứ tự ngược lại (chỉ so sánh compareCount điểm)
        for (int i = 0; i < compareCount; i++)
        {
            int polygonIdx = (startIdx - i + n) % n;
            if (polygonPoints[polygonIdx] != selectedPoints[i])
            {
                return false;
            }
        }
        
        return true;
    }
    
    void CompletePolygon(GamePolygon polygon)
    {
        Debug.Log($"[CompletePolygon] ✓ THÀNH CÔNG! PolygonId={polygon.polygonId}, Điểm đã nối={selectedPoints.Count}");
        
        // Log chi tiết các điểm đã nối
        string pointsLog = "Thứ tự nối: ";
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            pointsLog += selectedPoints[i].pointId;
            if (i < selectedPoints.Count - 1) pointsLog += " → ";
        }
        Debug.Log($"[CompletePolygon] {pointsLog}");
        
        // Lấy vị trí theo thứ tự đã nối
        var pointPositions = new List<Vector2>(selectedPoints.Count);
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            pointPositions.Add(selectedPoints[i].transform.position);
        }
        
        polygon.Complete(pointPositions);
        
        Debug.Log($"[CompletePolygon] Đã fill polygon {polygon.polygonId}");
        
        // Cập nhật trạng thái điểm
        for (int i = 0; i < polygon.pointIds.Count; i++)
        {
            if (pointsDict.TryGetValue(polygon.pointIds[i], out var point))
            {
                point.RemovePolygon(polygon.polygonId);
                point.SetState(point.CanInteract() ? PointState.Idle : PointState.Done);
            }
        }
        
        selectedPointIds.Clear();
        selectedPoints.Clear();
        possiblePolygons.Clear();
        lastAddedPoint = null;
        
        UpdateSelectionVisual();
        CheckLevelComplete();
    }
    
    void CheckLevelComplete()
    {
        // Tránh dùng LINQ
        bool allCompleted = true;
        foreach (var polygon in polygonsDict.Values)
        {
            if (!polygon.isCompleted)
            {
                allCompleted = false;
                break;
            }
        }
        
        if (allCompleted)
        {
            Debug.Log("Level Complete!");
        }
    }
    
    #endregion
    
    #region Visual Feedback
    
    void UpdateSelectionVisual()
    {
        if (selectionLineRenderer == null) return;
        
        
        if (selectedPoints.Count < 2)
        {
            selectionLineRenderer.positionCount = 0;
            return;
        }
        
        selectionLineRenderer.positionCount = selectedPoints.Count;
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            selectionLineRenderer.SetPosition(i, selectedPoints[i].transform.position);
        }
        
    }
    
    #endregion
}