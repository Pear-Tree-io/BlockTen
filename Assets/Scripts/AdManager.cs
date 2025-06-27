using Unity.Services.LevelPlay;
using UnityEngine.Events;
using UnityEngine;

namespace ManagersSpace
{
	public class AdManager : MonoBehaviour
	{
		public static AdManager Get => _instance;
		private static AdManager _instance;
		
		public void ShowRewardAd(UnityAction onAdSuccess)
		{
			if (_levelPlayRewardedAd.IsAdReady())
			{
				_onAdSuccess = onAdSuccess;
				_levelPlayRewardedAd.ShowAd();
			}
		}
		
#if UNITY_ANDROID
		private const string _appKey = "2294823c5";
#elif UNITY_IPHONE
		private const string _appKey = "1fad4069d";
#endif

		private LevelPlayRewardedAd _levelPlayRewardedAd;
		private LevelPlayInterstitialAd _levelPlayInterstitialAd;
		private LevelPlayBannerAd _levelPlayBannerAd;

		private void Awake()
		{
			_instance = this;
			
			LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
			LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

			_levelPlayRewardedAd = new("8937rlb9efrx3270");
			_levelPlayInterstitialAd = new("qfvaerrxoa4actcz");
			_levelPlayBannerAd = new("9kkh0ks13rv7r8ov");
			_levelPlayRewardedAd.OnAdRewarded += OnAdRewarded;
			_levelPlayInterstitialAd.OnAdClosed += OnAdClosed;
			
			_levelPlayBannerAd.ShowAd();
			
			LevelPlay.Init(_appKey);
		}

		private void SdkInitializationCompletedEvent(LevelPlayConfiguration obj)
		{
			Debug.Log("IronSource SDK initialized successfully.");
		}

		private void SdkInitializationFailedEvent(LevelPlayInitError error)
		{
			Debug.LogError($"IronSource SDK initialized failed.\n{error.ErrorMessage}");
		}

		private UnityAction _onAdSuccess;

		private void OnAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
		{
			_onAdSuccess?.Invoke();
			_onAdSuccess = null;

			Debug.Log("OnAdRewarded");
		}

		private void OnAdClosed(LevelPlayAdInfo obj)
		{
		}
	}
}