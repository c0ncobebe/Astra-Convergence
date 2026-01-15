using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Helper component để xử lý level complete event và chuyển scene
/// Attach vào GameObject có GamePlayManager hoặc tạo riêng
/// </summary>
public class LevelCompleteHandler : MonoBehaviour
{
    [Header("Win Settings")]
    [SerializeField] private float delayBeforeNextScene = 2f; // Delay trước khi chuyển scene
    [SerializeField] private string levelSelectionSceneName = "LevelSelection";
    [SerializeField] private bool autoLoadNextLevel = false; // Tự động load level kế tiếp
    
    [Header("UI (Optional)")]
    [SerializeField] private GameObject winPanel; // Panel hiển thị khi thắng
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip winSound;
    
    private bool levelCompleted = false;
    
    /// <summary>
    /// Gọi function này khi level hoàn thành
    /// Có thể gọi từ GamePlayManager hoặc event system
    /// </summary>
    public void OnLevelComplete()
    {
        if (levelCompleted) return; // Prevent multiple calls
        
        levelCompleted = true;
        
        Debug.Log("[LevelCompleteHandler] Level Complete!");
        
        // Play sound effect
        if (winSound != null)
        {
            AudioSource.PlayClipAtPoint(winSound, Camera.main.transform.position);
        }
        
        // Show win panel
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
        
        // Save progress
        SaveLevelProgress();
        
        // Start transition coroutine
        StartCoroutine(TransitionToNextScene());
    }
    
    /// <summary>
    /// Lưu tiến trình level vào LevelProgressManager
    /// </summary>
    private void SaveLevelProgress()
    {
        int currentLevel = LevelProgressManager.Instance.GetCurrentLevel();
        
        // Có thể tính số stars dựa trên performance (thời gian, moves, etc.)
        int stars = CalculateStars();
        
        // Save progress
        LevelProgressManager.Instance.CompleteLevel(currentLevel, stars);
        
        Debug.Log($"[LevelCompleteHandler] Saved progress for level {currentLevel} with {stars} stars");
    }
    
    /// <summary>
    /// Tính số stars cho level (1-3 stars)
    /// Override method này để custom logic tính stars
    /// </summary>
    protected virtual int CalculateStars()
    {
        // Mặc định cho 3 sao
        // Có thể custom dựa trên:
        // - Thời gian hoàn thành
        // - Số moves/actions
        // - Không dùng hint
        // - etc.
        
        return 3;
    }
    
    /// <summary>
    /// Coroutine chuyển scene sau delay
    /// </summary>
    private IEnumerator TransitionToNextScene()
    {
        yield return new WaitForSeconds(delayBeforeNextScene);
        
        if (autoLoadNextLevel)
        {
            LoadNextLevel();
        }
        else
        {
            BackToLevelSelection();
        }
    }
    
    /// <summary>
    /// Quay về màn hình chọn level
    /// </summary>
    public void BackToLevelSelection()
    {
        SceneManager.LoadScene(levelSelectionSceneName);
    }
    
    /// <summary>
    /// Load level kế tiếp
    /// </summary>
    public void LoadNextLevel()
    {
        int currentLevel = LevelProgressManager.Instance.GetCurrentLevel();
        int nextLevel = currentLevel + 1;
        
        // Kiểm tra level kế tiếp có tồn tại không
        // (cần có cách kiểm tra, ví dụ scene name pattern hoặc level list)
        string nextSceneName = $"Level_{nextLevel + 1}"; // Giả sử pattern Level_1, Level_2...
        
        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            LevelProgressManager.Instance.SetCurrentLevel(nextLevel);
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("[LevelCompleteHandler] No more levels, back to level selection");
            BackToLevelSelection();
        }
    }
    
    /// <summary>
    /// Replay level hiện tại
    /// </summary>
    public void ReplayLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
