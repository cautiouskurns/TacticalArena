using UnityEngine;
using UnityEditor;

/// <summary>
/// Debug tool for troubleshooting the selection system
/// </summary>
public class SelectionDebugger : EditorWindow
{
    [MenuItem("Tactical Tools/Debug Selection System")]
    public static void ShowWindow()
    {
        GetWindow<SelectionDebugger>("Selection Debugger");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Selection System Debug Info", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Check Scene State"))
        {
            CheckSceneState();
        }
        
        if (GUILayout.Button("Test Unit Selection Manually"))
        {
            TestUnitSelection();
        }
        
        if (GUILayout.Button("Check Unit Components"))
        {
            CheckUnitComponents();
        }
        
        if (GUILayout.Button("Fix Selection Highlight Renderers"))
        {
            FixSelectionHighlightRenderers();
        }
        
        if (GUILayout.Button("Enable Debug on All Highlights"))
        {
            EnableDebugOnAllHighlights();
        }
        
        if (GUILayout.Button("Detailed Renderer Analysis"))
        {
            DetailedRendererAnalysis();
        }
    }
    
    private void CheckSceneState()
    {
        Debug.Log("=== SELECTION SYSTEM DEBUG ===");
        
        // Check for SelectionManager
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        Debug.Log($"SelectionManager found: {selectionManager != null}");
        if (selectionManager != null)
        {
            Debug.Log($"SelectionManager on GameObject: {selectionManager.gameObject.name}");
        }
        
        // Check for MouseInputHandler
        MouseInputHandler inputHandler = FindFirstObjectByType<MouseInputHandler>();
        Debug.Log($"MouseInputHandler found: {inputHandler != null}");
        if (inputHandler != null)
        {
            Debug.Log($"MouseInputHandler on GameObject: {inputHandler.gameObject.name}");
        }
        
        // Check units
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Debug.Log($"Units found: {units.Length}");
        
        foreach (Unit unit in units)
        {
            Debug.Log($"Unit: {unit.name}");
            Debug.Log($"  - Has ISelectable: {unit is ISelectable}");
            Debug.Log($"  - Has SelectionHighlight: {unit.GetComponent<SelectionHighlight>() != null}");
            Debug.Log($"  - Has Collider: {unit.GetComponent<Collider>() != null}");
            Debug.Log($"  - CanBeSelected: {unit.CanBeSelected}");
        }
        
        Debug.Log("=== END DEBUG ===");
    }
    
    private void TestUnitSelection()
    {
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        if (selectionManager == null)
        {
            Debug.LogError("No SelectionManager found!");
            return;
        }
        
        if (units.Length == 0)
        {
            Debug.LogError("No units found!");
            return;
        }
        
        Unit testUnit = units[0];
        Debug.Log($"Testing selection on: {testUnit.name}");
        
        bool result = selectionManager.TrySelectObject(testUnit);
        Debug.Log($"Selection result: {result}");
        Debug.Log($"Unit is now selected: {testUnit.IsSelected}");
    }
    
    private void CheckUnitComponents()
    {
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in units)
        {
            Debug.Log($"=== {unit.name} Component Check ===");
            
            // Check all components on the unit itself
            var components = unit.GetComponents<Component>();
            Debug.Log($"Components on {unit.name}:");
            foreach (var component in components)
            {
                Debug.Log($"  - {component.GetType().Name}");
            }
            
            // Check for Renderer components using the same logic as SelectionHighlight
            Debug.Log($"\nRenderer component search for {unit.name}:");
            
            // Check self
            Renderer selfRenderer = unit.GetComponent<Renderer>();
            Debug.Log($"  GetComponent<Renderer>() on self: {selfRenderer?.name ?? "NULL"}");
            
            // Check children
            Renderer childRenderer = unit.GetComponentInChildren<Renderer>();
            Debug.Log($"  GetComponentInChildren<Renderer>(): {childRenderer?.name ?? "NULL"}");
            if (childRenderer != null && childRenderer.gameObject != unit.gameObject)
            {
                Debug.Log($"    Found on child GameObject: {childRenderer.gameObject.name}");
                Debug.Log($"    Child GameObject path: {GetGameObjectPath(childRenderer.gameObject)}");
            }
            
            // Check parent
            Renderer parentRenderer = unit.GetComponentInParent<Renderer>();
            Debug.Log($"  GetComponentInParent<Renderer>(): {parentRenderer?.name ?? "NULL"}");
            if (parentRenderer != null && parentRenderer.gameObject != unit.gameObject)
            {
                Debug.Log($"    Found on parent GameObject: {parentRenderer.gameObject.name}");
                Debug.Log($"    Parent GameObject path: {GetGameObjectPath(parentRenderer.gameObject)}");
            }
            
            // List all child GameObjects and their components
            Debug.Log($"\nComplete hierarchy for {unit.name}:");
            PrintHierarchy(unit.transform, 0);
            
            // Check SelectionHighlight specifically
            SelectionHighlight highlight = unit.GetComponent<SelectionHighlight>();
            if (highlight != null)
            {
                Debug.Log($"\nSelectionHighlight info:");
                Debug.Log($"  State: {highlight.CurrentState}");
                Debug.Log($"  Original material: {highlight.OriginalMaterial?.name ?? "NULL"}");
            }
            else
            {
                Debug.Log($"\nNo SelectionHighlight component found on {unit.name}");
            }
            
            Debug.Log($"=== End {unit.name} ===\n");
        }
    }
    
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
    
    private void PrintHierarchy(Transform transform, int depth)
    {
        string indent = new string(' ', depth * 2);
        
        // Get all components on this GameObject
        Component[] components = transform.GetComponents<Component>();
        string componentList = "";
        foreach (var comp in components)
        {
            if (comp != null && comp.GetType() != typeof(Transform))
            {
                componentList += comp.GetType().Name + " ";
            }
        }
        
        Debug.Log($"{indent}{transform.name} [{componentList.TrimEnd()}]");
        
        // Recursively print children
        for (int i = 0; i < transform.childCount; i++)
        {
            PrintHierarchy(transform.GetChild(i), depth + 1);
        }
    }
    
    private void FixSelectionHighlightRenderers()
    {
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int fixedCount = 0;
        
        Debug.Log("=== FIXING SELECTION HIGHLIGHT RENDERERS ===");
        
        foreach (Unit unit in units)
        {
            SelectionHighlight highlight = unit.GetComponent<SelectionHighlight>();
            if (highlight != null)
            {
                Debug.Log($"Refreshing SelectionHighlight on {unit.name}");
                highlight.RefreshHighlight();
                fixedCount++;
            }
        }
        
        Debug.Log($"Fixed {fixedCount} SelectionHighlight components");
        Debug.Log("=== END FIX ===");
    }
    
    private void EnableDebugOnAllHighlights()
    {
        SelectionHighlight[] highlights = FindObjectsByType<SelectionHighlight>(FindObjectsSortMode.None);
        int enabledCount = 0;
        
        Debug.Log("=== ENABLING DEBUG ON ALL HIGHLIGHTS ===");
        
        foreach (SelectionHighlight highlight in highlights)
        {
            // Use reflection to enable debug logging
            var field = typeof(SelectionHighlight).GetField("enableDebugLogging", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(highlight, true);
                enabledCount++;
                Debug.Log($"Enabled debug logging on {highlight.gameObject.name}");
            }
        }
        
        Debug.Log($"Enabled debug logging on {enabledCount} SelectionHighlight components");
        Debug.Log("=== END DEBUG ENABLE ===");
    }
    
    private void DetailedRendererAnalysis()
    {
        Debug.Log("=== DETAILED RENDERER ANALYSIS ===");
        
        // Find all units
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Debug.Log($"Found {units.Length} units in scene");
        
        // Find all renderers
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        Debug.Log($"Found {allRenderers.Length} renderers in scene");
        
        // Analyze each unit
        foreach (Unit unit in units)
        {
            Debug.Log($"\n--- ANALYZING {unit.name} ---");
            
            // Check for SelectionHighlight
            SelectionHighlight highlight = unit.GetComponent<SelectionHighlight>();
            Debug.Log($"Has SelectionHighlight: {highlight != null}");
            
            // Check for renderers using the same strategies as SelectionHighlight
            Debug.Log("Testing renderer finding strategies:");
            
            // Strategy 1: GetComponent<Renderer>
            Renderer directRenderer = unit.GetComponent<Renderer>();
            Debug.Log($"  Strategy 1 - GetComponent<Renderer>(): {directRenderer != null}");
            if (directRenderer != null)
            {
                Debug.Log($"    Type: {directRenderer.GetType().Name}, Material: {directRenderer.material?.name}");
            }
            
            // Strategy 2: GetComponent<MeshRenderer>
            MeshRenderer meshRenderer = unit.GetComponent<MeshRenderer>();
            Debug.Log($"  Strategy 2 - GetComponent<MeshRenderer>(): {meshRenderer != null}");
            if (meshRenderer != null)
            {
                Debug.Log($"    Material: {meshRenderer.material?.name}");
            }
            
            // Strategy 3: GetComponentsInChildren<Renderer>
            Renderer[] childRenderers = unit.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"  Strategy 3 - GetComponentsInChildren<Renderer>(): {childRenderers.Length} found");
            for (int i = 0; i < childRenderers.Length; i++)
            {
                var renderer = childRenderers[i];
                Debug.Log($"    [{i}] {renderer.GetType().Name} on '{renderer.gameObject.name}' (Active: {renderer.gameObject.activeInHierarchy})");
            }
            
            // Check what the SelectionHighlight found (if it exists)
            if (highlight != null)
            {
                // Use reflection to check targetRenderer field
                var targetRendererField = typeof(SelectionHighlight).GetField("targetRenderer", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (targetRendererField != null)
                {
                    Renderer foundRenderer = (Renderer)targetRendererField.GetValue(highlight);
                    Debug.Log($"  SelectionHighlight found renderer: {foundRenderer != null}");
                    if (foundRenderer != null)
                    {
                        Debug.Log($"    Using: {foundRenderer.GetType().Name} on '{foundRenderer.gameObject.name}'");
                    }
                }
            }
        }
        
        Debug.Log("=== END DETAILED ANALYSIS ===");
    }
}