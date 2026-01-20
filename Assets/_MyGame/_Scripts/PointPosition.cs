using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PointData", menuName = "Astra Nexus/Point Data")]
[System.Serializable]
public class PointPosition : ScriptableObject
{
#if  UNITY_EDITOR
    
    [SerializeField] public List<Vector2> _positions;
    [SerializeField] public DefaultAsset _folderPath;
    [Button]
    void GetPos()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { UnityEditor.AssetDatabase.GetAssetPath(_folderPath) });
        var _sprSingleMakeUps = new System.Collections.Generic.List<Sprite>();
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            _sprSingleMakeUps.Add(sprite);
        }
        
        _positions = new();

        for (int i = 0; i < _sprSingleMakeUps.Count; i++)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(_sprSingleMakeUps[i]);
            var newPath = path.Replace(".png", ".cor");

            if (File.Exists(newPath))
            {
                var tmp = File.ReadAllText(newPath).Split(' ');
                var ptsPosition = new Vector2(float.Parse(tmp[0]), float.Parse(tmp[1]));

                float unityX = -(1000 / 2 - ptsPosition.x - _sprSingleMakeUps[i].texture.width / 2f);
                float unityY = (1000 / 2 - _sprSingleMakeUps[i].texture.height / 2f - ptsPosition.y);

                _positions.Add(new Vector2(unityX, unityY) / 100); //100 la pixel per unit cua anh, quy uoc tat ca anh khi add vao khong duoc thay doi ppu
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
#endif
    
}
