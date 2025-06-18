using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class NumberBlock : MonoBehaviour
{
    // --- Composite‐chain links (used by DraggableCompositeBlock) ---
    [HideInInspector] public NumberBlock neighborUp;
    [HideInInspector] public NumberBlock neighborDown;
    [HideInInspector] public NumberBlock neighborLeft;
    [HideInInspector] public NumberBlock neighborRight;

    // --- Value and display ---
    public int Value { get; private set; }
    [SerializeField] private TextMeshPro valueText;
    public SpriteRenderer spriteRenderer;

    // 55% chance to pick from [1–4], else [5–9]
    private const float range1to4Chance = 0.55f;

    private void Awake()
    {
        // cache sprite renderer
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // grab the TMP_Text in your children
        if (valueText == null)
            valueText = GetComponentInChildren<TextMeshPro>();
    }

    /// <summary>
    /// Assigns a random value between 1 and 9 using a 55/45 split,
    /// then updates the label.
    /// </summary>
    public void AssignRandom()
    {
        // ensure we have the text component
        if (valueText == null)
            valueText = GetComponentInChildren<TextMeshPro>();

        float r = Random.value;
        if (r < range1to4Chance)
            Value = Random.Range(1, 5);   // 1,2,3,4
        else
            Value = Random.Range(5, 10);  // 5,6,7,8,9

        valueText.text = Value.ToString();
    }

    public void OnDragStart()
    {
        spriteRenderer.sortingOrder = 3;
        valueText.sortingOrder = 4;
    }

    public void OnDragEnd()
    {
        spriteRenderer.sortingOrder = 1;
        valueText.sortingOrder = 2;
    }

    /// <summary>
    /// Tint this block’s sprite to the given color.
    /// </summary>
    public void SetColor(Color col)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = col;
    }

    public TextMeshPro ValueText
    {
        get { return valueText; }
        set { valueText = value; }
    }
}
