using System.Collections.Generic;
using UnityEngine;

public class LevelProgressManager : MonoBehaviour
{
    private static LevelProgressManager instance;
    public static LevelProgressManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("LevelProgressManager");
                instance = go.AddComponent<LevelProgressManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    private const string LEVEL_COMPLETED_PREFIX = "Level_Completed_";
    private const string LEVEL_STARS_PREFIX = "Level_Stars_";
    private const string CURRENT_LEVEL_KEY = "Current_Level";
    private const string HIGHEST_LEVEL_KEY = "Highest_Level";
    
    private HashSet<int> completedLevels = new HashSet<int>();
    private Dictionary<int, int> levelStars = new Dictionary<int, int>();
    private int currentLevel = 0;
    private int highestUnlockedLevel = 0;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadProgress()
    {
        currentLevel = PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 0);
        highestUnlockedLevel = PlayerPrefs.GetInt(HIGHEST_LEVEL_KEY, 0);
        
        completedLevels.Clear();
        levelStars.Clear();
        
        for (int i = 0; i < 100; i++)
        {
            if (PlayerPrefs.GetInt(LEVEL_COMPLETED_PREFIX + i, 0) == 1)
            {
                completedLevels.Add(i);
                levelStars[i] = PlayerPrefs.GetInt(LEVEL_STARS_PREFIX + i, 0);
            }
        }
    }
    
    private void SaveProgress()
    {
        PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, currentLevel);
        PlayerPrefs.SetInt(HIGHEST_LEVEL_KEY, highestUnlockedLevel);
        PlayerPrefs.Save();
    }
    
    public void CompleteLevel(int levelIndex, int stars = 3)
    {
        if (!completedLevels.Contains(levelIndex))
        {
            completedLevels.Add(levelIndex);
            PlayerPrefs.SetInt(LEVEL_COMPLETED_PREFIX + levelIndex, 1);
        }
        
        if (!levelStars.ContainsKey(levelIndex) || levelStars[levelIndex] < stars)
        {
            levelStars[levelIndex] = stars;
            PlayerPrefs.SetInt(LEVEL_STARS_PREFIX + levelIndex, stars);
        }
        
        int nextLevel = levelIndex + 1;
        if (nextLevel > highestUnlockedLevel)
        {
            highestUnlockedLevel = nextLevel;
        }
        
        SaveProgress();
    }
    
    public bool IsLevelCompleted(int levelIndex)
    {
        return completedLevels.Contains(levelIndex);
    }
    
    public bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex == 0)
            return true;
        
        return levelIndex <= highestUnlockedLevel || IsLevelCompleted(levelIndex - 1);
    }
    
    public int GetLevelStars(int levelIndex)
    {
        return levelStars.ContainsKey(levelIndex) ? levelStars[levelIndex] : 0;
    }
    
    public void SetCurrentLevel(int levelIndex)
    {
        currentLevel = levelIndex;
        SaveProgress();
    }
    
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    public int GetHighestUnlockedLevel()
    {
        return highestUnlockedLevel;
    }
    
    public void ResetAllProgress()
    {
        completedLevels.Clear();
        levelStars.Clear();
        currentLevel = 0;
        highestUnlockedLevel = 0;
        
        for (int i = 0; i < 100; i++)
        {
            PlayerPrefs.DeleteKey(LEVEL_COMPLETED_PREFIX + i);
            PlayerPrefs.DeleteKey(LEVEL_STARS_PREFIX + i);
        }
        
        PlayerPrefs.DeleteKey(CURRENT_LEVEL_KEY);
        PlayerPrefs.DeleteKey(HIGHEST_LEVEL_KEY);
        PlayerPrefs.Save();
    }
    
    public int GetTotalCompletedLevels()
    {
        return completedLevels.Count;
    }
    
    public int GetTotalStars()
    {
        int total = 0;
        foreach (var stars in levelStars.Values)
        {
            total += stars;
        }
        return total;
    }
    
    [ContextMenu("Print Completed Levels")]
    public void PrintCompletedLevels()
    {
        Debug.Log($"[LevelProgressManager] Completed Levels ({completedLevels.Count}):");
        foreach (int level in completedLevels)
        {
            Debug.Log($"  Level {level}: {GetLevelStars(level)} stars");
        }
    }
}
