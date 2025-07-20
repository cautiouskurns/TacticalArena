using UnityEngine;

/// <summary>
/// Enumeration defining different types of obstacles in the tactical arena
/// </summary>
public enum ObstacleType
{
    LowCover,       // Partial line-of-sight blocking, allows shooting over
    HighWall,       // Full line-of-sight blocking, complete cover
    Terrain         // Low terrain feature, minimal blocking
}

/// <summary>
/// Configuration data for different obstacle types
/// </summary>
[System.Serializable]
public struct ObstacleData
{
    [Header("Obstacle Properties")]
    public ObstacleType type;
    public string displayName;
    public float height;
    public bool blocksLineOfSight;
    public bool providesPartialCover;
    public bool blocksMovement;
    
    [Header("Visual Properties")]
    public Color defaultColor;
    public GameObject prefab;
    public Material material;
    
    [Header("Gameplay Properties")]
    public float coverValue;        // 0.0 = no cover, 1.0 = full cover
    public float movementCost;      // Movement cost multiplier (1.0 = normal)
    public bool destructible;
    public float health;
    
    /// <summary>
    /// Gets default configuration for the specified obstacle type
    /// </summary>
    public static ObstacleData GetDefault(ObstacleType type)
    {
        switch (type)
        {
            case ObstacleType.LowCover:
                return new ObstacleData
                {
                    type = ObstacleType.LowCover,
                    displayName = "Low Cover",
                    height = 0.5f,
                    blocksLineOfSight = false,
                    providesPartialCover = true,
                    blocksMovement = true,
                    defaultColor = new Color(0.6f, 0.4f, 0.2f, 1f),
                    coverValue = 0.5f,
                    movementCost = float.PositiveInfinity, // Cannot move through
                    destructible = false,
                    health = 100f
                };
                
            case ObstacleType.HighWall:
                return new ObstacleData
                {
                    type = ObstacleType.HighWall,
                    displayName = "High Wall",
                    height = 1.5f,
                    blocksLineOfSight = true,
                    providesPartialCover = false,
                    blocksMovement = true,
                    defaultColor = new Color(0.5f, 0.5f, 0.5f, 1f),
                    coverValue = 1.0f,
                    movementCost = float.PositiveInfinity, // Cannot move through
                    destructible = false,
                    health = 200f
                };
                
            case ObstacleType.Terrain:
                return new ObstacleData
                {
                    type = ObstacleType.Terrain,
                    displayName = "Terrain Feature",
                    height = 0.3f,
                    blocksLineOfSight = false,
                    providesPartialCover = false,
                    blocksMovement = false,
                    defaultColor = new Color(0.3f, 0.6f, 0.3f, 1f),
                    coverValue = 0.2f,
                    movementCost = 1.5f, // Slower movement
                    destructible = false,
                    health = 50f
                };
                
            default:
                return GetDefault(ObstacleType.LowCover);
        }
    }
    
    /// <summary>
    /// Checks if this obstacle type blocks line of sight at the given height
    /// </summary>
    public bool BlocksLineOfSightAtHeight(float queryHeight)
    {
        if (!blocksLineOfSight)
            return false;
            
        return queryHeight <= height;
    }
    
    /// <summary>
    /// Gets the cover value provided by this obstacle for the given attack angle
    /// </summary>
    public float GetCoverValue(Vector3 attackDirection)
    {
        // For now, return base cover value
        // Future: Could calculate based on attack angle and obstacle orientation
        return coverValue;
    }
    
    /// <summary>
    /// Gets the movement cost for traversing this obstacle
    /// </summary>
    public float GetMovementCost()
    {
        return blocksMovement ? float.PositiveInfinity : movementCost;
    }
    
    /// <summary>
    /// Gets the prefab path for this obstacle type
    /// </summary>
    public string GetPrefabPath()
    {
        return $"Assets/Prefabs/Obstacles/Obstacle_{type}.prefab";
    }
}

/// <summary>
/// ScriptableObject for configuring obstacle types and their properties
/// </summary>
[CreateAssetMenu(fileName = "New Obstacle Configuration", menuName = "Tactical Arena/Obstacle Configuration")]
public class ObstacleConfiguration : ScriptableObject
{
    [Header("Obstacle Types")]
    public ObstacleData[] obstacleTypes = new ObstacleData[]
    {
        ObstacleData.GetDefault(ObstacleType.LowCover),
        ObstacleData.GetDefault(ObstacleType.HighWall),
        ObstacleData.GetDefault(ObstacleType.Terrain)
    };
    
    /// <summary>
    /// Gets the configuration data for the specified obstacle type
    /// </summary>
    public ObstacleData GetObstacleData(ObstacleType type)
    {
        foreach (var data in obstacleTypes)
        {
            if (data.type == type)
                return data;
        }
        
        // Return default if not found
        return ObstacleData.GetDefault(type);
    }
    
    /// <summary>
    /// Gets all available obstacle types
    /// </summary>
    public ObstacleType[] GetAvailableTypes()
    {
        ObstacleType[] types = new ObstacleType[obstacleTypes.Length];
        for (int i = 0; i < obstacleTypes.Length; i++)
        {
            types[i] = obstacleTypes[i].type;
        }
        return types;
    }
    
    /// <summary>
    /// Validates the configuration for consistency
    /// </summary>
    public bool ValidateConfiguration()
    {
        if (obstacleTypes == null || obstacleTypes.Length == 0)
        {
            Debug.LogError("ObstacleConfiguration: No obstacle types defined");
            return false;
        }
        
        for (int i = 0; i < obstacleTypes.Length; i++)
        {
            var data = obstacleTypes[i];
            
            if (data.height <= 0f)
            {
                Debug.LogError($"ObstacleConfiguration: Invalid height for {data.type}: {data.height}");
                return false;
            }
            
            if (data.coverValue < 0f || data.coverValue > 1f)
            {
                Debug.LogError($"ObstacleConfiguration: Invalid cover value for {data.type}: {data.coverValue}");
                return false;
            }
            
            if (data.movementCost < 0f)
            {
                Debug.LogError($"ObstacleConfiguration: Invalid movement cost for {data.type}: {data.movementCost}");
                return false;
            }
        }
        
        return true;
    }
}