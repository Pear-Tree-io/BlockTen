#if UNITY_ANDROID
using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class AndroidPlatform : PlatformBase
{
	private Action onSuccess;

	public AndroidPlatform()
	{
		Initialize();
	}

	private void Initialize()
	{
		PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
	}

	public override void OnLeaderboard()
	{
		PlayGamesPlatform.Instance.ShowLeaderboardUI(GPGSIds.leaderboard_highscore);
	}

	private bool _isTryingToManuallyAuthenticate = false;

	internal void ProcessAuthentication(SignInStatus status)
	{
		Debug.Log($"ProcessAuthentication {status}");
		
		if (status == SignInStatus.Success)
		{
			onSuccess?.Invoke();
			onSuccess = null;
		}
		else if (_isTryingToManuallyAuthenticate == false)
		{
			_isTryingToManuallyAuthenticate = true;
			PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
		}
	}

	public override void ReportScore(int highScore)
	{
		if (PlayGamesPlatform.Instance.IsAuthenticated())
			PlayGamesPlatform.Instance.ReportScore(highScore, GPGSIds.leaderboard_highscore, null);
		else
		{
			PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
			onSuccess = () => PlayGamesPlatform.Instance.ReportScore(highScore, GPGSIds.leaderboard_highscore, null);
		}
	}
}
#endif