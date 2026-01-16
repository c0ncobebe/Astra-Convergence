using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelCompleteHandler : MonoBehaviour
{
    [Header("Win Settings")]
    [SerializeField] private float delayBeforeNextScene = 2f;
    [SerializeField] private string levelSelectionSceneName = "LevelSelection";
    [SerializeField] private bool autoLoadNextLevel = false;
    
    [Header("UI (Optional)")]
    [SerializeField] private GameObject winPanel;
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip winSound;
    
    private bool levelCompleted = false;
    
    public void OnLevelComplete()
    {
        if (levelCompleted) return;
        
        levelCompleted = true;
        
        if (winSound != null)
        {
            AudioSource.PlayClipAtPoint(winSound, Camera.main.transform.position);
        }
        
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
        
        SaveLevelProgress();
        
        StartCoroutine(TransitionToNextScene());
    }
    
    private void SaveLevelProgress()
    {
        int currentLevel = LevelProgressManager.Instance.GetCurrentLevel();
        
        int stars = CalculateStars();
        
        LevelProgressManager.Instance.CompleteLevel(currentLevel, stars);
    }
    
    protected virtual int CalculateStars()
    {
        return 3;
    }
    
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
    
    public void BackToLevelSelection()
    {
        SceneManager.LoadScene(levelSelectionSceneName);
    }
    
    public void LoadNextLevel()
    {
        int currentLevel = LevelProgressManager.Instance.GetCurrentLevel();
        int nextLevel = currentLevel + 1;
        
        string nextSceneName = $"Level_{nextLevel + 1}";
        
        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            LevelProgressManager.Instance.SetCurrentLevel(nextLevel);
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            BackToLevelSelection();
        }
    }
    
    public void ReplayLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
