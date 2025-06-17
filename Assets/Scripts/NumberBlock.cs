using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class NumberBlock : MonoBehaviour
{
    public int Value { get; private set; }
    public bool IsJoker { get; private set; }
    [SerializeField] private TMP_Text valueText;

    // Chance for an X-joker instead of 1–9 (0.05 = 5%)
    private const float jokerChance = 0.01f;

    private void Awake()
    {
        if (valueText == null)
            valueText = GetComponentInChildren<TMP_Text>();
    }

    /// <summary>
    /// Assigns either a random 1–9 or, rarely, an X-joker.
    /// </summary>
    public void AssignRandom(int min = 1, int max = 9)
    {
        if (Random.value < jokerChance)
        {
            // Joker branch
            IsJoker = true;
            Value = 0;                // ignored in matching logic
            if (valueText != null)
                valueText.text = "X";
        }
        else
        {
            // Regular number
            IsJoker = false;
            Value = Random.Range(min, max + 1);
            if (valueText != null)
                valueText.text = Value.ToString();
        }
    }
}
