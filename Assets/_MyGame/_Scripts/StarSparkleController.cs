using UnityEngine;

/// <summary>
/// Controller for the Rainbow Star Sparkle shader.
/// Attach to a quad/sprite with the RainbowStarSparkle material.
/// </summary>
[ExecuteAlways]
public class StarSparkleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer targetRenderer;
    
    [Header("Core Settings")]
    [ColorUsage(true, true)]
    public Color coreColor = Color.white;
    [Range(0f, 10f)] public float coreIntensity = 2f;
    [Range(1f, 20f)] public float starSharpness = 8f;
    [Range(2, 8)] public int starPoints = 4;
    [Range(0f, 360f)] public float starRotation = 45f;
    
    [Header("RGB Ring Settings")]
    [Range(0f, 2f)] public float ringIntensity = 0.8f;
    [Range(0.01f, 0.5f)] public float ringWidth = 0.08f;
    [Range(0f, 1f)] public float ringRadius = 0.35f;
    [Range(0f, 0.15f)] public float rgbSeparation = 0.04f;
    [Range(0f, 2f)] public float redStrength = 1f;
    [Range(0f, 2f)] public float greenStrength = 1f;
    [Range(0f, 2f)] public float blueStrength = 1f;
    
    [Header("Eclipse Settings")]
    [Range(0f, 1f)] public float eclipseAmount = 0f;
    [Range(0f, 360f)] public float eclipseAngle = 0f;
    [Range(0.01f, 1f)] public float eclipseSoftness = 0.3f;
    
    [Header("Pulse Animation")]
    [Range(0f, 10f)] public float pulseSpeed = 2f;
    [Range(0f, 1f)] public float pulseAmount = 0.3f;
    [Range(0f, 1f)] public float pulseMin = 0.7f;
    
    [Header("Secondary Star")]
    public bool secondaryStarEnabled = true;
    [Range(0.1f, 2f)] public float secondaryStarScale = 0.6f;
    [Range(0f, 90f)] public float secondaryStarRotationOffset = 22.5f;
    [Range(1f, 20f)] public float secondaryStarSharpness = 4f;
    
    [Header("Additional Animation")]
    public bool autoRotate = false;
    [Range(-180f, 180f)] public float rotationSpeed = 30f;
    
    private MaterialPropertyBlock _propertyBlock;
    private static readonly int CoreColorID = Shader.PropertyToID("_CoreColor");
    private static readonly int CoreIntensityID = Shader.PropertyToID("_CoreIntensity");
    private static readonly int StarSharpnessID = Shader.PropertyToID("_StarSharpness");
    private static readonly int StarPointsID = Shader.PropertyToID("_StarPoints");
    private static readonly int StarRotationID = Shader.PropertyToID("_StarRotation");
    private static readonly int RainbowIntensityID = Shader.PropertyToID("_RainbowIntensity");
    private static readonly int RainbowWidthID = Shader.PropertyToID("_RainbowWidth");
    private static readonly int RainbowOffsetID = Shader.PropertyToID("_RainbowOffset");
    private static readonly int ChromaticSpreadID = Shader.PropertyToID("_ChromaticSpread");
    private static readonly int RedStrengthID = Shader.PropertyToID("_RedStrength");
    private static readonly int GreenStrengthID = Shader.PropertyToID("_GreenStrength");
    private static readonly int BlueStrengthID = Shader.PropertyToID("_BlueStrength");
    private static readonly int EclipseAmountID = Shader.PropertyToID("_EclipseAmount");
    private static readonly int EclipseAngleID = Shader.PropertyToID("_EclipseAngle");
    private static readonly int EclipseSoftnessID = Shader.PropertyToID("_EclipseSoftness");
    private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
    private static readonly int PulseAmountID = Shader.PropertyToID("_PulseAmount");
    private static readonly int PulseMinID = Shader.PropertyToID("_PulseMin");
    private static readonly int SecondaryStarEnabledID = Shader.PropertyToID("_SecondaryStarEnabled");
    private static readonly int SecondaryStarScaleID = Shader.PropertyToID("_SecondaryStarScale");
    private static readonly int SecondaryStarRotationOffsetID = Shader.PropertyToID("_SecondaryStarRotationOffset");
    private static readonly int SecondaryStarSharpnessID = Shader.PropertyToID("_SecondaryStarSharpness");
    
    private float _currentRotation;
    
    private void OnEnable()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
            
        _propertyBlock = new MaterialPropertyBlock();
        _currentRotation = starRotation;
    }
    
    private void Update()
    {
        if (targetRenderer == null) return;
        
        // Handle auto rotation
        if (autoRotate && Application.isPlaying)
        {
            _currentRotation += rotationSpeed * Time.deltaTime;
            _currentRotation %= 360f;
        }
        else
        {
            _currentRotation = starRotation;
        }
        
        UpdateMaterial();
    }
    
    private void UpdateMaterial()
    {
        targetRenderer.GetPropertyBlock(_propertyBlock);
        
        _propertyBlock.SetColor(CoreColorID, coreColor);
        _propertyBlock.SetFloat(CoreIntensityID, coreIntensity);
        _propertyBlock.SetFloat(StarSharpnessID, starSharpness);
        _propertyBlock.SetFloat(StarPointsID, starPoints);
        _propertyBlock.SetFloat(StarRotationID, _currentRotation);
        _propertyBlock.SetFloat(RainbowIntensityID, ringIntensity);
        _propertyBlock.SetFloat(RainbowWidthID, ringWidth);
        _propertyBlock.SetFloat(RainbowOffsetID, ringRadius);
        _propertyBlock.SetFloat(ChromaticSpreadID, rgbSeparation);
        _propertyBlock.SetFloat(RedStrengthID, redStrength);
        _propertyBlock.SetFloat(GreenStrengthID, greenStrength);
        _propertyBlock.SetFloat(BlueStrengthID, blueStrength);
        _propertyBlock.SetFloat(EclipseAmountID, eclipseAmount);
        _propertyBlock.SetFloat(EclipseAngleID, eclipseAngle);
        _propertyBlock.SetFloat(EclipseSoftnessID, eclipseSoftness);
        _propertyBlock.SetFloat(PulseSpeedID, pulseSpeed);
        _propertyBlock.SetFloat(PulseAmountID, pulseAmount);
        _propertyBlock.SetFloat(PulseMinID, pulseMin);
        _propertyBlock.SetFloat(SecondaryStarEnabledID, secondaryStarEnabled ? 1f : 0f);
        _propertyBlock.SetFloat(SecondaryStarScaleID, secondaryStarScale);
        _propertyBlock.SetFloat(SecondaryStarRotationOffsetID, secondaryStarRotationOffset);
        _propertyBlock.SetFloat(SecondaryStarSharpnessID, secondaryStarSharpness);
        
        targetRenderer.SetPropertyBlock(_propertyBlock);
    }
    
    /// <summary>
    /// Trigger a flash effect by temporarily boosting intensity
    /// </summary>
    public void Flash(float intensity = 5f, float duration = 0.2f)
    {
        StartCoroutine(FlashCoroutine(intensity, duration));
    }
    
    private System.Collections.IEnumerator FlashCoroutine(float intensity, float duration)
    {
        float originalIntensity = coreIntensity;
        coreIntensity = intensity;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            coreIntensity = Mathf.Lerp(intensity, originalIntensity, elapsed / duration);
            yield return null;
        }
        
        coreIntensity = originalIntensity;
    }
    
    /// <summary>
    /// Fade in the sparkle
    /// </summary>
    public void FadeIn(float duration = 0.5f)
    {
        StartCoroutine(FadeCoroutine(0f, coreIntensity, duration));
    }
    
    /// <summary>
    /// Fade out the sparkle
    /// </summary>
    public void FadeOut(float duration = 0.5f)
    {
        StartCoroutine(FadeCoroutine(coreIntensity, 0f, duration));
    }
    
    private System.Collections.IEnumerator FadeCoroutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            coreIntensity = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        coreIntensity = to;
    }
    
    /// <summary>
    /// Set random variation for this sparkle
    /// </summary>
    public void Randomize(float variationAmount = 0.3f)
    {
        starRotation = Random.Range(0f, 360f);
        coreIntensity *= Random.Range(1f - variationAmount, 1f + variationAmount);
        pulseSpeed *= Random.Range(0.8f, 1.2f);
        ringIntensity *= Random.Range(1f - variationAmount, 1f + variationAmount);
        
        // Randomize phase by adjusting pulse parameters slightly
        pulseMin = Mathf.Clamp01(pulseMin + Random.Range(-0.1f, 0.1f));
    }
}
