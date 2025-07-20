using UnityEngine;

/// <summary>
/// Manages the tactical camera system for isometric battlefield view.
/// Provides foundation for future camera operations and maintains optimal positioning.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Camera Configuration")]
    [SerializeField] private bool maintainIsometricView = true;
    [SerializeField] private float targetOrthographicSize = 6f;
    [SerializeField] private Vector3 targetRotation = new Vector3(45f, 45f, 0f);
    
    [Header("Grid Reference")]
    [SerializeField] private float gridSize = 4f;
    [SerializeField] private float tileSize = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showGridGizmos = true;
    [SerializeField] private Color gizmoColor = Color.yellow;
    
    private Camera targetCamera;
    private Vector3 gridCenter;
    private Vector3 initialPosition;
    
    void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
        {
            Debug.LogError("CameraController requires a Camera component!");
            return;
        }
        
        CalculateGridCenter();
        ValidateCameraSetup();
    }
    
    void Start()
    {
        if (maintainIsometricView)
        {
            EnsureIsometricConfiguration();
        }
        
        initialPosition = transform.position;
    }
    
    /// <summary>
    /// Calculates the center point of the future 4x4 grid battlefield
    /// </summary>
    private void CalculateGridCenter()
    {
        float totalGridWorldSize = gridSize * tileSize;
        gridCenter = new Vector3(totalGridWorldSize / 2f - 0.5f, 0, totalGridWorldSize / 2f - 0.5f);
    }
    
    /// <summary>
    /// Ensures camera is properly configured for tactical isometric view
    /// </summary>
    public void EnsureIsometricConfiguration()
    {
        if (targetCamera == null) return;
        
        // Ensure orthographic projection
        if (!targetCamera.orthographic)
        {
            targetCamera.orthographic = true;
            Debug.Log("Camera switched to orthographic projection for tactical view");
        }
        
        // Set appropriate orthographic size
        if (Mathf.Abs(targetCamera.orthographicSize - targetOrthographicSize) > 0.1f)
        {
            targetCamera.orthographicSize = targetOrthographicSize;
            Debug.Log($"Camera orthographic size set to {targetOrthographicSize}");
        }
        
        // Ensure isometric rotation
        Vector3 currentRotation = transform.eulerAngles;
        if (Vector3.Distance(currentRotation, targetRotation) > 5f)
        {
            transform.rotation = Quaternion.Euler(targetRotation);
            Debug.Log($"Camera rotation corrected to isometric angle: {targetRotation}");
        }
    }
    
    /// <summary>
    /// Validates current camera setup for tactical gameplay
    /// </summary>
    public bool ValidateCameraSetup()
    {
        if (targetCamera == null) return false;
        
        bool isValid = true;
        
        // Check orthographic projection
        if (!targetCamera.orthographic)
        {
            Debug.LogWarning("Camera should use orthographic projection for tactical gameplay");
            isValid = false;
        }
        
        // Check orthographic size
        if (targetCamera.orthographicSize < 4f || targetCamera.orthographicSize > 10f)
        {
            Debug.LogWarning($"Camera orthographic size ({targetCamera.orthographicSize}) may not provide optimal battlefield view");
            isValid = false;
        }
        
        // Check camera position can see grid center
        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(gridCenter);
        if (viewportPoint.x < 0.2f || viewportPoint.x > 0.8f || viewportPoint.y < 0.2f || viewportPoint.y > 0.8f)
        {
            Debug.LogWarning("Camera position may not optimally view the battlefield grid");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Returns the camera's view of the battlefield area
    /// </summary>
    public Bounds GetViewBounds()
    {
        if (targetCamera == null || !targetCamera.orthographic) 
            return new Bounds();
        
        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;
        
        Vector3 center = transform.position;
        center.y = 0; // Project to ground level
        
        return new Bounds(center, new Vector3(width, 0, height));
    }
    
    /// <summary>
    /// Checks if a world position is visible in the camera view
    /// </summary>
    public bool IsPositionVisible(Vector3 worldPosition)
    {
        if (targetCamera == null) return false;
        
        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(worldPosition);
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
               viewportPoint.y >= 0 && viewportPoint.y <= 1 && 
               viewportPoint.z > 0;
    }
    
    /// <summary>
    /// Performs raycast from camera through screen point (for future mouse interaction)
    /// </summary>
    public bool ScreenPointRaycast(Vector2 screenPoint, out RaycastHit hit, float maxDistance = 100f)
    {
        hit = new RaycastHit();
        
        if (targetCamera == null) return false;
        
        Ray ray = targetCamera.ScreenPointToRay(screenPoint);
        return Physics.Raycast(ray, out hit, maxDistance);
    }
    
    /// <summary>
    /// Gets camera information for debugging and editor tools
    /// </summary>
    public CameraInfo GetCameraInfo()
    {
        return new CameraInfo
        {
            position = transform.position,
            rotation = transform.eulerAngles,
            orthographicSize = targetCamera != null ? targetCamera.orthographicSize : 0f,
            isOrthographic = targetCamera != null ? targetCamera.orthographic : false,
            gridCenter = gridCenter,
            viewBounds = GetViewBounds()
        };
    }
    
    void OnValidate()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
        
        CalculateGridCenter();
    }
    
    void OnDrawGizmos()
    {
        if (!showGridGizmos) return;
        
        // Draw grid area reference
        Gizmos.color = gizmoColor;
        
        // Draw grid center
        Gizmos.DrawWireSphere(gridCenter, 0.2f);
        
        // Draw grid boundary
        float totalGridWorldSize = gridSize * tileSize;
        Vector3 gridBottomLeft = new Vector3(0, 0, 0);
        Vector3 gridTopRight = new Vector3(totalGridWorldSize, 0, totalGridWorldSize);
        
        // Draw grid corners
        Gizmos.DrawWireCube(gridCenter, new Vector3(totalGridWorldSize, 0.1f, totalGridWorldSize));
        
        // Draw camera view bounds if camera exists
        if (targetCamera != null && targetCamera.orthographic)
        {
            Gizmos.color = Color.cyan;
            Bounds viewBounds = GetViewBounds();
            Gizmos.DrawWireCube(viewBounds.center, viewBounds.size);
        }
        
        // Draw camera to grid center line
        Gizmos.color = Color.red;
        if (transform != null)
        {
            Gizmos.DrawLine(transform.position, gridCenter);
        }
    }
}

/// <summary>
/// Data structure for camera information
/// </summary>
[System.Serializable]
public struct CameraInfo
{
    public Vector3 position;
    public Vector3 rotation;
    public float orthographicSize;
    public bool isOrthographic;
    public Vector3 gridCenter;
    public Bounds viewBounds;
    
    public override string ToString()
    {
        return $"Camera Info - Pos: {position}, Rot: {rotation}, Size: {orthographicSize}, Ortho: {isOrthographic}";
    }
}