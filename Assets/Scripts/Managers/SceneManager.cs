using UnityEngine;

/// <summary>
/// Manages scene organization and initialization for the tactical arena.
/// Provides foundation for scene structure and system coordination.
/// </summary>
public class SceneManager : MonoBehaviour
{
    [Header("Scene Organization")]
    [SerializeField] private bool autoOrganizeOnStart = true;
    [SerializeField] private bool validateSceneStructure = true;
    
    [Header("Required Scene Elements")]
    [SerializeField] private string cameraObjectName = "Main Camera";
    [SerializeField] private string lightingGroupName = "Lighting";
    [SerializeField] private string environmentGroupName = "Environment";
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private CameraController cameraController;
    private Light mainLight;
    
    void Awake()
    {
        if (enableDebugLogs)
            Debug.Log("SceneManager: Initializing scene organization...");
        
        if (autoOrganizeOnStart)
        {
            OrganizeSceneHierarchy();
        }
        
        FindSceneComponents();
    }
    
    void Start()
    {
        if (validateSceneStructure)
        {
            ValidateSceneSetup();
        }
        
        InitializeSceneSystems();
    }
    
    /// <summary>
    /// Organizes the scene hierarchy for clean structure
    /// </summary>
    public void OrganizeSceneHierarchy()
    {
        CreateOrganizationGroups();
        OrganizeLightingObjects();
        PrepareEnvironmentArea();
        
        if (enableDebugLogs)
            Debug.Log("SceneManager: Scene hierarchy organized");
    }
    
    /// <summary>
    /// Creates the main organizational groups for the scene
    /// </summary>
    private void CreateOrganizationGroups()
    {
        // Create Lighting group
        GameObject lightingGroup = GameObject.Find(lightingGroupName);
        if (lightingGroup == null)
        {
            lightingGroup = new GameObject(lightingGroupName);
            if (enableDebugLogs)
                Debug.Log($"Created {lightingGroupName} group");
        }
        
        // Create Environment group  
        GameObject environmentGroup = GameObject.Find(environmentGroupName);
        if (environmentGroup == null)
        {
            environmentGroup = new GameObject(environmentGroupName);
            if (enableDebugLogs)
                Debug.Log($"Created {environmentGroupName} group");
        }
    }
    
