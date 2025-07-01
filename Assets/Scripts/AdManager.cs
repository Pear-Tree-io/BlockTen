using Unity.Services.LevelPlay;
using UnityEngine.Events;
using UnityEngine;

namespace ManagersSpace
{
	public class AdManager : MonoBehaviour
	{
		public static AdManager Get => _instance;
		private static AdManager _instance;

		public void LoadAds()
		{
			_levelPlayBannerAd.LoadAd();
			_levelPlayInterstitialAd.LoadAd();
			_levelPlayRewardedAd.LoadAd();
		}

		public void ShowRewardAd(UnityAction onAdSuccess)
		{
			if (_levelPlayRewardedAd.IsAdReady())
			{
				_onAdSuccess = onAdSuccess;
				_levelPlayRewardedAd.ShowAd();
			}
			else
				_levelPlayRewardedAd.LoadAd();
		}

		public void ShowAd()
		{
			if (_levelPlayInterstitialAd.IsAdReady())
				_levelPlayInterstitialAd.ShowAd();
			else
				_levelPlayInterstitialAd.LoadAd();
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
			if (_instance == null)
			{
				_instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else
			{
				Destroy(gameObject);
				return;
			}

			LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
			LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

			_levelPlayRewardedAd = new("8937rlb9efrx3270");
			_levelPlayInterstitialAd = new("qfvaerrxoa4actcz");
			_levelPlayBannerAd = new("9kkh0ks13rv7r8ov");
			_levelPlayRewardedAd.OnAdRewarded += OnAdRewarded;
			_levelPlayInterstitialAd.OnAdClosed += OnAdClosed;
			_levelPlayBannerAd.OnAdLoaded += OnShowBannerAd;

#if DEVELOPMENT_BUILD
			LevelPlay.SetMetaData("is_test_suite", "enable");
#endif

			LevelPlay.Init(_appKey);
			_levelPlayBannerAd.LoadAd();
		}

		private void OnShowBannerAd(LevelPlayAdInfo obj)
		{
			_levelPlayBannerAd.ShowAd();
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