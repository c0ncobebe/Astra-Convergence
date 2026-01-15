using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility để tạo LevelInfo assets nhanh chóng
/// Menu: Tools → Level Selection → Create Level Infos
/// </summary>
public class LevelInfoCreatorTool
{
    private const string DEFAULT_PATH = "Assets/_MyGame/Data/Levels";
    
    [MenuItem("Tools/Level Selection/Create Level Infos (Quick)")]
    static void CreateLevelInfosQuick()
    {
        CreateLevelInfosDialog(10); // Mặc định tạo 10 levels
    }
    
    [MenuItem("Tools/Level Selection/Create Level Infos (Custom)")]
    static void CreateLevelInfosCustom()
    {
        // Hiển thị dialog để nhập số lượng
        string input = EditorInputDialog.Show("Create Level Infos", "Number of levels to create:", "10");
        
        if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int count))
        {
            if (count > 0 && count <= 100)
            {
                CreateLevelInfosDialog(count);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please enter a number between 1 and 100", "OK");
            }
        }
    }
    
    static void CreateLevelInfosDialog(int count)
    {
        // Kiểm tra/tạo folder
        if (!AssetDatabase.IsValidFolder(DEFAULT_PATH))
        {
            string[] folders = DEFAULT_PATH.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
        
        int created = 0;
        int skipped = 0;
        
        for (int i = 0; i < count; i++)
        {
            string assetPath = $"{DEFAULT_PATH}/Level_{i + 1}.asset";
            
            // Kiểm tra đã tồn tại chưa
            if (File.Exists(assetPath))
            {
                skipped++;
                continue;
            }
            
            // Tạo LevelInfo
            LevelInfo info = ScriptableObject.CreateInstance<LevelInfo>();
            info.levelIndex = i;
            info.levelName = $"Level {i + 1}";
            // Unlock logic: Level 0 luôn mở, các level khác mở khi level trước hoàn thành
            
            // Save asset
            AssetDatabase.CreateAsset(info, assetPath);
            created++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog(
            "Level Infos Created",
            $"Created {created} level info(s).\n" +
            $"Skipped {skipped} existing file(s).\n\n" +
            $"Location: {DEFAULT_PATH}",
            "OK"
        );
        
        // Ping folder
        Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(DEFAULT_PATH);
        EditorGUIUtility.PingObject(folderObj);
    }
    
    [MenuItem("Tools/Level Selection/Open Levels Folder")]
    static void OpenLevelsFolder()
    {
        // Tạo folder nếu chưa có
        if (!AssetDatabase.IsValidFolder(DEFAULT_PATH))
        {
            string[] folders = DEFAULT_PATH.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
            AssetDatabase.Refresh();
        }
        
        // Select folder
        Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(DEFAULT_PATH);
        Selection.activeObject = folderObj;
        EditorGUIUtility.PingObject(folderObj);
    }
}

/// <summary>
/// Simple input dialog for editor
/// </summary>
public class EditorInputDialog : EditorWindow
{
    private string description;
    private string inputText;
    private string defaultText;
    private System.Action<string> onComplete;
    
    public static string Show(string title, string description, string defaultValue = "")
    {
        var window = CreateInstance<EditorInputDialog>();
        window.titleContent = new GUIContent(title);
        window.description = description;
        window.inputText = defaultValue;
        window.defaultText = defaultValue;
        
        window.minSize = new Vector2(300, 100);
        window.maxSize = new Vector2(300, 100);
        
        window.ShowModal();
        
        return window.inputText;
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(5);
        
        inputText = EditorGUILayout.TextField(inputText);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Cancel", GUILayout.Width(80)))
        {
            inputText = "";
            Close();
        }
        
        if (GUILayout.Button("OK", GUILayout.Width(80)))
        {
            Close();
        }
        
        EditorGUILayout.EndHorizontal();
    }
}
#endif
