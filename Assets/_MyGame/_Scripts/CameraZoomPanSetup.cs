using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script để tự động setup Camera Zoom/Pan system
/// Attach vào một GameObject trống và click Setup button trong Inspector
/// </summary>
public class CameraZoomPanSetup : MonoBehaviour
{
    [Header("Setup Options")]
    [SerializeField] private bool setupCamera = true;
    [SerializeField] private bool setupInputManager = true;
    [SerializeField] private bool setupBackground = true;
    [SerializeField] private bool setupEditorSimulator = true;
    [SerializeField] private bool enableAutoPanBounds = true; // Auto calculate từ level
    
    [Header("Camera Settings")]
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 12f;
    [SerializeField] private float zoomSpeed = 2f;
    
    [Header("Parallax Settings")]
    [SerializeField] private float parallaxPosition = 0.3f;
    [SerializeField] private float parallaxScale = 0.5f;
    
    [Header("Background")]
    [SerializeField] private GameObject backgroundPrefab;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.3f);
    [SerializeField] private Vector3 backgroundScale = Vector3.one * 10f;
    
#if UNITY_EDITOR
    [ContextMenu("Setup All Components")]
    public void SetupAll()
    {
        GameObject inputManagerObj = null;
        
        if (setupInputManager)
        {
            inputManagerObj = SetupInputManager();
        }
        
        if (setupCamera)
        {
            SetupMainCamera(inputManagerObj);
        }
        
        if (setupBackground)
        {
            SetupBackgroundWithParallax();
        }
        
        if (setupEditorSimulator)
        {
            SetupEditorTouchSimulator(inputManagerObj);
        }
        
        // Wire GamePlayController với InputManager
        WireGamePlayController(inputManagerObj);
        
        Debug.Log("[CameraZoomPanSetup] ✓ Setup complete! Check the documentation for usage instructions.");
        EditorUtility.DisplayDialog("Setup Complete", 
            "Camera Zoom/Pan system has been set up!\n\n" +
            "- InputManager: Ready\n" +
            "- CameraController: Added to Main Camera\n" +
            "- ParallaxBackground: Created\n" +
            "- EditorTouchSimulator: Added for testing\n" +
            "- GamePlayController: Wired to InputManager\n\n" +
            "See CAMERA_ZOOM_PAN_SETUP.md for details.", 
            "OK");
    }
    
    private GameObject SetupInputManager()
    {
        // Tìm InputManager hiện có
        InputManager existing = FindObjectOfType<InputManager>();
        if (existing != null)
        {
            Debug.Log("[Setup] InputManager already exists. Using existing one.");
            return existing.gameObject;
        }
        
        // Tạo mới
        GameObject obj = new GameObject("InputManager");
        obj.AddComponent<InputManager>();
        
        Debug.Log("[Setup] ✓ Created InputManager");
        return obj;
    }
    
    private void SetupMainCamera(GameObject inputManagerObj)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[Setup] ✗ Main Camera not found!");
            return;
        }
        
        // Thêm CameraController nếu chưa có
        CameraController controller = mainCam.GetComponent<CameraController>();
        if (controller == null)
        {
            controller = mainCam.gameObject.AddComponent<CameraController>();
        }
        
        // Setup via reflection để set private fields
        var so = new SerializedObject(controller);
        so.FindProperty("minOrthographicSize").floatValue = minZoom;
        so.FindProperty("maxOrthographicSize").floatValue = maxZoom;
        so.FindProperty("zoomSpeed").floatValue = zoomSpeed;
        
        // Note: enablePanBounds sẽ được set tự động bởi GamePlayManager.SetupCameraBounds()
        // khi level được load (tính toán bounds từ level data + 20% padding)
        
        if (inputManagerObj != null)
        {
            so.FindProperty("inputManager").objectReferenceValue = inputManagerObj.GetComponent<InputManager>();
        }
        
        so.ApplyModifiedProperties();
        
        Debug.Log("[Setup] ✓ Setup Main Camera with CameraController");
        
        if (enableAutoPanBounds)
        {
            Debug.Log("[Setup] ℹ Pan bounds will be auto-calculated by GamePlayManager when level loads (Level size + 20% padding)");
        }
    }
    
    private void SetupBackgroundWithParallax()
    {
        // Tìm background hiện có
        ParallaxBackground existing = FindObjectOfType<ParallaxBackground>();
        if (existing != null)
        {
            Debug.Log("[Setup] ParallaxBackground already exists. Skipping.");
            return;
        }
        
        GameObject bgObj;
        
        // Sử dụng prefab hoặc tạo mới
        if (backgroundPrefab != null)
        {
            bgObj = Instantiate(backgroundPrefab);
            bgObj.name = "Background_Parallax";
        }
        else
        {
            bgObj = new GameObject("Background_Parallax");
            
            // Thêm SpriteRenderer
            SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
            
            if (backgroundSprite != null)
            {
                sr.sprite = backgroundSprite;
            }
            else
            {
                // Tạo sprite đơn giản từ texture
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, backgroundColor);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            }
            
            sr.color = backgroundColor;
            sr.sortingOrder = -10; // Phía sau
            
            // Scale
            bgObj.transform.localScale = backgroundScale;
        }
        
        // Set position
        bgObj.transform.position = new Vector3(0, 0, -5);
        
        // Add ParallaxBackground component
        ParallaxBackground parallax = bgObj.AddComponent<ParallaxBackground>();
        
        // Setup via reflection
        var so = new SerializedObject(parallax);
        so.FindProperty("parallaxFactorPosition").floatValue = parallaxPosition;
        so.FindProperty("parallaxFactorScale").floatValue = parallaxScale;
        so.FindProperty("captureInitialStateOnStart").boolValue = true;
        so.ApplyModifiedProperties();
        
        Debug.Log("[Setup] ✓ Created Background with ParallaxBackground component");
    }
    
    private void SetupEditorTouchSimulator(GameObject inputManagerObj)
    {
        if (inputManagerObj == null) return;
        
        // Thêm vào InputManager object
        EditorTouchSimulator existing = inputManagerObj.GetComponent<EditorTouchSimulator>();
        if (existing != null)
        {
            Debug.Log("[Setup] EditorTouchSimulator already exists.");
            return;
        }
        
        inputManagerObj.AddComponent<EditorTouchSimulator>();
        Debug.Log("[Setup] ✓ Added EditorTouchSimulator for testing");
    }
    
    private void WireGamePlayController(GameObject inputManagerObj)
    {
        if (inputManagerObj == null) return;
        
        // Tìm GamePlayManager trong scene
        GamePlayManager gamePlayManager = FindObjectOfType<GamePlayManager>();
        if (gamePlayManager == null)
        {
            Debug.LogWarning("[Setup] GamePlayManager not found in scene. Skipping wire.");
            return;
        }
        
        // Wire InputManager vào GamePlayManager
        var so = new SerializedObject(gamePlayManager);
        var inputManagerProp = so.FindProperty("inputManager");
        if (inputManagerProp != null)
        {
            inputManagerProp.objectReferenceValue = inputManagerObj.GetComponent<InputManager>();
            so.ApplyModifiedProperties();
            Debug.Log("[Setup] ✓ Wired GamePlayManager to InputManager");
        }
        else
        {
            Debug.LogWarning("[Setup] Could not find 'inputManager' field in GamePlayManager");
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(CameraZoomPanSetup))]
public class CameraZoomPanSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click the button below to automatically setup Camera Zoom/Pan system.\n\n" +
            "This will:\n" +
            "• Create/Setup InputManager\n" +
            "• Add CameraController to Main Camera\n" +
            "• Create Background with ParallaxBackground\n" +
            "• Add EditorTouchSimulator for testing", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup All Components", GUILayout.Height(40)))
        {
            CameraZoomPanSetup setup = (CameraZoomPanSetup)target;
            setup.SetupAll();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Open Documentation"))
        {
            string path = System.IO.Path.Combine(Application.dataPath, "..", "CAMERA_ZOOM_PAN_SETUP.md");
            if (System.IO.File.Exists(path))
            {
                Application.OpenURL("file:///" + path);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation Not Found", 
                    "CAMERA_ZOOM_PAN_SETUP.md not found in project root.", 
                    "OK");
            }
        }
    }
}
#endif
