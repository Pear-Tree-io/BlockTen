using UnityEngine;
using TMPro;

public class ClassicModeManager : MonoBehaviour
{
    public static ClassicModeManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;      // assign in inspector

    private int score;
    private int comboMultiplier = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        ResetMode();
    }

    /// <summary>
    /// Call this whenever blocks are destroyed.
    /// </summary>
    /// <param name="destroyedCount">Number of blocks removed this round.</param>
    public void OnBlocksDestroyed(int destroyedCount)
    {
        if (destroyedCount <= 0)
        {
            // no clear => reset combo
            comboMultiplier = 1;
            UpdateComboText();
            return;
        }

        // award points: each block gives comboMultiplier points
        score += destroyedCount * comboMultiplier;
        scoreText.text = score.ToString();
        UpdateComboText();

        // next clear is worth more
        comboMultiplier++;
    }

    /// <summary>
    /// Resets score & combo—call this at level start or on restart.
    /// </summary>
    public void ResetMode()
    {
        score = 0;
        comboMultiplier = 1;
        scoreText.text = "0";
        UpdateComboText();
    }

    private void UpdateComboText()
    {
        if (comboText != null)
            comboText.text = $"Combo: x{comboMultiplier}";
    }
}
