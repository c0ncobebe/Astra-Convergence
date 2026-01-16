using UnityEngine;
using UnityEngine.UI;

public class GameStateUIHelper : MonoBehaviour
{
    public void OnBackToHomeButton()
    {
        GameStateManager.Instance.SwitchToHomeMenu();
    }
    
    public void OnReplayLevelButton()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsInGameplay())
        {
            LevelInfo currentLevel = GameStateManager.Instance.GetCurrentLevelInfo();
            GameStateManager.Instance.SwitchToGameplay(currentLevel);
        }
    }
    
    public void OnNextLevelButton()
    {
        if (GameStateManager.Instance != null)
        {
            LevelInfo currentLevel = GameStateManager.Instance.GetCurrentLevelInfo();
            if (currentLevel != null)
            {
                LevelSelectionManager levelSelection = FindObjectOfType<LevelSelectionManager>();
            }
        }
    }
    
    public void OnQuitButton()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameStateManager.Instance.OnBackButton();
        }
    }
}
