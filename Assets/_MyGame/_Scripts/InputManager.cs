using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float holdThreshold = 0.2f; // Thời gian để phân biệt tap và hold (giây)
    [SerializeField] private float dragThreshold = 10f; // Khoảng cách để coi là drag (pixels)
    
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
    
    // Getter để kiểm tra trạng thái
    public bool IsPressed => isPressed;
    public bool IsHolding => isHolding;
    public Vector2 CurrentPosition => currentPosition;
}

