using UnityEngine;

public class GamePlayController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;

    void Start()
    {
        // Find InputManager if not assigned
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<InputManager>();
        }

        // Subscribe to input events
        if (inputManager != null)
        {
            inputManager.OnTap.AddListener(HandleTap);
            inputManager.OnHoldStarted.AddListener(HandleHoldStarted);
            inputManager.OnHolding.AddListener(HandleHolding);
            inputManager.OnHoldEnded.AddListener(HandleHoldEnded);
            inputManager.OnSwipe.AddListener(HandleSwipe);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (inputManager != null)
        {
            inputManager.OnTap.RemoveListener(HandleTap);
            inputManager.OnHoldStarted.RemoveListener(HandleHoldStarted);
            inputManager.OnHolding.RemoveListener(HandleHolding);
            inputManager.OnHoldEnded.RemoveListener(HandleHoldEnded);
            inputManager.OnSwipe.RemoveListener(HandleSwipe);
        }
    }

    // Handle tap (chạm 1 cái rồi nhấc lên luôn)
    private void HandleTap(Vector2 position)
    {
        Debug.Log($"Tap at position: {position}");
        // Add your tap logic here
    }

    // Handle hold started (bắt đầu nhấn giữ)
    private void HandleHoldStarted(Vector2 position)
    {
        Debug.Log($"Hold started at position: {position}");
        // Add your hold start logic here
    }

    // Handle holding (đang nhấn giữ)
    private void HandleHolding(Vector2 position)
    {
        Debug.Log($"Holding at position: {position}");
        // Add your holding logic here (called every frame while holding)
    }

    // Handle hold ended (kết thúc nhấn giữ)
    private void HandleHoldEnded(Vector2 position)
    {
        Debug.Log($"Hold ended at position: {position}");
        // Add your hold end logic here
    }

    // Handle swipe (vuốt)
    private void HandleSwipe(Vector2 startPos, Vector2 endPos, InputManager.SwipeDirection direction)
    {
        Debug.Log($"Swipe from {startPos} to {endPos}, direction: {direction}");
        // Add your swipe logic here
    }

    void Update()
    {
        
    }
}
