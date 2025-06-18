using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ClassicModeManager : MonoBehaviour
{
    public static ClassicModeManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject comboTextPrefab;
    private Canvas _canvas;

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
        // Cache the canvas for world-to-screen conversions
        _canvas = scoreText.GetComponentInParent<Canvas>();
        ResetMode();
    }

    /// <summary>
    /// Call this whenever blocks are destroyed.
    /// </summary>
    public void OnBlocksDestroyed(int destroyedCount)
    {
        if (destroyedCount <= 0)
        {
            // Reset combo multiplier
            comboMultiplier = 1;
        }
        else
        {
            // Add points based on current combo
            score += destroyedCount * comboMultiplier;
            scoreText.text = score.ToString();

            // Show combo popup for multiplier >= 2
            if (comboMultiplier >= 2)
                ShowComboPopup(comboMultiplier);

            comboMultiplier++;
        }
    }

    /// <summary>
    /// Resets score & combo—call at level start or on restart.
    /// </summary>
    public void ResetMode()
    {
        score = 0;
        comboMultiplier = 1;
        scoreText.text = "0";
    }

    /// <summary>
    /// Instantiates a combo text prefab at the last drop position
    /// and positions it without auto-destroy (handled elsewhere).
    /// </summary>
    private void ShowComboPopup(int multiplier)
    {
        // Instantiate under the UI canvas
        GameObject go = Instantiate(comboTextPrefab, _canvas.transform);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp != null)
            tmp.text = $"{multiplier}";

        // Convert world drop position to canvas local position
        Vector3 worldPos = GridManager.Instance.LastPlacedPosition;
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransform canvasRect = _canvas.transform as RectTransform;
        RectTransform goRect = go.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
            out localPoint
        );
        goRect.anchoredPosition = localPoint;
    }
}
