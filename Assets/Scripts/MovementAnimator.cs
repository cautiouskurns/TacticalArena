using UnityEngine;
using System.Collections;

/// <summary>
/// Movement animation system for smooth grid-based unit movement.
/// Handles interpolation, easing curves, and precise grid snapping for tactical movement.
/// Provides professional-feeling movement animations with customizable timing and curves.
/// </summary>
public class MovementAnimator : MonoBehaviour
{
    [Header("Animation Configuration")]
    [SerializeField] private bool enableAnimation = true;
    [SerializeField] private float movementSpeed = 2.0f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float movementTolerance = 0.1f;
    
    [Header("Animation Features")]
    [SerializeField] private bool enableHeightAnimation = false; // Disabled to prevent ground embedding
    [SerializeField] private float maxHeightOffset = 0.5f;
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool enableRotationAlignment = false; // Disabled to prevent tilting
    [SerializeField] private float rotationSpeed = 5.0f;
    
    [Header("Performance Settings")]
    [SerializeField] private bool useFixedDeltaTime = false;
    [SerializeField] private int maxConcurrentAnimations = 10;
    [SerializeField] private bool enableAnimationLogging = true;
    
    // Animation state tracking
    private int activeAnimationCount = 0;
    
    // Events
    public System.Action<Transform, Vector3> OnAnimationComplete;
    public System.Action<Transform, string> OnAnimationCancelled;
    public System.Action<Transform, Vector3, float> OnAnimationProgress;
    
    void Awake()
    {
        InitializeAnimator();
    }
    
    /// <summary>
    /// Initializes the movement animator
    /// </summary>
    private void InitializeAnimator()
    {
        if (enableAnimationLogging)
        {
            Debug.Log("MovementAnimator initialized");
        }
    }
    
    /// <summary>
    /// Animates movement from current position to target position
    /// </summary>
    public IEnumerator AnimateMovement(Transform target, Vector3 targetPosition, float speed = 0)
    {
        if (target == null)
        {
            Debug.LogError("MovementAnimator: Cannot animate null transform");
            yield break;
        }
        
        // Use provided speed or default
        float actualSpeed = speed > 0 ? speed : movementSpeed;
        
        // Check animation limits
        if (activeAnimationCount >= maxConcurrentAnimations)
        {
            if (enableAnimationLogging)
            {
                Debug.LogWarning($"MovementAnimator: Max concurrent animations ({maxConcurrentAnimations}) reached");
            }
            OnAnimationCancelled?.Invoke(target, "Too many concurrent animations");
            yield break;
        }
        
        // Start animation
        activeAnimationCount++;
        
        if (enableAnimationLogging)
        {
            Debug.Log($"MovementAnimator: Starting animation for {target.name} to {targetPosition} (speed: {actualSpeed})");
        }
        
        // If animation is disabled, do instant movement
        if (!enableAnimation)
        {
            target.position = targetPosition;
            CompleteAnimation(target, targetPosition);
            yield break;
        }
        
        // Store start values
        Vector3 startPosition = target.position;
        Quaternion startRotation = target.rotation;
        float startTime = GetAnimationTime();
        
        // Calculate animation duration based on distance and speed
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / actualSpeed;
        
        // Minimum duration to prevent division by zero
        if (duration < 0.01f)
        {
            target.position = targetPosition;
            CompleteAnimation(target, targetPosition);
            yield break;
        }
        
        // Calculate target rotation if alignment is enabled
        Quaternion targetRotation = startRotation;
        if (enableRotationAlignment)
        {
            Vector3 direction = (targetPosition - startPosition).normalized;
            if (direction != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Calculate progress
            float progress = elapsed / duration;
            float curveValue = movementCurve.Evaluate(progress);
            
            // Interpolate position
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            // Add height animation if enabled
            if (enableHeightAnimation && maxHeightOffset > 0)
            {
                float heightProgress = heightCurve.Evaluate(progress);
                float heightOffset = Mathf.Sin(heightProgress * Mathf.PI) * maxHeightOffset;
                currentPosition.y += heightOffset;
            }
            
            // Apply position
            target.position = currentPosition;
            
            // Apply rotation if enabled
            if (enableRotationAlignment)
            {
                target.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);
            }
            
            // Fire progress event
            OnAnimationProgress?.Invoke(target, currentPosition, progress);
            
            // Update elapsed time
            elapsed += GetDeltaTime();
            yield return null;
        }
        
        // Ensure final position is exact
        target.position = targetPosition;
        if (enableRotationAlignment)
        {
            target.rotation = targetRotation;
        }
        
        CompleteAnimation(target, targetPosition);
    }
    
