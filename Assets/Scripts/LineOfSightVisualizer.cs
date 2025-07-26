using UnityEngine;
using System.Collections;

/// <summary>
/// Enhanced line-of-sight visualization for tactical clarity.
/// Part of Task 2.1.4 - Combat Visual Feedback - COMPLETES SUB-MILESTONE 2.1
/// </summary>
public class LineOfSightVisualizer : MonoBehaviour
{
    [Header("Line Visualization")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color blockedColor = Color.red;
    [SerializeField] private Color clearColor = Color.green;
    [SerializeField] private bool enablePulsing = true;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float fadeDuration = 1.0f;
    
    // Component references
    private CombatVisualManager visualManager;
    private LineOfSightManager losManager;
    private LineRenderer currentAttackLine;
    
    public bool IsInitialized { get; private set; } = false;
    
    public void Initialize(CombatVisualManager manager)
    {
        visualManager = manager;
        losManager = FindFirstObjectByType<LineOfSightManager>();
        
        if (losManager == null)
        {
            Debug.LogWarning("LineOfSightVisualizer: LineOfSightManager not found - visualization will use basic line-of-sight assumptions");
        }
        
        IsInitialized = true;
    }
    
    public void ShowAttackLine(IAttacker attacker, IAttackable target)
    {
        if (!IsInitialized || attacker?.Transform == null || target?.Transform == null) return;
        
        Vector3 startPos = attacker.Transform.position + Vector3.up * 0.5f;
        Vector3 endPos = target.Transform.position + Vector3.up * 0.5f;
        
        bool hasLOS = losManager?.HasLineOfSight(startPos, endPos) ?? true;
        CreateAttackLine(startPos, endPos, !hasLOS);
    }
    
    public void HideAttackLine()
    {
        if (currentAttackLine != null)
        {
            StartCoroutine(FadeOutLine(currentAttackLine));
            currentAttackLine = null;
        }
    }
    
    /// <summary>
    /// Shows line of sight visualization for debugging (called by LineOfSightManager)
    /// </summary>
    public void ShowLineOfSight(Vector3 fromPosition, Vector3 toPosition, bool hasLineOfSight, float duration = 1.0f)
    {
        Color lineColor = hasLineOfSight ? clearColor : blockedColor;
        CreateLineOfSightLine(fromPosition, toPosition, lineColor, duration);
    }
    
    /// <summary>
    /// Creates a temporary line of sight visualization
    /// </summary>
    private void CreateLineOfSightLine(Vector3 startPos, Vector3 endPos, Color lineColor, float duration)
    {
        GameObject lineObject = new GameObject("LineOfSightLine");
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        
        line.material = lineMaterial ?? new Material(Shader.Find("Sprites/Default"));
        line.widthMultiplier = lineWidth;
        line.positionCount = 2;
        line.useWorldSpace = true;
        
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
        line.material.color = lineColor;
        
        // Auto-destroy after duration
        Destroy(lineObject, duration);
    }
    
    private void CreateAttackLine(Vector3 startPos, Vector3 endPos, bool isBlocked)
    {
        if (currentAttackLine != null) Destroy(currentAttackLine.gameObject);
        
        GameObject lineObject = new GameObject("AttackLine");
        currentAttackLine = lineObject.AddComponent<LineRenderer>();
        
        currentAttackLine.material = lineMaterial ?? new Material(Shader.Find("Sprites/Default"));
        currentAttackLine.widthMultiplier = lineWidth;
        currentAttackLine.positionCount = 2;
        currentAttackLine.useWorldSpace = true;
        
        currentAttackLine.SetPosition(0, startPos);
        currentAttackLine.SetPosition(1, endPos);
        currentAttackLine.material.color = isBlocked ? blockedColor : clearColor;
        
        if (enablePulsing)
        {
            StartCoroutine(PulseLine(currentAttackLine));
        }
    }
    
    private IEnumerator PulseLine(LineRenderer line)
    {
        Color originalColor = line.material.color;
        while (line != null)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.3f;
            Color pulseColor = originalColor;
            pulseColor.a = originalColor.a + pulse;
            line.material.color = pulseColor;
            yield return null;
        }
    }
    
    private IEnumerator FadeOutLine(LineRenderer line)
    {
        if (line == null) yield break;
        
        Color originalColor = line.material.color;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration && line != null)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / fadeDuration);
            Color fadeColor = originalColor;
            fadeColor.a = alpha;
            line.material.color = fadeColor;
            yield return null;
        }
        
        if (line?.gameObject != null) Destroy(line.gameObject);
    }
    
    void OnDestroy()
    {
        if (currentAttackLine != null) Destroy(currentAttackLine.gameObject);
    }
}