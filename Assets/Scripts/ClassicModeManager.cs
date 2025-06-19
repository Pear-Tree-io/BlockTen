using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ClassicModeManager : ModeManager
{
    public static ClassicModeManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject comboTextPrefab;
    private Canvas _canvas;

    private int score;
    private int comboMultiplier = 1;

    protected override void Start()
    {
        // Cache the canvas for world-to-screen conversions
        _canvas = scoreText.GetComponentInParent<Canvas>();
        ResetMode();
    }

    /// <summary>
    /// Call this whenever blocks are destroyed.
    /// </summary>
    public override void OnBlocksDestroyed(int destroyedCount)
    {
        if (destroyedCount <= 0)
        {
            comboMultiplier = 1;
        }
        else
        {
            score += CalculateScore(destroyedCount);
            if (scoreText != null)
                scoreText.text = score.ToString();

            if (comboMultiplier >= 2)
                ShowComboPopup(comboTextPrefab,_canvas,comboMultiplier);

            comboMultiplier++;
        }
    }

    protected override int CalculateScore(int destroyedCount)
    {
        return destroyedCount * comboMultiplier;
    }

    /// <summary>
    /// Resets score & combo—call at level start or on restart.
    /// </summary>
    public override void ResetMode()
    {
        score = 0;
        comboMultiplier = 1;
        scoreText.text = "0";
    }
}
