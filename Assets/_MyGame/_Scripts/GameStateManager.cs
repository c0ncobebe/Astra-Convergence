using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Quản lý state của game: Home Menu và Gameplay
/// Singleton pattern, chỉ có 1 instance trong scene
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        HomeMenu,
        Gameplay
    }
    
    private static GameStateManager instance;
    public static GameStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameStateManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameStateManager");
                    instance = go.AddComponent<GameStateManager>();
                }
            }
            return instance;
        }
    }
    
    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.HomeMenu;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject homeMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    
    [Header("Controllers")]
    [SerializeField] private GamePlayManager gamePlayManager;
    [SerializeField] private LevelSelectionManager levelSelectionManager;
    
    [Header("Camera")]
    [SerializeField] private CameraController cameraController;
    
    [Header("Events")]
    public UnityEvent OnEnterHomeMenu;
    public UnityEvent OnEnterGameplay;
    public UnityEvent OnExitHomeMenu;
    public UnityEvent OnExitGameplay;
    
    // Current level being played
    private LevelInfo currentLevelInfo;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Auto-find components nếu chưa assign
        if (gamePlayManager == null)
            gamePlayManager = FindObjectOfType<GamePlayManager>(true);
        
        if (levelSelectionManager == null)
            levelSelectionManager = FindObjectOfType<LevelSelectionManager>(true);
        
        if (cameraController == null)
            cameraController = FindObjectOfType<CameraController>(true);
    }
    
    private void Start()
    {
        // Bắt đầu ở Home Menu
        SwitchToHomeMenu();
    }
    
    /// <summary>
    /// Chuyển sang state Home Menu
    /// </summary>
    public void SwitchToHomeMenu()
    {
        if (currentState == GameState.HomeMenu) return;
        
        Debug.Log("[GameStateManager] Switching to Home Menu");
        
        // Exit gameplay first
        if (currentState == GameState.Gameplay)
        {
            OnExitGameplay?.Invoke();
        }
        
        currentState = GameState.HomeMenu;
        
        // Update UI
        if (homeMenuPanel != null)
            homeMenuPanel.SetActive(true);
        
        if (gameplayPanel != null)
            gameplayPanel.SetActive(false);
        
        // Disable gameplay controller
        if (gamePlayManager != null)
        {
            gamePlayManager.enabled = false;
            gamePlayManager.gameObject.SetActive(false);
        }
        
        // Enable level selection
        if (levelSelectionManager != null)
        {
            levelSelectionManager.enabled = true;
            levelSelectionManager.gameObject.SetActive(true);
            levelSelectionManager.RefreshButtons(); // Refresh để update unlock states
        }
        
        // Reset camera
        if (cameraController != null)
        {
            cameraController.ResetCamera();
        }
        
        OnEnterHomeMenu?.Invoke();
    }
    
    /// <summary>
    /// Chuyển sang state Gameplay với level cụ thể
    /// </summary>
    public void SwitchToGameplay(LevelInfo levelInfo)
    {
        if (currentState == GameState.Gameplay) return;
        
        Debug.Log($"[GameStateManager] Switching to Gameplay - Level {levelInfo.levelName}");
        
        currentLevelInfo = levelInfo;
        
        // Exit home menu
        if (currentState == GameState.HomeMenu)
        {
            OnExitHomeMenu?.Invoke();
        }
        
        currentState = GameState.Gameplay;
        
        // Update UI
        if (homeMenuPanel != null)
            homeMenuPanel.SetActive(false);
        
        if (gameplayPanel != null)
            gameplayPanel.SetActive(true);
        
        // Disable level selection
        if (levelSelectionManager != null)
        {
            levelSelectionManager.enabled = false;
            levelSelectionManager.gameObject.SetActive(false);
        }
        
        // Enable and load level in gameplay controller
        if (gamePlayManager != null)
        {
            gamePlayManager.gameObject.SetActive(true);
            gamePlayManager.enabled = true;
            
            // Load level data
            if (levelInfo.levelData != null)
            {
                gamePlayManager.LoadLevel(levelInfo.levelData);
            }
            else
            {
                Debug.LogError($"[GameStateManager] LevelInfo {levelInfo.levelName} has no levelData assigned!");
            }
        }
        
        // Setup camera for gameplay
        if (cameraController != null)
        {
            cameraController.SetupForGameplay();
        }
        
        // Save current level
        LevelProgressManager.Instance.SetCurrentLevel(levelInfo.levelIndex);
        
        OnEnterGameplay?.Invoke();
    }
    
    /// <summary>
    /// Lấy state hiện tại
    /// </summary>
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// Kiểm tra có đang ở Home Menu không
    /// </summary>
    public bool IsInHomeMenu()
    {
        return currentState == GameState.HomeMenu;
    }
    
    /// <summary>
    /// Kiểm tra có đang gameplay không
    /// </summary>
    public bool IsInGameplay()
    {
        return currentState == GameState.Gameplay;
    }
    
    /// <summary>
    /// Lấy thông tin level đang chơi
    /// </summary>
    public LevelInfo GetCurrentLevelInfo()
    {
        return currentLevelInfo;
    }
    
    /// <summary>
    /// Back button handler
    /// </summary>
    public void OnBackButton()
    {
        if (currentState == GameState.Gameplay)
        {
            // Quay về home menu
            SwitchToHomeMenu();
        }
        else
        {
            // Có thể quit app hoặc show quit dialog
            Debug.Log("[GameStateManager] Back button in Home Menu - can quit app here");
        }
    }
}
