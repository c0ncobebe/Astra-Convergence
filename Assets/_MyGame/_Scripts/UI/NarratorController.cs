using System;
using System.Collections;
using AstraNexus.Audio;
using ChocDino.UIFX;
using Febucci.UI;
using KienNT;
using MoreMountains.NiceVibrations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NarratorController : MonoBehaviour
{
    public enum AppearanceEffect
    {
        None,           // Hiện luôn không có hiệu ứng
        Typewriter,     // Hiệu ứng đánh máy cơ bản
        Fade,           // Fade từ trong suốt
        Vertical,       // Từ trên/dưới xuất hiện
        Horizontal,     // Từ trái/phải xuất hiện
        Size,           // Scale từ nhỏ đến lớn
        Offset,         // Random offset position
        Rotation,       // Xoay vào
        Wave,           // Sóng từ dưới lên
        Shake,          // Rung lắc khi xuất hiện
    }
    
    [Header("Components")]
    [SerializeField] private TextAnimatorPlayer textAnimatorPlayer;
    [SerializeField] private TextAnimator textAnimator;
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private GlowFilter frameGlowFilter;
    [SerializeField] private Button nextButton;
    public GameObject tutorialCanvas;
    
    [Header("Appearance Settings")]
    [SerializeField] private AppearanceEffect appearanceEffect = AppearanceEffect.Typewriter;
    [SerializeField] private float typewriterSpeed = 30f; // Characters per second
    [SerializeField] private bool skipWithInput = true;
    [SerializeField] private bool autoHideAfterComplete = false;
    [SerializeField] private float autoHideDelay = 2f;
    
    [Header("Events")]
    public UnityEvent OnTextStart;
    public UnityEvent OnTextComplete;
    public UnityEvent OnSequenceComplete;
    
    private bool isPlaying = false;
    private Coroutine autoHideCoroutine;
    
    private string[] currentSequence;
    private int currentSequenceIndex = 0;
    private bool isInSequenceMode = false;
    
    void Awake()
    {
        if (textAnimatorPlayer == null)
            textAnimatorPlayer = GetComponent<TextAnimatorPlayer>();
        
        if (textAnimator == null)
            textAnimator = GetComponent<TextAnimator>();
        
        if (tmpText == null)
            tmpText = GetComponent<TextMeshProUGUI>();
        
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
            nextButton.gameObject.SetActive(false);
        }
    }
    
    void Start()
    {
        // frameGlowFilter.Color
    }
    
    void Update()
    {
        // Skip với input
        if (skipWithInput && isPlaying && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
        {
            SkipToEnd();
        }
    }
    
    /// <summary>
    /// Hiển thị text với hiệu ứng đã chọn
    /// </summary>
    public void ShowText(string text)
    {
        VibrationController.Instance.HapticPulse(HapticTypes.MediumImpact);
        SoundManager.Instance.PlaySound(SoundType.Meow);
        StopAllCoroutines();
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        
        // Ẩn next button khi đang show text mới
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
        
        isPlaying = true;
        
        // Wrap text với tag hiệu ứng nếu cần
        string formattedText = FormatTextWithEffect(text);
        
        if (tmpText != null)
        {
            tmpText.text = formattedText;
        }
        
        if (textAnimatorPlayer != null)
        {
            textAnimatorPlayer.onTypewriterStart.AddListener(OnTypewriterStart);
            textAnimatorPlayer.onTextShowed.AddListener(OnTypewriterComplete);
            textAnimatorPlayer.ShowText(formattedText);
        }
        
        OnTextStart?.Invoke();
    }
    
    /// <summary>
    /// Show text theo từng dòng với manual next
    /// </summary>
    public void ShowTextSequence(string[] texts, float delayBetweenLines = 1f)
    {
        currentSequence = texts;
        currentSequenceIndex = 0;
        isInSequenceMode = true;
        
        if (texts != null && texts.Length > 0)
        {
            ShowText(texts[0]);
        }
    }
    
    /// <summary>
    /// Gọi khi ấn next button
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (!isInSequenceMode || currentSequence == null)
            return;
        
        currentSequenceIndex++;
        
        if (currentSequenceIndex < currentSequence.Length)
        {
            ShowText(currentSequence[currentSequenceIndex]);
        }
        else
        {
            // Hết sequence
            isInSequenceMode = false;
            if (nextButton != null)
                nextButton.gameObject.SetActive(false);
            
            // Ẩn toàn bộ canvas tutorial
            if (tutorialCanvas != null)
                tutorialCanvas.SetActive(false);
            
            // Invoke event khi hết sequence
            OnSequenceComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// Skip đến cuối text ngay lập tức
    /// </summary>
    public void SkipToEnd()
    {
        if (textAnimatorPlayer != null && isPlaying)
        {
            textAnimatorPlayer.SkipTypewriter();
        }
    }
    
    /// <summary>
    /// Ẩn text
    /// </summary>
    public void HideText()
    {
        if (tmpText != null)
        {
            tmpText.text = "";
        }
        isPlaying = false;
    }
    
    /// <summary>
    /// Set speed của typewriter
    /// </summary>
    public void SetTypewriterSpeed(float speed)
    {
        typewriterSpeed = speed;
        // Speed được điều chỉnh qua waitForNormalChars của TextAnimatorPlayer
        if (textAnimatorPlayer != null)
        {
            textAnimatorPlayer.waitForNormalChars = 1f / speed; // Convert speed to wait time
        }
    }
    
    private string FormatTextWithEffect(string text)
    {
        switch (appearanceEffect)
        {
            case AppearanceEffect.None:
                return text;
            
            case AppearanceEffect.Typewriter:
                return text; // Default behavior
            
            case AppearanceEffect.Fade:
                return $"<fade>{text}<fade>";
            
            case AppearanceEffect.Vertical:
                return $"<vert>{text}<vert>";
            
            case AppearanceEffect.Horizontal:
                return $"<hori>{text}<hori>";
            
            case AppearanceEffect.Size:
                return $"<size>{text}<size>";
            
            case AppearanceEffect.Offset:
                return $"<offset>{text}<offset>";
            
            case AppearanceEffect.Rotation:
                return $"<rot>{text}<rot>";
            
            case AppearanceEffect.Wave:
                return $"<wave>{text}<wave>";
            
            case AppearanceEffect.Shake:
                return $"<shake>{text}<shake>";
            
            default:
                return text;
        }
    }
    
    private void OnTypewriterStart()
    {
        isPlaying = true;
    }
    
    private void OnTypewriterComplete()
    {
        isPlaying = false;
        OnTextComplete?.Invoke();
        
        // Hiện next button nếu đang trong sequence mode
        if (isInSequenceMode && nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
        }
        
        if (autoHideAfterComplete)
        {
            autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
        }
    }
    
    private IEnumerator AutoHideCoroutine()
    {
        yield return new WaitForSeconds(autoHideDelay);
        HideText();
    }
    
    void OnDestroy()
    {
        if (textAnimatorPlayer != null)
        {
            textAnimatorPlayer.onTypewriterStart.RemoveListener(OnTypewriterStart);
            textAnimatorPlayer.onTextShowed.RemoveListener(OnTypewriterComplete);
        }
    }
}
