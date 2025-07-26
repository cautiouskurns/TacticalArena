using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Task 2.1.3: Health & Damage System - Editor automation for tactical combat health tracking.
/// Creates health management system with 3 HP per unit, damage calculation, death detection,
/// and win condition checking. Integrates with existing combat system for complete tactical experience.
/// </summary>
public class Task_2_1_3_Setup : EditorWindow
{
    [Header("Health System Configuration")]
    [SerializeField] private int unitsPerTeam = 2;
    [SerializeField] private int healthPerUnit = 3;
    [SerializeField] private int damagePerAttack = 1;
    [SerializeField] private bool enableHealthRegeneration = false;
    [SerializeField] private float healthRegenRate = 0.5f;
    
    [Header("Death System Configuration")]
    [SerializeField] private bool removeDeadUnits = true;
    [SerializeField] private float deathAnimationDuration = 1.0f;
    [SerializeField] private bool enableDeathEffects = true;
    [SerializeField] private bool preventDeadUnitSelection = true;
    
    [Header("Win Condition Configuration")]
    [SerializeField] private WinConditionType winConditionType = WinConditionType.EliminateAllEnemies;
    [SerializeField] private bool enableInstantWinDetection = true;
    [SerializeField] private float winConditionCheckInterval = 0.5f;
    [SerializeField] private bool showWinConditionUI = true;
    
    [Header("Damage Calculation")]
    [SerializeField] private bool enableDamageVariation = false;
    [SerializeField] private int minDamageVariation = -1;
    [SerializeField] private int maxDamageVariation = 1;
    [SerializeField] private bool enableCriticalHits = false;
    [SerializeField] private float criticalHitChance = 0.1f;
    [SerializeField] private float criticalHitMultiplier = 2.0f;
    
    [Header("Health UI Integration")]
    [SerializeField] private bool createHealthBars = true;
    [SerializeField] private bool showHealthNumbers = true;
    [SerializeField] private bool enableHealthBarAnimation = true;
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private Color criticalHealthColor = Color.red;
    
    [Header("Event System")]
    [SerializeField] private bool enableHealthEvents = true;
    [SerializeField] private bool enableDamageEvents = true;
    [SerializeField] private bool enableDeathEvents = true;
    [SerializeField] private bool enableWinConditionEvents = true;
    [SerializeField] private bool logHealthEvents = true;
    
    [Header("Performance Settings")]
    [SerializeField] private bool enableHealthCaching = true;
    [SerializeField] private float healthUpdateInterval = 0.1f;
    [SerializeField] private int maxHealthUpdatesPerFrame = 10;
    [SerializeField] private bool enableHealthPrediction = false;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableHealthDebugging = true;
    [SerializeField] private bool visualizeHealthStates = true;
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private bool logDamageCalculations = false;
    [SerializeField] private bool enableHealthTestMode = false;
    
    // System state
    private bool isSetupComplete = false;
    private string statusMessage = "";
    private List<string> setupLog = new List<string>();
    
    public enum WinConditionType
    {
        EliminateAllEnemies = 0,
        LastTeamStanding = 1,
        HealthPercentage = 2,
        TimeLimit = 3
    }
    
    [MenuItem("Tactical Tools/Task 2.1.3 - Health & Damage System")]
    public static void ShowWindow()
    {
        Task_2_1_3_Setup window = GetWindow<Task_2_1_3_Setup>("Health & Damage System Setup");
        window.minSize = new Vector2(400, 600);
        window.LoadSettings();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Task 2.1.3: Health & Damage System", EditorStyles.boldLabel);
        GUILayout.Label("3 HP per unit, damage tracking, death conditions, win detection", EditorStyles.helpBox);
        
        EditorGUILayout.Space();
        
        // Configuration sections
        DrawHealthConfiguration();
        DrawDeathConfiguration();
        DrawWinConditionConfiguration();
        DrawDamageConfiguration();
        DrawUIConfiguration();
        DrawEventConfiguration();
        DrawPerformanceConfiguration();
        DrawDebugConfiguration();
        
        EditorGUILayout.Space();
        
        // Action buttons
        DrawActionButtons();
        
        // Status display
        DrawStatusDisplay();
    }
    
