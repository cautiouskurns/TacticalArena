using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized health bar management system.
/// Part of Task 2.1.4 - Combat Visual Feedback - COMPLETES SUB-MILESTONE 2.1
/// </summary>
public class HealthBarManager : MonoBehaviour
{
    [Header("Health Bar Configuration")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private bool enableDebugging = true;
    
    // Component references
    private CombatVisualManager visualManager;
    private Dictionary<Unit, HealthBarUI> healthBars;
    
    public bool IsInitialized { get; private set; } = false;
    
    public void Initialize(CombatVisualManager manager)
    {
        visualManager = manager;
        healthBars = new Dictionary<Unit, HealthBarUI>();
        
        // Find all units and create health bars
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in units)
        {
            CreateHealthBarForUnit(unit);
        }
        
        IsInitialized = true;
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarManager initialized - Created {healthBars.Count} health bars");
            
            // List all health bars created
            foreach (var kvp in healthBars)
            {
                Debug.Log($"  - Health bar for {kvp.Key.name}: {kvp.Value?.gameObject?.name ?? "NULL"}");
            }
        }
    }
    
    public void UpdateHealthBar(Unit unit, int newHealth)
    {
        if (!IsInitialized || unit == null) 
        {
            if (enableDebugging && unit != null)
            {
                Debug.LogWarning($"HealthBarManager: Cannot update health bar for {unit.name} - Manager not initialized");
            }
            return;
        }
        
        if (healthBars.TryGetValue(unit, out HealthBarUI healthBar))
        {
            HealthComponent healthComponent = unit.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                float percentage = (float)newHealth / healthComponent.MaxHealth;
                healthBar.SetHealthPercentage(percentage);
                
                if (enableDebugging)
                {
                    Debug.Log($"HealthBarManager: Updated health bar for {unit.name} - {newHealth}/{healthComponent.MaxHealth} ({percentage:P0})");
                }
            }
            else if (enableDebugging)
            {
                Debug.LogWarning($"HealthBarManager: No HealthComponent found on {unit.name}");
            }
        }
        else if (enableDebugging)
        {
            Debug.LogWarning($"HealthBarManager: No health bar found for {unit.name}");
        }
    }
    
    public void RemoveHealthBar(Unit unit)
    {
        if (!IsInitialized || unit == null) return;
        
        if (healthBars.TryGetValue(unit, out HealthBarUI healthBar))
        {
            if (healthBar != null) Destroy(healthBar.gameObject);
            healthBars.Remove(unit);
        }
    }
    
    private void CreateHealthBarForUnit(Unit unit)
    {
        if (unit == null) return;
        
        HealthBarUI existingHealthBar = unit.GetComponentInChildren<HealthBarUI>();
        if (existingHealthBar != null)
        {
            healthBars[unit] = existingHealthBar;
            existingHealthBar.SetTargetUnit(unit);
            return;
        }
        
        // Create simple health bar if no prefab - ALWAYS create simple for now
        CreateSimpleHealthBar(unit);
        
        // Original prefab code disabled for debugging
        // if (healthBarPrefab == null)
        // {
        //     CreateSimpleHealthBar(unit);
        // }
        // else
        // {
        //     GameObject healthBarInstance = Instantiate(healthBarPrefab, unit.transform);
        //     HealthBarUI healthBarUI = healthBarInstance.GetComponent<HealthBarUI>();
        //     if (healthBarUI != null)
        //     {
        //         healthBarUI.SetTargetUnit(unit);
        //         healthBars[unit] = healthBarUI;
        //     }
        // }
    }
    
    private void CreateSimpleHealthBar(Unit unit)
    {
        // Create a simple health bar using UI Canvas
        GameObject healthBarObject = new GameObject("HealthBar");
        healthBarObject.transform.SetParent(unit.transform);
        healthBarObject.transform.localPosition = Vector3.up * 1.5f;
        
        Canvas canvas = healthBarObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 100; // Ensure it renders on top
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2f, 0.4f); // Make it bigger and more visible
        
        HealthBarUI healthBarUI = healthBarObject.AddComponent<HealthBarUI>();
        healthBarUI.SetTargetUnit(unit);
        healthBars[unit] = healthBarUI;
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarManager: Created simple health bar for {unit.name} at position {healthBarObject.transform.position}");
        }
    }
    
    void OnDestroy()
    {
        if (healthBars != null)
        {
            foreach (var kvp in healthBars)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            healthBars.Clear();
        }
        
        if (enableDebugging)
        {
            Debug.Log("HealthBarManager destroyed - Health bars cleaned up");
        }
    }
}