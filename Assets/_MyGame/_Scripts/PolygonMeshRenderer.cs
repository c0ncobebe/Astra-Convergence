using System.Collections.Generic;
using UnityEngine;

namespace _MyGame._Scripts
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PolygonMeshRenderer : MonoBehaviour
    {
        Mesh mesh;
        [SerializeField] private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            mesh = new Mesh();
            meshFilter.mesh = mesh;
            propertyBlock = new MaterialPropertyBlock();
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

            // Sử dụng Ear Clipping Algorithm để triangulate đa giác lõm
            int[] triangles = TriangulatePolygon(vertices);

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors32 = colors;
            mesh.RecalculateBounds();
            
            // Set màu cho material sử dụng MaterialPropertyBlock
            SetColor(color);
        }
        
        // Ear Clipping Algorithm để hỗ trợ đa giác lõm
        private int[] TriangulatePolygon(Vector3[] vertices)
        {
            int n = vertices.Length;
            if (n < 3) return new int[0];
            
            List<int> indices = new List<int>(n);
            for (int i = 0; i < n; i++)
                indices.Add(i);
            
            List<int> triangles = new List<int>();
            
            // Xác định hướng của polygon (clockwise hay counter-clockwise)
            float area = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += vertices[i].x * vertices[j].y - vertices[j].x * vertices[i].y;
            }
            bool counterClockwise = area > 0;
            
            int attempts = 0;
            int maxAttempts = indices.Count * indices.Count;
            
            while (indices.Count > 3 && attempts < maxAttempts)
            {
                bool earFound = false;
                
                for (int i = 0; i < indices.Count; i++)
                {
                    int prevIdx = (i - 1 + indices.Count) % indices.Count;
                    int nextIdx = (i + 1) % indices.Count;
                    
                    int prev = indices[prevIdx];
                    int curr = indices[i];
                    int next = indices[nextIdx];
                    
                    Vector2 a = new Vector2(vertices[prev].x, vertices[prev].y);
                    Vector2 b = new Vector2(vertices[curr].x, vertices[curr].y);
                    Vector2 c = new Vector2(vertices[next].x, vertices[next].y);
                    
                    // Kiểm tra nếu đây là góc lồi (ear)
                    if (!IsConvexVertex(a, b, c, counterClockwise))
                        continue;
                    
                    // Kiểm tra không có điểm nào khác nằm trong tam giác này
                    bool isEar = true;
                    for (int j = 0; j < indices.Count; j++)
                    {
                        if (j == prevIdx || j == i || j == nextIdx)
                            continue;
                        
                        Vector2 p = new Vector2(vertices[indices[j]].x, vertices[indices[j]].y);
                        if (IsPointInTriangle(p, a, b, c))
                        {
                            isEar = false;
                            break;
                        }
                    }
                    
                    if (isEar)
                    {
                        // Thêm tam giác với thứ tự đúng
                        if (counterClockwise)
                        {
                            triangles.Add(prev);
                            triangles.Add(curr);
                            triangles.Add(next);
                        }
                        else
                        {
                            triangles.Add(prev);
                            triangles.Add(next);
                            triangles.Add(curr);
                        }
                        
                        // Xóa điểm curr khỏi danh sách
                        indices.RemoveAt(i);
                        earFound = true;
                        attempts = 0;
                        break;
                    }
                }
                
                if (!earFound)
                    attempts++;
            }
            
            // Thêm tam giác cuối cùng
            if (indices.Count == 3)
            {
                if (counterClockwise)
                {
                    triangles.Add(indices[0]);
                    triangles.Add(indices[1]);
                    triangles.Add(indices[2]);
                }
                else
                {
                    triangles.Add(indices[0]);
                    triangles.Add(indices[2]);
                    triangles.Add(indices[1]);
                }
            }
            
            return triangles.ToArray();
        }
        
        private bool IsConvexVertex(Vector2 a, Vector2 b, Vector2 c, bool counterClockwise)
        {
            float cross = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
            return counterClockwise ? cross > 0 : cross < 0;
        }
        
        private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            // Sử dụng barycentric coordinates
            float denominator = ((b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y));
            
            if (Mathf.Abs(denominator) < 0.0001f)
                return false;
            
            float alpha = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / denominator;
            float beta = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / denominator;
            float gamma = 1.0f - alpha - beta;
            
            // Điểm nằm trong tam giác nếu alpha, beta, gamma đều > 0 (không tính edge)
            return alpha > 0.0001f && beta > 0.0001f && gamma > 0.0001f;
        }
        
        private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
        
        // Set màu cho material mà không tạo instance mới
        public void SetColor(Color color)
        {
            if (meshRenderer == null || propertyBlock == null) return;
            
            // Set property "_BaseColor1" cho ShaderGraph GlitterSkybox
            propertyBlock.SetColor("_BaseColor1", color);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

    }
}