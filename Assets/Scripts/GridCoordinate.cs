using UnityEngine;

/// <summary>
/// Data structure representing a coordinate position on the tactical grid.
/// Provides efficient grid position representation and conversion utilities.
/// </summary>
[System.Serializable]
public struct GridCoordinate
{
    [SerializeField] public int x;
    [SerializeField] public int z;
    
    /// <summary>
    /// Creates a new grid coordinate
    /// </summary>
    public GridCoordinate(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
    
    /// <summary>
    /// Invalid coordinate constant for error checking
    /// </summary>
    public static GridCoordinate Invalid => new GridCoordinate(-1, -1);
    
    /// <summary>
    /// Zero coordinate constant
    /// </summary>
    public static GridCoordinate Zero => new GridCoordinate(0, 0);
    
    /// <summary>
    /// Checks if this coordinate is valid (non-negative)
    /// </summary>
    public bool IsValid => x >= 0 && z >= 0;
    
    /// <summary>
    /// Checks if this coordinate is within the specified grid bounds
    /// </summary>
    public bool IsWithinBounds(int gridWidth, int gridHeight)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }
    
    /// <summary>
    /// Calculates Manhattan distance to another coordinate
    /// </summary>
    public int ManhattanDistance(GridCoordinate other)
    {
        return Mathf.Abs(x - other.x) + Mathf.Abs(z - other.z);
    }
    
    /// <summary>
    /// Calculates Euclidean distance to another coordinate
    /// </summary>
    public float Distance(GridCoordinate other)
    {
        int dx = x - other.x;
        int dz = z - other.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
    
    /// <summary>
    /// Checks if this coordinate is adjacent to another (including diagonals)
    /// </summary>
    public bool IsAdjacentTo(GridCoordinate other)
    {
        int dx = Mathf.Abs(x - other.x);
        int dz = Mathf.Abs(z - other.z);
        return (dx <= 1 && dz <= 1) && !(dx == 0 && dz == 0);
    }
    
    /// <summary>
    /// Checks if this coordinate is orthogonally adjacent (no diagonals)
    /// </summary>
    public bool IsOrthogonallyAdjacentTo(GridCoordinate other)
    {
        int dx = Mathf.Abs(x - other.x);
        int dz = Mathf.Abs(z - other.z);
        return (dx == 1 && dz == 0) || (dx == 0 && dz == 1);
    }
    
    /// <summary>
    /// Gets all orthogonally adjacent coordinates (up, down, left, right)
    /// </summary>
    public GridCoordinate[] GetOrthogonalNeighbors()
    {
        return new GridCoordinate[]
        {
            new GridCoordinate(x, z + 1), // Up
            new GridCoordinate(x, z - 1), // Down
            new GridCoordinate(x - 1, z), // Left
            new GridCoordinate(x + 1, z)  // Right
        };
    }
    
    /// <summary>
    /// Gets all adjacent coordinates including diagonals
    /// </summary>
    public GridCoordinate[] GetAllNeighbors()
    {
        return new GridCoordinate[]
        {
            new GridCoordinate(x - 1, z - 1), // Bottom-Left
            new GridCoordinate(x,     z - 1), // Bottom
            new GridCoordinate(x + 1, z - 1), // Bottom-Right
            new GridCoordinate(x - 1, z),     // Left
            new GridCoordinate(x + 1, z),     // Right
            new GridCoordinate(x - 1, z + 1), // Top-Left
            new GridCoordinate(x,     z + 1), // Top
            new GridCoordinate(x + 1, z + 1)  // Top-Right
        };
    }
    
    /// <summary>
    /// Converts to Vector2Int for compatibility
    /// </summary>
    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, z);
    }
    
    /// <summary>
    /// Creates GridCoordinate from Vector2Int
    /// </summary>
    public static GridCoordinate FromVector2Int(Vector2Int vector)
    {
        return new GridCoordinate(vector.x, vector.y);
    }
    
    /// <summary>
    /// Converts to Vector3 representation (Y = 0)
    /// </summary>
    public Vector3 ToVector3()
    {
        return new Vector3(x, 0f, z);
    }
    
    // Operator overloads for convenient usage
    public static bool operator ==(GridCoordinate a, GridCoordinate b)
    {
        return a.x == b.x && a.z == b.z;
    }
    
    public static bool operator !=(GridCoordinate a, GridCoordinate b)
    {
        return !(a == b);
    }
    
    public static GridCoordinate operator +(GridCoordinate a, GridCoordinate b)
    {
        return new GridCoordinate(a.x + b.x, a.z + b.z);
    }
    
    public static GridCoordinate operator -(GridCoordinate a, GridCoordinate b)
    {
        return new GridCoordinate(a.x - b.x, a.z - b.z);
    }
    
    public static GridCoordinate operator *(GridCoordinate a, int multiplier)
    {
        return new GridCoordinate(a.x * multiplier, a.z * multiplier);
    }
    
    public override bool Equals(object obj)
    {
        if (obj is GridCoordinate coord)
        {
            return this == coord;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return x.GetHashCode() ^ (z.GetHashCode() << 2);
    }
    
    public override string ToString()
    {
        return $"({x}, {z})";
    }
    
    /// <summary>
    /// Returns a formatted string for debugging
    /// </summary>
    public string ToDebugString()
    {
        return $"GridCoord({x}, {z}) [Valid: {IsValid}]";
    }
}