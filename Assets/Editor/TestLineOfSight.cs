using UnityEngine;
using UnityEditor;

/// <summary>
/// Quick test tool for line-of-sight system
/// </summary>
public class TestLineOfSight : EditorWindow
{
    [MenuItem("Tactical Tools/Debug/Test Line of Sight")]
    public static void TestLOS()
    {
        Debug.Log("=== Testing Line of Sight System ===");
        
        LineOfSightManager losManager = FindFirstObjectByType<LineOfSightManager>();
        if (losManager == null)
        {
            Debug.LogError("LineOfSightManager not found! Run Task 2.1.2 setup first.");
            return;
        }
        
        Debug.Log("✓ LineOfSightManager found");
        
        // Find units to test with
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        if (units.Length < 2)
        {
            Debug.LogError("Need at least 2 units to test line-of-sight");
            return;
        }
        
        Debug.Log($"Found {units.Length} units for testing");
        
        // Test line-of-sight between first two units
        Unit unit1 = units[0];
        Unit unit2 = units[1];
        
        Debug.Log($"\nTesting line-of-sight from {unit1.name} to {unit2.name}:");
        
        bool hasLOS = losManager.HasLineOfSight(unit1.transform.position, unit2.transform.position);
        Debug.Log($"  Has line of sight: {hasLOS}");
        
        LineOfSightResult detailedResult = losManager.GetLineOfSightDetails(unit1.transform.position, unit2.transform.position);
        Debug.Log($"  Detailed result: {detailedResult}");
        
        // Visualize the line-of-sight
        losManager.VisualizeLineOfSight(unit1.transform.position, unit2.transform.position, 5.0f);
        
        // Test combat integration
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        AttackValidator attackValidator = combatManager?.GetComponent<AttackValidator>();
        
        if (attackValidator != null)
        {
            Debug.Log("\n✓ AttackValidator found - testing combat integration");
            
            // Check if line-of-sight is required
            string validationInfo = attackValidator.GetValidationInfo();
            Debug.Log($"  Validation settings: {validationInfo}");
        }
        else
        {
            Debug.LogWarning("AttackValidator not found on CombatManager");
        }
        
        Debug.Log("\n=== Line of Sight Test Complete ===");
    }
}