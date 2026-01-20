using AstraNexus.Audio;
using UnityEngine;
using UnityEngine.Events;

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
        
        if (gamePlayManager == null)
            gamePlayManager = FindObjectOfType<GamePlayManager>(true);
        
        if (levelSelectionManager == null)
            levelSelectionManager = FindObjectOfType<LevelSelectionManager>(true);
        
        if (cameraController == null)
            cameraController = FindObjectOfType<CameraController>(true);
    }
    
    private void Start()
    {
        SwitchToHomeMenu();
    }
    
    public void SwitchToHomeMenu()
    {
        if (currentState == GameState.HomeMenu) return;
        
        if (currentState == GameState.Gameplay)
        {
            OnExitGameplay?.Invoke();
        }
        
        currentState = GameState.HomeMenu;
        
        if (homeMenuPanel != null)
            homeMenuPanel.SetActive(true);
        
        if (gameplayPanel != null)
            gameplayPanel.SetActive(false);
        
        if (gamePlayManager != null)
        {
            gamePlayManager.enabled = false;
            gamePlayManager.gameObject.SetActive(false);
        }
        
        if (levelSelectionManager != null)
        {
            levelSelectionManager.enabled = true;
            levelSelectionManager.gameObject.SetActive(true);
            levelSelectionManager.RefreshButtons();
        }
        
        if (cameraController != null)
        {
            cameraController.ResetCamera();
        }
        
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayRandomMenuMusic();
        
        OnEnterHomeMenu?.Invoke();
    }
    
    public void SwitchToGameplay(LevelInfo levelInfo)
    {
        if (currentState == GameState.Gameplay) return;
        
        currentLevelInfo = levelInfo;
        
        if (currentState == GameState.HomeMenu)
        {
            OnExitHomeMenu?.Invoke();
        }
        
        currentState = GameState.Gameplay;
        
        if (homeMenuPanel != null)
            homeMenuPanel.SetActive(false);
        
        if (gameplayPanel != null)
            gameplayPanel.SetActive(true);
        
        if (levelSelectionManager != null)
        {
            levelSelectionManager.enabled = false;
            levelSelectionManager.gameObject.SetActive(false);
        }
        
        if (gamePlayManager != null)
        {
            gamePlayManager.gameObject.SetActive(true);
            gamePlayManager.enabled = true;
            
            gamePlayManager.LoadLevel(levelInfo.levelData);
        }
        
        if (cameraController != null)
        {
            cameraController.SetupForGameplay();
        }
        
        LevelProgressManager.Instance.SetCurrentLevel(levelInfo.levelIndex);
        
        OnEnterGameplay?.Invoke();
    }
    
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    public bool IsInHomeMenu()
    {
        return currentState == GameState.HomeMenu;
    }
    
    public bool IsInGameplay()
    {
        return currentState == GameState.Gameplay;
    }
    
    public LevelInfo GetCurrentLevelInfo()
    {
        return currentLevelInfo;
    }
    
    public void OnBackButton()
    {
        if (currentState == GameState.Gameplay)
        {
            SwitchToHomeMenu();
        }
    }
}
