using UnityEngine;

#if UNITY_EDITOR
/// <summary>
/// Helper để test zoom/pan trong Unity Editor
/// - Mouse Scroll Wheel = Zoom in/out (dễ nhất)
/// - Ctrl + Mouse drag = Simulate pinch zoom
/// - Mouse drag = Pan (nếu không có điểm)
/// </summary>
public class EditorTouchSimulator : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private bool enableScrollWheelZoom = true;
    [SerializeField] private float scrollWheelZoomSpeed = 0.5f;
    
    [Header("Pinch Simulation")]
    [SerializeField] private KeyCode zoomModifierKey = KeyCode.LeftControl;
    [SerializeField] private float simulatedFingerDistance = 200f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private CameraController cameraController;
    private Vector2 screenCenter;
    private bool wasSimulatingZoom = false;
    private float previousSimulatedDistance;
    
    void Start()
    {
        screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        cameraController = Camera.main?.GetComponent<CameraController>();
        
        if (cameraController == null)
        {
            Debug.LogWarning("[EditorTouchSimulator] CameraController not found on Main Camera!");
        }
    }
    
    void Update()
    {
        // Chỉ hoạt động trong Editor
        if (!Application.isEditor) return;
        
        // Scroll wheel zoom (dễ nhất)
        if (enableScrollWheelZoom && cameraController != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                Camera cam = Camera.main;
                float currentSize = cam.orthographicSize;
                float newSize = currentSize - (scroll * scrollWheelZoomSpeed * currentSize);
                cameraController.SetZoom(newSize, false);
                
                if (showDebugInfo && Mathf.Abs(scroll) > 0.01f)
                {
                    Debug.Log($"[EditorTouchSimulator] Scroll wheel zoom: {currentSize:F2} → {newSize:F2}");
                }
            }
        }
        
        // Ctrl + Mouse = Simulate pinch zoom
        bool isZoomKeyPressed = Input.GetKey(zoomModifierKey);
        bool isMousePressed = Input.GetMouseButton(0);
        
        if (isZoomKeyPressed && isMousePressed)
        {
            SimulatePinchZoom();
        }
        else if (wasSimulatingZoom)
        {
            // Kết thúc simulation
            wasSimulatingZoom = false;
            if (showDebugInfo)
            {
                Debug.Log("[EditorTouchSimulator] Pinch zoom simulation ended");
            }
        }
    }
    
    private void SimulatePinchZoom()
    {
        Vector2 mousePos = Input.mousePosition;
        
        // Calculate simulated finger positions
        Vector2 direction = (mousePos - screenCenter).normalized;
        
        // Thay đổi khoảng cách dựa trên mouse wheel hoặc vertical mouse movement
        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(mouseWheel) > 0.01f)
        {
            simulatedFingerDistance += mouseWheel * 100f;
            simulatedFingerDistance = Mathf.Clamp(simulatedFingerDistance, 50f, 500f);
        }
        
        Vector2 finger1Pos = screenCenter + direction * simulatedFingerDistance * 0.5f;
        Vector2 finger2Pos = screenCenter - direction * simulatedFingerDistance * 0.5f;
        
        if (!wasSimulatingZoom)
        {
            // Bắt đầu simulation
            wasSimulatingZoom = true;
            previousSimulatedDistance = simulatedFingerDistance;
            
            if (showDebugInfo)
            {
                Debug.Log($"[EditorTouchSimulator] Started pinch zoom simulation.");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.DrawLine(Camera.main.ScreenToWorldPoint(finger1Pos), 
                          Camera.main.ScreenToWorldPoint(finger2Pos), 
                          Color.yellow);
        }
        
        previousSimulatedDistance = simulatedFingerDistance;
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isEditor) return;
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.cyan;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        
        string instructions = $"Editor Controls:\n" +
                            $"• Scroll Wheel = Zoom In/Out (Easy!)\n" +
                            $"• {zoomModifierKey} + Mouse Drag = Pinch Zoom\n" +
                            $"• Mouse Drag = Pan Camera\n" +
                            $"• Click + Drag Point = Connect Points";
        
        GUI.Label(new Rect(10, 10, 500, 100), instructions, style);
        
        if (wasSimulatingZoom)
        {
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(10, 120, 400, 30), $"Pinch Zooming (Distance: {simulatedFingerDistance:F0})", style);
        }
        
        // Show current zoom level
        if (cameraController != null)
        {
            Camera cam = Camera.main;
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(10, 150, 400, 30), $"Zoom: {cam.orthographicSize:F2} (Min: 2, Max: 12)", style);
        }
    }
}
#endif
