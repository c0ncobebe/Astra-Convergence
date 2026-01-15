using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý tiến trình và trạng thái mở khóa của levels (Singleton)
/// Lưu/load dữ liệu từ PlayerPrefs
/// </summary>
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
    
    // Keys cho PlayerPrefs
    private const string LEVEL_COMPLETED_PREFIX = "Level_Completed_";
    private const string LEVEL_STARS_PREFIX = "Level_Stars_";
    private const string CURRENT_LEVEL_KEY = "Current_Level";
    private const string HIGHEST_LEVEL_KEY = "Highest_Level";
    
    // Cache dữ liệu trong runtime
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
    
    /// <summary>
    /// Load tiến trình từ PlayerPrefs
    /// </summary>
    private void LoadProgress()
    {
        currentLevel = PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 0);
        highestUnlockedLevel = PlayerPrefs.GetInt(HIGHEST_LEVEL_KEY, 0);
        
        // Load completed levels và stars
        completedLevels.Clear();
        levelStars.Clear();
        
        // Giả sử có tối đa 100 levels (có thể tăng nếu cần)
        for (int i = 0; i < 100; i++)
        {
            if (PlayerPrefs.GetInt(LEVEL_COMPLETED_PREFIX + i, 0) == 1)
            {
                completedLevels.Add(i);
                levelStars[i] = PlayerPrefs.GetInt(LEVEL_STARS_PREFIX + i, 0);
            }
        }
        
        Debug.Log($"[LevelProgressManager] Loaded progress: Current={currentLevel}, Highest={highestUnlockedLevel}, Completed={completedLevels.Count}");
    }
    
    /// <summary>
    /// Lưu tiến trình vào PlayerPrefs
    /// </summary>
    private void SaveProgress()
    {
        PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, currentLevel);
        PlayerPrefs.SetInt(HIGHEST_LEVEL_KEY, highestUnlockedLevel);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Đánh dấu level đã hoàn thành
    /// </summary>
    public void CompleteLevel(int levelIndex, int stars = 3)
    {
        if (!completedLevels.Contains(levelIndex))
        {
            completedLevels.Add(levelIndex);
            PlayerPrefs.SetInt(LEVEL_COMPLETED_PREFIX + levelIndex, 1);
            Debug.Log($"[LevelProgressManager] Level {levelIndex} completed!");
        }
        
        // Cập nhật số sao (chỉ cập nhật nếu cao hơn)
        if (!levelStars.ContainsKey(levelIndex) || levelStars[levelIndex] < stars)
        {
            levelStars[levelIndex] = stars;
            PlayerPrefs.SetInt(LEVEL_STARS_PREFIX + levelIndex, stars);
        }
        
        // Mở khóa level kế tiếp
        int nextLevel = levelIndex + 1;
        if (nextLevel > highestUnlockedLevel)
        {
            highestUnlockedLevel = nextLevel;
        }
        
        SaveProgress();
    }
    
    /// <summary>
    /// Kiểm tra level đã hoàn thành chưa
    /// </summary>
    public bool IsLevelCompleted(int levelIndex)
    {
        return completedLevels.Contains(levelIndex);
    }
    
    /// <summary>
    /// Kiểm tra level có mở khóa không
    /// </summary>
    public bool IsLevelUnlocked(int levelIndex)
    {
        // Level 0 luôn mở
        if (levelIndex == 0)
            return true;
        
        // Các level sau mở khi level trước hoàn thành
        return levelIndex <= highestUnlockedLevel || IsLevelCompleted(levelIndex - 1);
    }
    
    /// <summary>
    /// Lấy số sao của level
    /// </summary>
    public int GetLevelStars(int levelIndex)
    {
        return levelStars.ContainsKey(levelIndex) ? levelStars[levelIndex] : 0;
    }
    
    /// <summary>
    /// Set level hiện tại
    /// </summary>
    public void SetCurrentLevel(int levelIndex)
    {
        currentLevel = levelIndex;
        SaveProgress();
    }
    
    /// <summary>
    /// Lấy level hiện tại
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    /// <summary>
    /// Lấy level cao nhất đã mở khóa
    /// </summary>
    public int GetHighestUnlockedLevel()
    {
        return highestUnlockedLevel;
    }
    
    /// <summary>
    /// Reset tất cả tiến trình
    /// </summary>
    public void ResetAllProgress()
    {
        completedLevels.Clear();
        levelStars.Clear();
        currentLevel = 0;
        highestUnlockedLevel = 0;
        
        // Clear PlayerPrefs
        for (int i = 0; i < 100; i++)
        {
            PlayerPrefs.DeleteKey(LEVEL_COMPLETED_PREFIX + i);
            PlayerPrefs.DeleteKey(LEVEL_STARS_PREFIX + i);
        }
        
        PlayerPrefs.DeleteKey(CURRENT_LEVEL_KEY);
        PlayerPrefs.DeleteKey(HIGHEST_LEVEL_KEY);
        PlayerPrefs.Save();
        
        Debug.Log("[LevelProgressManager] All progress reset!");
    }
    
    /// <summary>
    /// Lấy tổng số level đã hoàn thành
    /// </summary>
    public int GetTotalCompletedLevels()
    {
        return completedLevels.Count;
    }
    
    /// <summary>
    /// Lấy tổng số sao đã đạt được
    /// </summary>
    public int GetTotalStars()
    {
        int total = 0;
        foreach (var stars in levelStars.Values)
        {
            total += stars;
        }
        return total;
    }
    
    /// <summary>
    /// Debug: In ra tất cả levels đã hoàn thành
    /// </summary>
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
