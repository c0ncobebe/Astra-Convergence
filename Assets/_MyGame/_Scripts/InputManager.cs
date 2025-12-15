using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class InputManager : MonoBehaviour
{
    [Header("Tap Settings")]
    [Tooltip("Maximum time for a tap in seconds")]
    [SerializeField] private float tapTimeThreshold = 0.3f;
    
    [Tooltip("Maximum distance for a tap in screen pixels")]
    [SerializeField] private float tapDistanceThreshold = 50f;

    [Header("Hold Settings")]
    [Tooltip("Minimum time to register as hold in seconds")]
    [SerializeField] private float holdTimeThreshold = 0.5f;

    [Header("Swipe Settings")]
    [Tooltip("Minimum distance for a swipe in screen pixels")]
    [SerializeField] private float swipeDistanceThreshold = 100f;
    
    [Tooltip("Maximum time for a swipe in seconds")]
    [SerializeField] private float swipeTimeThreshold = 1f;

    [Header("Events")]
    public UnityEvent<Vector2> OnTap;
    public UnityEvent<Vector2> OnHoldStarted;
    public UnityEvent<Vector2> OnHolding;
    public UnityEvent<Vector2> OnHoldEnded;
    public UnityEvent<Vector2, Vector2, SwipeDirection> OnSwipe;

    // Private variables
    private bool isPressed = false;
    private bool isHolding = false;
    private Vector2 startPosition;
    private Vector2 currentPosition;
    private float pressStartTime;
    private Coroutine holdCoroutine;

    public enum SwipeDirection
    {
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }

    // Singleton pattern (optional)
    public static InputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Handle mouse input (for PC)
        if (Input.GetMouseButtonDown(0))
        {
            OnPressStart(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            OnPressUpdate(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnPressEnd(Input.mousePosition);
        }

        // Handle touch input (for mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnPressStart(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnPressUpdate(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnPressEnd(touch.position);
                    break;
            }
        }
    }

    private void OnPressStart(Vector2 position)
    {
        if (isPressed) return; // Prevent multiple presses
        
        isPressed = true;
        isHolding = false;
        startPosition = position;
        currentPosition = position;
        pressStartTime = Time.time;

        // Start hold detection coroutine
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
        }
        holdCoroutine = StartCoroutine(DetectHold());
    }

    private void OnPressUpdate(Vector2 position)
    {
        if (!isPressed) return;
        
        currentPosition = position;

        // If holding, invoke holding event
        if (isHolding)
        {
            OnHolding?.Invoke(currentPosition);
        }
    }

    private void OnPressEnd(Vector2 position)
    {
        if (!isPressed) return;

        currentPosition = position;
        float pressDuration = Time.time - pressStartTime;
        float distance = Vector2.Distance(startPosition, currentPosition);

        // Stop hold detection
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        // If was holding, invoke hold ended
        if (isHolding)
        {
            OnHoldEnded?.Invoke(currentPosition);
        }
        // Check for tap
        else if (pressDuration < tapTimeThreshold && distance < tapDistanceThreshold)
        {
            OnTap?.Invoke(currentPosition);
        }
        // Check for swipe
        else if (distance >= swipeDistanceThreshold && pressDuration < swipeTimeThreshold)
        {
            Vector2 swipeVector = currentPosition - startPosition;
            SwipeDirection direction = GetSwipeDirection(swipeVector);
            OnSwipe?.Invoke(startPosition, currentPosition, direction);
        }

        // Reset state
        isPressed = false;
        isHolding = false;
    }

    private IEnumerator DetectHold()
    {
        yield return new WaitForSeconds(holdTimeThreshold);

        // Check if still pressed and hasn't moved much
        if (isPressed && Vector2.Distance(startPosition, currentPosition) < tapDistanceThreshold)
        {
            isHolding = true;
            OnHoldStarted?.Invoke(currentPosition);
        }
    }

    private SwipeDirection GetSwipeDirection(Vector2 swipeVector)
    {
        swipeVector.Normalize();
        
        float angle = Mathf.Atan2(swipeVector.y, swipeVector.x) * Mathf.Rad2Deg;
        
        // Normalize angle to 0-360
        if (angle < 0) angle += 360;

        // Determine direction based on angle (8 directions)
        if (angle >= 337.5f || angle < 22.5f)
            return SwipeDirection.Right;
        else if (angle >= 22.5f && angle < 67.5f)
            return SwipeDirection.UpRight;
        else if (angle >= 67.5f && angle < 112.5f)
            return SwipeDirection.Up;
        else if (angle >= 112.5f && angle < 157.5f)
            return SwipeDirection.UpLeft;
        else if (angle >= 157.5f && angle < 202.5f)
            return SwipeDirection.Left;
        else if (angle >= 202.5f && angle < 247.5f)
            return SwipeDirection.DownLeft;
        else if (angle >= 247.5f && angle < 292.5f)
            return SwipeDirection.Down;
        else
            return SwipeDirection.DownRight;
    }

    // Public helper methods
    public bool IsPressed() => isPressed;
    public bool IsHolding() => isHolding;
    public Vector2 GetCurrentPosition() => currentPosition;
    public Vector2 GetStartPosition() => startPosition;
    public float GetPressDuration() => isPressed ? Time.time - pressStartTime : 0f;
}

