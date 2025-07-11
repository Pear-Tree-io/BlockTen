#if UNITY_IOS
using System;
using System.Linq;
using System.Threading.Tasks;
using Apple.GameKit;
using Apple.GameKit.Leaderboards;
using UnityEngine;

public class IOSPlatform : PlatformBase
{
	private const string _leaderboardID = "MyLeaderboard";
	
	private readonly GKLeaderboard _leaderboard;
	private readonly GKGameCenterViewController _viewController;
	
	private string _signature;
	private string _teamPlayerID;
	private string _salt;
	private string _publicKeyUrl;
	private string _timestamp;
	
	public IOSPlatform()
	{
		Login();
		_leaderboard = GKLeaderboard.LoadLeaderboards(_leaderboardID).Result.FirstOrDefault();
		_viewController = GKGameCenterViewController.Init(GKGameCenterViewController.GKGameCenterViewControllerState.Leaderboards);
	}
	
	public async Task Login()
	{
		if (GKLocalPlayer.Local.IsAuthenticated == false)
		{
			var player = await GKLocalPlayer.Authenticate();
			Debug.Log($"GameKit Authentication: player {player}");

			// Grab the display name.
			var localPlayer = GKLocalPlayer.Local;
			Debug.Log($"Local Player: {localPlayer.DisplayName}");

			// Fetch the items.
			var fetchItemsResponse =  await GKLocalPlayer.Local.FetchItems();

			_signature = Convert.ToBase64String(fetchItemsResponse.GetSignature());
			_teamPlayerID = localPlayer.TeamPlayerId;
			Debug.Log($"Team Player ID: {_teamPlayerID}");

			_salt = Convert.ToBase64String(fetchItemsResponse.GetSalt());
			_publicKeyUrl = fetchItemsResponse.PublicKeyUrl;
			_timestamp = fetchItemsResponse.Timestamp.ToString();

			Debug.Log($"GameKit Authentication: signature => {_signature}");
			Debug.Log($"GameKit Authentication: publickeyurl => {_publicKeyUrl}");
			Debug.Log($"GameKit Authentication: salt => {_salt}");
			Debug.Log($"GameKit Authentication: Timestamp => {_timestamp}");
		}
		else
		{
			Debug.Log("AppleGameCenter player already logged in.");
		}
	}

	public override void ReportScore(int score)
	{
		_leaderboard.SubmitScore(score, 0, GKLocalPlayer.Local);
	}

	public override void OnLeaderboard()
	{
		_viewController.Present();
	}
}
#endif