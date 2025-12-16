using UnityEngine;

public class GamePlayController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Camera mainCamera;
    
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
        Vector3 worldPosition = inputManager.GetWorldPosition(screenPosition, mainCamera);
        Debug.Log($"[TAP] Screen: {screenPosition}, World: {worldPosition}");
        
        // TODO: Xử lý logic tap cho game nối điểm
        // Ví dụ: Chọn điểm đầu tiên hoặc điểm cuối cùng
    }
    
    // Xử lý khi người chơi bắt đầu hold/drag
    private void HandleHoldStart(Vector2 screenPosition)
    {
        Vector3 worldPosition = inputManager.GetWorldPosition(screenPosition, mainCamera);
        Debug.Log($"[HOLD START] Screen: {screenPosition}, World: {worldPosition}");
        
        // TODO: Xử lý logic bắt đầu nối điểm
        // Ví dụ: Tạo line renderer, bắt đầu từ điểm đầu tiên
    }
    
    // Xử lý khi người chơi đang hold/drag
    private void HandleHoldUpdate(Vector2 screenPosition)
    {
        Vector3 worldPosition = inputManager.GetWorldPosition(screenPosition, mainCamera);
        Debug.Log($"[HOLD UPDATE] Screen: {screenPosition}, World: {worldPosition}");
        
        // TODO: Xử lý logic khi đang nối điểm
        // Ví dụ: Cập nhật line renderer, detect điểm đang được hover
    }
    
    // Xử lý khi người chơi kết thúc hold/drag
    private void HandleHoldEnd(Vector2 screenPosition)
    {
        Vector3 worldPosition = inputManager.GetWorldPosition(screenPosition, mainCamera);
        Debug.Log($"[HOLD END] Screen: {screenPosition}, World: {worldPosition}");
        
        // TODO: Xử lý logic kết thúc nối điểm
        // Ví dụ: Kiểm tra xem đã nối đúng các điểm chưa, xóa line renderer
    }
}
