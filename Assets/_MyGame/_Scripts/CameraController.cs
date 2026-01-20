using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Camera controller hỗ trợ zoom và pan với smooth damping
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float minOrthographicSize = 2f;
    [SerializeField] private float maxOrthographicSize = 12f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothTime = 0.1f;
    
    [Header("Pan Settings")]
    [SerializeField] private bool enablePanBounds = false;
    [SerializeField] private Rect panBounds = new Rect(-20, -20, 40, 40);
    [SerializeField] private float panSmoothTime = 0.1f;
    [SerializeField] private bool disablePanSmoothingWhilePanning = true; // Pan responsive hơn
    
    [Header("References")]
    [SerializeField] private InputManager inputManager;
    
    private Camera cam;
    private float targetOrthographicSize;
    private float zoomVelocity;
    
    private Vector3 targetPosition;
    private Vector3 panVelocity;
    
    private bool isPanning = false;
    private bool isZooming = false;
    
    // Store initial state for reset
    private Vector3 initialPosition;
    private float initialOrthographicSize;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
        }
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        targetOrthographicSize = cam.orthographicSize;
        targetPosition = transform.position;
        
        // Store initial state
        initialPosition = transform.position;
        initialOrthographicSize = cam.orthographicSize;
        
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
            // Subscribe camera events
            inputManager.OnCameraPanStart.AddListener(OnPanStart);
            inputManager.OnCameraPanUpdate.AddListener(OnPanUpdate);
            inputManager.OnCameraPanEnd.AddListener(OnPanEnd);
            
            inputManager.OnCameraZoomStart.AddListener(OnZoomStart);
            inputManager.OnCameraZoomUpdate.AddListener(OnZoomUpdate);
            inputManager.OnCameraZoomEnd.AddListener(OnZoomEnd);
        }
    }
    
    void OnDisable()
    {
        if (inputManager != null)
        {
            // Unsubscribe camera events
            inputManager.OnCameraPanStart.RemoveListener(OnPanStart);
            inputManager.OnCameraPanUpdate.RemoveListener(OnPanUpdate);
            inputManager.OnCameraPanEnd.RemoveListener(OnPanEnd);
            
            inputManager.OnCameraZoomStart.RemoveListener(OnZoomStart);
            inputManager.OnCameraZoomUpdate.RemoveListener(OnZoomUpdate);
            inputManager.OnCameraZoomEnd.RemoveListener(OnZoomEnd);
        }
    }
    
    void LateUpdate()
    {
        // Chỉ áp dụng smooth zoom khi KHÔNG đang zoom (để tránh conflict)
        if (!isZooming)
        {
            bool zoomChanged = false;
            if (Mathf.Abs(cam.orthographicSize - targetOrthographicSize) > 0.01f)
            {
                cam.orthographicSize = Mathf.SmoothDamp(
                    cam.orthographicSize,
                    targetOrthographicSize,
                    ref zoomVelocity,
                    zoomSmoothTime
                );
                zoomChanged = true;
            }
            
            // Clamp position khi zoom thay đổi (vì bounds extent thay đổi)
            if (zoomChanged && enablePanBounds)
            {
                Vector3 clampedPos = ClampToBoundsHard(transform.position);
                targetPosition = clampedPos;
                
                // Apply ngay nếu không đang pan
                if (!isPanning)
                {
                    transform.position = clampedPos;
                }
            }
        }
        
        // Pan handling - immediate khi đang pan, smooth khi không pan
        // Chỉ smooth khi KHÔNG đang zoom hoặc pan
        if (!isZooming && !isPanning)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance > 0.001f)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref panVelocity,
                    panSmoothTime
                );
            }
        }
    }
    
    #region Pan Methods
    
    private void OnPanStart(Vector2 screenPosition)
    {
        isPanning = true;
    }
    
    private void OnPanUpdate(Vector2 screenDelta)
    {
        if (!isPanning) return;
        
        // Convert screen delta to world delta
        // Scale theo camera size để pan motion tự nhiên
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;
        
        Vector2 worldDelta = new Vector2(
            (screenDelta.x / Screen.width) * worldWidth,
            (screenDelta.y / Screen.height) * worldHeight
        );
        
        // Negative vì khi finger di chuyển sang phải, camera phải di chuyển sang trái
        Vector3 newPosition = transform.position - new Vector3(worldDelta.x, worldDelta.y, 0);
        
        // Hard clamp - ngừng kẹt luôn không elastic
        if (enablePanBounds)
        {
            newPosition = ClampToBoundsHard(newPosition);
        }
        
        // Set position và target cùng lúc
        targetPosition = newPosition;
        
        // Nếu pan immediate enabled, apply ngay
        if (disablePanSmoothingWhilePanning)
        {
            transform.position = targetPosition;
        }
    }
    
    private void OnPanEnd()
    {
        isPanning = false;
    }
    
    private Vector3 ClampToBounds(Vector3 position)
    {
        // Sử dụng targetOrthographicSize thay vì cam.orthographicSize
        // để tránh jitter khi zoom
        float vertExtent = targetOrthographicSize;
        float horzExtent = vertExtent * cam.aspect;
        
        float minX = panBounds.xMin + horzExtent;
        float maxX = panBounds.xMax - horzExtent;
        float minY = panBounds.yMin + vertExtent;
        float maxY = panBounds.yMax - vertExtent;
        
        return new Vector3(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY),
            position.z
        );
    }
    
    private Vector3 ClampToBoundsHard(Vector3 position)
    {
        // Hard clamp sử dụng current camera size cho immediate feedback
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;
        
        float minX = panBounds.xMin + horzExtent;
        float maxX = panBounds.xMax - horzExtent;
        float minY = panBounds.yMin + vertExtent;
        float maxY = panBounds.yMax - vertExtent;
        
        // Đảm bảo bounds hợp lệ (trường hợp level nhỏ hơn camera view)
        if (minX > maxX) minX = maxX = (minX + maxX) * 0.5f;
        if (minY > maxY) minY = maxY = (minY + maxY) * 0.5f;
        
        return new Vector3(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY),
            position.z
        );
    }
    
    #endregion
    
    #region Zoom Methods
    
    private void OnZoomStart(float initialDelta, Vector2 centerPoint)
    {
        isZooming = true;
    }
    
    private void OnZoomUpdate(float zoomDelta, Vector2 centerPoint)
    {
        if (!isZooming) return;
        
        // Lấy vị trí world của zoom center TRƯỚC khi zoom
        Vector3 worldPointBeforeZoom = cam.ScreenToWorldPoint(new Vector3(centerPoint.x, centerPoint.y, cam.nearClipPlane));
        
        // Calculate new orthographic size
        float newSize = targetOrthographicSize - (zoomDelta * zoomSpeed * targetOrthographicSize);
        
        // Clamp to min/max
        newSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
        
        // Zoom towards center point (pinch center)
        if (Mathf.Abs(newSize - targetOrthographicSize) > 0.001f)
        {
            // Lưu size cũ
            float oldSize = cam.orthographicSize;
            
            // Update target zoom
            targetOrthographicSize = newSize;
            
            // Apply zoom ngay lập tức để tính toán offset chính xác
            cam.orthographicSize = targetOrthographicSize;
            
            // Tính toán vị trí world của zoom center SAU khi zoom
            Vector3 worldPointAfterZoom = cam.ScreenToWorldPoint(new Vector3(centerPoint.x, centerPoint.y, cam.nearClipPlane));
            
            // Tính offset để giữ nguyên điểm zoom ở cùng vị trí screen
            Vector3 offset = worldPointBeforeZoom - worldPointAfterZoom;
            
            // Di chuyển camera để bù offset
            Vector3 newPosition = transform.position + offset;
            
            // Apply bounds if enabled
            if (enablePanBounds)
            {
                newPosition = ClampToBoundsHard(newPosition);
            }
            
            // Update cả transform và target position ngay lập tức
            transform.position = newPosition;
            targetPosition = newPosition;
            
            // Reset velocity để tránh smooth lag
            zoomVelocity = 0;
            panVelocity = Vector3.zero;
        }
    }
    
    private void OnZoomEnd()
    {
        isZooming = false;
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Set pan bounds và enable pan bounds
    /// </summary>
    public void SetPanBounds(Rect bounds, bool enable = true)
    {
        panBounds = bounds;
        enablePanBounds = enable;
        
        // Clamp vị trí hiện tại vào bounds mới
        if (enablePanBounds)
        {
            targetPosition = ClampToBounds(targetPosition);
        }
    }
    
    /// <summary>
    /// Enable/disable pan bounds
    /// </summary>
    public void SetPanBoundsEnabled(bool enabled)
    {
        enablePanBounds = enabled;
    }
    
    /// <summary>
    /// Get current pan bounds
    /// </summary>
    public Rect GetPanBounds() => panBounds;
    
    /// <summary>
    /// Set zoom level (orthographic size)
    /// </summary>
    public void SetZoom(float orthographicSize, bool immediate = false)
    {
        targetOrthographicSize = Mathf.Clamp(orthographicSize, minOrthographicSize, maxOrthographicSize);
        
        if (immediate)
        {
            cam.orthographicSize = targetOrthographicSize;
            zoomVelocity = 0;
        }
    }
    
    /// <summary>
    /// Set camera position
    /// </summary>
    public void SetPosition(Vector3 position, bool immediate = false)
    {
        targetPosition = position;
        targetPosition.z = transform.position.z; // Giữ nguyên Z
        
        if (enablePanBounds)
        {
            targetPosition = ClampToBounds(targetPosition);
        }
        
        if (immediate)
        {
            transform.position = targetPosition;
            panVelocity = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Reset camera về vị trí và zoom mặc định
    /// </summary>
    public void ResetCamera(Vector3 position, float orthographicSize)
    {
        SetPosition(position, true);
        SetZoom(orthographicSize, true);
    }
    
    /// <summary>
    /// Reset camera về initial state (cho Home Menu)
    /// </summary>
    public void ResetCamera()
    {
        SetPosition(initialPosition, true);
        SetZoom(initialOrthographicSize, true);
    }
    
    /// <summary>
    /// Setup camera cho gameplay (có thể customize)
    /// </summary>
    public void SetupForGameplay(float? customZoom = null)
    {
        if (customZoom.HasValue)
        {
            SetZoom(customZoom.Value, true);
        }
    }
    
    #endregion
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (enablePanBounds)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(panBounds.center, panBounds.size);
        }
    }
}
