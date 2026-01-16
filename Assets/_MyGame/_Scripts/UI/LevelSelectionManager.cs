using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Quản lý UI chọn level với scroll view
/// </summary>
public class LevelSelectionManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private List<LevelInfo> allLevels = new List<LevelInfo>(); // Danh sách tất cả levels
    
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform levelButtonContainer; // Content của ScrollView
    [SerializeField] private LevelButton levelButtonPrefab; // Prefab của level button
    
    [Header("Loading")]
    [SerializeField] private string loadingSceneName = "Loading"; // Scene loading trung gian
    [SerializeField] private bool useLoadingScene = true; // Có dùng scene loading không
    
    [Header("Animation")]
    [SerializeField] private float buttonSpawnDelay = 0.05f; // Delay giữa mỗi button spawn
    [SerializeField] private bool animateButtonSpawn = true;
    
    private List<LevelButton> levelButtons = new List<LevelButton>();
    private LevelProgressManager progressManager;
    
    private void Start()
    {
        progressManager = LevelProgressManager.Instance;
        InitializeLevelButtons();
    }
    
    /// <summary>
    /// Khởi tạo tất cả level buttons
    /// </summary>
    private void InitializeLevelButtons()
    {
        if (levelButtonPrefab == null || levelButtonContainer == null)
        {
            Debug.LogError("[LevelSelectionManager] Missing prefab or container!");
            return;
        }
        
        // Clear existing buttons
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }
        levelButtons.Clear();
        
        // Không sort - thứ tự level được xác định bởi thứ tự trong list allLevels
        
        // Tạo buttons
        if (animateButtonSpawn)
        {
            StartCoroutine(SpawnButtonsAnimated());
        }
        else
        {
            SpawnAllButtons();
        }
    }
    
    /// <summary>
    /// Spawn tất cả buttons ngay lập tức
    /// </summary>
    private void SpawnAllButtons()
    {
        for (int i = 0; i < allLevels.Count; i++)
        {
            CreateLevelButton(allLevels[i], i);
        }
    }
    
    /// <summary>
    /// Spawn buttons với animation
    /// </summary>
    private System.Collections.IEnumerator SpawnButtonsAnimated()
    {
        for (int i = 0; i < allLevels.Count; i++)
        {
            LevelButton button = CreateLevelButton(allLevels[i], i);
            
            // Scale animation
            button.transform.localScale = Vector3.zero;
            button.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(i * buttonSpawnDelay);
            
            yield return new WaitForSeconds(buttonSpawnDelay);
        }
    }
    
    /// <summary>
    /// Tạo một level button
    /// </summary>
    private LevelButton CreateLevelButton(LevelInfo levelInfo, int index)
    {
        LevelButton button = Instantiate(levelButtonPrefab, levelButtonContainer);
        button.name = $"LevelButton_{index}_{levelInfo.levelName}";
        
        // Kiểm tra level có mở khóa không (dựa trên vị trí trong list)
        bool isUnlocked = IsLevelUnlocked(index);
        
        // Initialize button
        button.Initialize(levelInfo, isUnlocked, OnLevelSelected);
        
        levelButtons.Add(button);
        return button;
    }
    
    /// <summary>
    /// Kiểm tra level có được mở khóa không
    /// Logic đơn giản: Level đầu tiên luôn mở, các level sau mở khi level trước hoàn thành
    /// </summary>
    /// <param name="positionInList">Vị trí của level trong list allLevels</param>
    private bool IsLevelUnlocked(int positionInList)
    {
        // Level đầu tiên (vị trí 0) luôn mở
        if (positionInList == 0)
        {
            return true;
        }
        
        // Các level khác: kiểm tra level trước (theo vị trí trong list) đã hoàn thành chưa
        // Lấy levelIndex của level trước để check với save data
        LevelInfo previousLevel = allLevels[positionInList - 1];
        return progressManager.IsLevelCompleted(previousLevel.levelIndex);
    }
    
    /// <summary>
    /// Xử lý khi chọn một level
    /// </summary>
    private void OnLevelSelected(LevelInfo levelInfo)
    {
        Debug.Log($"[LevelSelectionManager] Selected level: {levelInfo.levelName}");
        
        // Lưu level hiện tại
        progressManager.SetCurrentLevel(levelInfo.levelIndex);
        
        // Chuyển sang gameplay state
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SwitchToGameplay(levelInfo);
        }
        else
        {
            Debug.LogError("[LevelSelectionManager] GameStateManager not found! Please add GameStateManager to scene.");
        }
    }
    
    /// <summary>
    /// Refresh trạng thái của tất cả buttons
    /// </summary>
    public void RefreshButtons()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            bool isUnlocked = IsLevelUnlocked(i);
            levelButtons[i].SetUnlocked(isUnlocked);
        }
    }
    
    /// <summary>
    /// Scroll đến một level cụ thể
    /// </summary>
    public void ScrollToLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelButtons.Count)
            return;
        
        if (scrollRect == null)
            return;
        
        // Calculate position
        RectTransform buttonRect = levelButtons[levelIndex].GetComponent<RectTransform>();
        RectTransform contentRect = levelButtonContainer.GetComponent<RectTransform>();
        RectTransform viewportRect = scrollRect.viewport;
        
        // Smooth scroll
        Canvas.ForceUpdateCanvases();
        
        float targetY = (buttonRect.anchoredPosition.y - contentRect.anchoredPosition.y) 
                       / (contentRect.rect.height - viewportRect.rect.height);
        
        DOTween.To(() => scrollRect.verticalNormalizedPosition, 
                   x => scrollRect.verticalNormalizedPosition = x, 
                   targetY, 
                   0.5f)
            .SetEase(Ease.OutCubic);
    }
    
    /// <summary>
    /// Thêm level mới vào danh sách
    /// </summary>
    public void AddLevel(LevelInfo levelInfo)
    {
        if (!allLevels.Contains(levelInfo))
        {
            allLevels.Add(levelInfo);
            InitializeLevelButtons();
        }
    }
    
    /// <summary>
    /// Debug: Mở khóa tất cả levels
    /// </summary>
    [ContextMenu("Unlock All Levels")]
    public void UnlockAllLevels()
    {
        for (int i = 0; i < allLevels.Count; i++)
        {
            progressManager.CompleteLevel(i);
        }
        RefreshButtons();
    }
    
    /// <summary>
    /// Debug: Reset tất cả progress
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        progressManager.ResetAllProgress();
        RefreshButtons();
    }
}
