using UnityEngine;
using DG.Tweening;

/// <summary>
/// Component gán vào các object Star để quản lý trạng thái và hành vi
/// Sử dụng DOTween cho smooth animations
/// </summary>
public class Star : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform visualTransform; // Transform để scale/animate
    
    [Header("State Colors")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color connectedColor = Color.green;
    
    [Header("Scale Settings")]
    [SerializeField] private float idleScale = 1f;
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float connectedScale = 1.1f;
    
    [Header("Animation Settings (DOTween)")]
    [SerializeField] private float scaleDuration = 0.2f; // Thời gian chuyển scale
    [SerializeField] private float colorDuration = 0.2f; // Thời gian chuyển màu
    [SerializeField] private Ease scaleEase = Ease.OutBack; // Kiểu easing cho scale
    [SerializeField] private Ease colorEase = Ease.OutQuad; // Kiểu easing cho color
    
    [Header("Pulse Animation (Hover)")]
    [SerializeField] private bool usePulseAnimation = true;
    [SerializeField] private float pulseDuration = 0.5f; // Thời gian 1 lần đập
    [SerializeField] private float pulseScale = 0.1f; // Mức độ phóng to khi đập
    
    // State tracking
    private StarState currentState = StarState.Idle;
    
    // DOTween references
    private Tweener scaleTween;
    private Tweener colorTween;
    private Sequence pulseSequence;
    
    // Properties
    public StarState CurrentState => currentState;
    public int StarID { get; set; } // ID hoặc index của sao
    public bool IsConnected => currentState == StarState.Connected;
    
    private void Awake()
    {
        // Auto-find components nếu chưa assign
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (visualTransform == null)
            visualTransform = transform;
            
        // Khởi tạo state ban đầu
        SetState(StarState.Idle);
    }
    
    private void OnDestroy()
    {
        // Kill tất cả tweens để tránh memory leak
        KillAllTweens();
    }
    
    /// <summary>
    /// Đặt trạng thái cho sao sử dụng DOTween
    /// </summary>
    public void SetState(StarState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        
        // Kill pulse animation nếu đang chạy
        StopPulseAnimation();
        
        // Xác định target scale và color theo state
        Vector3 targetScale;
        Color targetColor;
        
        switch (newState)
        {
            case StarState.Idle:
                targetScale = Vector3.one * idleScale;
                targetColor = idleColor;
                break;
                
            case StarState.Hover:
                targetScale = Vector3.one * hoverScale;
                targetColor = hoverColor;
                break;
                
            case StarState.Connected:
                targetScale = Vector3.one * connectedScale;
                targetColor = connectedColor;
                break;
                
            default:
                targetScale = Vector3.one * idleScale;
                targetColor = idleColor;
                break;
        }
        
        // Animate scale với DOTween
        AnimateScale(targetScale);
        
        // Animate color với DOTween
        AnimateColor(targetColor);
        
        // Start pulse animation nếu hover
        if (newState == StarState.Hover && usePulseAnimation)
        {
            StartPulseAnimation();
        }
    }
    
    /// <summary>
    /// Animate scale sử dụng DOTween
    /// </summary>
    private void AnimateScale(Vector3 target)
    {
        if (visualTransform == null) return;
        
        // Kill tween cũ nếu đang chạy
        scaleTween?.Kill();
        
        // Tạo tween mới
        scaleTween = visualTransform.DOScale(target, scaleDuration)
            .SetEase(scaleEase)
            .SetUpdate(true); // Update even when timescale = 0
    }
    
    /// <summary>
    /// Animate color sử dụng DOTween
    /// </summary>
    private void AnimateColor(Color target)
    {
        if (spriteRenderer == null) return;
        
        // Kill tween cũ nếu đang chạy
        colorTween?.Kill();
        
        // Tạo tween mới
        colorTween = spriteRenderer.DOColor(target, colorDuration)
            .SetEase(colorEase)
            .SetUpdate(true);
    }
    
    /// <summary>
    /// Start pulse animation cho Hover state
    /// </summary>
    private void StartPulseAnimation()
    {
        if (visualTransform == null) return;
        
        // Kill pulse cũ nếu có
        pulseSequence?.Kill();
        
        // Tạo pulse sequence
        Vector3 baseScale = Vector3.one * hoverScale;
        Vector3 pulseMax = baseScale + Vector3.one * pulseScale;
        
        pulseSequence = DOTween.Sequence();
        pulseSequence.Append(visualTransform.DOScale(pulseMax, pulseDuration / 2).SetEase(Ease.InOutSine));
        pulseSequence.Append(visualTransform.DOScale(baseScale, pulseDuration / 2).SetEase(Ease.InOutSine));
        pulseSequence.SetLoops(-1) // Loop vô hạn
            .SetUpdate(true);
    }
    
    /// <summary>
    /// Stop pulse animation
    /// </summary>
    private void StopPulseAnimation()
    {
        pulseSequence?.Kill();
        pulseSequence = null;
    }
    
    /// <summary>
    /// Kill tất cả tweens
    /// </summary>
    private void KillAllTweens()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        pulseSequence?.Kill();
        
        scaleTween = null;
        colorTween = null;
        pulseSequence = null;
    }
    
    /// <summary>
    /// Chuyển sang trạng thái Hover
    /// </summary>
    public void OnHoverEnter()
    {
        if (currentState != StarState.Connected)
        {
            SetState(StarState.Hover);
        }
    }
    
    /// <summary>
    /// Thoát khỏi trạng thái Hover
    /// </summary>
    public void OnHoverExit()
    {
        if (currentState == StarState.Hover)
        {
            SetState(StarState.Idle);
        }
    }
    
    /// <summary>
    /// Đánh dấu sao đã được nối
    /// </summary>
    public void OnConnected()
    {
        SetState(StarState.Connected);
    }
    
    /// <summary>
    /// Reset về trạng thái ban đầu
    /// </summary>
    public void ResetState()
    {
        SetState(StarState.Idle);
    }
    
    /// <summary>
    /// Set màu sắc cho từng trạng thái (có thể gọi từ code)
    /// </summary>
    public void SetStateColors(Color idle, Color hover, Color connected)
    {
        idleColor = idle;
        hoverColor = hover;
        connectedColor = connected;
        
        // Re-apply current state với màu mới
        StarState temp = currentState;
        currentState = StarState.Idle; // Reset để trigger SetState
        SetState(temp);
    }
    
    /// <summary>
    /// Set scale cho từng trạng thái
    /// </summary>
    public void SetStateScales(float idle, float hover, float connected)
    {
        idleScale = idle;
        hoverScale = hover;
        connectedScale = connected;
        
        // Re-apply current state với scale mới
        StarState temp = currentState;
        currentState = StarState.Idle; // Reset để trigger SetState
        SetState(temp);
    }
    
    /// <summary>
    /// Set animation settings
    /// </summary>
    public void SetAnimationSettings(float scaleDur, float colorDur, Ease scaleEasing, Ease colorEasing)
    {
        scaleDuration = scaleDur;
        colorDuration = colorDur;
        scaleEase = scaleEasing;
        colorEase = colorEasing;
    }
    
    /// <summary>
    /// Set pulse animation settings
    /// </summary>
    public void SetPulseSettings(bool enable, float duration, float scale)
    {
        usePulseAnimation = enable;
        pulseDuration = duration;
        pulseScale = scale;
        
        // Restart pulse nếu đang hover
        if (currentState == StarState.Hover)
        {
            StopPulseAnimation();
            if (usePulseAnimation)
            {
                StartPulseAnimation();
            }
        }
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = currentState switch
        {
            StarState.Idle => Color.white,
            StarState.Hover => Color.yellow,
            StarState.Connected => Color.green,
            _ => Color.white
        };
        
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}

/// <summary>
/// Enum định nghĩa các trạng thái của Star
/// </summary>
public enum StarState
{
    Idle,       // Trạng thái bình thường, chưa tương tác
    Hover,      // Đang được hover (ngón tay/chuột đang ở trên)
    Connected   // Đã được nối
}

