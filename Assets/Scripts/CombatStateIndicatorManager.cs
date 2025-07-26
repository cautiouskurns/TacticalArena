using UnityEngine;

/// <summary>
/// Combat state visual feedback and tactical indicators.
/// Part of Task 2.1.4 - Combat Visual Feedback - COMPLETES SUB-MILESTONE 2.1
/// </summary>
public class CombatStateIndicatorManager : MonoBehaviour
{
    [Header("Combat State Configuration")]
    [SerializeField] private Material canAttackMaterial;
    [SerializeField] private Material cannotAttackMaterial;
    [SerializeField] private bool enableRangeVisualization = true;
    [SerializeField] private Color rangeColor = Color.orange;
    [SerializeField] private float fadeSpeed = 3.0f;
    
    // Component references
    private CombatVisualManager visualManager;
    private GameObject currentIndicator;
    
    public bool IsInitialized { get; private set; } = false;
    
    public void Initialize(CombatVisualManager manager)
    {
        visualManager = manager;
        IsInitialized = true;
        Debug.Log("CombatStateIndicatorManager initialized");
    }
    
    public void ShowCombatIndicators(Unit unit)
    {
        if (!IsInitialized || unit == null) return;
        
        // Create simple indicator around unit
        if (currentIndicator != null) Destroy(currentIndicator);
        
        currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        currentIndicator.name = "CombatIndicator";
        currentIndicator.transform.position = unit.transform.position;
        currentIndicator.transform.localScale = new Vector3(2f, 0.1f, 2f);
        
        // Remove collider
        Destroy(currentIndicator.GetComponent<Collider>());
        
        // Set material based on combat state
        Renderer renderer = currentIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = canAttackMaterial ?? new Material(Shader.Find("Standard"));
            renderer.material.color = rangeColor;
        }
    }
    
    public void HideCombatIndicators(Unit unit)
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
        }
    }
    
    void OnDestroy()
    {
        if (currentIndicator != null) Destroy(currentIndicator);
    }
}