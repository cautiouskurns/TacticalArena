using UnityEngine;

/// <summary>
/// Interface for objects that can provide line-of-sight validation capability.
/// Enables tactical combat mechanics through obstacle blocking and cover systems.
/// </summary>
public interface ILineOfSightProvider
{
    /// <summary>
    /// Checks if there is a clear line of sight between two world positions
    /// </summary>
    /// <param name="fromPosition">Starting position for line-of-sight check</param>
    /// <param name="toPosition">Target position for line-of-sight check</param>
    /// <returns>True if line of sight is clear, false if blocked by obstacles</returns>
    bool HasLineOfSight(Vector3 fromPosition, Vector3 toPosition);

    /// <summary>
    /// Checks if there is line of sight between two transforms
    /// </summary>
    /// <param name="fromTransform">Starting transform for line-of-sight check</param>
    /// <param name="toTransform">Target transform for line-of-sight check</param>
    /// <returns>True if line of sight is clear, false if blocked by obstacles</returns>
    bool HasLineOfSight(Transform fromTransform, Transform toTransform);

    /// <summary>
    /// Gets detailed line-of-sight information including blocking objects
    /// </summary>
    /// <param name="fromPosition">Starting position for line-of-sight check</param>
    /// <param name="toPosition">Target position for line-of-sight check</param>
    /// <returns>Detailed result with blocking information</returns>
    LineOfSightResult GetLineOfSightDetails(Vector3 fromPosition, Vector3 toPosition);

    /// <summary>
    /// Gets the distance at which line of sight is blocked, or full distance if clear
    /// </summary>
    /// <param name="fromPosition">Starting position for line-of-sight check</param>
    /// <param name="toPosition">Target position for line-of-sight check</param>
    /// <returns>Distance to blocking obstacle, or full distance if clear</returns>
    float GetLineOfSightDistance(Vector3 fromPosition, Vector3 toPosition);

    /// <summary>
    /// Checks if a specific object is blocking line of sight between two positions
    /// </summary>
    /// <param name="fromPosition">Starting position</param>
    /// <param name="toPosition">Target position</param>
    /// <param name="potentialBlocker">Object to check for blocking</param>
    /// <returns>True if the object is blocking line of sight</returns>
    bool IsObjectBlockingLineOfSight(Vector3 fromPosition, Vector3 toPosition, GameObject potentialBlocker);

    /// <summary>
    /// Gets all objects that are blocking line of sight between two positions
    /// </summary>
    /// <param name="fromPosition">Starting position</param>
    /// <param name="toPosition">Target position</param>
    /// <returns>Array of GameObjects blocking line of sight</returns>
    GameObject[] GetBlockingObjects(Vector3 fromPosition, Vector3 toPosition);

    /// <summary>
    /// Visualizes line of sight for debugging and player feedback
    /// </summary>
    /// <param name="fromPosition">Starting position</param>
    /// <param name="toPosition">Target position</param>
    /// <param name="duration">How long to show the visualization</param>
    void VisualizeLineOfSight(Vector3 fromPosition, Vector3 toPosition, float duration = 1.0f);
}

/// <summary>
/// Detailed result of a line-of-sight check with blocking information
/// </summary>
[System.Serializable]
public struct LineOfSightResult
{
    public bool isBlocked;
    public float distance;
    public Vector3 blockingPoint;
    public GameObject blockingObject;
    public string blockingReason;

    public LineOfSightResult(bool blocked, float dist, Vector3 blockPoint, GameObject blockObj, string reason)
    {
        isBlocked = blocked;
        distance = dist;
        blockingPoint = blockPoint;
        blockingObject = blockObj;
        blockingReason = reason;
    }

    public static LineOfSightResult Clear(float distance)
    {
        return new LineOfSightResult(false, distance, Vector3.zero, null, "Clear line of sight");
    }

    public static LineOfSightResult Blocked(float distance, Vector3 blockPoint, GameObject blockObj, string reason)
    {
        return new LineOfSightResult(true, distance, blockPoint, blockObj, reason);
    }

    public override string ToString()
    {
        if (isBlocked)
        {
            return $"BLOCKED at {distance:F2}m by {blockingObject?.name ?? "unknown"}: {blockingReason}";
        }
        else
        {
            return $"CLEAR for {distance:F2}m: {blockingReason}";
        }
    }
}