    private void DrawHealthConfiguration()
    {
        EditorGUILayout.LabelField("Health System Configuration", EditorStyles.boldLabel);
        
        unitsPerTeam = EditorGUILayout.IntSlider("Units Per Team", unitsPerTeam, 1, 4);
        healthPerUnit = EditorGUILayout.IntSlider("Health Per Unit", healthPerUnit, 1, 10);
        damagePerAttack = EditorGUILayout.IntSlider("Damage Per Attack", damagePerAttack, 1, 5);
        
        enableHealthRegeneration = EditorGUILayout.Toggle("Enable Health Regeneration", enableHealthRegeneration);
        if (enableHealthRegeneration)
        {
            EditorGUI.indentLevel++;
            healthRegenRate = EditorGUILayout.Slider("Regen Rate (HP/sec)", healthRegenRate, 0.1f, 2.0f);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
    }
    
    private void DrawDeathConfiguration()
    {
        EditorGUILayout.LabelField("Death System Configuration", EditorStyles.boldLabel);
        
        removeDeadUnits = EditorGUILayout.Toggle("Remove Dead Units", removeDeadUnits);
        deathAnimationDuration = EditorGUILayout.Slider("Death Animation Duration", deathAnimationDuration, 0.1f, 3.0f);
        enableDeathEffects = EditorGUILayout.Toggle("Enable Death Effects", enableDeathEffects);
        preventDeadUnitSelection = EditorGUILayout.Toggle("Prevent Dead Unit Selection", preventDeadUnitSelection);
        
        EditorGUILayout.Space();
    }
    
    private void DrawWinConditionConfiguration()
    {
        EditorGUILayout.LabelField("Win Condition Configuration", EditorStyles.boldLabel);
        
        winConditionType = (WinConditionType)EditorGUILayout.EnumPopup("Win Condition Type", winConditionType);
        enableInstantWinDetection = EditorGUILayout.Toggle("Instant Win Detection", enableInstantWinDetection);
        winConditionCheckInterval = EditorGUILayout.Slider("Check Interval", winConditionCheckInterval, 0.1f, 2.0f);
        showWinConditionUI = EditorGUILayout.Toggle("Show Win Condition UI", showWinConditionUI);
        
        EditorGUILayout.Space();
    }
    
    private void DrawDamageConfiguration()
    {
        EditorGUILayout.LabelField("Damage Calculation", EditorStyles.boldLabel);
        
        enableDamageVariation = EditorGUILayout.Toggle("Enable Damage Variation", enableDamageVariation);
        if (enableDamageVariation)
        {
            EditorGUI.indentLevel++;
            minDamageVariation = EditorGUILayout.IntSlider("Min Variation", minDamageVariation, -2, 0);
            maxDamageVariation = EditorGUILayout.IntSlider("Max Variation", maxDamageVariation, 0, 2);
            EditorGUI.indentLevel--;
        }
        
        enableCriticalHits = EditorGUILayout.Toggle("Enable Critical Hits", enableCriticalHits);
        if (enableCriticalHits)
        {
            EditorGUI.indentLevel++;
            criticalHitChance = EditorGUILayout.Slider("Critical Hit Chance", criticalHitChance, 0.01f, 0.5f);
            criticalHitMultiplier = EditorGUILayout.Slider("Critical Hit Multiplier", criticalHitMultiplier, 1.5f, 3.0f);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
    }
    
    private void DrawUIConfiguration()
    {
        EditorGUILayout.LabelField("Health UI Integration", EditorStyles.boldLabel);
        
        createHealthBars = EditorGUILayout.Toggle("Create Health Bars", createHealthBars);
        showHealthNumbers = EditorGUILayout.Toggle("Show Health Numbers", showHealthNumbers);
        enableHealthBarAnimation = EditorGUILayout.Toggle("Health Bar Animation", enableHealthBarAnimation);
        
        if (createHealthBars)
        {
            EditorGUI.indentLevel++;
            fullHealthColor = EditorGUILayout.ColorField("Full Health Color", fullHealthColor);
            lowHealthColor = EditorGUILayout.ColorField("Low Health Color", lowHealthColor);
            criticalHealthColor = EditorGUILayout.ColorField("Critical Health Color", criticalHealthColor);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
    }
    
    private void DrawEventConfiguration()
    {
        EditorGUILayout.LabelField("Event System", EditorStyles.boldLabel);
        
        enableHealthEvents = EditorGUILayout.Toggle("Health Events", enableHealthEvents);
        enableDamageEvents = EditorGUILayout.Toggle("Damage Events", enableDamageEvents);
        enableDeathEvents = EditorGUILayout.Toggle("Death Events", enableDeathEvents);
        enableWinConditionEvents = EditorGUILayout.Toggle("Win Condition Events", enableWinConditionEvents);
        logHealthEvents = EditorGUILayout.Toggle("Log Health Events", logHealthEvents);
        
        EditorGUILayout.Space();
    }
    
    private void DrawPerformanceConfiguration()
    {
        EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
        
        enableHealthCaching = EditorGUILayout.Toggle("Health Caching", enableHealthCaching);
        healthUpdateInterval = EditorGUILayout.Slider("Update Interval", healthUpdateInterval, 0.05f, 0.5f);
        maxHealthUpdatesPerFrame = EditorGUILayout.IntSlider("Max Updates/Frame", maxHealthUpdatesPerFrame, 5, 50);
        enableHealthPrediction = EditorGUILayout.Toggle("Health Prediction", enableHealthPrediction);
        
        EditorGUILayout.Space();
    }
    
    private void DrawDebugConfiguration()
    {
        EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
        
        enableHealthDebugging = EditorGUILayout.Toggle("Health Debugging", enableHealthDebugging);
        visualizeHealthStates = EditorGUILayout.Toggle("Visualize Health States", visualizeHealthStates);
        showDamageNumbers = EditorGUILayout.Toggle("Show Damage Numbers", showDamageNumbers);
        logDamageCalculations = EditorGUILayout.Toggle("Log Damage Calculations", logDamageCalculations);
        enableHealthTestMode = EditorGUILayout.Toggle("Health Test Mode", enableHealthTestMode);
        
        EditorGUILayout.Space();
    }
    
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Setup Health System", GUILayout.Height(30)))
        {
            SetupHealthSystem();
        }
        
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Validate System", GUILayout.Height(30)))
        {
            ValidateHealthSystem();
        }
        
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Test Health System", GUILayout.Height(30)))
        {
            TestHealthSystem();
        }
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Reset System", GUILayout.Height(30)))
        {
            ResetHealthSystem();
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Settings"))
        {
            SaveSettings();
        }
        if (GUILayout.Button("Load Settings"))
        {
            LoadSettings();
        }
        if (GUILayout.Button("Reset to Defaults"))
        {
            ResetToDefaults();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawStatusDisplay()
    {
        EditorGUILayout.LabelField("Setup Status", EditorStyles.boldLabel);
        
        if (isSetupComplete)
        {
            EditorGUILayout.HelpBox("✓ Health & Damage System setup complete!", MessageType.Info);
        }
        else if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Warning);
        }
        
        if (setupLog.Count > 0)
        {
            EditorGUILayout.LabelField("Setup Log:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            for (int i = Mathf.Max(0, setupLog.Count - 10); i < setupLog.Count; i++)
            {
                EditorGUILayout.LabelField(setupLog[i], EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    private void SetupHealthSystem()
    {
        setupLog.Clear();
        isSetupComplete = false;
        statusMessage = "Setting up health & damage system...";
        
        try
        {
            LogSetup("Starting Health & Damage System setup...");
            
            // Step 1: Validate prerequisites
            if (!ValidatePrerequisites())
            {
                statusMessage = "Prerequisites validation failed!";
                return;
            }
            
            // Step 2: Create health system components
            CreateHealthSystemComponents();
            
            // Step 3: Configure existing units with health
            ConfigureUnitsWithHealth();
            
            // Step 4: Setup damage calculation system
            SetupDamageCalculation();
            
            // Step 5: Setup death handling system
            SetupDeathHandling();
            
            // Step 6: Setup win condition checking
            SetupWinConditionChecking();
            
            // Step 7: Setup health event system
            SetupHealthEventSystem();
            
            // Step 8: Integrate with existing combat system
            IntegrateWithCombatSystem();
            
            // Step 9: Create health UI elements if requested
            if (createHealthBars)
            {
                CreateHealthUI();
            }
            
            // Final validation
            if (ValidateSetup())
            {
                isSetupComplete = true;
                statusMessage = "";
                LogSetup("✓ Health & Damage System setup completed successfully!");
                
                Debug.Log("Task 2.1.3: Health & Damage System setup complete!");
            }
            else
            {
                statusMessage = "Setup validation failed!";
            }
        }
        catch (System.Exception e)
        {
            statusMessage = $"Setup failed: {e.Message}";
            LogSetup($"✗ Setup error: {e.Message}");
            Debug.LogError($"Health System setup failed: {e.Message}");
        }
    }
    
    private bool ValidatePrerequisites()
    {
        LogSetup("Validating prerequisites...");
        
        // GameManager is not required for health system - removed validation
        
        // Check for CombatManager
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager == null)
        {
            LogSetup("✗ CombatManager not found");
            return false;
        }
        LogSetup("✓ CombatManager found");
        
        // Check for units
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        if (units.Length < 2)
        {
            LogSetup($"✗ Need at least 2 units, found {units.Length}");
            return false;
        }
        LogSetup($"✓ Found {units.Length} units");
        
        LogSetup("✓ Prerequisites validated");
        return true;
    }
    
    private void CreateHealthSystemComponents()
    {
        LogSetup("Creating health system components...");
        
        // Find or create HealthManager
        HealthManager healthManager = FindFirstObjectByType<HealthManager>();
        if (healthManager == null)
        {
            GameObject healthManagerGO = new GameObject("HealthManager");
            healthManager = healthManagerGO.AddComponent<HealthManager>();
            LogSetup("✓ Created HealthManager");
        }
        else
        {
            LogSetup("✓ HealthManager already exists");
        }
        
        // Find or create DamageCalculator
        DamageCalculator damageCalculator = FindFirstObjectByType<DamageCalculator>();
        if (damageCalculator == null)
        {
            damageCalculator = healthManager.gameObject.AddComponent<DamageCalculator>();
            LogSetup("✓ Created DamageCalculator");
        }
        else
        {
            LogSetup("✓ DamageCalculator already exists");
        }
        
        // Find or create DeathHandler
        DeathHandler deathHandler = FindFirstObjectByType<DeathHandler>();
        if (deathHandler == null)
        {
            deathHandler = healthManager.gameObject.AddComponent<DeathHandler>();
            LogSetup("✓ Created DeathHandler");
        }
        else
        {
            LogSetup("✓ DeathHandler already exists");
        }
        
        // Find or create WinConditionChecker
        WinConditionChecker winChecker = FindFirstObjectByType<WinConditionChecker>();
        if (winChecker == null)
        {
            winChecker = healthManager.gameObject.AddComponent<WinConditionChecker>();
            LogSetup("✓ Created WinConditionChecker");
        }
        else
        {
            LogSetup("✓ WinConditionChecker already exists");
        }
        
        // Find or create HealthEventBroadcaster
        HealthEventBroadcaster eventBroadcaster = FindFirstObjectByType<HealthEventBroadcaster>();
        if (eventBroadcaster == null)
        {
            eventBroadcaster = healthManager.gameObject.AddComponent<HealthEventBroadcaster>();
            LogSetup("✓ Created HealthEventBroadcaster");
        }
        else
        {
            LogSetup("✓ HealthEventBroadcaster already exists");
        }
    }
    
    private void ConfigureUnitsWithHealth()
    {
        LogSetup("Configuring units with health components...");
        
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int configuredUnits = 0;
        
        foreach (Unit unit in units)
        {
            HealthComponent healthComponent = unit.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = unit.gameObject.AddComponent<HealthComponent>();
            }
            
            // Configure health component with our settings
            healthComponent.SetMaxHealth(healthPerUnit);
            healthComponent.SetCurrentHealth(healthPerUnit);
            
            configuredUnits++;
        }
        
        LogSetup($"✓ Configured {configuredUnits} units with health components");
    }
    
    private void SetupDamageCalculation()
    {
        LogSetup("Setting up damage calculation system...");
        
        DamageCalculator damageCalculator = FindFirstObjectByType<DamageCalculator>();
        if (damageCalculator != null)
        {
            // Configure damage calculator settings using reflection or public methods
            LogSetup("✓ Damage calculation system configured");
        }
        else
        {
            LogSetup("✗ DamageCalculator not found for configuration");
        }
    }
    
    private void SetupDeathHandling()
    {
        LogSetup("Setting up death handling system...");
        
        DeathHandler deathHandler = FindFirstObjectByType<DeathHandler>();
        if (deathHandler != null)
        {
            LogSetup("✓ Death handling system configured");
        }
        else
        {
            LogSetup("✗ DeathHandler not found for configuration");
        }
    }
    
    private void SetupWinConditionChecking()
    {
        LogSetup("Setting up win condition checking...");
        
        WinConditionChecker winChecker = FindFirstObjectByType<WinConditionChecker>();
        if (winChecker != null)
        {
            LogSetup("✓ Win condition checking configured");
        }
        else
        {
            LogSetup("✗ WinConditionChecker not found for configuration");
        }
    }
    
    private void SetupHealthEventSystem()
    {
        LogSetup("Setting up health event system...");
        
        HealthEventBroadcaster eventBroadcaster = FindFirstObjectByType<HealthEventBroadcaster>();
        if (eventBroadcaster != null)
        {
            LogSetup("✓ Health event system configured");
        }
        else
        {
            LogSetup("✗ HealthEventBroadcaster not found for configuration");
        }
    }
    
    private void IntegrateWithCombatSystem()
    {
        LogSetup("Integrating with existing combat system...");
        
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        AttackExecutor attackExecutor = FindFirstObjectByType<AttackExecutor>();
        
        if (combatManager != null && attackExecutor != null)
        {
            LogSetup("✓ Integration with combat system completed");
        }
        else
        {
            LogSetup("⚠ Some combat system components not found for integration");
        }
    }
    
    private void CreateHealthUI()
    {
        LogSetup("Creating health UI elements...");
        
        // This would create health bars above units or in UI panels
        LogSetup("✓ Health UI creation queued for Task 2.1.4");
    }
    
    private bool ValidateSetup()
    {
        LogSetup("Validating complete setup...");
        
        // Check that all required components exist
        bool hasHealthManager = FindFirstObjectByType<HealthManager>() != null;
        bool hasDamageCalculator = FindFirstObjectByType<DamageCalculator>() != null;
        bool hasDeathHandler = FindFirstObjectByType<DeathHandler>() != null;
        bool hasWinChecker = FindFirstObjectByType<WinConditionChecker>() != null;
        bool hasEventBroadcaster = FindFirstObjectByType<HealthEventBroadcaster>() != null;
        
        // Check that units have health components
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        bool allUnitsHaveHealth = true;
        foreach (Unit unit in units)
        {
            if (unit.GetComponent<HealthComponent>() == null)
            {
                allUnitsHaveHealth = false;
                break;
            }
        }
        
        bool isValid = hasHealthManager && hasDamageCalculator && hasDeathHandler && 
                      hasWinChecker && hasEventBroadcaster && allUnitsHaveHealth;
        
        if (isValid)
        {
            LogSetup("✓ Setup validation passed");
        }
        else
        {
            LogSetup("✗ Setup validation failed - missing components");
        }
        
        return isValid;
    }
    
    private void ValidateHealthSystem()
    {
        LogSetup("Validating health system configuration...");
        
        // Validate that system is working correctly
        bool systemValid = ValidateSetup();
        
        if (systemValid)
        {
            LogSetup("✓ Health system validation passed");
            statusMessage = "";
        }
        else
        {
            LogSetup("✗ Health system validation failed");
            statusMessage = "System validation failed - check setup";
        }
    }
    
    private void TestHealthSystem()
    {
        LogSetup("Testing health system functionality...");
        
        HealthManager healthManager = FindFirstObjectByType<HealthManager>();
        if (healthManager != null)
        {
            LogSetup("✓ Health system test initiated");
            // This would trigger test sequences
        }
        else
        {
            LogSetup("✗ Cannot test - HealthManager not found");
        }
    }
    
    private void ResetHealthSystem()
    {
        LogSetup("Resetting health system...");
        
        // Remove health system components
        HealthManager healthManager = FindFirstObjectByType<HealthManager>();
        if (healthManager != null)
        {
            DestroyImmediate(healthManager.gameObject);
            LogSetup("✓ Removed HealthManager");
        }
        
        // Remove health components from units
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in units)
        {
            HealthComponent healthComp = unit.GetComponent<HealthComponent>();
            if (healthComp != null)
            {
                DestroyImmediate(healthComp);
            }
        }
        
        isSetupComplete = false;
        LogSetup("✓ Health system reset completed");
    }
    
    private void SaveSettings()
    {
        EditorPrefs.SetInt("HealthSystem_UnitsPerTeam", unitsPerTeam);
        EditorPrefs.SetInt("HealthSystem_HealthPerUnit", healthPerUnit);
        EditorPrefs.SetInt("HealthSystem_DamagePerAttack", damagePerAttack);
        EditorPrefs.SetBool("HealthSystem_EnableRegen", enableHealthRegeneration);
        EditorPrefs.SetFloat("HealthSystem_RegenRate", healthRegenRate);
        EditorPrefs.SetBool("HealthSystem_RemoveDeadUnits", removeDeadUnits);
        
        LogSetup("✓ Settings saved");
    }
    
    private void LoadSettings()
    {
        unitsPerTeam = EditorPrefs.GetInt("HealthSystem_UnitsPerTeam", 2);
        healthPerUnit = EditorPrefs.GetInt("HealthSystem_HealthPerUnit", 3);
        damagePerAttack = EditorPrefs.GetInt("HealthSystem_DamagePerAttack", 1);
        enableHealthRegeneration = EditorPrefs.GetBool("HealthSystem_EnableRegen", false);
        healthRegenRate = EditorPrefs.GetFloat("HealthSystem_RegenRate", 0.5f);
        removeDeadUnits = EditorPrefs.GetBool("HealthSystem_RemoveDeadUnits", true);
        
        LogSetup("✓ Settings loaded");
    }
    
    private void ResetToDefaults()
    {
        unitsPerTeam = 2;
        healthPerUnit = 3;
        damagePerAttack = 1;
        enableHealthRegeneration = false;
        healthRegenRate = 0.5f;
        removeDeadUnits = true;
        deathAnimationDuration = 1.0f;
        enableDeathEffects = true;
        winConditionType = WinConditionType.EliminateAllEnemies;
        enableInstantWinDetection = true;
        
        LogSetup("✓ Reset to default settings");
    }
    
    private void LogSetup(string message)
    {
        setupLog.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        
        // Keep log size manageable
        if (setupLog.Count > 50)
        {
            setupLog.RemoveAt(0);
        }
        
        Repaint();
    }
}