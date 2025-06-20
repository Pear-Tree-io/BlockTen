using UnityEngine;
using TMPro;

/// <summary>
/// Base class for all game mode managers. Handles score, combos, and UI popups.
/// Inherit and override scoring logic if needed.
/// </summary>
public abstract class ModeManager : MonoBehaviour
{
    public GameObject Settings;

    protected virtual void Awake()
    {
        // Optional hook for subclasses (e.g. singleton setup)
    }

    protected virtual void Start()
    {

    }

    /// <summary>
    /// Call when blocks are destroyed.
    /// </summary>
    public virtual void OnBlocksDestroyed(int destroyedCount)
    {
        
    }

    /// <summary>
    /// Call for each matches.
    /// </summary>
    public virtual void OnMatchDestroyed(int matchCount)
    {

    }

    /// <summary>
    /// Default scoring: destroyedCount × comboMultiplier.
    /// Override to customize.
    /// </summary>
    protected virtual int CalculateScore(int destroyedCount)
    {
        return 0;
    }

    /// <summary>
    /// Resets score and combo—call at start or on restart.
    /// </summary>
    public virtual void ResetMode()
    {

    }

    /// <summary>
    /// Instantiates combo popup at last placed position.
    /// </summary>
    protected virtual void ShowComboPopup(GameObject comboTextPrefab, Canvas canvas, int multiplier)
    {
        if (comboTextPrefab == null || canvas == null) return;

        GameObject go = Instantiate(comboTextPrefab, canvas.transform);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp != null)
            tmp.text = multiplier.ToString();

        // Positioning
        Vector3 worldPos = GridManager.Instance.LastPlacedPosition;
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransform goRect = go.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
            out localPoint
        );
        goRect.anchoredPosition = localPoint;
    }

    public virtual void GameOver() 
    {
        SaveGame();
        ResetMode();
    }
    protected virtual void SaveGame() { }

    protected virtual void LoadGame() { }

    public virtual void OpenSettings()
    {
        Settings.SetActive(true);
    }

    public virtual void CloseSettings()
    {
        Settings.SetActive(false);
    }
}