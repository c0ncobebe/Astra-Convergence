using System.Collections.Generic;
using UnityEngine;

namespace _MyGame._Scripts
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PolygonMeshRenderer : MonoBehaviour
    {
        Mesh mesh;
        [SerializeField] private MeshFilter meshFilter;

        void Awake()
        {
            mesh = new Mesh();
            meshFilter.mesh = mesh;
        }

        // Gọi hàm này khi bạn có đủ điểm
        public void BuildPolygon(List<Transform> points, Color32 color)
        {
            int count = points.Count;
            if (count < 3) return;

            Vector3[] vertices = new Vector3[count];
            Color32[] colors = new Color32[count];

            for (int i = 0; i < count; i++)
            {
                vertices[i] = transform.InverseTransformPoint(points[i].position);
                colors[i] = color;
            }

            int[] triangles = new int[(count - 2) * 3];
            for (int i = 0; i < count - 2; i++)
            {
                triangles[i * 3 + 0] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors32 = colors;
            mesh.RecalculateBounds();
        }

    }
}