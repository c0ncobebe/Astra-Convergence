using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class LevelButton : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image lockIcon;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject unlockedOverlay;
    [SerializeField] private Button button;
    
    [Header("Visual States")]
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;
    
    private LevelInfo levelInfo;
    private bool isUnlocked;
    private System.Action<LevelInfo> onLevelSelected;
    
    public void Initialize(LevelInfo info, bool unlocked, System.Action<LevelInfo> callback)
    {
        levelInfo = info;
        isUnlocked = unlocked;
        onLevelSelected = callback;
        
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (levelInfo == null) return;
        
        if (thumbnailImage != null && levelInfo.thumbnailImage != null)
        {
            thumbnailImage.sprite = levelInfo.thumbnailImage;
            thumbnailImage.color = isUnlocked ? unlockedColor : lockedColor;
            thumbnailImage.SetNativeSize();
        }
        
        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(!isUnlocked);
        }
        
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }
        if (unlockedOverlay != null)
        {
            unlockedOverlay.SetActive(isUnlocked);
        }
        
        if (button != null)
        {
            button.interactable = isUnlocked;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isUnlocked && levelInfo != null)
        {
            onLevelSelected?.Invoke(levelInfo);
        }
        else
        {
            PlayLockedFeedback();
        }
    }
    
    private void PlayLockedFeedback()
    {
        if (lockIcon != null)
        {
            lockIcon.transform.DOComplete();
            lockIcon.transform.DOShakeRotation(0.3f, new Vector3(0, 0, 15f), 10, 90f);
        }
    }
    
    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        UpdateVisuals();
        
        if (unlocked && lockIcon != null)
        {
            lockIcon.transform.DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => lockIcon.gameObject.SetActive(false));
        }
    }
    
    public LevelInfo GetLevelInfo()
    {
        return levelInfo;
    }
    
    public bool IsUnlocked()
    {
        return isUnlocked;
    }
}
