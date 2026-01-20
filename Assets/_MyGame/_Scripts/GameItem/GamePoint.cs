using System;
using System.Collections.Generic;
using DG.Tweening;
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
    
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color32 selectedColor;
    [SerializeField] private Color32 idleColor;
    [SerializeField] private Text debugText;
    [SerializeField] private Transform enableVisual;
    [SerializeField] private Transform disableVisual;
    [SerializeField] private ParticleSystem effect;
    
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
        Debug.Log("Set State " + newState);
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
                spriteRenderer.material.color = idleColor;
                break;
            case PointState.Selected:
                spriteRenderer.material.color = selectedColor;
                break;
            case PointState.Done:
                spriteRenderer.material.color = idleColor;
                enableVisual.gameObject.SetActive(false);
                disableVisual.gameObject.SetActive(true);
                break;
        }
    }

    public void Animating()
    {
        transform.DOKill();
        effect.Play();
        transform.DOScale(Vector3.one * 1.5f, 0.1f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);
        
        if (spriteRenderer != null && spriteRenderer.material.HasProperty("_GlowColor"))
        {
            spriteRenderer.material.DOColor(Color.yellow, "_GlowColor", 0.1f).SetEase(Ease.OutQuad);
        }
    }
    
    public void ResetGlow()
    {
        if (spriteRenderer != null && spriteRenderer.material.HasProperty("_GlowColor"))
        {
            spriteRenderer.material.DOColor(Color.white, "_GlowColor", 0.2f).SetEase(Ease.OutQuad);
        }
    }
    
    public void ShowErrorGlow()
    {
        if (spriteRenderer != null && spriteRenderer.material.HasProperty("_GlowColor"))
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(spriteRenderer.material.DOColor(Color.red, "_GlowColor", 0.15f).SetEase(Ease.OutQuad));
            seq.Append(spriteRenderer.material.DOColor(Color.white, "_GlowColor", 0.2f).SetEase(Ease.InQuad));
        }
    }
}