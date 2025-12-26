using System.Collections.Generic;
using _MyGame._Scripts;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public TextAsset levelJson;
    public GameObject pointPrefab;
    public PolygonMeshRenderer polygonPrefab;

    Dictionary<int, Transform> pointMap = new Dictionary<int, Transform>();

    void Start()
    {
        LoadLevel();
    }

    void LoadLevel()
    {
        LevelData data = JsonUtility.FromJson<LevelData>(levelJson.text);

        SpawnPoints(data.points);
        SpawnGroups(data.groups);
    }

    void SpawnPoints(List<PointData> points)
    {
        foreach (var p in points)
        {
            Vector3 pos = new Vector3(p.x, p.y, 0);
            GameObject go = Instantiate(pointPrefab, pos, Quaternion.identity);
            go.name = $"Point_{p.id}";
            pointMap[p.id] = go.transform;
        }
    }

    void SpawnGroups(List<GroupData> groups)
    {
        foreach (var g in groups)
        {
            PolygonMeshRenderer poly = Instantiate(polygonPrefab);

            List<Transform> pts = new List<Transform>();
            foreach (int id in g.pointIds)
                pts.Add(pointMap[id]);

            poly.BuildPolygon(pts, g.color.ToColor32());
        }
    }
}