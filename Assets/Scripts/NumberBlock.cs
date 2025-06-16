// NumberBlock.cs
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class NumberBlock : MonoBehaviour
{
    public int Value { get; private set; }
    public TMP_Text valueText;

    private void Awake()
    {
        if (valueText == null)
            valueText = GetComponentInChildren<TMP_Text>();
    }

    public void AssignRandom(int min = 1, int max = 9)
    {
        Value = Random.Range(min, max + 1);
        if (valueText != null)
            valueText.text = Value.ToString();
    }
}