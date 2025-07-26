using UnityEngine;
using UnityEditor;

/// <summary>
/// Quick fix tool to add missing combat components to units
/// </summary>
public class FixCombatComponents : EditorWindow
{
    [MenuItem("Tactical Tools/Fix Combat Components")]
    public static void ShowWindow()
    {
        GetWindow<FixCombatComponents>("Fix Combat");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Combat Component Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Add Combat Components to All Units", GUILayout.Height(30)))
        {
            FixAllUnits();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("This will add AttackCapability and TargetCapability\ncomponents to all Unit objects in the scene.");
    }
    
    void FixAllUnits()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        int fixedCount = 0;
        foreach (Unit unit in allUnits)
        {
            bool wasFixed = false;
            
            // Add AttackCapability if missing
            if (unit.GetComponent<AttackCapability>() == null)
            {
                unit.gameObject.AddComponent<AttackCapability>();
                wasFixed = true;
            }
            
            // Add TargetCapability if missing  
            if (unit.GetComponent<TargetCapability>() == null)
            {
                unit.gameObject.AddComponent<TargetCapability>();
                wasFixed = true;
            }
            
            if (wasFixed)
            {
                fixedCount++;
                EditorUtility.SetDirty(unit);
            }
        }
        
        Debug.Log($"✅ Fixed {fixedCount} units with missing combat components!");
    }
}