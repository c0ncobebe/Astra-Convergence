using System.Collections.Generic;
using UnityEngine;

public class GamePlayManager : MonoBehaviour
{
    [Header("References")]
    public LevelData currentLevel;
    public GameObject pointPrefab;
    public GameObject polygonPrefab;
    public GameObject linePrefab; // Prefab cho line giữa các điểm hợp lệ
    [SerializeField] private InputManager inputManager;
    
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
    private List<GameObject> createdLines = new (10); // Track các line đã tạo trong selection hiện tại
    
    // Edge validation cache
    private List<int> tempPolygonList = new (2); // Reuse để tránh allocation
    
    // Input tracking
    private bool isDragging = false;
    private bool isClickMode = false; // True khi đang sử dụng click mode (click từng điểm)
    private bool ignoreInputUntilRelease = false; // Ignore input sau khi clear/complete cho đến khi touch up
    private Camera mainCamera;
    private GamePoint lastAddedPoint = null;
    
    // Visual feedback
    private LineRenderer selectionLineRenderer;
    private Vector2 currentCursorPosition;
    
    void Start()
    {
        mainCamera = Camera.main;
        SetupSelectionLineRenderer();
        InitializeLevel();
        
        // Tự động tìm InputManager nếu chưa set
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<InputManager>();
        }
    }
    
    void OnEnable()
    {
        if (inputManager != null)
        {
            // Subscribe input events cho nối điểm (drag mode)
            inputManager.OnHoldStart.AddListener(OnTouchDown);
            inputManager.OnHoldUpdate.AddListener(OnTouchMove);
            inputManager.OnHoldEnd.AddListener(OnTouchUp);
            
            // Subscribe input event cho click mode
            inputManager.OnTap.AddListener(OnClick);
        }
    }
    
    void OnDisable()
    {
        if (inputManager != null)
        {
            // Unsubscribe input events
            inputManager.OnHoldStart.RemoveListener(OnTouchDown);
            inputManager.OnHoldUpdate.RemoveListener(OnTouchMove);
            inputManager.OnHoldEnd.RemoveListener(OnTouchUp);
            inputManager.OnTap.RemoveListener(OnClick);
        }
    }
    
    void Update()
    {
        // Input được xử lý qua InputManager events
        // Chỉ update visual feedback ở đây nếu cần
        if (isDragging)
        {
            UpdateSelectionVisual();
        }
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
        
        // Setup camera bounds dựa trên level size
        SetupCameraBounds();
    }
    
    void SetupCameraBounds()
    {
        // Tìm CameraController
        CameraController camController = Camera.main?.GetComponent<CameraController>();
        if (camController == null)
        {
            return;
        }
        
        // Tính bounds cố định dựa trên ortho size 12 (max zoom out)
        float maxOrthoSize = 12f;
        float aspect = mainCamera.aspect;
        
        // Kích thước view tại ortho 12
        float viewHeight = maxOrthoSize * 2f;
        float viewWidth = viewHeight * aspect;
        
        // Thêm padding 20% mỗi phía
        float paddingX = viewWidth * 0.2f;
        float paddingY = viewHeight * 0.2f;
        
        // Bounds centered tại (0, 0) với padding
        float halfWidth = (viewWidth ) * 0.5f;
        float halfHeight = (viewHeight ) * 0.5f;
        
        Rect cameraBounds = new Rect(
            -halfWidth,
            -halfHeight,
            halfWidth * 2f,
            halfHeight * 2f
        );
        
        // Set bounds cho camera
        camController.SetPanBounds(cameraBounds, true);
        
        Debug.Log($"[GamePlayManager] Camera bounds set: {cameraBounds} (Based on ortho 12, aspect {aspect:F2}, padding 20%)");
    }
    
    void SetupSelectionLineRenderer()
    {
        var obj = new GameObject("SelectionLine");
        selectionLineRenderer = obj.AddComponent<LineRenderer>();
        
        // Setup cho game 2D - width cố định
        selectionLineRenderer.useWorldSpace = true;
        selectionLineRenderer.alignment = LineAlignment.TransformZ; // Luôn hướng về camera 2D
        selectionLineRenderer.startWidth = 0.05f;
        selectionLineRenderer.endWidth = 0.05f;
        selectionLineRenderer.widthMultiplier = 1f; // Đảm bảo width không bị scale
        
        // Material và color
        selectionLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        selectionLineRenderer.startColor = Color.yellow;
        selectionLineRenderer.endColor = Color.yellow;
        
        // Setup sorting
        selectionLineRenderer.sortingOrder = 5;
        selectionLineRenderer.positionCount = 0;
        
        // Đảm bảo line ở cùng mặt phẳng Z với các điểm (cho 2D)
        obj.transform.position = new Vector3(0, 0, 0);
    }
    
    #endregion
    
    #region Input Handling (Event-based)
    
    void OnTouchDown(Vector2 screenPos)
    {
        // Nếu đang ignore input, không xử lý gì
        if (ignoreInputUntilRelease)
        {
            return;
        }
        
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        currentCursorPosition = worldPos;
        GamePoint point = GetPointAtPosition(worldPos);
        
        
        if (point != null && point.CanInteract())
        {
            isDragging = true;
            isClickMode = false; // Khi bắt đầu drag, thoát khỏi click mode
            AddPointToSelection(point);
        }
    }
    
    void OnTouchMove(Vector2 screenPos)
    {
        // Nếu đang ignore input, không xử lý gì
        if (ignoreInputUntilRelease)
        {
            return;
        }
        
        if (!isDragging) return;
        
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        currentCursorPosition = worldPos;
        
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
    
    void OnTouchUp(Vector2 screenPos)
    {
        isDragging = false;
        
        // Nếu không đang ignore và không phải click mode, validate và complete
        if (!ignoreInputUntilRelease && !isClickMode)
        {
            ValidateAndCompleteSelection();
        }
        
        // Reset ignore flag khi user thả tay ra (nhưng giữ click mode nếu đang active)
        ignoreInputUntilRelease = false;
    }
    
    void OnClick(Vector2 screenPos)
    {
        // Nếu đang ignore input, không xử lý gì
        if (ignoreInputUntilRelease)
        {
            return;
        }
        
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        GamePoint point = GetPointAtPosition(worldPos);
        
        if (point != null && point.CanInteract())
        {
            // Bật click mode khi click điểm đầu tiên
            isClickMode = true;
            
            // Nếu click vào điểm đầu tiên và đã có >= 3 điểm -> hoàn thành polygon
            if (selectedPointIds.Count >= 3 && point.pointId == selectedPointIds[0])
            {
                // Thêm điểm đầu vào cuối để đóng polygon
                AddPointToSelection(point);
                ValidateAndCompleteSelection();
                isClickMode = false; // Thoát click mode sau khi complete
            }
            else
            {
                // Click điểm mới -> thêm vào selection
                AddPointToSelection(point);
            }
        }
        else if (selectedPointIds.Count > 0)
        {
            // Click vào vùng trống khi đang có selection -> clear selection
            ClearSelection();
            isClickMode = false;
        }
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
        
        if (!isValidEdge || tempPolygonList == null || tempPolygonList.Count == 0)
        {
            // Không phải cạnh hợp lệ hoặc không thuộc polygon nào -> Clear selection
            Debug.LogWarning($"[AddPoint] ❌ Edge không hợp lệ hoặc không thuộc polygon nào! Clearing selection");
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
            Debug.LogWarning($"[AddPoint] ❌ Không tìm thấy polygon chung giữa edge và possiblePolygons! Clearing selection");
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
        
        // Tạo line giữa lastAddedPoint và point hiện tại (nếu có 2 điểm trở lên)
        if (lastAddedPoint != null && linePrefab != null)
        {
            CreateLineBetweenPoints(lastAddedPoint, point);
        }
        
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
        
        // Destroy tất cả các line đã tạo (vì không hợp lệ)
        DestroyCreatedLines();
        
        UpdateSelectionVisual();
        
        // Trong click mode: không ignore input (cho phép click tiếp)
        // Trong drag mode: ignore input cho đến khi thả tay
        if (!isClickMode)
        {
            ignoreInputUntilRelease = true;
        }
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
        
        // Giữ các line (không destroy) vì polygon hợp lệ
        // Chỉ clear list tracking
        createdLines.Clear();
        
        UpdateSelectionVisual();
        CheckLevelComplete();
        
        // Trong click mode: không ignore input (cho phép click polygon tiếp theo)
        // Trong drag mode: ignore input cho đến khi thả tay
        if (!isClickMode)
        {
            ignoreInputUntilRelease = true;
        }
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
    
    #region Line Visual Management
    
    void CreateLineBetweenPoints(GamePoint point1, GamePoint point2)
    {
        if (linePrefab == null)
        {
            Debug.LogWarning("[GamePlayManager] linePrefab is null, cannot create line!");
            return;
        }
        
        // Instantiate line
        GameObject lineObj = Instantiate(linePrefab);
        
        // Setup line renderer nếu có
        LineRenderer lineRenderer = lineObj.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, point1.transform.position);
            lineRenderer.SetPosition(1, point2.transform.position);
            lineRenderer.useWorldSpace = true;
        }
        else
        {
            // Nếu không có LineRenderer, position line ở giữa 2 điểm
            Vector3 midPoint = (point1.transform.position + point2.transform.position) * 0.5f;
            lineObj.transform.position = midPoint;
            
            // Rotate line để hướng từ point1 đến point2
            Vector3 direction = point2.transform.position - point1.transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            lineObj.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Scale theo khoảng cách
            float distance = Vector3.Distance(point1.transform.position, point2.transform.position);
            lineObj.transform.localScale = new Vector3(distance, lineObj.transform.localScale.y, lineObj.transform.localScale.z);
        }
        
        // Track line
        createdLines.Add(lineObj);
        
        Debug.Log($"[GamePlayManager] Created line between point {point1.pointId} and {point2.pointId}");
    }
    
    void DestroyCreatedLines()
    {
        // Destroy tất cả các line đã tạo
        for (int i = 0; i < createdLines.Count; i++)
        {
            if (createdLines[i] != null)
            {
                Destroy(createdLines[i]);
            }
        }
        
        createdLines.Clear();
        Debug.Log($"[GamePlayManager] Destroyed all created lines");
    }
    
    #endregion
    
    #region Visual Feedback
    
    void UpdateSelectionVisual()
    {
        if (selectionLineRenderer == null) return;
        
        // Không có điểm nào được chọn
        if (selectedPoints.Count == 0)
        {
            selectionLineRenderer.positionCount = 0;
            return;
        }
        
        // Chỉ hiển thị line từ điểm cuối cùng đến cursor (khi đang drag)
        if (isDragging && selectedPoints.Count > 0)
        {
            // Chỉ 2 điểm: điểm cuối cùng + cursor
            selectionLineRenderer.positionCount = 2;
            
            // Điểm cuối cùng đã chọn
            Vector3 lastPointPos = selectedPoints[selectedPoints.Count - 1].transform.position;
            lastPointPos.z = 0;
            selectionLineRenderer.SetPosition(0, lastPointPos);
            
            // Cursor position (ghost line)
            Vector3 cursorPos = currentCursorPosition;
            cursorPos.z = 0;
            selectionLineRenderer.SetPosition(1, cursorPos);
        }
        else
        {
            // Không drag: không hiển thị line
            selectionLineRenderer.positionCount = 0;
        }
    }
    
    #endregion
}