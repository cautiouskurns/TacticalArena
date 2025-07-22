using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor automation tool for Task 2.1.1 - Attack System Implementation.
/// Creates tactical combat system with adjacent targeting, attack validation, and damage application.
/// Integrates with existing Sub-milestone 1.2 systems for complete combat capability.
/// </summary>
public class Task_2_1_1_Setup : EditorWindow
{
    [Header("Attack Rules Configuration")]
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private bool allowDiagonalAttacks = false;
    [SerializeField] private bool requireLineOfSight = false; // Will be enabled in Task 2.1.2
    [SerializeField] private int attacksPerTurn = 1;
    [SerializeField] private float attackRange = 1.5f; // Range for adjacency detection
    
    [Header("Targeting Configuration")]
    [SerializeField] private LayerMask targetLayerMask = -1;
    [SerializeField] private Material attackTargetHighlightMaterial;
    [SerializeField] private Color validTargetColor = Color.red;
    [SerializeField] private Color invalidTargetColor = Color.gray;
    [SerializeField] private Color attackRangeColor = new Color(1f, 0.5f, 0f, 0.3f);
    
    [Header("Attack Feedback Configuration")]
    [SerializeField] private float attackAnimationDuration = 0.5f;
    [SerializeField] private bool enableAttackPreview = true;
    [SerializeField] private bool showAttackRange = true;
    [SerializeField] private float targetHighlightDuration = 1.0f;
    [SerializeField] private float attackCooldownDuration = 0.2f;
    
    [Header("Combat Integration")]
    [SerializeField] private bool integrateWithSelection = true;
    [SerializeField] private bool integrateWithMovement = true;
    [SerializeField] private bool enableCombatLogging = true;
    [SerializeField] private bool preventFriendlyFire = true;
    
    [Header("Debug Configuration")]
    [SerializeField] private bool enableAttackLogging = true;
    [SerializeField] private bool showAttackDebugInfo = false;
    [SerializeField] private bool enableValidationGizmos = false;
    
    // Validation state
    private bool validationComplete = false;
    private AttackSystemValidationResult lastValidationResult;

