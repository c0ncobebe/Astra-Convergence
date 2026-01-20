using System.Collections.Generic;
using _MyGame._Scripts;
using AstraNexus.Audio;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class GamePlayManager : MonoBehaviour
{
    [Header("References")]
    public LevelData currentLevel;
    public GameObject pointPrefab;
    public GameObject polygonPrefab;
    public GameObject linePrefab;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private PolygonMeshRenderer test;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private Transform levelContainer;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject levelCompletePanel;
    
    [Header("Settings")]
    public float swipeDetectionRadius = 0.5f;
    public bool showDebugLines = true;
    
    private GamePoint[] pointsArray;
    private Dictionary<int, GamePoint> pointsDict;
    private Dictionary<int, GamePolygon> polygonsDict;
    private List<int> selectedPointIds = new (10);
    private List<GamePoint> selectedPoints = new (10);
    private HashSet<int> possiblePolygons = new ();
    private List<GameObject> createdLines = new (10);
    private HashSet<Edge> completedEdges = new ();
    private HashSet<Edge> currentSelectionEdges = new ();
    private List<int> tempPolygonList = new (2);
    private bool isDragging = false;
    private bool isClickMode = false;
    private bool ignoreInputUntilRelease = false;
    private bool isLevelInitialized = false;
    private Camera mainCamera;
    private GamePoint lastAddedPoint = null;
    private LineRenderer selectionLineRenderer;
    private Vector2 currentCursorPosition;

    [Button]
    public void Test()
    {
        test.BuildPolygon(spawnPoints,Color.deepPink);
        
    }

    void Start()
    {
        mainCamera = Camera.main;
        SetupSelectionLineRenderer();
        
        if (inputManager == null)
            inputManager = FindObjectOfType<InputManager>();
    }
    
    public void LoadLevel(LevelData levelData)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        ClearLevel();
        currentLevel = levelData;
        InitializeLevel();
        
        if (backButton != null)
            backButton.SetActive(true);
        
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
        
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayRandomIngameMusic();
    }
    
    public void ClearLevel()
    {
        if (levelContainer != null)
        {
            for (int i = levelContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(levelContainer.GetChild(i).gameObject);
            }
        }
        
        if (pointsDict != null)
        {
            pointsDict.Clear();
        }
        
        if (polygonsDict != null)
        {
            polygonsDict.Clear();
        }
        
        DestroyCreatedLines();
        
        // Clear selection without triggering ignore flag
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            var point = selectedPoints[i];
            if (point != null && point.currentState == PointState.Selected)
            {
                point.SetState(PointState.Idle);
            }
        }
        selectedPointIds.Clear();
        selectedPoints.Clear();
        possiblePolygons.Clear();
        currentSelectionEdges.Clear();
        lastAddedPoint = null;
        
        completedEdges.Clear();
        
        // Reset all flags
        isLevelInitialized = false;
        isDragging = false;
        isClickMode = false;
        ignoreInputUntilRelease = false;
        
        UpdateSelectionVisual();
    }
    
    void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnHoldStart.AddListener(OnTouchDown);
            inputManager.OnHoldUpdate.AddListener(OnTouchMove);
            inputManager.OnHoldEnd.AddListener(OnTouchUp);
            inputManager.OnTap.AddListener(OnClick);
        }
    }
    
    void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnHoldStart.RemoveListener(OnTouchDown);
            inputManager.OnHoldUpdate.RemoveListener(OnTouchMove);
            inputManager.OnHoldEnd.RemoveListener(OnTouchUp);
            inputManager.OnTap.RemoveListener(OnClick);
        }
    }
    
    void Update()
    {
        if (isDragging)
            UpdateSelectionVisual();
    }
    
    void InitializeLevel()
    {
        if (isLevelInitialized)
        {
            Debug.LogWarning("[GamePlayManager] InitializeLevel() called but level already initialized. Skipping to prevent duplicates.");
            return;
        }
        
        currentLevel.Initialize();
        
        int pointCount = currentLevel.points.Count;
        
        pointsArray = new GamePoint[pointCount];
        pointsDict = new Dictionary<int, GamePoint>(pointCount);
        polygonsDict = new Dictionary<int, GamePolygon>(currentLevel.polygons.Count);
        
        Debug.Log($"[GamePlayManager] Spawning {pointCount} points...");
        
        for (int i = 0; i < pointCount; i++)
        {
            var pointData = currentLevel.points[i];
            var pointObj = Instantiate(pointPrefab, levelContainer);
            var gamePoint = pointObj.GetComponent<GamePoint>();
            gamePoint.Initialize(pointData);
            pointsArray[i] = gamePoint;
            pointsDict[pointData.pointId] = gamePoint;
        }
        
        Debug.Log($"[GamePlayManager] Spawning {currentLevel.polygons.Count} polygons...");
        
        for (int i = 0; i < currentLevel.polygons.Count; i++)
        {
            var polygonData = currentLevel.polygons[i];
            var polygonObj = Instantiate(polygonPrefab, levelContainer);
            var gamePolygon = polygonObj.GetComponent<GamePolygon>();
            gamePolygon.Initialize(polygonData);
            polygonsDict[polygonData.polygonId] = gamePolygon;
        }
        
        SetupCameraBounds();
        
        // Set flag sau khi initialize xong
        isLevelInitialized = true;
        Debug.Log("[GamePlayManager] Level initialization complete.");
    }
    
    void SetupCameraBounds()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        CameraController camController = mainCamera.GetComponent<CameraController>();
        if (camController == null)
            return;
        
        float maxOrthoSize = 12f;
        float aspect = mainCamera.aspect;
        
        float viewHeight = maxOrthoSize * 2f;
        float viewWidth = viewHeight * aspect;
        
        float paddingX = viewWidth * 0.2f;
        float paddingY = viewHeight * 0.2f;
        
        float halfWidth = (viewWidth ) * 0.5f;
        float halfHeight = (viewHeight ) * 0.5f;
        
        Rect cameraBounds = new Rect(
            -halfWidth,
            -halfHeight,
            halfWidth * 2f,
            halfHeight * 2f
        );
        
        camController.SetPanBounds(cameraBounds, true);
    }
    
    void SetupSelectionLineRenderer()
    {
        var obj = new GameObject("SelectionLine");
        selectionLineRenderer = obj.AddComponent<LineRenderer>();
        
        selectionLineRenderer.useWorldSpace = true;
        selectionLineRenderer.alignment = LineAlignment.TransformZ;
        selectionLineRenderer.startWidth = 0.05f;
        selectionLineRenderer.endWidth = 0.05f;
        selectionLineRenderer.widthMultiplier = 1f;
        
        selectionLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        selectionLineRenderer.startColor = new Color(1,0.92f,0.7f,0.5f);
        selectionLineRenderer.endColor = new Color(1,0.92f,0.7f,0.5f);
        
        selectionLineRenderer.sortingOrder = 5;
        selectionLineRenderer.positionCount = 0;
        
        obj.transform.position = new Vector3(0, 0, 0);
    }
    
    void OnTouchDown(Vector2 screenPos)
    {
        if (ignoreInputUntilRelease)
            return;
        
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        currentCursorPosition = worldPos;
        GamePoint point = GetPointAtPosition(worldPos);
        
        if (point != null && point.CanInteract())
        {
            isDragging = true;
            isClickMode = false;
            AddPointToSelection(point);
        }
    }
    
    void OnTouchMove(Vector2 screenPos)
    {
        if (ignoreInputUntilRelease)
            return;
        
        if (!isDragging) return;
        
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        currentCursorPosition = worldPos;
        
        GamePoint point = GetPointAtPosition(worldPos);
        
        if (point != null && point.CanInteract())
        {
            bool isClosingMove = (selectedPointIds.Count >= 3) && (point.pointId == selectedPointIds[0]);
            
            if (isClosingMove || !selectedPointIds.Contains(point.pointId))
            {
                AddPointToSelection(point);
            }
        }
    }
    
    void OnTouchUp(Vector2 screenPos)
    {
        if (ignoreInputUntilRelease)
        {
            Debug.Log("[OnTouchUp] Resetting ignoreInputUntilRelease flag");
        }
        
        if (!ignoreInputUntilRelease && isDragging && !isClickMode)
        {
            ValidateAndCompleteSelection();
        }
        
        isDragging = false;
        ignoreInputUntilRelease = false;
    }
    
    void OnClick(Vector2 screenPos)
    {
        if (ignoreInputUntilRelease)
        {
            Debug.Log("[OnClick] Input ignored - waiting for release");
            return;
        }
        
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        GamePoint point = GetPointAtPosition(worldPos);
        
        if (point != null && point.CanInteract())
        {
            isClickMode = true;
            
            if (selectedPointIds.Count >= 3 && point.pointId == selectedPointIds[0])
            {
                AddPointToSelection(point);
                ValidateAndCompleteSelection();
                isClickMode = false;
            }
            else
            {
                AddPointToSelection(point);
            }
        }
        else if (selectedPointIds.Count > 0)
        {
            ClearSelection();
            isClickMode = false;
        }
    }
    
    GamePoint GetPointAtPosition(Vector2 worldPos)
    {
        float radiusSq = swipeDetectionRadius * swipeDetectionRadius;
        
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
    
    void AddPointToSelection(GamePoint point)
    {
        if (lastAddedPoint != null)
        {
            lastAddedPoint.ResetGlow();
        }
        
        point.Animating();
        
        if (selectedPointIds.Count == 0)
        {
            possiblePolygons.Clear();
            for (int i = 0; i < point.remainingPolygons.Count; i++)
            {
                possiblePolygons.Add(point.remainingPolygons[i]);
            }
            
            selectedPointIds.Add(point.pointId);
            selectedPoints.Add(point);
            point.SetState(PointState.Selected);
            lastAddedPoint = point;
            
            UpdateSelectionVisual();
            return;
        }
        
        int lastPointId = lastAddedPoint.pointId;
        int newPointId = point.pointId;
        
        if (lastPointId == newPointId)
            return;
        
        if (selectedPointIds.Contains(newPointId))
        {
            bool isClosingPolygon = (newPointId == selectedPointIds[0]) && (selectedPointIds.Count >= 3);
            
            if (!isClosingPolygon)
                return;
        }
        
        bool isValidEdge = currentLevel.IsValidEdge(lastPointId, newPointId, out tempPolygonList);
        
        if (!isValidEdge || tempPolygonList == null || tempPolygonList.Count == 0)
        {
            point.ShowErrorGlow();
            ClearSelection(true);
            return;
        }
        
        bool foundCommonPolygon = false;
        
        for (int i = 0; i < tempPolygonList.Count; i++)
        {
            if (possiblePolygons.Contains(tempPolygonList[i]))
            {
                foundCommonPolygon = true;
            }
        }
        
        if (!foundCommonPolygon)
        {
            point.ShowErrorGlow();
            ClearSelection(true);
            return;
        }
        
        possiblePolygons.IntersectWith(tempPolygonList);
        
        if (possiblePolygons.Count == 0)
        {
            point.ShowErrorGlow();
            ClearSelection(true);
            return;
        }
        
        selectedPointIds.Add(point.pointId);
        selectedPoints.Add(point);
        point.SetState(PointState.Selected);
        
        if (lastAddedPoint != null)
        {
            Edge newEdge = new Edge(lastAddedPoint.pointId, point.pointId);
            
            if (!currentSelectionEdges.Contains(newEdge))
            {
                currentSelectionEdges.Add(newEdge);
            }
            
            bool isEdgeAlreadyCompleted = completedEdges.Contains(newEdge);
            if (!isEdgeAlreadyCompleted && linePrefab != null)
            {
                CreateLineBetweenPoints(lastAddedPoint, point);
            }
        }
        
        lastAddedPoint = point;
        
        CheckAndCompletePolygon();
        
        UpdateSelectionVisual();
    }
    
    void ClearSelection(bool isError = false)
    {
        // Chỉ set ignore flag nếu đang trong quá trình drag (có tương tác thực sự)
        bool shouldIgnoreUntilRelease = isDragging && !isClickMode;
        
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            var point = selectedPoints[i];
            if (point.currentState == PointState.Selected)
            {
                point.SetState(PointState.Idle);
                
                if (isError)
                {
                    point.ShowErrorGlow();
                }
                else
                {
                    point.ResetGlow();
                }
            }
        }

        selectedPointIds.Clear();
        selectedPoints.Clear();
        possiblePolygons.Clear();
        currentSelectionEdges.Clear();
        lastAddedPoint = null;
        
        DestroyCreatedLines();
        
        UpdateSelectionVisual();
        
        if (shouldIgnoreUntilRelease)
        {
            Debug.Log("[ClearSelection] Setting ignoreInputUntilRelease = true due to failed drag interaction");
            ignoreInputUntilRelease = true;
        }
    }
    
    void CheckAndCompletePolygon()
    {
        if (selectedPointIds.Count == 0) return;
        
        foreach (var polygonId in possiblePolygons)
        {
            if (polygonsDict.TryGetValue(polygonId, out var polygon))
            {
                if (polygon.isCompleted) continue;
                
                bool isComplete = polygon.HasAllEdgesCompleted();
                
                if (!isComplete)
                {
                    isComplete = IsValidPolygonCompletion(polygon);
                }
                
                if (isComplete)
                {
                    CompletePolygon(polygon);
                    return;
                }
            }
        }
    }
    
    void ValidateAndCompleteSelection()
    {
        if (selectedPointIds.Count == 0) return;
        
        foreach (var polygonId in possiblePolygons)
        {
            if (polygonsDict.TryGetValue(polygonId, out var polygon))
            {
                if (polygon.isCompleted) continue;
                
                bool isValid = IsValidPolygonCompletion(polygon);
                
                if (isValid)
                {
                    CompletePolygon(polygon);
                    return;
                }
            }
        }
        
        ClearSelection();
    }
    
    bool IsValidPolygonCompletion(GamePolygon polygon)
    {
        int totalEdges = polygon.edges.Count;
        int completedCount = 0;
        
        foreach (var edge in polygon.edges)
        {
            bool isCompleted = polygon.IsEdgeCompleted(edge) || currentSelectionEdges.Contains(edge);
            
            if (isCompleted)
            {
                completedCount++;
            }
        }
        
        bool isComplete = (completedCount == totalEdges);
        
        return isComplete;
    }
    
    void CompletePolygon(GamePolygon polygon)
    {
        var pointPositions = new List<Vector2>(selectedPoints.Count);
        var pointTransforms = new List<Transform>(selectedPoints.Count);
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            pointPositions.Add(selectedPoints[i].transform.position);
            pointTransforms.Add(selectedPoints[i].transform);
        }
        
        polygon.Complete(pointPositions, pointTransforms, polygon.color);
        
        foreach (var edge in currentSelectionEdges)
        {
            foreach (var polyDict in polygonsDict.Values)
            {
                if (polyDict.edges.Contains(edge))
                {
                    polyDict.AddCompletedEdge(edge);
                }
            }
            
            completedEdges.Add(edge);
        }
        
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
        currentSelectionEdges.Clear();
        lastAddedPoint = null;
        
        createdLines.Clear();
        
        UpdateSelectionVisual();
        CheckLevelComplete();
        
        if (!isClickMode)
        {
            ignoreInputUntilRelease = true;
        }
    }
    
    void CheckLevelComplete()
    {
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
            OnLevelComplete();
        }
    }
    
    private void OnLevelComplete()
    {
        int currentLevelIndex = LevelProgressManager.Instance.GetCurrentLevel();
        LevelProgressManager.Instance.CompleteLevel(currentLevelIndex, 3);
        
        if (backButton != null)
            backButton.SetActive(false);
        
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);
        
        // StartCoroutine(ReturnToHomeMenuAfterDelay(2f));
    }
    
    private System.Collections.IEnumerator ReturnToHomeMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SwitchToHomeMenu();
        }
    }
    
    void CreateLineBetweenPoints(GamePoint point1, GamePoint point2)
    {
        GameObject lineObj = Instantiate(linePrefab, levelContainer);
        
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
            Vector3 midPoint = (point1.transform.position + point2.transform.position) * 0.5f;
            lineObj.transform.position = midPoint;
            
            Vector3 direction = point2.transform.position - point1.transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            lineObj.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            float distance = Vector3.Distance(point1.transform.position, point2.transform.position);
            lineObj.transform.localScale = new Vector3(distance, lineObj.transform.localScale.y, lineObj.transform.localScale.z);
        }
        
        createdLines.Add(lineObj);
    }
    
    void DestroyCreatedLines()
    {
        for (int i = 0; i < createdLines.Count; i++)
        {
            if (createdLines[i] != null)
            {
                Destroy(createdLines[i]);
            }
        }
        
        createdLines.Clear();
    }
    
    void UpdateSelectionVisual()
    {
        if (selectionLineRenderer == null) return;
        if (selectedPoints.Count == 0)
        {
            selectionLineRenderer.positionCount = 0;
            return;
        }
        
        if (isDragging && selectedPoints.Count > 0)
        {
            selectionLineRenderer.positionCount = 2;
            
            Vector3 lastPointPos = selectedPoints[selectedPoints.Count - 1].transform.position;
            lastPointPos.z = 0;
            selectionLineRenderer.SetPosition(0, lastPointPos);
            
            Vector3 cursorPos = currentCursorPosition;
            cursorPos.z = 0;
            selectionLineRenderer.SetPosition(1, cursorPos);
        }
        else
        {
            selectionLineRenderer.positionCount = 0;
        }
    }
}
