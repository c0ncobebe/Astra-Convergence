using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dữ liệu định nghĩa một điểm trong level
/// </summary>
[Serializable]
public class PointData
{
    public int pointId;
    public Vector2 position;
    public List<int> belongToPolygons;
    
    public PointData(int id, Vector2 pos)
    {
        pointId = id;
        position = pos;
        belongToPolygons = new List<int>();
    }
}