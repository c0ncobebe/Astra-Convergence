using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum InputMode
{
    PointConnection, // Đang nối điểm
    CameraPan,       // Đang di chuyển camera (1 ngón tay, không có điểm)
    CameraZoom       // Đang zoom camera (2 ngón tay)
}

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float holdThreshold = 0.2f; // Thời gian để phân biệt tap và hold (giây)
    [SerializeField] private float dragThreshold = 10f; // Khoảng cách để coi là drag (pixels)
    [SerializeField] private float pinchThreshold = 20f; // Khoảng cách tối thiểu để detect pinch (pixels)
    
    [Header("Detection Settings")]
    [SerializeField] private string dotTag = "Dot"; // Tag của các điểm
    [SerializeField] private LayerMask dotLayer = -1; // Layer của các điểm (mặc định all layers)
    
    [Header("Events")]
    public UnityEvent<Vector2> OnTap; // Sự kiện khi tap nhanh
    public UnityEvent<Vector2> OnHoldStart; // Sự kiện khi bắt đầu hold
    public UnityEvent<Vector2> OnHoldUpdate; // Sự kiện khi đang hold/drag
    public UnityEvent<Vector2> OnHoldEnd; // Sự kiện khi kết thúc hold/drag
    
    // Camera control events
    public UnityEvent<Vector2> OnCameraPanStart;
    public UnityEvent<Vector2> OnCameraPanUpdate;
    public UnityEvent OnCameraPanEnd;
    
    public UnityEvent<float, Vector2> OnCameraZoomStart; // zoom delta, center point
    public UnityEvent<float, Vector2> OnCameraZoomUpdate; // zoom delta, center point
    public UnityEvent OnCameraZoomEnd;
    
    private bool isPressed = false;
    private bool isHolding = false;
    private float pressStartTime;
    private Vector2 pressStartPosition;
    private Vector2 currentPosition;
    
    // Multi-touch tracking
    private InputMode currentMode = InputMode.PointConnection;
    private int previousTouchCount = 0;
    private float previousPinchDistance = 0f;
    private Vector2 previousPanPosition;
    private bool isDotAtStartPosition = false; // Có điểm tại vị trí bắt đầu touch không
    
    void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        // Mobile touch input (có thể multi-touch)
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        // Fallback cho PC (mouse = 1 touch)
        else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
        {
            HandleMouseInput();
        }
        else
        {
            // Không có input, reset state
            if (previousTouchCount > 0)
            {
                EndCurrentMode();
            }
            previousTouchCount = 0;
        }
    }
    
    private void HandleTouchInput()
    {
        int touchCount = Input.touchCount;
        
        // Touch count changed - reset mode
        if (touchCount != previousTouchCount && previousTouchCount > 0)
        {
            EndCurrentMode();
        }
        
        // 2 fingers = Zoom
        if (touchCount >= 2)
        {
            HandlePinchZoom();
        }
        // 1 finger
        else if (touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                OnSingleTouchStart(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                OnSingleTouchUpdate(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                OnSingleTouchEnd(touch.position);
            }
        }
        
        previousTouchCount = touchCount;
    }
    
    private void HandleMouseInput()
    {
        // Xử lý input cho cả mobile và PC
        if (Input.GetMouseButtonDown(0))
        {
            OnSingleTouchStart(Input.mousePosition);
            previousTouchCount = 1;
        }
        else if (Input.GetMouseButton(0))
        {
            OnSingleTouchUpdate(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnSingleTouchEnd(Input.mousePosition);
            previousTouchCount = 0;
        }
    }
    
    private void HandlePinchZoom()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);
        
        Vector2 touch0Pos = touch0.position;
        Vector2 touch1Pos = touch1.position;
        float currentDistance = Vector2.Distance(touch0Pos, touch1Pos);
        Vector2 centerPoint = (touch0Pos + touch1Pos) * 0.5f;
        
        // Start zoom
        if (currentMode != InputMode.CameraZoom)
        {
            currentMode = InputMode.CameraZoom;
            previousPinchDistance = currentDistance;
            OnCameraZoomStart?.Invoke(0f, centerPoint);
            return;
        }
        
        // Update zoom
        float deltaDistance = currentDistance - previousPinchDistance;
        
        // Chỉ update nếu thay đổi đủ lớn (tránh jitter)
        if (Mathf.Abs(deltaDistance) > pinchThreshold * 0.1f)
        {
            float zoomDelta = deltaDistance / Screen.height; // Normalize theo screen height
            OnCameraZoomUpdate?.Invoke(zoomDelta, centerPoint);
            previousPinchDistance = currentDistance;
        }
    }
    
    private void OnSingleTouchStart(Vector2 position)
    {
        isPressed = true;
        isHolding = false;
        pressStartTime = Time.time;
        pressStartPosition = position;
        currentPosition = position;
        previousPanPosition = position;
        
        // Detect xem có điểm tại vị trí này không
        isDotAtStartPosition = DetectDotAtScreenPosition(position) != null;
        
        // Nếu có điểm -> mode nối điểm
        // Nếu không có điểm -> mode pan camera (sẽ xác định sau khi drag)
        if (isDotAtStartPosition)
        {
            currentMode = InputMode.PointConnection;
        }
    }
    
    private void OnSingleTouchUpdate(Vector2 position)
    {
        currentPosition = position;
        float holdDuration = Time.time - pressStartTime;
        float dragDistance = Vector2.Distance(pressStartPosition, currentPosition);
        
        // Nếu bắt đầu từ điểm -> luôn là mode nối điểm
        if (isDotAtStartPosition)
        {
            currentMode = InputMode.PointConnection;
            
            // Kiểm tra nếu đã giữ đủ lâu hoặc kéo đủ xa
            if (!isHolding && (holdDuration >= holdThreshold || dragDistance > dragThreshold))
            {
                isHolding = true;
                OnHoldStart?.Invoke(currentPosition);
            }
            
            // Nếu đang hold, gọi update liên tục
            if (isHolding)
            {
                OnHoldUpdate?.Invoke(currentPosition);
            }
        }
        // Nếu không có điểm tại vị trí bắt đầu -> mode pan camera
        else
        {
            // Chuyển sang pan mode nếu đã drag đủ xa
            if (currentMode != InputMode.CameraPan && dragDistance > dragThreshold)
            {
                currentMode = InputMode.CameraPan;
                OnCameraPanStart?.Invoke(position);
            }
            
            // Update pan nếu đang trong pan mode
            if (currentMode == InputMode.CameraPan)
            {
                Vector2 delta = position - previousPanPosition;
                OnCameraPanUpdate?.Invoke(delta);
                previousPanPosition = position;
            }
        }
    }
    
    private void OnSingleTouchEnd(Vector2 position)
    {
        currentPosition = position;
        
        if (currentMode == InputMode.PointConnection)
        {
            if (isHolding)
            {
                // Kết thúc hold/drag khi nối điểm
                OnHoldEnd?.Invoke(currentPosition);
            }
            else
            {
                // Tap nhanh
                OnTap?.Invoke(currentPosition);
            }
        }
        else if (currentMode == InputMode.CameraPan)
        {
            OnCameraPanEnd?.Invoke();
        }
        
        // Reset states
        isPressed = false;
        isHolding = false;
        isDotAtStartPosition = false;
        currentMode = InputMode.PointConnection; // Reset về default mode
    }
    
    private void EndCurrentMode()
    {
        if (currentMode == InputMode.CameraZoom)
        {
            OnCameraZoomEnd?.Invoke();
        }
        else if (currentMode == InputMode.CameraPan)
        {
            OnCameraPanEnd?.Invoke();
        }
        else if (isHolding)
        {
            OnHoldEnd?.Invoke(currentPosition);
        }
        
        isPressed = false;
        isHolding = false;
        currentMode = InputMode.PointConnection;
    }
    
    // Hàm tiện ích để lấy vị trí world từ screen position
    public Vector3 GetWorldPosition(Vector2 screenPosition, Camera camera = null)
    {
        if (camera == null)
            camera = Camera.main;
            
        return camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, camera.nearClipPlane));
    }
    
    // Hàm tiện ích để lấy vị trí world 2D (cho game 2D)
    public Vector2 GetWorldPosition2D(Vector2 screenPosition, Camera camera = null)
    {
        if (camera == null)
            camera = Camera.main;
            
        return camera.ScreenToWorldPoint(screenPosition);
    }
    
    // Detect điểm tại vị trí screen (sử dụng OverlapPoint - nhẹ)
    public Collider2D DetectDotAtScreenPosition(Vector2 screenPosition, Camera camera = null)
    {
        Vector2 worldPos = GetWorldPosition2D(screenPosition, camera);
        return DetectDotAtWorldPosition(worldPos);
    }
    
    // Detect điểm tại vị trí world (sử dụng OverlapPoint)
    public Collider2D DetectDotAtWorldPosition(Vector2 worldPosition)
    {
        Collider2D collider = Physics2D.OverlapPoint(worldPosition, dotLayer);
        
        // Kiểm tra tag nếu có collider
        if (collider != null && !string.IsNullOrEmpty(dotTag))
        {
            if (collider.CompareTag(dotTag))
                return collider;
            else
                return null;
        }
        
        return collider;
    }
    
    // Detect tất cả các điểm tại vị trí (nếu có nhiều điểm overlap)
    public Collider2D[] DetectAllDotsAtScreenPosition(Vector2 screenPosition, Camera camera = null)
    {
        Vector2 worldPos = GetWorldPosition2D(screenPosition, camera);
        return DetectAllDotsAtWorldPosition(worldPos);
    }
    
    // Detect tất cả các điểm tại vị trí world
    public Collider2D[] DetectAllDotsAtWorldPosition(Vector2 worldPosition)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition, dotLayer);
        
        // Lọc theo tag nếu cần
        if (!string.IsNullOrEmpty(dotTag))
        {
            return System.Array.FindAll(colliders, c => c.CompareTag(dotTag));
        }
        
        return colliders;
    }
    
    // Getter để kiểm tra trạng thái
    public bool IsPressed => isPressed;
    public bool IsHolding => isHolding;
    public Vector2 CurrentPosition => currentPosition;
    public InputMode CurrentMode => currentMode;
    public string DotTag => dotTag;
    public LayerMask DotLayer => dotLayer;
}
