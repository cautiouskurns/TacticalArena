using UnityEngine;
using System.Collections;

/// <summary>
/// Individual unit movement component for grid-based tactical movement.
/// Handles movement state, animation coordination, and integration with the movement system.
/// Attached to individual units to manage their movement behavior and state.
/// </summary>
public class GridMovementComponent : MonoBehaviour
{
    [Header("Movement Configuration")]
    [SerializeField] private float movementSpeed = 2.0f;
    [SerializeField] private bool enableMovementAnimation = true;
    [SerializeField] private bool useCustomAnimationCurve = false;
    [SerializeField] private AnimationCurve customMovementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Movement State")]
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool isMoving = false;
    [SerializeField] private Vector2Int currentGridPosition;
    [SerializeField] private Vector2Int targetGridPosition;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool enableMovementEffects = true;
    [SerializeField] private ParticleSystem movementParticles;
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private AudioClip movementStartSound;
    [SerializeField] private AudioClip movementCompleteSound;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableMovementLogging = true;
    
    // Component references
    private IMovable movableInterface;
    private MovementManager movementManager;
    private GridManager gridManager;
    private Animator unitAnimator;
    
    // Movement state
    private Coroutine activeMovementCoroutine;
    private Vector3 movementStartPosition;
    private Vector3 movementTargetPosition;
    private float movementStartTime;
    private bool movementPaused = false;
    
    // Events
    public System.Action<GridMovementComponent, Vector2Int> OnMovementStart;
    public System.Action<GridMovementComponent, Vector2Int> OnMovementComplete;
    public System.Action<GridMovementComponent, string> OnMovementCancel;
    public System.Action<GridMovementComponent, float> OnMovementProgress;
    
    // Properties
    public bool CanMove => canMove && !isMoving && !movementPaused;
    public bool IsMoving => isMoving;
    public Vector2Int CurrentGridPosition => currentGridPosition;
    public Vector2Int TargetGridPosition => targetGridPosition;
    public float MovementProgress { get; private set; } = 0f;
    
    void Awake()
    {
        InitializeComponent();
    }
    
    void Start()
    {
        FindSystemReferences();
        SetupMovementIntegration();
    }
    
