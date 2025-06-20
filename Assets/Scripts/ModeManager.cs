using UnityEngine;
using TMPro;
using System;
using System.Runtime.InteropServices;
using Mono.Cecil.Cil;
using Unity.IO.LowLevel.Unsafe;

[Serializable]
public class SettingsData
{
    public bool Sound = true;
    public bool BGM = true;
    public bool Vibration = true;
}
/// <summary>
/// Base class for all game mode managers. Handles score, combos, and UI popups.
/// Inherit and override scoring logic if needed.
/// </summary>
public abstract class ModeManager : MonoBehaviour
{
    [Header("Settings UI")]
    [SerializeField] protected GameObject SettingsPanel;
    [SerializeField] protected GameObject soundOff;
    [SerializeField] protected GameObject bgmOff;
    [SerializeField] protected GameObject vibrateOff;

    private const string SettingsKey = "GameSettings";
    protected SettingsData settings = new SettingsData();
      

    protected virtual void Awake()
    {
        LoadSettings();
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

    #region Settings UI Methods
    public virtual void OpenSettings()
    {
        if (SettingsPanel != null)
            SettingsPanel.SetActive(true);
    }

    public virtual void CloseSettings()
    {
        if (SettingsPanel != null)
            SettingsPanel.SetActive(false);
    }

    public virtual void ToggleSound()
    {
        settings.Sound = !settings.Sound;
        ApplySettings();
        SaveSettings();
    }
    public virtual void ToggleBGM()
    {
        settings.BGM = !settings.BGM;
        ApplySettings();
        SaveSettings();
    }

    public virtual void ToggleVibration()
    {
        settings.Vibration = !settings.Vibration;
        ApplySettings();
        SaveSettings();
    }
    #endregion

    #region Persistence
    protected void SaveSettings()
    {
        string json = JsonUtility.ToJson(settings);
        PlayerPrefs.SetString(SettingsKey, json);
        PlayerPrefs.Save();
    }

    protected void LoadSettings()
    {
        if (PlayerPrefs.HasKey(SettingsKey))
        {
            string json = PlayerPrefs.GetString(SettingsKey);
            settings = JsonUtility.FromJson<SettingsData>(json) ?? new SettingsData();
        }
        ApplySettings();
    }

    protected void ApplySettings()
    {
        if (soundOff != null)
            soundOff.SetActive(!settings.Sound);
        if (bgmOff != null)
            bgmOff.SetActive(!settings.BGM);
        if (vibrateOff != null)
            vibrateOff.SetActive(!settings.Vibration);
    }
    #endregion
}