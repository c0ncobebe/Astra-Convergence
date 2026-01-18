#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneMenuShortcut
{
    [MenuItem("Scenes/Open Scene 1 &1")]
    static void OpenScene1()
    {
        Open("Assets/_MyGame/Scenes/Loading.unity");
    }

    [MenuItem("Scenes/Open Scene 2 &2")]
    static void OpenScene2()
    {
        Open("Assets/_MyGame/Scenes/Gameplay.unity");
    }

    [MenuItem("Scenes/Open Scene 3 &3")]
    static void OpenScene3()
    {
        Open("Assets/Scenes/Scene3.unity");
    }

    static void Open(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }
}
#endif