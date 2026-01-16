using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Thông tin về một level trong UI
/// </summary>
[CreateAssetMenu(fileName = "LevelInfo", menuName = "Game/Level Info")]
public class LevelInfo : SerializedScriptableObject
{
    [Header("Level Information")]
    public int levelIndex; // ID duy nhất cho save/load (không nhất thiết phải theo thứ tự)
    public string levelName; // Tên hiển thị (Level 1, Level 2...)
    public string detail; // Mô tả chi tiết về level (có thể để trống)
    
    [Header("Visual")]
    public Sprite thumbnailImage; // Ảnh thumbnail của level
    
    [Header("Level Data Reference")]
    public LevelData levelData; // Reference đến LevelData ScriptableObject
}
