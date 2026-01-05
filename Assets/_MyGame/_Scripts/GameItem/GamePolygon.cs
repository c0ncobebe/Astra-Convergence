using System.Collections.Generic;
using UnityEngine;

public class GamePolygon : MonoBehaviour
{
    public int polygonId;
    public List<int> pointIds;
    public List<Edge> edges;
    public int sideCount;
    public bool isCompleted = false;
    
    private LineRenderer lineRenderer;
    private TextMesh hintText;
    
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        hintText = GetComponentInChildren<TextMesh>();
    }
    
    public void Initialize(PolygonData data)
    {
        polygonId = data.polygonId;
        pointIds = new List<int>(data.pointIds);
        edges = new List<Edge>(data.edges);
        sideCount = data.sideCount;
        
        // Setup hint text hiển thị số cạnh
        if (hintText != null)
        {
            hintText.text = sideCount.ToString();
            hintText.fontSize = 5;
            hintText.color = Color.white;
            hintText.anchor = TextAnchor.MiddleCenter;
            hintText.alignment = TextAlignment.Center;
            hintText.gameObject.SetActive(true);
        }
        
        // Đặt position ở center của polygon
        transform.position = new Vector3(data.centerPosition.x, data.centerPosition.y, 0);
        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
        }
    }
    
    public void Complete(List<Vector2> pointPositions)
    {
        isCompleted = true;
        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = pointPositions.Count + 1;
            for (int i = 0; i < pointPositions.Count; i++)
            {
                lineRenderer.SetPosition(i, pointPositions[i]);
            }
            lineRenderer.SetPosition(pointPositions.Count, pointPositions[0]);
        }
        
        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }
    }
}