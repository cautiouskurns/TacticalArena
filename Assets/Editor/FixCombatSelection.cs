using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to fix combat selection issues
/// </summary>
public class FixCombatSelection : EditorWindow
{
    [MenuItem("Tactical Tools/Debug/Test Combat Selection")]
    public static void TestSelection()
    {
        Debug.Log("=== Testing Combat Selection ===");
        
        // Find selection manager
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager == null)
        {
            Debug.LogError("SelectionManager not found!");
            return;
        }
        
        // Get current selection
        var selectedObjects = selectionManager.GetSelectedObjects<ISelectable>();
        Debug.Log($"Currently selected objects: {selectedObjects.Count}");
        
        foreach (var selected in selectedObjects)
        {
            Debug.Log($"  - {selected.GetDisplayInfo()}");
            
            // Check if it's an attacker
            if (selected is IAttacker attacker)
            {
                Debug.Log($"    ✓ Is IAttacker with damage: {attacker.AttackDamage}");
            }
            else
            {
                Debug.Log($"    ✗ Is NOT an IAttacker");
                
                // Check if it has AttackCapability component
                GameObject go = (selected as MonoBehaviour)?.gameObject;
                if (go != null)
                {
                    var attackCap = go.GetComponent<AttackCapability>();
                    if (attackCap != null)
                    {
                        Debug.LogWarning($"    ! Has AttackCapability but not implementing IAttacker properly");
                    }
                }
            }
        }
        
        // Find CombatInputHandler
        CombatInputHandler inputHandler = FindFirstObjectByType<CombatInputHandler>();
        if (inputHandler != null)
        {
            Debug.Log($"\nCombatInputHandler status:");
            Debug.Log($"  - Selected Attacker: {inputHandler.SelectedAttacker?.GetDisplayInfo() ?? "None"}");
            Debug.Log($"  - Combat Mode Active: {inputHandler.CombatModeActive}");
        }
        else
        {
            Debug.LogError("CombatInputHandler not found!");
        }
    }
    
    [MenuItem("Tactical Tools/Debug/Force Select Blue Unit")]
    public static void ForceSelectBlueUnit()
    {
        // Find a blue unit
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit blueUnit = null;
        
        foreach (Unit unit in units)
        {
            if (unit.Team == UnitTeam.Blue)
            {
                blueUnit = unit;
                break;
            }
        }
        
        if (blueUnit == null)
        {
            Debug.LogError("No blue unit found!");
            return;
        }
        
        // Force selection
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager != null)
        {
            // Call the selection method directly
            //selectionManager.SelectObject(blueUnit);
            Debug.Log($"Force selected: {blueUnit.name}");
            
            // Check if combat input handler got the selection
            CombatInputHandler inputHandler = FindFirstObjectByType<CombatInputHandler>();
            if (inputHandler != null)
            {
                // Wait a frame for events to process
                EditorApplication.delayCall += () =>
                {
                    Debug.Log($"CombatInputHandler selected attacker: {inputHandler.SelectedAttacker?.GetDisplayInfo() ?? "None"}");
                };
            }
        }
    }
}