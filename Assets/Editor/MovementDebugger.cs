using UnityEngine;
using UnityEditor;

/// <summary>
/// Debug tool for troubleshooting the grid movement system
/// </summary>
public class MovementDebugger : EditorWindow
{
    [MenuItem("Tactical Tools/Debug Movement System")]
    public static void ShowWindow()
    {
        GetWindow<MovementDebugger>("Movement Debugger");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Movement System Debug Info", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Check Movement System State"))
        {
            CheckMovementSystemState();
        }
        
        if (GUILayout.Button("Test Unit Movement Manually"))
        {
            TestUnitMovement();
        }
        
        if (GUILayout.Button("Validate Movement Components"))
        {
            ValidateMovementComponents();
        }
        
        if (GUILayout.Button("Check Grid System Integration"))
        {
            CheckGridSystemIntegration();
        }
    }
    
    private void CheckMovementSystemState()
    {
        Debug.Log("=== MOVEMENT SYSTEM DEBUG ==");
        
        // Check for MovementManager
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        Debug.Log($"MovementManager found: {movementManager != null}");
        if (movementManager != null)
        {
            Debug.Log($"MovementManager info: {movementManager.GetMovementInfo()}");
        }
        
        // Check for MovementValidator
        MovementValidator validator = FindFirstObjectByType<MovementValidator>();
        Debug.Log($"MovementValidator found: {validator != null}");
        if (validator != null)
        {
            Debug.Log($"MovementValidator info: {validator.GetValidationInfo()}");
        }
        
        // Check for MovementAnimator
        MovementAnimator animator = FindFirstObjectByType<MovementAnimator>();
        Debug.Log($"MovementAnimator found: {animator != null}");
        if (animator != null)
        {
            Debug.Log($"MovementAnimator info: {animator.GetAnimationInfo()}");
        }
        
        // Check SelectionManager integration
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        Debug.Log($"SelectionManager found: {selectionManager != null}");
        if (selectionManager != null)
        {
            Debug.Log($"SelectionManager info: {selectionManager.GetSelectionInfo()}");
        }
        
        Debug.Log("=== END MOVEMENT DEBUG ===");
    }
    
    private void TestUnitMovement()
    {
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        
        if (movementManager == null)
        {
            Debug.LogError("No MovementManager found!");
            return;
        }
        
        if (gridManager == null)
        {
            Debug.LogError("No GridManager found!");
            return;
        }
        
        if (units.Length == 0)
        {
            Debug.LogError("No units found!");
            return;
        }
        
        Unit testUnit = units[0];
        Debug.Log($"Testing movement on: {testUnit.name}");
        Debug.Log($"Unit current position: {testUnit.GridPosition}");
        Debug.Log($"Unit can move: {testUnit.CanMove}");
        Debug.Log($"Unit implements IMovable: {testUnit is IMovable}");
        
        // Test adjacent movement
        Vector2Int currentPos = testUnit.GridPosition;
        Vector2Int targetPos = new Vector2Int(currentPos.x + 1, currentPos.y);
        
        Debug.Log($"Testing movement to: {targetPos}");
        
        // Test movement validation
        MovementValidationResult validation = testUnit.ValidateMovement(targetPos);
        Debug.Log($"Movement validation: {validation}");
        
        if (validation.isValid)
        {
            bool result = movementManager.RequestUnitMovement(testUnit, targetPos);
            Debug.Log($"Movement request result: {result}");
        }
    }
    
    private void ValidateMovementComponents()
    {
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in units)
        {
            Debug.Log($"=== {unit.name} Movement Component Check ===");
            
            // Check IMovable implementation
            Debug.Log($"  - Implements IMovable: {unit is IMovable}");
            Debug.Log($"  - CanMove: {unit.CanMove}");
            Debug.Log($"  - IsMoving: {unit.IsMoving}");
            Debug.Log($"  - GridPosition: {unit.GridPosition}");
            Debug.Log($"  - Team: {unit.Team}");
            
            // Check GridMovementComponent
            GridMovementComponent movementComponent = unit.GetComponent<GridMovementComponent>();
            if (movementComponent != null)
            {
                Debug.Log($"  - Has GridMovementComponent: true");
                Debug.Log($"  - Movement Component Info: {movementComponent.GetMovementInfo()}");
            }
            else
            {
                Debug.Log($"  - Has GridMovementComponent: false");
            }
            
            // Check collider for mouse interaction
            Collider collider = unit.GetComponent<Collider>();
            Debug.Log($"  - Has Collider: {collider != null}");
            
            Debug.Log($"=== End {unit.name} ===");
        }
    }
    
    private void CheckGridSystemIntegration()
    {
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        
        if (gridManager == null)
        {
            Debug.LogError("No GridManager found!");
            return;
        }
        
        Debug.Log("=== GRID SYSTEM INTEGRATION ===");
        Debug.Log($"GridManager found: {gridManager != null}");
        
        // Test grid coordinate conversion
        GridCoordinate testGridCoord = new GridCoordinate(1, 1);
        
        try
        {
            bool isValid = gridManager.IsValidCoordinate(testGridCoord);
            Debug.Log($"Grid coordinate {testGridCoord} is valid: {isValid}");
            
            if (isValid)
            {
                Vector3 worldPos = gridManager.GridToWorld(testGridCoord);
                GridCoordinate backToGrid = gridManager.WorldToGrid(worldPos);
                Debug.Log($"Grid->World->Grid conversion: {testGridCoord} -> {worldPos} -> {backToGrid}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Grid system integration error: {e.Message}");
        }
        
        Debug.Log("=== END GRID INTEGRATION ===");
    }
}