using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PointState
{
    Idle,
    Selected,
    Done
}

public class GamePoint : MonoBehaviour
{
    public int pointId;
    public PointState currentState = PointState.Idle;
    public List<int> remainingPolygons;
    
    private SpriteRenderer spriteRenderer;
    private Color idleColor = Color.white;
    private Color selectedColor = Color.yellow;
    private Color doneColor = Color.gray;

    [SerializeField] private Text debugText;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void Initialize(PointData data)
    {
        pointId = data.pointId;
        transform.position = data.position;
        remainingPolygons = new List<int>(data.belongToPolygons);
        UpdateVisual();

    }

    private void FixedUpdate()
    {
        if (debugText != null)
        {
            debugText.text = "";
            for (int i = 0; i < remainingPolygons.Count; i++)
            {
                debugText.text += remainingPolygons[i] + "-";
            }
        }
    }

    public void SetState(PointState newState)
    {
        currentState = newState;
        UpdateVisual();
    }
    
    public bool CanInteract()
    {
        return currentState != PointState.Done && remainingPolygons.Count > 0;
    }
    
    public void RemovePolygon(int polygonId)
    {
        Debug.Log("Remove Polygon ");
        remainingPolygons.Remove(polygonId);
        if (remainingPolygons.Count == 0)
        {
            SetState(PointState.Done);
        }
    }
    
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;
        
        switch (currentState)
        {
            case PointState.Idle:
                spriteRenderer.color = idleColor;
                spriteRenderer.sortingOrder = 0;
                transform.localScale = Vector3.one;
                break;
            case PointState.Selected:
                spriteRenderer.color = selectedColor;
                spriteRenderer.sortingOrder = 1;
                transform.localScale = Vector3.one * 1.2f;
                break;
            case PointState.Done:
                spriteRenderer.color = doneColor;
                spriteRenderer.sortingOrder = -1;
                transform.localScale = Vector3.one * 0.8f;
                break;
        }
    }
}