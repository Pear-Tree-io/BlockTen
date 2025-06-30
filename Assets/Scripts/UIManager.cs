using DG.Tweening;
using GooglePlayGames;
using UnityEngine;

public class UIManager : MonoBehaviour
{
	[SerializeField]
	private SpriteRenderer initRenderer;
	[SerializeField]
	private AnimatedButton[] leaderboardButtons;

	private void Start()
	{
		initRenderer.SetActive(true);
		initRenderer.DOFade(0f, .3456f).SetDelay(.3456f).OnComplete(() => Destroy(initRenderer.gameObject));
		
		foreach (var bt in leaderboardButtons)
		{
			bt.SetOnClick(OnLeaderboard);
		}
	}

	private void OnLeaderboard()
	{
		PlayGamesPlatform.Instance.ShowLeaderboardUI();
	}
}