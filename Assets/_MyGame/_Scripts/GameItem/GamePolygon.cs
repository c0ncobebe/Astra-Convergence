using System.Collections.Generic;
using _MyGame._Scripts;
using UnityEngine;

public class GamePolygon : MonoBehaviour
{
    public int polygonId;
    public List<int> pointIds;
    public List<Edge> edges;
    public int sideCount;
    public bool isCompleted = false;
    public Color32 color; // Màu sắc của polygon
    
    [Header("Number Display Settings")]
    [SerializeField] private bool useSprite = true; // True = dùng sprite, False = dùng text
    [SerializeField] private List<Sprite> numberSprites; // Sprite cho số 3, 4, 5, 6, 7, 8...
    [SerializeField] private int startNumber = 3; // Số bắt đầu (mặc định 3)
    [SerializeField] private PolygonMeshRenderer _meshRenderer; // Mesh renderer cho polygon
    
    // Lưu các cạnh đã được hoàn thành của polygon này
    private HashSet<Edge> completedEdges = new HashSet<Edge>();
    
    private LineRenderer lineRenderer;
    private TextMesh hintText;
    private SpriteRenderer numberSpriteRenderer;
    
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        hintText = GetComponentInChildren<TextMesh>();
        numberSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    
    public void Initialize(PolygonData data)
    {
        polygonId = data.polygonId;
        pointIds = new List<int>(data.pointIds);
        edges = new List<Edge>(data.edges);
        sideCount = data.sideCount;
        color = data.color; // Lưu màu sắc từ data
        completedEdges = new HashSet<Edge>();
        
        // Setup hiển thị số cạnh
        if (useSprite)
        {
            // Dùng Sprite
            if (numberSpriteRenderer != null)
            {
                SetNumberSprite(sideCount);
                numberSpriteRenderer.gameObject.SetActive(true);
            }
            
            // Tắt text nếu có
            if (hintText != null)
            {
                hintText.gameObject.SetActive(false);
            }
        }
        else
        {
            // Dùng TextMesh
            if (hintText != null)
            {
                hintText.text = sideCount.ToString();
                hintText.fontSize = 5;
                hintText.color = Color.white;
                hintText.anchor = TextAnchor.MiddleCenter;
                hintText.alignment = TextAlignment.Center;
                hintText.gameObject.SetActive(true);
            }
            
            // Tắt sprite nếu có
            if (numberSpriteRenderer != null)
            {
                numberSpriteRenderer.gameObject.SetActive(false);
            }
        }
        
        // Đặt position ở center của polygon
        transform.position = new Vector3(data.centerPosition.x, data.centerPosition.y, 0);
        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
        }
    }
    
    public void Complete(List<Vector2> pointPositions, List<Transform> pointTransforms, Color32 color)
    {
        isCompleted = true;
        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = pointPositions.Count + 1;
            for (int i = 0; i < pointPositions.Count; i++)
            {
                lineRenderer.SetPosition(i, pointPositions[i]);
            }
            lineRenderer.SetPosition(pointPositions.Count, pointPositions[0]);
        }
        
        // Gọi BuildPolygon để tạo mesh
        if (_meshRenderer != null && pointTransforms != null && pointTransforms.Count >= 3)
        {
            _meshRenderer.BuildPolygon(pointTransforms, color);
            Debug.Log($"[GamePolygon {polygonId}] BuildPolygon called with {pointTransforms.Count} points, color: {color}");
        }
        else
        {
            Debug.LogWarning($"[GamePolygon {polygonId}] Cannot build polygon mesh: _meshRenderer={_meshRenderer}, points={pointTransforms?.Count}");
        }
        
        // Ẩn cả text và sprite khi complete
        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }
        
        if (numberSpriteRenderer != null)
        {
            numberSpriteRenderer.gameObject.SetActive(false);
        }
    }
    
    // Thêm cạnh vào danh sách các cạnh đã hoàn thành
    public void AddCompletedEdge(Edge edge)
    {
        completedEdges.Add(edge);
        Debug.Log($"[Polygon {polygonId}] Added completed edge ({edge.point1}, {edge.point2}). Total: {completedEdges.Count}/{edges.Count}");
    }
    
    /// <summary>
    /// Gán sprite số tương ứng với số cạnh
    /// </summary>
    private void SetNumberSprite(int number)
    {
        if (numberSpriteRenderer == null || numberSprites == null || numberSprites.Count == 0)
            return;
        
        // Tính index trong list sprite
        // Ví dụ: nếu startNumber = 3, thì số 3 -> index 0, số 4 -> index 1, ...
        int index = number - startNumber;
        
        if (index >= 0 && index < numberSprites.Count)
        {
            numberSpriteRenderer.sprite = numberSprites[index];
        }
        else
        {
            Debug.LogWarning($"[GamePolygon] Không tìm thấy sprite cho số {number}. Index: {index}, Sprite Count: {numberSprites.Count}");
        }
    }
    
    // Thêm nhiều cạnh cùng lúc
    public void AddCompletedEdges(IEnumerable<Edge> edgesToAdd)
    {
        foreach (var edge in edgesToAdd)
        {
            completedEdges.Add(edge);
        }
        Debug.Log($"[Polygon {polygonId}] Added multiple completed edges. Total: {completedEdges.Count}/{edges.Count}");
    }
    
    // Kiểm tra polygon đã đủ cạnh hoàn thành chưa
    public bool HasAllEdgesCompleted()
    {
        int requiredEdges = edges.Count;
        int completedCount = 0;
        
        foreach (var edge in edges)
        {
            if (completedEdges.Contains(edge))
            {
                completedCount++;
            }
        }
        
        bool isComplete = (completedCount == requiredEdges);
        Debug.Log($"[Polygon {polygonId}] HasAllEdgesCompleted: {completedCount}/{requiredEdges} = {isComplete}");
        return isComplete;
    }
    
    // Kiểm tra 1 cạnh đã được hoàn thành chưa
    public bool IsEdgeCompleted(Edge edge)
    {
        return completedEdges.Contains(edge);
    }
}