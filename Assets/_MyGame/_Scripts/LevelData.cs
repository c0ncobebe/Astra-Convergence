using System.Collections.Generic;
using UnityEngine;

namespace _MyGame._Scripts
{
    [System.Serializable]
    public class LevelData
    {
        public List<PointData> points;
        public List<GroupData> groups;
    }
    [System.Serializable]
    public class PointData
    {
        public int id;
        public float x;
        public float y;
    }
    [System.Serializable]
    public class GroupData
    {
        public List<int> pointIds;
        public ColorData color;
    }

    [System.Serializable]
    public class ColorData
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a = 255;

        public Color32 ToColor32()
        {
            return new Color32(r, g, b, a);
        }
    }
}