    /// <summary>
    /// Organizes lighting objects under the Lighting group
    /// </summary>
    private void OrganizeLightingObjects()
    {
        GameObject lightingGroup = GameObject.Find(lightingGroupName);
        if (lightingGroup == null) return;
        
        // Find all lights and organize them
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in allLights)
        {
            if (light.transform.parent == null)
            {
                light.transform.SetParent(lightingGroup.transform);
                if (enableDebugLogs)
                    Debug.Log($"Organized {light.name} under {lightingGroupName}");
            }
        }
    }
    
    /// <summary>
    /// Prepares the environment area for future grid and objects
    /// </summary>
    private void PrepareEnvironmentArea()
    {
        GameObject environmentGroup = GameObject.Find(environmentGroupName);
        if (environmentGroup == null) return;
        
        // Position environment group at origin for grid system
        environmentGroup.transform.position = Vector3.zero;
        environmentGroup.transform.rotation = Quaternion.identity;
        
        if (enableDebugLogs)
            Debug.Log("Environment area prepared for future grid system");
    }
    
    /// <summary>
    /// Finds and caches references to key scene components
    /// </summary>
    private void FindSceneComponents()
    {
        // Find camera controller
        GameObject cameraObject = GameObject.Find(cameraObjectName);
        if (cameraObject != null)
        {
            cameraController = cameraObject.GetComponent<CameraController>();
            if (cameraController == null && enableDebugLogs)
            {
                Debug.Log("Main Camera found but no CameraController component attached");
            }
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"No GameObject named '{cameraObjectName}' found in scene");
        }
        
        // Find main light
        mainLight = FindFirstObjectByType<Light>();
        if (mainLight == null && enableDebugLogs)
        {
            Debug.LogWarning("No Light component found in scene");
        }
    }
    
    /// <summary>
    /// Validates that the scene is properly set up for tactical gameplay
    /// </summary>
    public bool ValidateSceneSetup()
    {
        bool isValid = true;
        string validationReport = "Scene Validation:\n";
        
        // Check camera setup
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            if (mainCamera.orthographic)
            {
                validationReport += "✓ Main Camera configured for orthographic projection\n";
            }
            else
            {
                validationReport += "✗ Main Camera should use orthographic projection\n";
                isValid = false;
            }
            
            // Check if camera controller is attached
            if (cameraController != null)
            {
                validationReport += "✓ CameraController component found\n";
                
                if (cameraController.ValidateCameraSetup())
                {
                    validationReport += "✓ Camera positioning validated\n";
                }
                else
                {
                    validationReport += "✗ Camera positioning needs adjustment\n";
                    isValid = false;
                }
            }
            else
            {
                validationReport += "⚠ CameraController component not attached (optional)\n";
            }
        }
        else
        {
            validationReport += "✗ No Main Camera found in scene\n";
            isValid = false;
        }
        
        // Check lighting setup
        if (mainLight != null && mainLight.type == LightType.Directional)
        {
            validationReport += "✓ Directional lighting configured\n";
        }
        else
        {
            validationReport += "✗ No Directional Light found\n";
            isValid = false;
        }
        
        // Check scene organization
        if (GameObject.Find(environmentGroupName) != null)
        {
            validationReport += "✓ Environment group created\n";
        }
        else
        {
            validationReport += "✗ Environment group missing\n";
            isValid = false;
        }
        
        if (GameObject.Find(lightingGroupName) != null)
        {
            validationReport += "✓ Lighting group created\n";
        }
        else
        {
            validationReport += "✗ Lighting group missing\n";
            isValid = false;
        }
        
        if (enableDebugLogs)
        {
            if (isValid)
            {
                Debug.Log(validationReport + "\n✓ Scene validation passed!");
            }
            else
            {
                Debug.LogWarning(validationReport + "\n✗ Scene validation failed!");
            }
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Initializes scene systems and components
    /// </summary>
    private void InitializeSceneSystems()
    {
        // Initialize camera controller if present
        if (cameraController != null)
        {
            cameraController.EnsureIsometricConfiguration();
            if (enableDebugLogs)
                Debug.Log("CameraController initialized");
        }
        
        // Future system initialization can be added here
        // Example: GridManager, UnitManager, etc.
        
        if (enableDebugLogs)
            Debug.Log("SceneManager: Scene systems initialized");
    }
    
    /// <summary>
    /// Gets information about current scene state
    /// </summary>
    public SceneInfo GetSceneInfo()
    {
        return new SceneInfo
        {
            hasMainCamera = Camera.main != null,
            hasDirectionalLight = mainLight != null && mainLight.type == LightType.Directional,
            hasCameraController = cameraController != null,
            hasEnvironmentGroup = GameObject.Find(environmentGroupName) != null,
            hasLightingGroup = GameObject.Find(lightingGroupName) != null,
            sceneIsValid = ValidateSceneSetup()
        };
    }
    
    /// <summary>
    /// Gets reference to the environment group for future grid placement
    /// </summary>
    public Transform GetEnvironmentGroup()
    {
        GameObject environmentGroup = GameObject.Find(environmentGroupName);
        return environmentGroup != null ? environmentGroup.transform : null;
    }
    
    /// <summary>
    /// Gets reference to the camera controller if available
    /// </summary>
    public CameraController GetCameraController()
    {
        return cameraController;
    }
}

/// <summary>
/// Information about the current scene state
/// </summary>
[System.Serializable]
public struct SceneInfo
{
    public bool hasMainCamera;
    public bool hasDirectionalLight;
    public bool hasCameraController;
    public bool hasEnvironmentGroup;
    public bool hasLightingGroup;
    public bool sceneIsValid;
    
    public override string ToString()
    {
        return $"Scene Info - Camera: {hasMainCamera}, Light: {hasDirectionalLight}, " +
               $"Controller: {hasCameraController}, Valid: {sceneIsValid}";
    }
}