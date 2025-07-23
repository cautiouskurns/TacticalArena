using UnityEngine;

/// <summary>
/// Debug component to help troubleshoot combat system issues.
/// Attach to any GameObject to monitor combat events in the scene.
/// </summary>
public class CombatDebugger : MonoBehaviour
{
    [Header("Debug Configuration")]
    [SerializeField] private bool enableCombatLogging = true;
    [SerializeField] private bool showHealthValues = true;
    [SerializeField] private bool logRaycastHits = true;
    
    private CombatManager combatManager;
    private SelectionManager selectionManager;
    
    void Start()
    {
        SetupDebugMonitoring();
    }
    
    void Update()
    {
        if (enableCombatLogging)
        {
            MonitorMouseClicks();
            ShowHealthStatus();
        }
    }
    
    private void SetupDebugMonitoring()
    {
        // Find and monitor combat manager
        combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null && enableCombatLogging)
        {
            Debug.Log("CombatDebugger: Found CombatManager - monitoring combat events");
        }
        
        // Find and monitor selection manager
        selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager != null && enableCombatLogging)
        {
            Debug.Log("CombatDebugger: Found SelectionManager - monitoring selection events");
        }
    }
    
    private void MonitorMouseClicks()
    {
        if (Input.GetMouseButtonDown(0) && logRaycastHits)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 mousePosition = Input.mousePosition;
                Ray ray = mainCamera.ScreenPointToRay(mousePosition);
                
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    GameObject hitObject = hit.collider.gameObject;
                    Debug.Log($"CombatDebugger: Mouse click hit {hitObject.name}");
                    
                    // Check for combat components
                    var attackable = hitObject.GetComponent<IAttackable>();
                    var attacker = hitObject.GetComponent<IAttacker>();
                    var attackCap = hitObject.GetComponent<AttackCapability>();
                    var targetCap = hitObject.GetComponent<TargetCapability>();
                    var unit = hitObject.GetComponent<Unit>();
                    var unitHealth = hitObject.GetComponent<UnitHealth>();
                    
                    Debug.Log($"CombatDebugger: Components on {hitObject.name}: " +
                             $"IAttackable={attackable != null}, IAttacker={attacker != null}, " +
                             $"AttackCapability={attackCap != null}, TargetCapability={targetCap != null}, " +
                             $"Unit={unit != null}, UnitHealth={unitHealth != null}");
                    
                    if (attackable != null)
                    {
                        Debug.Log($"CombatDebugger: Target info - {attackable.GetDisplayInfo()}");
                        Debug.Log($"CombatDebugger: Target health - {attackable.CurrentHealth}/{attackable.MaxHealth}");
                    }
                }
                else
                {
                    Debug.Log("CombatDebugger: Mouse click hit nothing");
                }
            }
        }
    }
    
    private void ShowHealthStatus()
    {
        if (!showHealthValues) return;
        
        // Find all units and show their health status
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;
            
            var unitHealth = unit.GetComponent<UnitHealth>();
            var targetCapability = unit.GetComponent<TargetCapability>();
            
            if (unitHealth != null && targetCapability != null)
            {
                // Only log if there's a health discrepancy
                if (unitHealth.CurrentHealth != targetCapability.CurrentHealth)
                {
                    Debug.LogWarning($"CombatDebugger: Health mismatch on {unit.name} - " +
                                   $"UnitHealth: {unitHealth.CurrentHealth}/{unitHealth.MaxHealth}, " +
                                   $"TargetCapability: {targetCapability.CurrentHealth}/{targetCapability.MaxHealth}");
                }
            }
        }
    }
    
    void OnGUI()
    {
        if (!enableCombatLogging) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label("=== Combat Debug Info ===", GUI.skin.box);
        
        // Show selected unit info
        if (selectionManager != null)
        {
            var selectedObjects = selectionManager.GetSelectedObjects<ISelectable>();
            if (selectedObjects.Count > 0)
            {
                var selected = selectedObjects[0];
                GUILayout.Label($"Selected: {selected.GetDisplayInfo()}");
                
                if (selected is IAttacker attacker)
                {
                    GUILayout.Label($"Can Attack: {attacker.CanAttack}");
                    GUILayout.Label($"Attack Damage: {attacker.AttackDamage}");
                    GUILayout.Label($"Attack Range: {attacker.AttackRange}");
                }
            }
            else
            {
                GUILayout.Label("No unit selected");
            }
        }
        
        // Show all unit health
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        GUILayout.Label($"Units in scene: {allUnits.Length}");
        
        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;
            
            var unitHealth = unit.GetComponent<UnitHealth>();
            if (unitHealth != null)
            {
                GUILayout.Label($"{unit.name}: {unitHealth.CurrentHealth}/{unitHealth.MaxHealth} HP ({unit.Team})");
            }
        }
        
        GUILayout.EndArea();
    }
}