using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Efficient spawner and pooler for star sparkle effects.
/// Use this when you need many sparkles on screen.
/// </summary>
public class StarSparkleSpawner : MonoBehaviour
{
    [Header("Prefab Setup")]
    [SerializeField] private GameObject sparklePrefab;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 100;
    
    [Header("Spawn Settings")]
    [SerializeField] private bool autoSpawn = false;
    [SerializeField] private float spawnRate = 2f; // sparkles per second
    [SerializeField] private Vector3 spawnAreaMin = new Vector3(-5f, -3f, 0f);
    [SerializeField] private Vector3 spawnAreaMax = new Vector3(5f, 3f, 0f);
    
    [Header("Sparkle Lifetime")]
    [SerializeField] private float minLifetime = 0.5f;
    [SerializeField] private float maxLifetime = 2f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    [Header("Variation")]
    [SerializeField] private float scaleMin = 0.5f;
    [SerializeField] private float scaleMax = 1.5f;
    [SerializeField] private bool randomizeOnSpawn = true;
    [SerializeField] private float variationAmount = 0.3f;
    
    private Queue<GameObject> _pool = new Queue<GameObject>();
    private List<ActiveSparkle> _activeSparkles = new List<ActiveSparkle>();
    private float _spawnTimer;
    
    private class ActiveSparkle
    {
        public GameObject gameObject;
        public StarSparkleController controller;
        public float lifetime;
        public float elapsed;
        public float fadeOutDuration;
        public bool isFadingOut;
    }
    
    private void Start()
    {
        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledObject();
        }
    }
    
    private GameObject CreatePooledObject()
    {
        if (sparklePrefab == null)
        {
            Debug.LogError("StarSparkleSpawner: No prefab assigned!");
            return null;
        }
        
        GameObject obj = Instantiate(sparklePrefab, transform);
        obj.SetActive(false);
        _pool.Enqueue(obj);
        return obj;
    }
    
    private GameObject GetFromPool()
    {
        if (_pool.Count == 0)
        {
            if (_activeSparkles.Count < maxPoolSize)
            {
                return CreatePooledObject();
            }
            return null; // Pool exhausted
        }
        
        return _pool.Dequeue();
    }
    
    private void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }
    
    private void Update()
    {
        // Handle auto spawning
        if (autoSpawn && spawnRate > 0)
        {
            _spawnTimer += Time.deltaTime;
            float spawnInterval = 1f / spawnRate;
            
            while (_spawnTimer >= spawnInterval)
            {
                _spawnTimer -= spawnInterval;
                SpawnRandom();
            }
        }
        
        // Update active sparkles
        for (int i = _activeSparkles.Count - 1; i >= 0; i--)
        {
            var sparkle = _activeSparkles[i];
            sparkle.elapsed += Time.deltaTime;
            
            // Check if should start fading out
            if (!sparkle.isFadingOut && sparkle.elapsed >= sparkle.lifetime - sparkle.fadeOutDuration)
            {
                sparkle.isFadingOut = true;
                sparkle.controller?.FadeOut(sparkle.fadeOutDuration);
            }
            
            // Check if lifetime expired
            if (sparkle.elapsed >= sparkle.lifetime)
            {
                ReturnToPool(sparkle.gameObject);
                _activeSparkles.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Spawn a sparkle at a random position within the spawn area
    /// </summary>
    public StarSparkleController SpawnRandom()
    {
        Vector3 position = new Vector3(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y),
            Random.Range(spawnAreaMin.z, spawnAreaMax.z)
        );
        
        return SpawnAt(position);
    }
    
    /// <summary>
    /// Spawn a sparkle at a specific world position
    /// </summary>
    public StarSparkleController SpawnAt(Vector3 worldPosition)
    {
        return SpawnAt(worldPosition, Random.Range(minLifetime, maxLifetime));
    }
    
    /// <summary>
    /// Spawn a sparkle at a specific position with custom lifetime
    /// </summary>
    public StarSparkleController SpawnAt(Vector3 worldPosition, float lifetime)
    {
        GameObject obj = GetFromPool();
        if (obj == null) return null;
        
        obj.transform.position = worldPosition;
        
        // Random scale
        float scale = Random.Range(scaleMin, scaleMax);
        obj.transform.localScale = Vector3.one * scale;
        
        obj.SetActive(true);
        
        var controller = obj.GetComponent<StarSparkleController>();
        
        // Apply random variation
        if (randomizeOnSpawn && controller != null)
        {
            controller.Randomize(variationAmount);
        }
        
        // Fade in
        controller?.FadeIn(0.1f);
        
        // Track active sparkle
        _activeSparkles.Add(new ActiveSparkle
        {
            gameObject = obj,
            controller = controller,
            lifetime = lifetime,
            elapsed = 0f,
            fadeOutDuration = fadeOutDuration,
            isFadingOut = false
        });
        
        return controller;
    }
    
    /// <summary>
    /// Spawn multiple sparkles in a burst pattern
    /// </summary>
    public void SpawnBurst(Vector3 center, int count, float radius = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 position = center + new Vector3(randomCircle.x, randomCircle.y, 0f);
            SpawnAt(position);
        }
    }
    
    /// <summary>
    /// Spawn sparkles along a line
    /// </summary>
    public void SpawnLine(Vector3 start, Vector3 end, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            Vector3 position = Vector3.Lerp(start, end, t);
            SpawnAt(position);
        }
    }
    
    /// <summary>
    /// Clear all active sparkles immediately
    /// </summary>
    public void ClearAll()
    {
        foreach (var sparkle in _activeSparkles)
        {
            ReturnToPool(sparkle.gameObject);
        }
        _activeSparkles.Clear();
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visualize spawn area
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 center = (spawnAreaMin + spawnAreaMax) / 2f;
        Vector3 size = spawnAreaMax - spawnAreaMin;
        Gizmos.DrawCube(transform.position + center, size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + center, size);
    }
}
