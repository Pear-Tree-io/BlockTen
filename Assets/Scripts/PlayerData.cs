using UnityEngine;

public class PlayerData
{
	public class PlayerPrefsKeys
	{
		public const string CompletedTutorial = "Tutorial";
		public const string HighScore = "HighScore";

		public static string GetHighScoreKey(StageModeManager.StageModeType modeType) => $"{modeType}_{HighScore}_{GetModeVersion(modeType)}";

		private static int GetModeVersion(StageModeManager.StageModeType modeType) => modeType switch
		{
			_ => 1
		};

		public const string Ad = "Ad";
	}

	public static bool ShouldTutorial() => PlayerPrefs.GetInt(PlayerPrefsKeys.CompletedTutorial, 0) == 0;

	public static void CompleteTutorial()
	{
		PlayerPrefs.SetInt(PlayerPrefsKeys.CompletedTutorial, 1);
		PlayerPrefs.Save();
	}

	public static void SetAdCount(int adCount)
	{
		PlayerPrefs.SetInt(PlayerPrefsKeys.Ad, adCount);
		PlayerPrefs.Save();
	}

	public static int GetAdCount() => PlayerPrefs.GetInt(PlayerPrefsKeys.Ad, 0);

	public static int GetHighScore(StageModeManager.StageModeType modeType) => PlayerPrefs.GetInt(PlayerPrefsKeys.GetHighScoreKey(modeType), 0);

	public static void SetHighScore(StageModeManager.StageModeType mode, int highScore)
	{
		PlayerPrefs.SetInt(PlayerPrefsKeys.GetHighScoreKey(mode), highScore);
		PlayerPrefs.Save();
	}
}