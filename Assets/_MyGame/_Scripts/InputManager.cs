using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float holdThreshold = 0.2f; // Thời gian để phân biệt tap và hold (giây)
    [SerializeField] private float dragThreshold = 10f; // Khoảng cách để coi là drag (pixels)
    
    [Header("Detection Settings")]
    [SerializeField] private string dotTag = "Dot"; // Tag của các điểm
    [SerializeField] private LayerMask dotLayer = -1; // Layer của các điểm (mặc định all layers)
    
    [Header("Events")]
    public UnityEvent<Vector2> OnTap; // Sự kiện khi tap nhanh
    public UnityEvent<Vector2> OnHoldStart; // Sự kiện khi bắt đầu hold
    public UnityEvent<Vector2> OnHoldUpdate; // Sự kiện khi đang hold/drag
    public UnityEvent<Vector2> OnHoldEnd; // Sự kiện khi kết thúc hold/drag
    
    private bool isPressed = false;
    private bool isHolding = false;
    private float pressStartTime;
    private Vector2 pressStartPosition;
    private Vector2 currentPosition;
    
    void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        // Xử lý input cho cả mobile và PC
        if (Input.GetMouseButtonDown(0))
        {
            OnPressStart(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isPressed)
        {
            OnPressUpdate(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isPressed)
        {
            OnPressEnd(Input.mousePosition);
        }
    }
    
    private void OnPressStart(Vector2 position)
    {
        isPressed = true;
        isHolding = false;
        pressStartTime = Time.time;
        pressStartPosition = position;
        currentPosition = position;
    }
    
    private void OnPressUpdate(Vector2 position)
    {
        currentPosition = position;
        float holdDuration = Time.time - pressStartTime;
        
        // Kiểm tra nếu đã giữ đủ lâu hoặc kéo đủ xa
        if (!isHolding && (holdDuration >= holdThreshold || Vector2.Distance(pressStartPosition, currentPosition) > dragThreshold))
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
    
    private void OnPressEnd(Vector2 position)
    {
        currentPosition = position;
        
        if (isHolding)
        {
            // Kết thúc hold/drag
            OnHoldEnd?.Invoke(currentPosition);
        }
        else
        {
            // Tap nhanh
            OnTap?.Invoke(currentPosition);
        }
        
        isPressed = false;
        isHolding = false;
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
    public string DotTag => dotTag;
    public LayerMask DotLayer => dotLayer;
}

