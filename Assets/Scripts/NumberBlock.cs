using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class NumberBlock : MonoBehaviour
{
    public int Value { get; private set; }
    public bool IsJoker { get; private set; }
    [SerializeField] private TextMeshPro valueText;

    // Chance for an X-joker instead of 1–9
    private const float jokerChance = 0.01f;  // 5% for X
    // Within the non-joker pool, 55% for [1–4], 45% for [5–9]
    private const float range1to4Chance = 0.55f;
    public SpriteRenderer spriteRenderer;

    [Header("Neighbor Links (assign in prefab)")]
    public NumberBlock neighborUp;
    public NumberBlock neighborDown;
    public NumberBlock neighborLeft;
    public NumberBlock neighborRight;

    private void Awake()
    {
	    if (spriteRenderer == null)
			spriteRenderer = GetComponent<SpriteRenderer>();
	    
        if (valueText == null)
            valueText = GetComponentInChildren<TextMeshPro>();
    }

    /// <summary>
    /// Assigns either a random 1–9 (with your custom weighting) or, rarely, an X-joker.
    /// </summary>
    public void AssignRandom()
    {
        float r = Random.value;

        // 1) Joker check
        if (r < jokerChance)
        {
            IsJoker = true;
            Value = 0;
            valueText.text = "X";
            return;
        }

        // 2) Number roll with single Random.value
        float s = (r - jokerChance) / (1f - jokerChance);
        if (s < range1to4Chance)
            Value = Random.Range(1, 5);
        else
            Value = Random.Range(5, 10);

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
}
