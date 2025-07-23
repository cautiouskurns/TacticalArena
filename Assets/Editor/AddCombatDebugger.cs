using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to add combat debugging to the scene
/// </summary>
public class AddCombatDebugger : EditorWindow
{
    [MenuItem("Tactical Tools/Debug/Add Combat Debugger")]
    public static void AddDebugger()
    {
        // Create or find debugger object
        GameObject debuggerObj = GameObject.Find("Combat Debugger");
        if (debuggerObj == null)
        {
            debuggerObj = new GameObject("Combat Debugger");
        }
        
        // Add the debugger component
        if (debuggerObj.GetComponent<CombatDebugger>() == null)
        {
            debuggerObj.AddComponent<CombatDebugger>();
        }
        
        Debug.Log("Combat Debugger added to scene. Check the Game view for debug info and console for detailed logs.");
        
        // Select the debugger object
        Selection.activeGameObject = debuggerObj;
    }
}