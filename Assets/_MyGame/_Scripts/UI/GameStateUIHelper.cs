using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component cho UI buttons để tương tác với GameStateManager
/// Attach vào buttons trong UI
/// </summary>
public class GameStateUIHelper : MonoBehaviour
{
    /// <summary>
    /// Button Back - quay về Home Menu
    /// </summary>
    public void OnBackToHomeButton()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SwitchToHomeMenu();
        }
    }
    
    /// <summary>
    /// Button Replay - chơi lại level hiện tại
    /// </summary>
    public void OnReplayLevelButton()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsInGameplay())
        {
            LevelInfo currentLevel = GameStateManager.Instance.GetCurrentLevelInfo();
            if (currentLevel != null)
            {
                GameStateManager.Instance.SwitchToGameplay(currentLevel);
            }
        }
    }
    
    /// <summary>
    /// Button Next Level - chơi level tiếp theo
    /// </summary>
    public void OnNextLevelButton()
    {
        if (GameStateManager.Instance != null)
        {
            LevelInfo currentLevel = GameStateManager.Instance.GetCurrentLevelInfo();
            if (currentLevel != null)
            {
                // Tìm level kế tiếp
                LevelSelectionManager levelSelection = FindObjectOfType<LevelSelectionManager>();
                if (levelSelection != null)
                {
                    // TODO: Implement GetNextLevel in LevelSelectionManager
                    Debug.Log("Next level button - need to implement GetNextLevel");
                }
            }
        }
    }
    
    /// <summary>
    /// Button Quit - thoát app
    /// </summary>
    public void OnQuitButton()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Handle Android Back Button
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnBackButton();
            }
        }
    }
}
