using System.Collections.Generic;
using AstraNexus.Audio;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LevelSelectionManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private List<LevelInfo> allLevels = new List<LevelInfo>();
    
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private LevelButton levelButtonPrefab;
    
    [Header("Loading")]
    [SerializeField] private string loadingSceneName = "Loading";
    [SerializeField] private bool useLoadingScene = true;
    
    [Header("Animation")]
    [SerializeField] private float buttonSpawnDelay = 0.05f;
    [SerializeField] private bool animateButtonSpawn = true;
    
    private List<LevelButton> levelButtons = new List<LevelButton>();
    private LevelProgressManager progressManager;
    
    private void Start()
    {
        progressManager = LevelProgressManager.Instance;
        InitializeLevelButtons();
    }
    
    private void InitializeLevelButtons()
    {
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }
        levelButtons.Clear();
        
        if (animateButtonSpawn)
        {
            StartCoroutine(SpawnButtonsAnimated());
        }
        else
        {
            SpawnAllButtons();
        }
    }
    
    private void SpawnAllButtons()
    {
        for (int i = 0; i < allLevels.Count; i++)
        {
            CreateLevelButton(allLevels[i], i);
        }
    }
    
    private System.Collections.IEnumerator SpawnButtonsAnimated()
    {
        for (int i = 0; i < allLevels.Count; i++)
        {
            LevelButton button = CreateLevelButton(allLevels[i], i);
            
            button.transform.localScale = Vector3.zero;
            button.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(i * buttonSpawnDelay);
            
            yield return new WaitForSeconds(buttonSpawnDelay);
        }
    }
    
    private LevelButton CreateLevelButton(LevelInfo levelInfo, int index)
    {
        LevelButton button = Instantiate(levelButtonPrefab, levelButtonContainer);
        button.name = $"LevelButton_{index}_{levelInfo.levelName}";
        
        bool isUnlocked = IsLevelUnlocked(index);
        
        button.Initialize(levelInfo, isUnlocked, OnLevelSelected);
        
        levelButtons.Add(button);
        return button;
    }
    
    private bool IsLevelUnlocked(int positionInList)
    {
        if (positionInList == 0)
        {
            return true;
        }
        
        LevelInfo previousLevel = allLevels[positionInList - 1];
        return progressManager.IsLevelCompleted(previousLevel.levelIndex);
    }
    
    private void OnLevelSelected(LevelInfo levelInfo)
    {
        progressManager.SetCurrentLevel(levelInfo.levelIndex);
        
        GameStateManager.Instance.SwitchToGameplay(levelInfo);
        SoundManager.Instance.PlaySound(SoundType.Pop);
    }
    
    public void RefreshButtons()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            bool isUnlocked = IsLevelUnlocked(i);
            levelButtons[i].SetUnlocked(isUnlocked);
        }
    }
    
    public void ScrollToLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelButtons.Count)
            return;
        
        if (scrollRect == null)
            return;
        
        RectTransform buttonRect = levelButtons[levelIndex].GetComponent<RectTransform>();
        RectTransform contentRect = levelButtonContainer.GetComponent<RectTransform>();
        RectTransform viewportRect = scrollRect.viewport;
        
        Canvas.ForceUpdateCanvases();
        
        float targetY = (buttonRect.anchoredPosition.y - contentRect.anchoredPosition.y) 
                       / (contentRect.rect.height - viewportRect.rect.height);
        
        DOTween.To(() => scrollRect.verticalNormalizedPosition, 
                   x => scrollRect.verticalNormalizedPosition = x, 
                   targetY, 
                   0.5f)
            .SetEase(Ease.OutCubic);
    }
    
    public void AddLevel(LevelInfo levelInfo)
    {
        if (!allLevels.Contains(levelInfo))
        {
            allLevels.Add(levelInfo);
            InitializeLevelButtons();
        }
    }
    
    [ContextMenu("Unlock All Levels")]
    public void UnlockAllLevels()
    {
        for (int i = 0; i < allLevels.Count; i++)
        {
            progressManager.CompleteLevel(i);
        }
        RefreshButtons();
    }
    
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        progressManager.ResetAllProgress();
        RefreshButtons();
    }
}
