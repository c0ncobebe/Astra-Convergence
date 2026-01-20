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
    
    [Header("Tutorial")]
    [SerializeField] private LevelInfo levelTutorial;
    [SerializeField] private NarratorController narratorController;
    [SerializeField] private float tutorialCameraSize = 6f;
    private string[] tutorialMessages1 = new string[]
    {
        "Xin chào, ta là Astra Sentinel!",
        "Hãy đến giúp ta sưởi ấm những <swing> vì sao </swing> này.",
        "Nối chúng lại bằng cách chạm lần lượt vào mỗi <swing> ngôi sao </swing>.",
        "Hoặc cũng có thể vuốt qua các chúng để nối nhanh hơn.",
        "Khi hoàn thành, các <rainb> chòm sao </rainb> sẽ xuất hiện.",
        "Hãy chắc chắn rằng số cạnh của chòm sao khớp với <swing> con số </swing> hiển thị trên đó.",
        "Chúc may mắn!"
    };
    private string[] tutorialMessages2 = new string[]
    {
        "Ngươi làm tốt lắm!",
        "Giờ thì hãy đến với những thử thách khó hơn!.",
    };
    
    [Header("Events")]
    public UnityEvent OnEnterHomeMenu;
    public UnityEvent OnEnterGameplay;
    public UnityEvent OnExitHomeMenu;
    public UnityEvent OnExitGameplay;
    
    private LevelInfo currentLevelInfo;
    private bool isInTutorial = false;
    
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
        CheckAndPlayTutorial();
    }
    
    private void CheckAndPlayTutorial()
    {
        const string FIRST_TIME_KEY = "HasPlayedBefore";
        
        bool hasPlayedBefore = PlayerPrefs.GetInt(FIRST_TIME_KEY, 0) == 1;
        
        if (!hasPlayedBefore && levelTutorial != null)
        {
            PlayerPrefs.SetInt(FIRST_TIME_KEY, 1);
            PlayerPrefs.Save();
            
            SwitchToGameplay(levelTutorial);
            narratorController.gameObject.SetActive(true);
            if (narratorController != null)
            {
                narratorController.ShowTextSequence(tutorialMessages1);
            }
        }
        else
        {
            SwitchToHomeMenu();
        }
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
            
            // Zoom to hơn nếu là level tutorial
            if (levelInfo == levelTutorial)
            {
                cameraController.SetZoom(tutorialCameraSize, true);
            }
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
    
    private void OnFirstTutorialComplete()
    {
        // Chờ level được hoàn thành, không làm gì ở đây
    }
    
    public void OnTutorialLevelComplete()
    {
        if (!isInTutorial || narratorController == null)
            return;
        
        // Hiện tutorial messages 2
        narratorController.OnSequenceComplete.RemoveAllListeners();
        narratorController.OnSequenceComplete.AddListener(OnSecondTutorialComplete);
        
        // Hiện lại canvas và chạy sequence 2
        if (narratorController.tutorialCanvas != null)
            narratorController.tutorialCanvas.SetActive(true);
        
        narratorController.ShowTextSequence(tutorialMessages2);
    }
    
    private void OnSecondTutorialComplete()
    {
        isInTutorial = false;
        SwitchToHomeMenu();
    }
}