    /// <summary>
    /// Initializes the movement component
    /// </summary>
    private void InitializeComponent()
    {
        // Get IMovable interface from this GameObject or parents
        movableInterface = GetComponent<IMovable>();
        if (movableInterface == null)
        {
            movableInterface = GetComponentInParent<IMovable>();
        }
        
        // Get animator if available
        unitAnimator = GetComponent<Animator>();
        if (unitAnimator == null)
        {
            unitAnimator = GetComponentInChildren<Animator>();
        }
        
        // Setup audio source
        if (movementAudioSource == null)
        {
            movementAudioSource = GetComponent<AudioSource>();
            if (movementAudioSource == null && (movementStartSound != null || movementCompleteSound != null))
            {
                movementAudioSource = gameObject.AddComponent<AudioSource>();
                movementAudioSource.playOnAwake = false;
                movementAudioSource.volume = 0.7f;
            }
        }
        
        if (enableMovementLogging)
        {
            Debug.Log($"GridMovementComponent initialized on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Finds references to system managers
    /// </summary>
    private void FindSystemReferences()
    {
        // Find MovementManager
        movementManager = FindFirstObjectByType<MovementManager>();
        if (movementManager == null)
        {
            Debug.LogWarning($"GridMovementComponent on {gameObject.name}: MovementManager not found");
        }
        
        // Find GridManager
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogWarning($"GridMovementComponent on {gameObject.name}: GridManager not found");
        }
        
        // Sync current grid position with IMovable if available
        if (movableInterface != null)
        {
            currentGridPosition = movableInterface.GridPosition;
        }
        else if (gridManager != null)
        {
            // Calculate from world position
            GridCoordinate gridCoord = gridManager.WorldToGrid(transform.position);
            currentGridPosition = new Vector2Int(gridCoord.x, gridCoord.z);
        }
        
        if (enableMovementLogging)
        {
            Debug.Log($"GridMovementComponent on {gameObject.name}: Current grid position: {currentGridPosition}");
        }
    }
    
    /// <summary>
    /// Sets up integration with movement system
    /// </summary>
    private void SetupMovementIntegration()
    {
        // Register with IMovable events if available
        if (movableInterface != null)
        {
            movableInterface.OnMovementStart += OnMovableMovementStart;
            movableInterface.OnMovementComplete += OnMovableMovementComplete;
            movableInterface.OnMovementCancel += OnMovableMovementCancel;
        }
    }
    
    /// <summary>
    /// Starts movement to a target grid position
    /// </summary>
    public bool StartMovement(Vector2Int targetPos)
    {
        if (!CanMove)
        {
            if (enableMovementLogging)
            {
                Debug.Log($"GridMovementComponent on {gameObject.name}: Cannot start movement (CanMove = false)");
            }
            return false;
        }
        
        if (gridManager == null)
        {
            Debug.LogError($"GridMovementComponent on {gameObject.name}: Cannot move without GridManager");
            return false;
        }
        
        // Set movement state
        isMoving = true;
        targetGridPosition = targetPos;
        movementStartPosition = transform.position;
        movementTargetPosition = gridManager.GridToWorld(new GridCoordinate(targetPos.x, targetPos.y));
        movementStartTime = Time.time;
        MovementProgress = 0f;
        
        // Start movement coroutine
        if (activeMovementCoroutine != null)
        {
            StopCoroutine(activeMovementCoroutine);
        }
        
        activeMovementCoroutine = StartCoroutine(ExecuteMovement());
        
        // Fire events
        OnMovementStart?.Invoke(this, targetPos);
        
        // Play start sound
        PlayMovementSound(movementStartSound);
        
        // Start movement effects
        StartMovementEffects();
        
        // Update animator
        UpdateAnimatorState();
        
        if (enableMovementLogging)
        {
            Debug.Log($"GridMovementComponent on {gameObject.name}: Started movement to {targetPos}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Executes the movement animation
    /// </summary>
    private IEnumerator ExecuteMovement()
    {
        if (!enableMovementAnimation)
        {
            // Instant movement
            transform.position = movementTargetPosition;
            CompleteMovement();
            yield break;
        }
        
        float distance = Vector3.Distance(movementStartPosition, movementTargetPosition);
        float duration = distance / movementSpeed;
        
        if (duration < 0.01f)
        {
            transform.position = movementTargetPosition;
            CompleteMovement();
            yield break;
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration && isMoving)
        {
            // Handle pause
            if (movementPaused)
            {
                yield return null;
                continue;
            }
            
            float progress = elapsed / duration;
            float curveValue = useCustomAnimationCurve ? customMovementCurve.Evaluate(progress) : progress;
            
            // Update position
            Vector3 currentPosition = Vector3.Lerp(movementStartPosition, movementTargetPosition, curveValue);
            transform.position = currentPosition;
            
            // Update progress
            MovementProgress = progress;
            OnMovementProgress?.Invoke(this, progress);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final position
        if (isMoving)
        {
            transform.position = movementTargetPosition;
            CompleteMovement();
        }
    }
    
    /// <summary>
    /// Completes the movement
    /// </summary>
    private void CompleteMovement()
    {
        isMoving = false;
        currentGridPosition = targetGridPosition;
        MovementProgress = 1f;
        
        // Update IMovable interface
        if (movableInterface != null)
        {
            movableInterface.GridPosition = currentGridPosition;
        }
        
        // Clean up
        activeMovementCoroutine = null;
        
        // Fire events
        OnMovementComplete?.Invoke(this, currentGridPosition);
        
        // Play complete sound
        PlayMovementSound(movementCompleteSound);
        
        // Stop movement effects
        StopMovementEffects();
        
        // Update animator
        UpdateAnimatorState();
        
        if (enableMovementLogging)
        {
            Debug.Log($"GridMovementComponent on {gameObject.name}: Completed movement at {currentGridPosition}");
        }
    }
    
    /// <summary>
    /// Cancels current movement
    /// </summary>
    public void CancelMovement(string reason = "Movement cancelled")
    {
        if (!isMoving) return;
        
        isMoving = false;
        MovementProgress = 0f;
        
        // Stop movement coroutine
        if (activeMovementCoroutine != null)
        {
            StopCoroutine(activeMovementCoroutine);
            activeMovementCoroutine = null;
        }
        
        // Fire events
        OnMovementCancel?.Invoke(this, reason);
        
        // Stop movement effects
        StopMovementEffects();
        
        // Update animator
        UpdateAnimatorState();
        
        if (enableMovementLogging)
        {
            Debug.Log($"GridMovementComponent on {gameObject.name}: Cancelled movement - {reason}");
        }
    }
    
    /// <summary>
    /// Pauses or resumes movement
    /// </summary>
    public void SetMovementPaused(bool paused)
    {
        movementPaused = paused;
        
        if (enableMovementLogging)
        {
            Debug.Log($"GridMovementComponent on {gameObject.name}: Movement {(paused ? "paused" : "resumed")}");
        }
    }
    
    /// <summary>
    /// Sets whether this component can initiate movement
    /// </summary>
    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
        
        if (!canMove && isMoving)
        {
            CancelMovement("Movement disabled");
        }
    }
    
    /// <summary>
    /// Instantly moves to target position without animation
    /// </summary>
    public void InstantMoveTo(Vector2Int targetPos)
    {
        if (gridManager == null) return;
        
        CancelMovement("Instant movement requested");
        
        currentGridPosition = targetPos;
        targetGridPosition = targetPos;
        transform.position = gridManager.GridToWorld(new GridCoordinate(targetPos.x, targetPos.y));
        
        // Update IMovable interface
        if (movableInterface != null)
        {
            movableInterface.GridPosition = currentGridPosition;
        }
        
        if (enableMovementLogging)
        {
            Debug.Log($"GridMovementComponent on {gameObject.name}: Instant move to {targetPos}");
        }
    }
    
    /// <summary>
    /// Starts movement effects
    /// </summary>
    private void StartMovementEffects()
    {
        if (!enableMovementEffects) return;
        
        if (movementParticles != null)
        {
            movementParticles.Play();
        }
    }
    
    /// <summary>
    /// Stops movement effects
    /// </summary>
    private void StopMovementEffects()
    {
        if (!enableMovementEffects) return;
        
        if (movementParticles != null)
        {
            movementParticles.Stop();
        }
    }
    
    /// <summary>
    /// Plays movement sound
    /// </summary>
    private void PlayMovementSound(AudioClip clip)
    {
        if (movementAudioSource != null && clip != null)
        {
            movementAudioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Updates animator state based on movement
    /// </summary>
    private void UpdateAnimatorState()
    {
        if (unitAnimator == null) return;
        
        // Set animator parameters
        unitAnimator.SetBool("IsMoving", isMoving);
        
        if (isMoving)
        {
            // Calculate movement direction for animation blending
            Vector3 direction = (movementTargetPosition - movementStartPosition).normalized;
            unitAnimator.SetFloat("MoveX", direction.x);
            unitAnimator.SetFloat("MoveZ", direction.z);
        }
        else
        {
            unitAnimator.SetFloat("MoveX", 0f);
            unitAnimator.SetFloat("MoveZ", 0f);
        }
    }
    
    /// <summary>
    /// Handles IMovable movement start event
    /// </summary>
    private void OnMovableMovementStart(IMovable movable, Vector2Int targetPos)
    {
        if (movable == movableInterface)
        {
            // Movement started from external source (MovementManager)
            // Update our internal state to match
            if (!isMoving)
            {
                StartMovement(targetPos);
            }
        }
    }
    
    /// <summary>
    /// Handles IMovable movement complete event
    /// </summary>
    private void OnMovableMovementComplete(IMovable movable, Vector2Int finalPos)
    {
        if (movable == movableInterface)
        {
            currentGridPosition = finalPos;
            
            if (isMoving)
            {
                CompleteMovement();
            }
        }
    }
    
    /// <summary>
    /// Handles IMovable movement cancel event
    /// </summary>
    private void OnMovableMovementCancel(IMovable movable, string reason)
    {
        if (movable == movableInterface)
        {
            CancelMovement(reason);
        }
    }
    
    /// <summary>
    /// Gets movement information for debugging
    /// </summary>
    public string GetMovementInfo()
    {
        if (isMoving)
        {
            return $"Moving: {currentGridPosition} -> {targetGridPosition} (Progress: {MovementProgress:F2})";
        }
        else
        {
            return $"Idle at: {currentGridPosition} (CanMove: {CanMove})";
        }
    }
    
    /// <summary>
    /// Unity Inspector validation
    /// </summary>
    void OnValidate()
    {
        if (movementSpeed <= 0) movementSpeed = 1f;
        
        // Sync grid position in editor
        if (!Application.isPlaying && gridManager != null)
        {
            GridCoordinate gridCoord = gridManager.WorldToGrid(transform.position);
            currentGridPosition = new Vector2Int(gridCoord.x, gridCoord.z);
        }
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableMovementLogging) return;
        
        // Draw current state indicator
        Gizmos.color = isMoving ? Color.cyan : (CanMove ? Color.green : Color.red);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
        
        // Draw movement path when moving
        if (isMoving)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(movementStartPosition, movementTargetPosition);
            Gizmos.DrawWireCube(movementTargetPosition, Vector3.one * 0.5f);
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Unregister from events
        if (movableInterface != null)
        {
            movableInterface.OnMovementStart -= OnMovableMovementStart;
            movableInterface.OnMovementComplete -= OnMovableMovementComplete;
            movableInterface.OnMovementCancel -= OnMovableMovementCancel;
        }
        
        // Stop any active movement
        CancelMovement("Component destroyed");
        
        // Clear event references
        OnMovementStart = null;
        OnMovementComplete = null;
        OnMovementCancel = null;
        OnMovementProgress = null;
    }
}