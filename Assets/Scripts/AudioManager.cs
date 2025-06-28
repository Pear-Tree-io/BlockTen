using System.Collections.Generic;
using UnityEngine;

public enum SFXType
{
    Button,
    PlaceBlock,
    GameOver,
    GameOverHighScore,
    goScore,
    bestScore,
    brickBreak,
    pickUp,
    noStamp,
    goodStamp,
    good,
    greatStamp,
    great,
    fantasticStamp,
    fantastic,
    // Add more as needed
}

[System.Serializable]
public class SFXItem
{
    public SFXType type;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM Settings")]
    // An AudioSource for background music (BGM)
    public AudioSource bgmSource;
    // BGM clips for different scenes
    public AudioClip mainMenuBGM;

    [Header("SFX Settings")]
    // An AudioSource for one-shot sound effects (SFX)
    public AudioSource sfxSource;
    // List of SFX items to assign in the Inspector
    public List<SFXItem> sfxItems = new List<SFXItem>();

    // Internal dictionary for quick lookup of SFX
    private Dictionary<SFXType, AudioClip> sfxDictionary = new();

    [Header("User Settings")]
    public bool isBgmOn = true;
    public bool isSfxOn = true;
    public bool isVibOn = true;

    private const string BGM_PREF_KEY = "BGM_SETTING";
    private const string SFX_PREF_KEY = "SFX_SETTING";
    private const string VIB_PREF_KEY = "VIB_SETTING";

    private void Awake()
    {
        // Singleton pattern: ensure only one instance persists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // also preserve SFX child:
            if (sfxSource != null)
                DontDestroyOnLoad(sfxSource.gameObject);
            LoadSettings();
            PopulateSFXDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayMainMenuBGM();
    }

    private void LoadSettings()
    {
        // Load BGM and SFX settings from PlayerPrefs (default on = 1)
        isBgmOn = PlayerPrefs.GetInt(BGM_PREF_KEY, 1) == 1;
        isSfxOn = PlayerPrefs.GetInt(SFX_PREF_KEY, 1) == 1;
        isVibOn = PlayerPrefs.GetInt(VIB_PREF_KEY, 1) == 1;
        if (bgmSource != null)
            bgmSource.mute = !isBgmOn;
        if (sfxSource != null)
            sfxSource.mute = !isSfxOn;
    }

    private void PopulateSFXDictionary()
    {
        sfxDictionary.Clear();
        foreach (SFXItem item in sfxItems)
        {
            if (item.clip != null && !sfxDictionary.ContainsKey(item.type))
            {
                sfxDictionary.Add(item.type, item.clip);
            }
        }
    }

    #region BGM Methods

    /// <summary>
    /// Play main menu BGM.
    /// </summary>
    public void PlayMainMenuBGM()
    {
        if (bgmSource == null || mainMenuBGM == null)
            return;

        // If we're already playing this exact clip, do nothing
        if (bgmSource.isPlaying && bgmSource.clip == mainMenuBGM)
            return;

        bgmSource.clip = mainMenuBGM;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>
    /// Stop playing BGM.
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    /// <summary>
    /// Toggle background music on/off.
    /// </summary>
    public void ToggleBGM()
    {
        isBgmOn = !isBgmOn;
        if (bgmSource != null)
            bgmSource.mute = !isBgmOn;
        PlayerPrefs.SetInt(BGM_PREF_KEY, isBgmOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleVIB()
    {
        isVibOn = !isVibOn;
        PlayerPrefs.SetInt(VIB_PREF_KEY, isVibOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    #endregion

    #region SFX Methods

    /// <summary>
    /// Play a one-shot sound effect corresponding to the given type.
    /// </summary>
    public void PlaySFX(SFXType type)
    {
        if (sfxSource == null || !isSfxOn)
            return;

        if (sfxDictionary.TryGetValue(type, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("SFX of type " + type + " not found in dictionary.");
        }
    }

    /// <summary>
    /// Toggle sound effects on/off.
    /// </summary>
    public void ToggleSFX()
    {
        isSfxOn = !isSfxOn;
        if (sfxSource != null)
            sfxSource.mute = !isSfxOn;
        PlayerPrefs.SetInt(SFX_PREF_KEY, isSfxOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    #endregion
}
