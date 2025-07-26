using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Performance optimization through visual effect pooling.
/// Part of Task 2.1.4 - Combat Visual Feedback - COMPLETES SUB-MILESTONE 2.1
/// </summary>
public class EffectPoolManager : MonoBehaviour
{
    [Header("Pool Configuration")]
    [SerializeField] private int poolSize = 20;
    [SerializeField] private int maxActiveEffects = 10;
    [SerializeField] private bool enableDebugging = false;
    
    // Pool management
    private Queue<GameObject> effectPool;
    private List<GameObject> activeEffects;
    
    public bool IsInitialized { get; private set; } = false;
    public float PoolUtilization => effectPool != null ? 1f - (effectPool.Count / (float)poolSize) : 0f;
    
    public void Initialize(int size, int maxActive)
    {
        poolSize = size;
        maxActiveEffects = maxActive;
        
        effectPool = new Queue<GameObject>();
        activeEffects = new List<GameObject>();
        
        IsInitialized = true;
        
        if (enableDebugging)
        {
            Debug.Log($"EffectPoolManager initialized - Pool size: {poolSize}, Max active: {maxActiveEffects}");
        }
    }
    
    public GameObject GetPooledEffect(string effectName)
    {
        if (!IsInitialized || effectPool.Count == 0) return null;
        
        GameObject effect = effectPool.Dequeue();
        activeEffects.Add(effect);
        return effect;
    }
    
    public void ReturnEffectToPool(GameObject effect)
    {
        if (effect == null) return;
        
        effect.SetActive(false);
        activeEffects.Remove(effect);
        
        if (effectPool.Count < poolSize)
        {
            effectPool.Enqueue(effect);
        }
        else
        {
            Destroy(effect);
        }
    }
    
    public void ReturnAllEffectsToPool()
    {
        if (activeEffects != null)
        {
            foreach (GameObject effect in activeEffects)
            {
                if (effect != null)
                {
                    effect.SetActive(false);
                    if (effectPool != null && effectPool.Count < poolSize)
                    {
                        effectPool.Enqueue(effect);
                    }
                    else
                    {
                        Destroy(effect);
                    }
                }
            }
            activeEffects.Clear();
        }
    }
    
    void OnDestroy()
    {
        ReturnAllEffectsToPool();
        
        // Clean up pool
        if (effectPool != null)
        {
            while (effectPool.Count > 0)
            {
                GameObject effect = effectPool.Dequeue();
                if (effect != null) Destroy(effect);
            }
        }
        
        if (enableDebugging)
        {
            Debug.Log("EffectPoolManager destroyed - Pool cleaned up");
        }
    }
}