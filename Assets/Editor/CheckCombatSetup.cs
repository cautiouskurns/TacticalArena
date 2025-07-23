using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to check combat system setup
/// </summary>
public class CheckCombatSetup : EditorWindow
{
    [MenuItem("Tactical Tools/Debug/Check Combat Setup")]
    public static void CheckSetup()
    {
        Debug.Log("=== Checking Combat System Setup ===");
        
        // Check for CombatManager
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
        {
            Debug.Log("✓ CombatManager found");
            
            // Check sub-components
            var validator = combatManager.GetComponent<AttackValidator>();
            var executor = combatManager.GetComponent<AttackExecutor>();
            var targeting = combatManager.GetComponent<TargetingSystem>();
            
            Debug.Log($"  - AttackValidator: {(validator != null ? "✓" : "✗")}");
            Debug.Log($"  - AttackExecutor: {(executor != null ? "✓" : "✗")}");
            Debug.Log($"  - TargetingSystem: {(targeting != null ? "✓" : "✗")}");
        }
        else
        {
            Debug.LogError("✗ CombatManager NOT found!");
        }
        
        // Check for SelectionManager
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager != null)
        {
            Debug.Log("✓ SelectionManager found");
            
            // Check for CombatInputHandler
            CombatInputHandler inputHandler = selectionManager.GetComponent<CombatInputHandler>();
            if (inputHandler != null)
            {
                Debug.Log("✓ CombatInputHandler found on SelectionManager");
                Debug.Log($"  - Enabled: {inputHandler.enabled}");
                Debug.Log($"  - Input Mode: {inputHandler.InputMode}");
                Debug.Log($"  - Combat Mode Active: {inputHandler.CombatModeActive}");
            }
            else
            {
                Debug.LogError("✗ CombatInputHandler NOT found on SelectionManager!");
                
                // Check if it exists elsewhere
                inputHandler = FindFirstObjectByType<CombatInputHandler>();
                if (inputHandler != null)
                {
                    Debug.LogWarning($"  - Found CombatInputHandler on: {inputHandler.gameObject.name}");
                }
            }
        }
        else
        {
            Debug.LogError("✗ SelectionManager NOT found!");
        }
        
        // Check units
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Debug.Log($"\n=== Units: {units.Length} found ===");
        
        foreach (Unit unit in units)
        {
            var attacker = unit.GetComponent<AttackCapability>();
            var target = unit.GetComponent<TargetCapability>();
            var health = unit.GetComponent<UnitHealth>();
            
            Debug.Log($"{unit.name} ({unit.Team}):");
            Debug.Log($"  - AttackCapability: {(attacker != null ? "✓" : "✗")}");
            Debug.Log($"  - TargetCapability: {(target != null ? "✓" : "✗")}");
            Debug.Log($"  - UnitHealth: {(health != null ? "✓" : "✗")}");
            
            if (health != null && target != null)
            {
                Debug.Log($"  - Health: {health.CurrentHealth}/{health.MaxHealth}");
            }
        }
        
        Debug.Log("\n=== Setup Check Complete ===");
    }
    
    [MenuItem("Tactical Tools/Debug/Fix Combat Input Handler")]
    public static void FixCombatInputHandler()
    {
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager == null)
        {
            Debug.LogError("Cannot fix - SelectionManager not found!");
            return;
        }
        
        CombatInputHandler inputHandler = selectionManager.GetComponent<CombatInputHandler>();
        if (inputHandler == null)
        {
            Debug.Log("Adding CombatInputHandler to SelectionManager...");
            inputHandler = selectionManager.gameObject.AddComponent<CombatInputHandler>();
            Debug.Log("✓ CombatInputHandler added successfully!");
        }
        else
        {
            Debug.Log("CombatInputHandler already exists on SelectionManager");
        }
        
        // Ensure it's enabled
        inputHandler.enabled = true;
        Debug.Log("✓ CombatInputHandler is enabled");
        
        EditorUtility.SetDirty(selectionManager.gameObject);
    }
}