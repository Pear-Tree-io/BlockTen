using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class GoogleManager : MonoBehaviour
{
	public AnimatedButton btLeaderboard;
	private Action onSuccess;
	public static GoogleManager Get => _instance;
	private static GoogleManager _instance;

	private void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}

		InitializeGooglePlayGames();
	}

	private void Start()
	{
		btLeaderboard.SetOnClick(OnLeaderboard);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Backspace))
			InitializeGooglePlayGames();
	}

	private void OnLeaderboard()
	{
		PlayGamesPlatform.Instance.ShowLeaderboardUI();
	}

	private void InitializeGooglePlayGames()
	{
		PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
	}

	internal void ProcessAuthentication(SignInStatus status)
	{
		if (status == SignInStatus.Success)
		{
			onSuccess?.Invoke();
			onSuccess = null;
		}
	}

	public void ReportScore(int highScore)
	{
		if (PlayGamesPlatform.Instance.IsAuthenticated())
			PlayGamesPlatform.Instance.ReportScore(highScore, GPGSIds.leaderboard_highscore, null);
		else
		{
			InitializeGooglePlayGames();
			onSuccess = () => { PlayGamesPlatform.Instance.ReportScore(highScore, GPGSIds.leaderboard_highscore, null); };
		}
	}
}