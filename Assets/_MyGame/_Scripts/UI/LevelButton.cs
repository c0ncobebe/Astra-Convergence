using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// Component điều khiển UI của một level button
/// </summary>
public class LevelButton : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image lockIcon;
    [SerializeField] private GameObject lockedOverlay; // Dark overlay khi bị khóa
    [SerializeField] private GameObject unlockedOverlay; // Dark overlay khi bị khóa
    [SerializeField] private Button button;
    
    [Header("Visual States")]
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;
    
    private LevelInfo levelInfo;
    private bool isUnlocked;
    private System.Action<LevelInfo> onLevelSelected;
    
    /// <summary>
    /// Khởi tạo level button với thông tin level
    /// </summary>
    public void Initialize(LevelInfo info, bool unlocked, System.Action<LevelInfo> callback)
    {
        levelInfo = info;
        isUnlocked = unlocked;
        onLevelSelected = callback;
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// Cập nhật giao diện theo trạng thái khóa/mở
    /// </summary>
    private void UpdateVisuals()
    {
        if (levelInfo == null) return;
        
        // Set thumbnail
        if (thumbnailImage != null && levelInfo.thumbnailImage != null)
        {
            thumbnailImage.sprite = levelInfo.thumbnailImage;
            thumbnailImage.color = isUnlocked ? unlockedColor : lockedColor;
            thumbnailImage.SetNativeSize();
        }
        
        
        // Show/hide lock icon
        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(!isUnlocked);
        }
        
        // Show/hide locked overlay
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }
        if (unlockedOverlay != null)
        {
            unlockedOverlay.SetActive(isUnlocked);
        }
        // Enable/disable button
        if (button != null)
        {
            button.interactable = isUnlocked;
        }
    }
    
    /// <summary>
    /// Xử lý khi click vào button
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isUnlocked && levelInfo != null)
        {
            onLevelSelected?.Invoke(levelInfo);
        }
        else
        {
            // Có thể thêm hiệu ứng shake hoặc âm thanh khi click vào level bị khóa
            PlayLockedFeedback();
        }
    }
    
    /// <summary>
    /// Hiệu ứng khi click vào level bị khóa
    /// </summary>
    private void PlayLockedFeedback()
    {
        // Có thể thêm âm thanh
        // AudioManager.Instance?.PlaySound("locked");
        
        // Shake animation đơn giản
        if (lockIcon != null)
        {
            lockIcon.transform.DOKill();
            lockIcon.transform.DOShakeRotation(0.3f, new Vector3(0, 0, 15f), 10, 90f);
        }
    }
    
    /// <summary>
    /// Cập nhật trạng thái mở khóa
    /// </summary>
    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        UpdateVisuals();
        
        // Animation mở khóa
        if (unlocked && lockIcon != null)
        {
            lockIcon.transform.DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => lockIcon.gameObject.SetActive(false));
        }
    }
    
    /// <summary>
    /// Lấy thông tin level
    /// </summary>
    public LevelInfo GetLevelInfo()
    {
        return levelInfo;
    }
    
    /// <summary>
    /// Kiểm tra level có mở khóa không
    /// </summary>
    public bool IsUnlocked()
    {
        return isUnlocked;
    }
}
