using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum InputMode
{
    PointConnection,
    CameraPan,
    CameraZoom
}

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float holdThreshold = 0.2f;
    [SerializeField] private float dragThreshold = 10f;
    [SerializeField] private float pinchThreshold = 20f;
    
    [Header("Detection Settings")]
    [SerializeField] private string dotTag = "Dot";
    [SerializeField] private LayerMask dotLayer = -1;
    
    [Header("Events")]
    public UnityEvent<Vector2> OnTap;
    public UnityEvent<Vector2> OnHoldStart;
    public UnityEvent<Vector2> OnHoldUpdate;
    public UnityEvent<Vector2> OnHoldEnd;
    
    public UnityEvent<Vector2> OnCameraPanStart;
    public UnityEvent<Vector2> OnCameraPanUpdate;
    public UnityEvent OnCameraPanEnd;
    
    public UnityEvent<float, Vector2> OnCameraZoomStart;
    public UnityEvent<float, Vector2> OnCameraZoomUpdate;
    public UnityEvent OnCameraZoomEnd;
    
    private bool isPressed = false;
    private bool isHolding = false;
    private float pressStartTime;
    private Vector2 pressStartPosition;
    private Vector2 currentPosition;
    
    private InputMode currentMode = InputMode.PointConnection;
    private int previousTouchCount = 0;
    private float previousPinchDistance = 0f;
    private Vector2 previousPanPosition;
    private bool isDotAtStartPosition = false;
    
    void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
        {
            HandleMouseInput();
        }
        else
        {
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
        
        if (touchCount != previousTouchCount && previousTouchCount > 0)
        {
            EndCurrentMode();
        }
        
        if (touchCount >= 2)
        {
            HandlePinchZoom();
        }
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
        
        if (currentMode != InputMode.CameraZoom)
        {
            currentMode = InputMode.CameraZoom;
            previousPinchDistance = currentDistance;
            OnCameraZoomStart?.Invoke(0f, centerPoint);
            return;
        }
        
        float deltaDistance = currentDistance - previousPinchDistance;
        
        if (Mathf.Abs(deltaDistance) > pinchThreshold * 0.1f)
        {
            float zoomDelta = deltaDistance / Screen.height;
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
        
        isDotAtStartPosition = DetectDotAtScreenPosition(position) != null;
        
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
        
        if (isDotAtStartPosition)
        {
            currentMode = InputMode.PointConnection;
            
            if (!isHolding && (holdDuration >= holdThreshold || dragDistance > dragThreshold))
            {
                isHolding = true;
                OnHoldStart?.Invoke(currentPosition);
            }
            
            if (isHolding)
            {
                OnHoldUpdate?.Invoke(currentPosition);
            }
        }
        else
        {
            if (currentMode != InputMode.CameraPan && dragDistance > dragThreshold)
            {
                currentMode = InputMode.CameraPan;
                OnCameraPanStart?.Invoke(position);
            }
            
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
                OnHoldEnd?.Invoke(currentPosition);
            }
            else
            {
                OnTap?.Invoke(currentPosition);
            }
        }
        else if (currentMode == InputMode.CameraPan)
        {
            OnCameraPanEnd?.Invoke();
        }
        
        isPressed = false;
        isHolding = false;
        isDotAtStartPosition = false;
        currentMode = InputMode.PointConnection;
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
    
    public Vector3 GetWorldPosition(Vector2 screenPosition, Camera camera = null)
    {
        if (camera == null)
            camera = Camera.main;
            
        return camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, camera.nearClipPlane));
    }
    
    public Vector2 GetWorldPosition2D(Vector2 screenPosition, Camera camera = null)
    {
        if (camera == null)
            camera = Camera.main;
            
        return camera.ScreenToWorldPoint(screenPosition);
    }
    
    public Collider2D DetectDotAtScreenPosition(Vector2 screenPosition, Camera camera = null)
    {
        Vector2 worldPos = GetWorldPosition2D(screenPosition, camera);
        return DetectDotAtWorldPosition(worldPos);
    }
    
    public Collider2D DetectDotAtWorldPosition(Vector2 worldPosition)
    {
        Collider2D collider = Physics2D.OverlapPoint(worldPosition, dotLayer);
        
        if (collider != null && !string.IsNullOrEmpty(dotTag))
        {
            if (collider.CompareTag(dotTag))
                return collider;
            else
                return null;
        }
        
        return collider;
    }
    
    public Collider2D[] DetectAllDotsAtScreenPosition(Vector2 screenPosition, Camera camera = null)
    {
        Vector2 worldPos = GetWorldPosition2D(screenPosition, camera);
        return DetectAllDotsAtWorldPosition(worldPos);
    }
    
    public Collider2D[] DetectAllDotsAtWorldPosition(Vector2 worldPosition)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition, dotLayer);
        
        if (!string.IsNullOrEmpty(dotTag))
        {
            return System.Array.FindAll(colliders, c => c.CompareTag(dotTag));
        }
        
        return colliders;
    }
    
    public bool IsPressed => isPressed;
    public bool IsHolding => isHolding;
    public Vector2 CurrentPosition => currentPosition;
    public InputMode CurrentMode => currentMode;
    public string DotTag => dotTag;
    public LayerMask DotLayer => dotLayer;
}
