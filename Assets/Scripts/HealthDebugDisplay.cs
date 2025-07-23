using UnityEngine;

/// <summary>
/// Debug component to display unit health information in the Scene view and Console.
/// Helps diagnose combat damage application issues.
/// </summary>
[RequireComponent(typeof(UnitHealth))]
[RequireComponent(typeof(TargetCapability))]
public class HealthDebugDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool showHealthInSceneView = true;
    [SerializeField] private bool logHealthChanges = true;
    [SerializeField] private bool logDamageEvents = true;
    [SerializeField] private Color healthTextColor = Color.green;
    [SerializeField] private float textOffset = 2f;
    
    private UnitHealth unitHealth;
    private TargetCapability targetCapability;
    private int lastHealth = -1;
    
    void Start()
    {
        unitHealth = GetComponent<UnitHealth>();
        targetCapability = GetComponent<TargetCapability>();
        
        if (unitHealth != null)
        {
            lastHealth = unitHealth.CurrentHealth;
            
            // Subscribe to health events
            unitHealth.OnHealthChanged += OnHealthChanged;
            unitHealth.OnDamageTaken += OnDamageTaken;
            unitHealth.OnUnitDeath += OnUnitDeath;
            
            if (logHealthChanges)
            {
                Debug.Log($"[HealthDebug] {gameObject.name} initialized with {unitHealth.CurrentHealth}/{unitHealth.MaxHealth} HP");
            }
        }
        
        if (targetCapability != null)
        {
            // Subscribe to target capability events
            targetCapability.OnDamageReceived += OnTargetDamageReceived;
            targetCapability.OnHealthChanged += OnTargetHealthChanged;
            
            if (logDamageEvents)
            {
                Debug.Log($"[HealthDebug] {gameObject.name} TargetCapability connected");
            }
        }
    }
    
    void Update()
    {
        // Check for health changes each frame
        if (unitHealth != null && unitHealth.CurrentHealth != lastHealth)
        {
            if (logHealthChanges)
            {
                Debug.Log($"[HealthDebug] {gameObject.name} health changed: {lastHealth} -> {unitHealth.CurrentHealth}");
            }
            lastHealth = unitHealth.CurrentHealth;
        }
    }
    
    void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (logHealthChanges)
        {
            Debug.Log($"[HealthDebug] {gameObject.name} OnHealthChanged: {currentHealth}/{maxHealth} HP");
        }
    }
    
    void OnDamageTaken(int damage)
    {
        if (logDamageEvents)
        {
            Debug.Log($"[HealthDebug] {gameObject.name} OnDamageTaken: {damage} damage -> Health now: {unitHealth.CurrentHealth}/{unitHealth.MaxHealth}");
        }
    }
    
    void OnUnitDeath()
    {
        if (logDamageEvents)
        {
            Debug.LogWarning($"[HealthDebug] {gameObject.name} DIED!");
        }
    }
    
    void OnTargetDamageReceived(IAttacker attacker, int damage)
    {
        if (logDamageEvents)
        {
            string attackerName = attacker?.GetDisplayInfo() ?? "Unknown";
            Debug.Log($"[HealthDebug] {gameObject.name} TargetCapability received {damage} damage from {attackerName}");
        }
    }
    
    void OnTargetHealthChanged(IAttacker attacker, int currentHealth)
    {
        if (logHealthChanges)
        {
            string attackerName = attacker?.GetDisplayInfo() ?? "Unknown";
            Debug.Log($"[HealthDebug] {gameObject.name} TargetCapability health changed to {currentHealth} (attacker: {attackerName})");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showHealthInSceneView || unitHealth == null) return;
        
        // Draw health text above unit
        Vector3 textPosition = transform.position + Vector3.up * textOffset;
        string healthText = $"{unitHealth.CurrentHealth}/{unitHealth.MaxHealth}";
        
        #if UNITY_EDITOR
        UnityEditor.Handles.color = healthTextColor;
        UnityEditor.Handles.Label(textPosition, healthText);
        #endif
        
        // Draw health bar
        float healthPercent = unitHealth.HealthPercentage;
        Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercent);
        Vector3 barPosition = transform.position + Vector3.up * (textOffset - 0.3f);
        Vector3 barSize = new Vector3(healthPercent, 0.1f, 0.1f);
        Gizmos.DrawCube(barPosition, barSize);
        
        // Draw outline for full bar
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(barPosition, new Vector3(1f, 0.1f, 0.1f));
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (unitHealth != null)
        {
            unitHealth.OnHealthChanged -= OnHealthChanged;
            unitHealth.OnDamageTaken -= OnDamageTaken;
            unitHealth.OnUnitDeath -= OnUnitDeath;
        }
        
        if (targetCapability != null)
        {
            targetCapability.OnDamageReceived -= OnTargetDamageReceived;
            targetCapability.OnHealthChanged -= OnTargetHealthChanged;
        }
    }
}