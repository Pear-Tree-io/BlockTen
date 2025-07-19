using UnityEngine;
using TMPro;
using System.Collections;
using Firebase.Analytics;
using ManagersSpace;
using UnityEngine.SceneManagement;

/// <summary>
/// Base class for all game mode managers. Handles score, combos, and UI popups.
/// Inherit and override scoring logic if needed.
/// </summary>
public abstract class ModeManager : MonoBehaviour
{
	public StageModeManager.StageModeType modeType;
	public SpawnManager spawnManager;

	[Header("Settings UI")]
	[SerializeField]
	protected GameObject SettingsPanel;

	public GameObject bgmOff;
	public GameObject sfxOff;
	public GameObject vibOff;

	public bool isHighScore;
	public bool isRevivable = true;

    private int adCount = 0;

    public bool ConsumeRevivableState()
	{
		var value = isRevivable;
		isRevivable = false;
		return value;
	}

	protected virtual void Awake()
	{
	}

	protected virtual void Start()
	{
		SettingsPanel.SetActive(false);
		ResetMode();
		MenuUpdate();
		if (AudioManager.Instance.bgmSource.isPlaying == false)
			AudioManager.Instance.PlayMainMenuBGM();
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

	public virtual void OnMatchBlocksDestroyed(int matchCount, int destroyedCount)
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
#if UNITY_EDITOR == false
		AdManager.Get.LoadAds();
	    FirebaseAnalytics.LogEvent("ResetMode", new Parameter("mode", GetType().Name));
#endif
	}

	/// <summary>
	/// Instantiates combo popup at last placed position.
	/// </summary>
	protected virtual IEnumerator ShowTextOnCanvas(GameObject textPrefab, Canvas canvas, int number, float waitTime)
	{
		if (textPrefab == null || canvas == null) yield break;

		textPrefab.SetActive(true);

		TMP_Text tmp = textPrefab.GetComponent<TMP_Text>();
		if (tmp != null)
			tmp.text = number.ToString();

		// Positioning
		Vector3 worldPos = GridManager.Instance.LastPlacedPosition;
		Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
		RectTransform canvasRect = canvas.transform as RectTransform;
		RectTransform goRect = textPrefab.GetComponent<RectTransform>();
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			canvasRect,
			screenPos,
			canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
			out localPoint
		);
		goRect.anchoredPosition = localPoint;

		yield return new WaitForSeconds(waitTime);

		textPrefab.SetActive(false);
	}

	public virtual void SetNoSpaceLeftMessage(bool active)
	{
	}

	public virtual void GameOver()
	{
		switch (modeType)
		{
			case StageModeManager.StageModeType.Classic:
				GridManager.Instance.InitializeEndGrid();
				AudioManager.Instance.StopBGM();
				SaveGame();
				break;
			case StageModeManager.StageModeType.Tutorial:
			case StageModeManager.StageModeType.Clear:
				if (GridManager.Instance.IsClear())
					StageModeManager.Get.NextStage();
				else
					StageModeManager.Get.ResetStage();
				break;
		}
		
		FirebaseAnalytics.LogEvent("GameOver", new Parameter("mode", modeType.ToString()));

        AdCountUp();
        PlayAd();
    }

	protected virtual void SaveGame()
	{
        PlayerData.SetAdCount(adCount);
        PlayerPrefs.Save();
    }

	protected virtual void LoadGame()
	{
        adCount = PlayerData.GetAdCount();
    }

	public virtual void ToggleSettings()
	{
		SettingsPanel.SetActive(!SettingsPanel.activeSelf);
		AudioManager.Instance.PlaySFX(SFXType.Button);
	}

	public void ToggleBGM()
	{
		AudioManager.Instance.ToggleBGM();
		AudioManager.Instance.PlaySFX(SFXType.Button);
		MenuUpdate();
	}

	public void ToggleSFX()
	{
		AudioManager.Instance.ToggleSFX();
		AudioManager.Instance.PlaySFX(SFXType.Button);
		MenuUpdate();
	}

	public void ToggleVIB()
	{
		AudioManager.Instance.ToggleVIB();
		AudioManager.Instance.PlaySFX(SFXType.Button);
		MenuUpdate();
	}

	private void MenuUpdate()
	{
		bgmOff.SetActive(!AudioManager.Instance.isBgmOn);
		sfxOff.SetActive(!AudioManager.Instance.isSfxOn);
		vibOff.SetActive(!AudioManager.Instance.isVibOn);
	}

	public bool CheckGameOver(int needCount)
	{
		if (!GridManager.Instance.HasFreeSlots(needCount))
		{
			Debug.Log("Game Over: Not enough space for a full wave!");
			return true;
		}
		
		return false;
	}

	public virtual void SetModeType(StageModeManager.StageModeType stageModeType)
	{
		modeType = stageModeType;
		spawnManager.Init(this);
	}

	protected virtual void PlayAd()
	{
        if (adCount > 2)
        {
			Debug.Log("AD is played");
            AdManager.Get.ShowAd();
        }
    }

	protected virtual void AdCountUp()
	{
		if(adCount <= 2)
		{
			adCount++;
		}
	}

	public virtual void ToMenu() 
	{
		PlayAd();
        AudioManager.Instance.PlaySFX(SFXType.Button);
        SceneManager.LoadScene(0);
    }

    public virtual void Replay() {
		PlayAd();
        AudioManager.Instance.PlaySFX(SFXType.Button);
    }
}