    [MenuItem("Tactical Tools/Task 2.1.1 - Attack System")]
    public static void ShowWindow()
    {
        Task_2_1_1_Setup window = GetWindow<Task_2_1_1_Setup>("Task 2.1.1 Setup");
        window.minSize = new Vector2(500, 800);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Attack System Implementation", EditorStyles.largeLabel);
        EditorGUILayout.LabelField("Task 2.1.1 - Adjacent targeting with validation and damage application", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Attack Rules Configuration Section
        EditorGUILayout.LabelField("Attack Rules Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        baseDamage = EditorGUILayout.IntSlider("Base Damage", baseDamage, 1, 5);
        attacksPerTurn = EditorGUILayout.IntSlider("Attacks Per Turn", attacksPerTurn, 1, 3);
        allowDiagonalAttacks = EditorGUILayout.Toggle("Allow Diagonal Attacks", allowDiagonalAttacks);
        attackRange = EditorGUILayout.Slider("Attack Range", attackRange, 1.0f, 3.0f);
        
        requireLineOfSight = EditorGUILayout.Toggle("Require Line of Sight", requireLineOfSight);
        if (requireLineOfSight)
        {
            EditorGUILayout.HelpBox("Line of Sight will be fully implemented in Task 2.1.2", MessageType.Info);
        }
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Targeting Configuration Section
        EditorGUILayout.LabelField("Targeting Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        targetLayerMask = EditorGUILayout.MaskField("Target Layer Mask", targetLayerMask, UnityEditorInternal.InternalEditorUtility.layers);
        attackTargetHighlightMaterial = EditorGUILayout.ObjectField("Target Highlight Material", attackTargetHighlightMaterial, typeof(Material), false) as Material;
        validTargetColor = EditorGUILayout.ColorField("Valid Target Color", validTargetColor);
        invalidTargetColor = EditorGUILayout.ColorField("Invalid Target Color", invalidTargetColor);
        attackRangeColor = EditorGUILayout.ColorField("Attack Range Color", attackRangeColor);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Attack Feedback Configuration Section
        EditorGUILayout.LabelField("Attack Feedback Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        attackAnimationDuration = EditorGUILayout.Slider("Attack Animation Duration", attackAnimationDuration, 0.1f, 2.0f);
        enableAttackPreview = EditorGUILayout.Toggle("Enable Attack Preview", enableAttackPreview);
        showAttackRange = EditorGUILayout.Toggle("Show Attack Range", showAttackRange);
        targetHighlightDuration = EditorGUILayout.Slider("Target Highlight Duration", targetHighlightDuration, 0.5f, 3.0f);
        attackCooldownDuration = EditorGUILayout.Slider("Attack Cooldown Duration", attackCooldownDuration, 0.1f, 1.0f);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Combat Integration Section
        EditorGUILayout.LabelField("Combat Integration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        integrateWithSelection = EditorGUILayout.Toggle("Integrate with Selection", integrateWithSelection);
        integrateWithMovement = EditorGUILayout.Toggle("Integrate with Movement", integrateWithMovement);
        preventFriendlyFire = EditorGUILayout.Toggle("Prevent Friendly Fire", preventFriendlyFire);
        enableCombatLogging = EditorGUILayout.Toggle("Enable Combat Logging", enableCombatLogging);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Debug Configuration Section
        EditorGUILayout.LabelField("Debug Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        enableAttackLogging = EditorGUILayout.Toggle("Enable Attack Logging", enableAttackLogging);
        showAttackDebugInfo = EditorGUILayout.Toggle("Show Debug Info", showAttackDebugInfo);
        enableValidationGizmos = EditorGUILayout.Toggle("Enable Validation Gizmos", enableValidationGizmos);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(15);
        
        // Action Buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Setup Attack System", GUILayout.Height(40)))
        {
            SetupAttackSystem();
        }
        
        if (GUILayout.Button("Reset/Delete Setup", GUILayout.Height(40)))
        {
            ResetSetup();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Validation Section
        EditorGUILayout.LabelField("Validation & Testing", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Validate Attack System", GUILayout.Height(30)))
        {
            ValidateSetup();
        }
        
        // Display validation results
        if (validationComplete && lastValidationResult != null)
        {
            DisplayValidationResults();
        }
        
        GUILayout.Space(10);
        
        // Help Section
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Usage Instructions:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Ensure Sub-milestone 1.2 is complete (Unit System, Selection, Movement)", EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Configure attack rules (damage, diagonal attacks, attacks per turn)", EditorStyles.helpBox);
        EditorGUILayout.LabelField("3. Set Target Layer Mask to include layers containing enemy units", EditorStyles.helpBox);
        EditorGUILayout.LabelField("4. Assign attack highlight materials for target visualization (optional)", EditorStyles.helpBox);
        EditorGUILayout.LabelField("5. Configure attack feedback timing and visual options", EditorStyles.helpBox);
        EditorGUILayout.LabelField("6. Click 'Setup Attack System' to create all combat components", EditorStyles.helpBox);
        EditorGUILayout.LabelField("7. Use 'Validate Attack System' to test attack functionality", EditorStyles.helpBox);
        EditorGUILayout.LabelField("8. Test in Play Mode: Select unit, click on adjacent enemy to attack", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Main setup method that creates the complete attack system
    /// </summary>
    private void SetupAttackSystem()
    {
        Debug.Log("Setting up Attack System for Task 2.1.1...");
        
        try
        {
            // Step 1: Create CombatManager
            SetupCombatManager();
            
            // Step 2: Setup AttackValidator
            SetupAttackValidator();
            
            // Step 3: Setup AttackExecutor
            SetupAttackExecutor();
            
            // Step 4: Setup TargetingSystem
            SetupTargetingSystem();
            
            // Step 5: Enhance existing Unit components with combat interfaces
            EnhanceUnitsWithCombatCapability();
            
            // Step 6: Integrate with existing systems
            IntegrateWithExistingSystems();
            
            // Step 7: Create attack materials if needed
            CreateAttackMaterials();
            
            Debug.Log("Attack System setup complete!");
            EditorUtility.DisplayDialog("Setup Complete", 
                "Attack System has been successfully configured!\n\n" +
                "Task 2.1.1 is now complete with tactical combat capability.\n" +
                "Select units in Play Mode and click on adjacent enemies to attack.", "OK");
            
            // Run automatic validation
            ValidateSetup();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up Attack System: {e.Message}\nStack Trace:\n{e.StackTrace}");
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Setup Error", $"Failed to setup Attack System:\n{e.Message}\n\nCheck console for details.", "OK");
        }
    }

    /// <summary>
    /// Sets up the central CombatManager component
    /// </summary>
    private void SetupCombatManager()
    {
        GameObject combatManagerObj = GameObject.Find("Combat Manager");
        if (combatManagerObj == null)
        {
            combatManagerObj = new GameObject("Combat Manager");
        }

        CombatManager combatManager = combatManagerObj.GetComponent<CombatManager>();
        if (combatManager == null)
        {
            combatManager = combatManagerObj.AddComponent<CombatManager>();
        }

        // Configure CombatManager properties
        var serializedCombat = new SerializedObject(combatManager);
        serializedCombat.FindProperty("baseDamage").intValue = baseDamage;
        serializedCombat.FindProperty("attacksPerTurn").intValue = attacksPerTurn;
        serializedCombat.FindProperty("allowDiagonalAttacks").boolValue = allowDiagonalAttacks;
        serializedCombat.FindProperty("requireLineOfSight").boolValue = requireLineOfSight;
        serializedCombat.FindProperty("attackRange").floatValue = attackRange;
        serializedCombat.FindProperty("preventFriendlyFire").boolValue = preventFriendlyFire;
        serializedCombat.FindProperty("enableCombatLogging").boolValue = enableCombatLogging;
        serializedCombat.ApplyModifiedProperties();

        Debug.Log("CombatManager configured");
    }

    /// <summary>
    /// Sets up the AttackValidator component
    /// </summary>
    private void SetupAttackValidator()
    {
        GameObject combatManagerObj = GameObject.Find("Combat Manager");
        AttackValidator validator = combatManagerObj.GetComponent<AttackValidator>();
        if (validator == null)
        {
            validator = combatManagerObj.AddComponent<AttackValidator>();
        }

        // Configure AttackValidator properties
        var serializedValidator = new SerializedObject(validator);
        serializedValidator.FindProperty("enableValidation").boolValue = true;
        serializedValidator.FindProperty("checkAdjacency").boolValue = true;
        serializedValidator.FindProperty("allowDiagonalAttacks").boolValue = allowDiagonalAttacks;
        serializedValidator.FindProperty("preventFriendlyFire").boolValue = preventFriendlyFire;
        serializedValidator.FindProperty("maxAttackRange").floatValue = attackRange;
        serializedValidator.FindProperty("requireLineOfSight").boolValue = requireLineOfSight;
        serializedValidator.FindProperty("enableValidationLogging").boolValue = enableAttackLogging;
        serializedValidator.ApplyModifiedProperties();

        Debug.Log("AttackValidator configured");
    }

    /// <summary>
    /// Sets up the AttackExecutor component
    /// </summary>
    private void SetupAttackExecutor()
    {
        GameObject combatManagerObj = GameObject.Find("Combat Manager");
        AttackExecutor executor = combatManagerObj.GetComponent<AttackExecutor>();
        if (executor == null)
        {
            executor = combatManagerObj.AddComponent<AttackExecutor>();
        }

        // Configure AttackExecutor properties
        var serializedExecutor = new SerializedObject(executor);
        serializedExecutor.FindProperty("attackAnimationDuration").floatValue = attackAnimationDuration;
        serializedExecutor.FindProperty("attackCooldownDuration").floatValue = attackCooldownDuration;
        serializedExecutor.FindProperty("enableAttackEffects").boolValue = true;
        serializedExecutor.FindProperty("enableDamageNumbers").boolValue = true;
        serializedExecutor.FindProperty("enableExecutionLogging").boolValue = enableAttackLogging;
        serializedExecutor.ApplyModifiedProperties();

        Debug.Log("AttackExecutor configured");
    }

    /// <summary>
    /// Sets up the TargetingSystem component
    /// </summary>
    private void SetupTargetingSystem()
    {
        GameObject combatManagerObj = GameObject.Find("Combat Manager");
        TargetingSystem targeting = combatManagerObj.GetComponent<TargetingSystem>();
        if (targeting == null)
        {
            targeting = combatManagerObj.AddComponent<TargetingSystem>();
        }

        // Configure TargetingSystem properties
        var serializedTargeting = new SerializedObject(targeting);
        serializedTargeting.FindProperty("enableTargetPreview").boolValue = enableAttackPreview;
        serializedTargeting.FindProperty("showAttackRange").boolValue = showAttackRange;
        serializedTargeting.FindProperty("targetLayerMask").intValue = targetLayerMask;
        serializedTargeting.FindProperty("validTargetColor").colorValue = validTargetColor;
        serializedTargeting.FindProperty("invalidTargetColor").colorValue = invalidTargetColor;
        serializedTargeting.FindProperty("attackRangeColor").colorValue = attackRangeColor;
        serializedTargeting.FindProperty("targetHighlightDuration").floatValue = targetHighlightDuration;
        serializedTargeting.FindProperty("targetHighlightMaterial").objectReferenceValue = attackTargetHighlightMaterial;
        serializedTargeting.FindProperty("enableTargetingLogging").boolValue = enableAttackLogging;
        serializedTargeting.ApplyModifiedProperties();

        Debug.Log("TargetingSystem configured");
    }

    /// <summary>
    /// Enhances existing Unit components with combat interfaces
    /// </summary>
    private void EnhanceUnitsWithCombatCapability()
    {
        Debug.Log("Enhancing units with combat capability...");
        
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int unitsEnhanced = 0;

        Debug.Log($"Found {allUnits.Length} units to enhance");

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;

            // Add IAttacker capability
            if (unit.GetComponent<AttackCapability>() == null)
            {
                Debug.Log($"Adding AttackCapability to {unit.name}");
                AttackCapability attacker = unit.gameObject.AddComponent<AttackCapability>();
                
                // Configure AttackCapability
                var serializedAttacker = new SerializedObject(attacker);
                
                SetPropertySafe(serializedAttacker, "attackDamage", baseDamage);
                SetPropertySafe(serializedAttacker, "attacksPerTurn", attacksPerTurn);
                SetPropertySafe(serializedAttacker, "attackRange", attackRange);
                SetPropertySafe(serializedAttacker, "canAttackDiagonally", allowDiagonalAttacks);
                
                serializedAttacker.ApplyModifiedProperties();
            }

            // Add IAttackable capability
            if (unit.GetComponent<TargetCapability>() == null)
            {
                Debug.Log($"Adding TargetCapability to {unit.name}");
                TargetCapability target = unit.gameObject.AddComponent<TargetCapability>();
                
                // Configure TargetCapability
                var serializedTarget = new SerializedObject(target);
                SetPropertySafe(serializedTarget, "canBeTargeted", true);
                SetPropertySafe(serializedTarget, "currentHealth", 3); // Foundation for Task 2.1.3
                SetPropertySafe(serializedTarget, "maxHealth", 3);
                serializedTarget.ApplyModifiedProperties();
            }

            unitsEnhanced++;
        }

        Debug.Log($"Enhanced {unitsEnhanced} units with combat capability");
    }

    /// <summary>
    /// Integrates combat system with existing Sub-milestone 1.2 systems
    /// </summary>
    private void IntegrateWithExistingSystems()
    {
        Debug.Log("Starting integration with existing systems...");
        
        // Integrate with SelectionManager
        if (integrateWithSelection)
        {
            Debug.Log("Looking for SelectionManager...");
            SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
            if (selectionManager != null)
            {
                Debug.Log("Found SelectionManager, adding CombatInputHandler...");
                CombatInputHandler inputHandler = selectionManager.GetComponent<CombatInputHandler>();
                if (inputHandler == null)
                {
                    inputHandler = selectionManager.gameObject.AddComponent<CombatInputHandler>();
                }

                // Configure CombatInputHandler
                Debug.Log("Configuring CombatInputHandler...");
                var serializedInput = new SerializedObject(inputHandler);
                
                var enableProp = serializedInput.FindProperty("enableCombatInput");
                if (enableProp != null)
                    enableProp.boolValue = true;
                else
                    Debug.LogWarning("Could not find enableCombatInput property");
                
                var modeProp = serializedInput.FindProperty("combatInputMode");
                if (modeProp != null)
                    modeProp.enumValueIndex = 0; // ClickToAttack
                else
                    Debug.LogWarning("Could not find combatInputMode property");
                
                var loggingProp = serializedInput.FindProperty("enableInputLogging");
                if (loggingProp != null)
                    loggingProp.boolValue = enableAttackLogging;
                else
                    Debug.LogWarning("Could not find enableInputLogging property");
                
                serializedInput.ApplyModifiedProperties();

                Debug.Log("Integrated with SelectionManager for attack input");
            }
            else
            {
                Debug.LogWarning("SelectionManager not found - skipping integration");
            }
        }

        // Integrate with MovementManager
        if (integrateWithMovement)
        {
            Debug.Log("Looking for MovementManager...");
            MovementManager movementManager = FindFirstObjectByType<MovementManager>();
            if (movementManager != null)
            {
                Debug.Log("Found MovementManager - integration with combat system is automatic");
                // MovementManager doesn't need special configuration for combat
                // The combat system works alongside movement without explicit integration
            }
            else
            {
                Debug.LogWarning("MovementManager not found - movement system integration skipped");
            }
        }

        Debug.Log("System integration complete");
    }

    /// <summary>
    /// Creates attack materials if not provided
    /// </summary>
    private void CreateAttackMaterials()
    {
        // Ensure Materials folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Combat"))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Combat");
        }

        // Create target highlight material if not assigned
        if (attackTargetHighlightMaterial == null)
        {
            attackTargetHighlightMaterial = CreateTargetHighlightMaterial("AttackTargetHighlight", validTargetColor);
            Debug.Log("Created attack target highlight material");
        }
    }

    /// <summary>
    /// Creates a target highlight material with specified color
    /// </summary>
    private Material CreateTargetHighlightMaterial(string name, Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = name;
        
        // Configure for highlighting
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0); // Alpha blend
        material.SetFloat("_AlphaClip", 0);
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 0.5f);
        
        // Apply color with alpha
        color.a = 0.7f;
        material.color = color;
        
        // Enable emission for highlighting effect
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 0.8f);
        
        material.renderQueue = 3000; // Transparent queue
        
        string path = $"Assets/Materials/Combat/{name}.mat";
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    /// <summary>
    /// Validates the attack system setup
    /// </summary>
    private void ValidateSetup()
    {
        lastValidationResult = new AttackSystemValidationResult();
        
        // Test 1: CombatManager exists
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        lastValidationResult.combatManagerExists = combatManager != null;
        
        // Test 2: AttackValidator exists
        AttackValidator attackValidator = FindFirstObjectByType<AttackValidator>();
        lastValidationResult.attackValidatorExists = attackValidator != null;
        
        // Test 3: AttackExecutor exists
        AttackExecutor attackExecutor = FindFirstObjectByType<AttackExecutor>();
        lastValidationResult.attackExecutorExists = attackExecutor != null;
        
        // Test 4: TargetingSystem exists
        TargetingSystem targetingSystem = FindFirstObjectByType<TargetingSystem>();
        lastValidationResult.targetingSystemExists = targetingSystem != null;
        
        // Test 5: Units have combat capability
        ValidateUnitCombatCapability();
        
        // Test 6: System integration
        ValidateSystemIntegration();
        
        lastValidationResult.CalculateOverallResult();
        validationComplete = true;
        
        // Log results
        if (lastValidationResult.overallSuccess)
        {
            Debug.Log("✓ Attack System validation passed!");
        }
        else
        {
            Debug.LogWarning($"Attack System validation issues found. Passed: {lastValidationResult.GetPassedCount()}/6");
        }
    }

    /// <summary>
    /// Validates that units have combat capability
    /// </summary>
    private void ValidateUnitCombatCapability()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int unitsWithAttackCapability = 0;
        int unitsWithTargetCapability = 0;

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;

            if (unit.GetComponent<AttackCapability>() != null)
            {
                unitsWithAttackCapability++;
            }

            if (unit.GetComponent<TargetCapability>() != null)
            {
                unitsWithTargetCapability++;
            }
        }

        lastValidationResult.unitsCombatCapable = 
            unitsWithAttackCapability > 0 && 
            unitsWithTargetCapability > 0 &&
            unitsWithAttackCapability == allUnits.Length &&
            unitsWithTargetCapability == allUnits.Length;

        if (lastValidationResult.unitsCombatCapable)
        {
            Debug.Log($"✓ All {allUnits.Length} units have combat capability");
        }
        else
        {
            Debug.LogError($"Combat capability validation failed: {unitsWithAttackCapability}/{allUnits.Length} attackers, {unitsWithTargetCapability}/{allUnits.Length} targets");
        }
    }

    /// <summary>
    /// Validates system integration
    /// </summary>
    private void ValidateSystemIntegration()
    {
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        CombatInputHandler inputHandler = selectionManager != null ? selectionManager.GetComponent<CombatInputHandler>() : null;

        lastValidationResult.systemIntegrationValid = 
            selectionManager != null && 
            movementManager != null && 
            inputHandler != null;

        if (!lastValidationResult.systemIntegrationValid)
        {
            Debug.LogError("System integration validation failed - missing required components");
        }
    }

    /// <summary>
    /// Displays validation results in the editor
    /// </summary>
    private void DisplayValidationResults()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
        
        // Overall status
        Color originalColor = GUI.color;
        GUI.color = lastValidationResult.overallSuccess ? Color.green : Color.red;
        EditorGUILayout.LabelField($"Overall Status: {(lastValidationResult.overallSuccess ? "PASSED" : "NEEDS ATTENTION")}", EditorStyles.boldLabel);
        GUI.color = originalColor;
        
        EditorGUILayout.LabelField($"Tests Passed: {lastValidationResult.GetPassedCount()}/6", EditorStyles.helpBox);
        
        // Individual test results
        DrawValidationItem("Combat Manager", lastValidationResult.combatManagerExists);
        DrawValidationItem("Attack Validator", lastValidationResult.attackValidatorExists);
        DrawValidationItem("Attack Executor", lastValidationResult.attackExecutorExists);
        DrawValidationItem("Targeting System", lastValidationResult.targetingSystemExists);
        DrawValidationItem("Unit Combat Capability", lastValidationResult.unitsCombatCapable);
        DrawValidationItem("System Integration", lastValidationResult.systemIntegrationValid);
        
        if (lastValidationResult.overallSuccess)
        {
            EditorGUILayout.Space(5);
            GUI.color = Color.green;
            EditorGUILayout.LabelField("🎉 Task 2.1.1 COMPLETE! Attack System ready for tactical combat.", EditorStyles.helpBox);
            GUI.color = originalColor;
        }
        
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draws a validation result item
    /// </summary>
    private void DrawValidationItem(string testName, bool passed)
    {
        Color originalColor = GUI.color;
        GUI.color = passed ? Color.green : Color.red;
        string status = passed ? "✓" : "✗";
        EditorGUILayout.LabelField($"{status} {testName}");
        GUI.color = originalColor;
    }

    /// <summary>
    /// Resets/deletes the attack system setup
    /// </summary>
    private void ResetSetup()
    {
        if (EditorUtility.DisplayDialog("Reset Attack System", 
            "This will remove all combat components and materials. Continue?", 
            "Yes", "Cancel"))
        {
            try
            {
                // Remove CombatManager
                CombatManager combatManager = FindFirstObjectByType<CombatManager>();
                if (combatManager != null)
                {
                    DestroyImmediate(combatManager.gameObject);
                }
                
                // Remove combat capability from units
                AttackCapability[] attackers = FindObjectsByType<AttackCapability>(FindObjectsSortMode.None);
                foreach (AttackCapability attacker in attackers)
                {
                    if (attacker != null)
                    {
                        DestroyImmediate(attacker);
                    }
                }
                
                TargetCapability[] targets = FindObjectsByType<TargetCapability>(FindObjectsSortMode.None);
                foreach (TargetCapability target in targets)
                {
                    if (target != null)
                    {
                        DestroyImmediate(target);
                    }
                }
                
                // Remove combat input handlers
                CombatInputHandler[] inputHandlers = FindObjectsByType<CombatInputHandler>(FindObjectsSortMode.None);
                foreach (CombatInputHandler handler in inputHandlers)
                {
                    if (handler != null)
                    {
                        DestroyImmediate(handler);
                    }
                }
                
                // Delete attack materials
                DeleteMaterialIfExists(attackTargetHighlightMaterial);
                
                // Reset references
                attackTargetHighlightMaterial = null;
                validationComplete = false;
                
                Debug.Log("Attack System reset complete");
                EditorUtility.DisplayDialog("Reset Complete", "Attack System has been reset.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting attack system: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Deletes a material asset if it exists
    /// </summary>
    private void DeleteMaterialIfExists(Material material)
    {
        if (material != null)
        {
            string path = AssetDatabase.GetAssetPath(material);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
    
    /// <summary>
    /// Safely sets a property value on a SerializedObject
    /// </summary>
    private void SetPropertySafe(SerializedObject obj, string propertyName, object value)
    {
        var prop = obj.FindProperty(propertyName);
        if (prop != null)
        {
            switch (value)
            {
                case int intValue:
                    prop.intValue = intValue;
                    break;
                case float floatValue:
                    prop.floatValue = floatValue;
                    break;
                case bool boolValue:
                    prop.boolValue = boolValue;
                    break;
                case string stringValue:
                    prop.stringValue = stringValue;
                    break;
                default:
                    Debug.LogWarning($"Unsupported property type for {propertyName}");
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"Could not find property: {propertyName}");
        }
    }
}

/// <summary>
/// Structure to hold attack system validation results
/// </summary>
[System.Serializable]
public class AttackSystemValidationResult
{
    public bool combatManagerExists = false;
    public bool attackValidatorExists = false;
    public bool attackExecutorExists = false;
    public bool targetingSystemExists = false;
    public bool unitsCombatCapable = false;
    public bool systemIntegrationValid = false;
    public bool overallSuccess = false;
    
    public void CalculateOverallResult()
    {
        overallSuccess = combatManagerExists && 
                        attackValidatorExists && 
                        attackExecutorExists && 
                        targetingSystemExists && 
                        unitsCombatCapable && 
                        systemIntegrationValid;
    }
    
    public int GetPassedCount()
    {
        int count = 0;
        if (combatManagerExists) count++;
        if (attackValidatorExists) count++;
        if (attackExecutorExists) count++;
        if (targetingSystemExists) count++;
        if (unitsCombatCapable) count++;
        if (systemIntegrationValid) count++;
        return count;
    }
}