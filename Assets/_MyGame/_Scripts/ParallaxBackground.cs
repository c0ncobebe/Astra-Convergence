using UnityEngine;

/// <summary>
/// Parallax effect cho background - tạo cảm giác depth khi camera zoom/pan
/// Background sẽ di chuyển và scale chậm hơn foreground
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("Hệ số parallax cho movement (0-1). 0 = không di chuyển, 1 = di chuyển như foreground")]
    [SerializeField] private float parallaxFactorPosition = 0.3f;
    
    [Tooltip("Hệ số parallax cho scale/zoom (0-1). 0 = không zoom, 1 = zoom như foreground")]
    [SerializeField] private float parallaxFactorScale = 0.5f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Initial State")]
    [SerializeField] private bool captureInitialStateOnStart = true;
    
    private Vector3 initialCameraPosition;
    private float initialCameraOrthographicSize;
    private Vector3 initialBackgroundPosition;
    private Vector3 initialBackgroundScale;
    
    private Camera mainCamera;
    
    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        mainCamera = cameraTransform.GetComponent<Camera>();
        
        if (captureInitialStateOnStart)
        {
            CaptureInitialState();
        }
    }
    
    void LateUpdate()
    {
        if (cameraTransform == null || mainCamera == null) return;
        
        ApplyParallax();
    }
    
    /// <summary>
    /// Capture initial camera and background state
    /// </summary>
    public void CaptureInitialState()
    {
        if (cameraTransform != null)
        {
            initialCameraPosition = cameraTransform.position;
            
            if (mainCamera == null)
            {
                mainCamera = cameraTransform.GetComponent<Camera>();
            }
            
            if (mainCamera != null)
            {
                initialCameraOrthographicSize = mainCamera.orthographicSize;
            }
        }
        
        initialBackgroundPosition = transform.position;
        initialBackgroundScale = transform.localScale;
    }
    
    /// <summary>
    /// Apply parallax effect based on camera movement and zoom
    /// </summary>
    private void ApplyParallax()
    {
        // Calculate camera delta
        Vector3 cameraDelta = cameraTransform.position - initialCameraPosition;
        
        // Apply parallax to position
        // Background di chuyển chậm hơn (parallaxFactorPosition < 1)
        Vector3 parallaxOffset = new Vector3(
            cameraDelta.x * parallaxFactorPosition,
            cameraDelta.y * parallaxFactorPosition,
            0
        );
        
        Vector3 targetPosition = initialBackgroundPosition + parallaxOffset;
        targetPosition.z = transform.position.z; // Giữ nguyên Z
        transform.position = targetPosition;
        
        // Apply parallax to scale (zoom effect)
        if (mainCamera != null && parallaxFactorScale > 0)
        {
            float zoomRatio = mainCamera.orthographicSize / initialCameraOrthographicSize;
            
            // Background scale chậm hơn (parallaxFactorScale < 1)
            // Công thức: newScale = initialScale * (1 + (zoomRatio - 1) * parallaxFactor)
            float scaleMultiplier = 1f + (zoomRatio - 1f) * parallaxFactorScale;
            
            Vector3 targetScale = initialBackgroundScale * scaleMultiplier;
            transform.localScale = targetScale;
        }
    }
    
    /// <summary>
    /// Reset background to initial state
    /// </summary>
    public void ResetToInitial()
    {
        transform.position = initialBackgroundPosition;
        transform.localScale = initialBackgroundScale;
    }
    
    /// <summary>
    /// Set parallax factors at runtime
    /// </summary>
    public void SetParallaxFactors(float positionFactor, float scaleFactor)
    {
        parallaxFactorPosition = Mathf.Clamp01(positionFactor);
        parallaxFactorScale = Mathf.Clamp01(scaleFactor);
    }
}
