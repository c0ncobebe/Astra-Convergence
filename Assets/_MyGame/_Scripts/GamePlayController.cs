using UnityEngine;

public class GamePlayController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Camera mainCamera;
    
    // Tracking các điểm đã nối
    private System.Collections.Generic.List<Collider2D> connectedDots = new System.Collections.Generic.List<Collider2D>();
    private Collider2D lastDetectedDot;
    private Collider2D currentHoverDot;
    
    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        // Đăng ký các sự kiện từ InputManager
        if (inputManager != null)
        {
            inputManager.OnTap.AddListener(HandleTap);
            inputManager.OnHoldStart.AddListener(HandleHoldStart);
            inputManager.OnHoldUpdate.AddListener(HandleHoldUpdate);
            inputManager.OnHoldEnd.AddListener(HandleHoldEnd);
        }
    }
    
    private void OnDestroy()
    {
        // Hủy đăng ký các sự kiện khi object bị destroy
        if (inputManager != null)
        {
            inputManager.OnTap.RemoveListener(HandleTap);
            inputManager.OnHoldStart.RemoveListener(HandleHoldStart);
            inputManager.OnHoldUpdate.RemoveListener(HandleHoldUpdate);
            inputManager.OnHoldEnd.RemoveListener(HandleHoldEnd);
        }
    }
    
    // Xử lý khi người chơi tap nhanh
    private void HandleTap(Vector2 screenPosition)
    {
        Collider2D detectedDot = inputManager.DetectDotAtScreenPosition(screenPosition, mainCamera);
        
        if (detectedDot != null)
        {
            Debug.Log($"[TAP] Detected Dot: {detectedDot.gameObject.name}");
            
            // Xử lý logic tap - ví dụ: chọn/bỏ chọn điểm
            OnDotTapped(detectedDot);
        }
        else
        {
            Debug.Log($"[TAP] No dot detected at position: {screenPosition}");
        }
    }
    
    private void OnDotTapped(Collider2D dot)
    {
        // Lấy Star component
        Star star = dot.GetComponent<Star>();
        if (star != null)
        {
            // Toggle state hoặc xử lý logic khác
            if (star.CurrentState == StarState.Idle)
            {
                star.OnHoverEnter();
            }
            else
            {
                star.ResetState();
            }
        }
    }
    
    // Xử lý khi người chơi bắt đầu hold/drag
    private void HandleHoldStart(Vector2 screenPosition)
    {
        Collider2D detectedDot = inputManager.DetectDotAtScreenPosition(screenPosition, mainCamera);
        
        if (detectedDot != null)
        {
            Debug.Log($"[HOLD START] Starting from Dot: {detectedDot.gameObject.name}");
            
            // Reset danh sách điểm đã nối
            connectedDots.Clear();
            
            // Thêm điểm đầu tiên
            connectedDots.Add(detectedDot);
            lastDetectedDot = detectedDot;
            
            // Bắt đầu vẽ đường nối
            OnStartConnecting(detectedDot);
        }
        else
        {
            Debug.Log($"[HOLD START] No dot detected - cannot start connecting");
        }
    }
    
    private void OnStartConnecting(Collider2D firstDot)
    {
        // Đánh dấu sao đầu tiên là connected
        Star star = firstDot.GetComponent<Star>();
        if (star != null)
        {
            star.OnConnected();
        }
        
        // TODO: Tạo LineRenderer để vẽ đường nối
    }
    
    // Xử lý khi người chơi đang hold/drag
    private void HandleHoldUpdate(Vector2 screenPosition)
    {
        Collider2D detectedDot = inputManager.DetectDotAtScreenPosition(screenPosition, mainCamera);
        
        // Cập nhật hover state
        if (detectedDot != currentHoverDot)
        {
            if (currentHoverDot != null)
            {
                OnDotHoverExit(currentHoverDot);
            }
            
            currentHoverDot = detectedDot;
            
            if (currentHoverDot != null)
            {
                OnDotHoverEnter(currentHoverDot);
            }
        }
        
        // Nếu phát hiện điểm mới và khác điểm cuối cùng
        if (detectedDot != null && detectedDot != lastDetectedDot)
        {
            // Kiểm tra xem điểm này đã được nối chưa
            if (!connectedDots.Contains(detectedDot))
            {
                Debug.Log($"[HOLD UPDATE] Connected to new Dot: {detectedDot.gameObject.name}");
                
                // Thêm điểm mới vào danh sách
                connectedDots.Add(detectedDot);
                lastDetectedDot = detectedDot;
                
                // Cập nhật đường nối
                OnDotConnected(detectedDot);
            }
            else
            {
                Debug.Log($"[HOLD UPDATE] Dot already connected: {detectedDot.gameObject.name}");
            }
        }
        
        // Cập nhật vị trí line renderer theo ngón tay
        Vector2 worldPos = inputManager.GetWorldPosition2D(screenPosition, mainCamera);
        OnUpdateConnecting(worldPos);
    }
    
    private void OnDotHoverEnter(Collider2D dot)
    {
        Star star = dot.GetComponent<Star>();
        if (star != null)
        {
            star.OnHoverEnter();
        }
    }
    
    private void OnDotHoverExit(Collider2D dot)
    {
        Star star = dot.GetComponent<Star>();
        if (star != null)
        {
            star.OnHoverExit();
        }
    }
    
    private void OnDotConnected(Collider2D dot)
    {
        Star star = dot.GetComponent<Star>();
        if (star != null)
        {
            star.OnConnected();
        }
        
        // TODO: Cập nhật LineRenderer, play sound, particle effect
    }
    
    private void OnUpdateConnecting(Vector2 worldPosition)
    {
        // TODO: Implement logic cập nhật đường nối theo ngón tay
        // Ví dụ: Cập nhật điểm cuối của LineRenderer
    }
    
    // Xử lý khi người chơi kết thúc hold/drag
    private void HandleHoldEnd(Vector2 screenPosition)
    {
        Debug.Log($"[HOLD END] Finished connecting. Total dots connected: {connectedDots.Count}");
        
        // Clear hover state
        if (currentHoverDot != null)
        {
            OnDotHoverExit(currentHoverDot);
            currentHoverDot = null;
        }
        
        // Kiểm tra và xử lý kết quả nối điểm
        if (connectedDots.Count > 1)
        {
            OnFinishConnecting(connectedDots);
        }
        else
        {
            OnCancelConnecting();
        }
        
        // Reset
        lastDetectedDot = null;
    }
    
    private void OnFinishConnecting(System.Collections.Generic.List<Collider2D> dots)
    {
        Debug.Log($"[FINISH CONNECTING] Connected {dots.Count} dots:");
        for (int i = 0; i < dots.Count; i++)
        {
            Debug.Log($"  {i + 1}. {dots[i].gameObject.name}");
        }
        
        // TODO: Validate xem có nối đúng thứ tự không
        bool isValidConnection = ValidateConnection(dots);
        
        if (isValidConnection)
        {
            // Giữ trạng thái connected cho tất cả các sao
            Debug.Log("[SUCCESS] Valid connection!");
            // TODO: Play success effect, tính điểm
        }
        else
        {
            // Reset tất cả các sao về idle
            Debug.Log("[FAILED] Invalid connection!");
            ResetAllStars(dots);
        }
    }
    
    private bool ValidateConnection(System.Collections.Generic.List<Collider2D> dots)
    {
        // TODO: Implement logic validation
        // Ví dụ: Kiểm tra thứ tự StarID, kiểm tra số lượng tối thiểu, etc.
        
        // Tạm thời return true để test
        return dots.Count >= 2;
    }
    
    private void ResetAllStars(System.Collections.Generic.List<Collider2D> dots)
    {
        foreach (var dot in dots)
        {
            Star star = dot.GetComponent<Star>();
            if (star != null)
            {
                star.ResetState();
            }
        }
    }
    
    private void OnCancelConnecting()
    {
        Debug.Log("[CANCEL CONNECTING] Not enough dots connected");
        
        // Reset tất cả các sao đã connect về idle
        ResetAllStars(connectedDots);
        
        // TODO: Xóa LineRenderer
    }
}
