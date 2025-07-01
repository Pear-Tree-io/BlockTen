using DG.Tweening;
using ManagersSpace;
using UnityEngine;
using UnityEngine.Purchasing;

public class UIManager : MonoBehaviour
{
	public static UIManager Get => _instance;
	private static UIManager _instance;
	
	public CodelessIAPButton btAdBlock;
	
	[SerializeField]
	private SpriteRenderer initRenderer;
	[SerializeField]
	private AnimatedButton[] leaderboardButtons;

	private void Awake()
	{
		_instance = this;
	}

	private void Start()
	{
		if (initRenderer)
		{
			initRenderer.SetActive(true);
			initRenderer.DOFade(0f, .3456f).SetDelay(.3456f).OnComplete(() => Destroy(initRenderer.gameObject));
		}

		if (btAdBlock)
		{
			btAdBlock.onPurchaseComplete.RemoveAllListeners();
			btAdBlock.onPurchaseComplete.AddListener(_ =>
			{
				btAdBlock.SetActive(false);
				AdManager.Get.OnAdBlockBought();
			});
		}

		foreach (var bt in leaderboardButtons)
		{
			bt.SetOnClick(GoogleManager.Get.OnLeaderboard);
		}
	}
}