    /// <summary>
    /// Animates movement with custom animation curve
    /// </summary>
    public IEnumerator AnimateMovementWithCurve(Transform target, Vector3 targetPosition, AnimationCurve customCurve, float duration)
    {
        if (target == null)
        {
            Debug.LogError("MovementAnimator: Cannot animate null transform");
            yield break;
        }
        
        if (duration <= 0)
        {
            target.position = targetPosition;
            CompleteAnimation(target, targetPosition);
            yield break;
        }
        
        activeAnimationCount++;
        
        Vector3 startPosition = target.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float curveValue = customCurve.Evaluate(progress);
            
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            target.position = currentPosition;
            
            OnAnimationProgress?.Invoke(target, currentPosition, progress);
            
            elapsed += GetDeltaTime();
            yield return null;
        }
        
        target.position = targetPosition;
        CompleteAnimation(target, targetPosition);
    }
    
    /// <summary>
    /// Instantly moves to target position (no animation)
    /// </summary>
    public void InstantMovement(Transform target, Vector3 targetPosition)
    {
        if (target == null)
        {
            Debug.LogError("MovementAnimator: Cannot move null transform");
            return;
        }
        
        target.position = targetPosition;
        CompleteAnimation(target, targetPosition);
        
        if (enableAnimationLogging)
        {
            Debug.Log($"MovementAnimator: Instant movement for {target.name} to {targetPosition}");
        }
    }
    
    /// <summary>
    /// Cancels all animations for a specific transform
    /// </summary>
    public void CancelAnimation(Transform target, string reason = "Animation cancelled")
    {
        if (target == null) return;
        
        // Stop all coroutines for this target (Unity will handle this automatically when we complete)
        OnAnimationCancelled?.Invoke(target, reason);
        
        if (enableAnimationLogging)
        {
            Debug.Log($"MovementAnimator: Cancelled animation for {target.name} - {reason}");
        }
    }
    
    /// <summary>
    /// Completes an animation
    /// </summary>
    private void CompleteAnimation(Transform target, Vector3 finalPosition)
    {
        activeAnimationCount = Mathf.Max(0, activeAnimationCount - 1);
        OnAnimationComplete?.Invoke(target, finalPosition);
        
        if (enableAnimationLogging)
        {
            Debug.Log($"MovementAnimator: Completed animation for {target.name} at {finalPosition} (Active: {activeAnimationCount})");
        }
    }
    
    /// <summary>
    /// Gets the appropriate time value for animation timing
    /// </summary>
    private float GetAnimationTime()
    {
        return useFixedDeltaTime ? Time.fixedTime : Time.time;
    }
    
    /// <summary>
    /// Gets the appropriate delta time for animation updates
    /// </summary>
    private float GetDeltaTime()
    {
        return useFixedDeltaTime ? Time.fixedDeltaTime : Time.deltaTime;
    }
    
    /// <summary>
    /// Checks if position is within movement tolerance
    /// </summary>
    public bool IsPositionCloseEnough(Vector3 current, Vector3 target)
    {
        return Vector3.Distance(current, target) <= movementTolerance;
    }
    
    /// <summary>
    /// Gets estimated animation duration for a movement
    /// </summary>
    public float GetEstimatedDuration(Vector3 start, Vector3 end, float speed = 0)
    {
        float actualSpeed = speed > 0 ? speed : movementSpeed;
        float distance = Vector3.Distance(start, end);
        return distance / actualSpeed;
    }
    
    /// <summary>
    /// Validates if an animation can be started
    /// </summary>
    public bool CanStartAnimation()
    {
        return activeAnimationCount < maxConcurrentAnimations;
    }
    
    /// <summary>
    /// Gets animation system info for debugging
    /// </summary>
    public string GetAnimationInfo()
    {
        return $"Active Animations: {activeAnimationCount}/{maxConcurrentAnimations}, Speed: {movementSpeed}, Animation: {enableAnimation}";
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableAnimationLogging) return;
        
        // Draw animation system status indicator
        Gizmos.color = activeAnimationCount > 0 ? Color.green : Color.gray;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        // Draw performance indicator
        if (activeAnimationCount >= maxConcurrentAnimations)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1.0f);
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Clear event references
        OnAnimationComplete = null;
        OnAnimationCancelled = null;
        OnAnimationProgress = null;
        
        // Reset state
        activeAnimationCount = 0;
    }
}

/// <summary>
/// Helper class for movement animation configurations
/// </summary>
[System.Serializable]
public class MovementAnimationConfig
{
    public float speed = 2.0f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool enableHeightAnimation = true;
    public float heightOffset = 0.5f;
    public bool enableRotation = true;
    
    public MovementAnimationConfig() { }
    
    public MovementAnimationConfig(float speed, AnimationCurve curve = null)
    {
        this.speed = speed;
        this.curve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
}