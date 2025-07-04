using Unity.Services.LevelPlay;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.Purchasing;
using LevelPlayBannerPosition = com.unity3d.mediation.LevelPlayBannerPosition;

namespace ManagersSpace
{
	public class AdManager : MonoBehaviour
	{
		public static AdManager Get => _instance
#if UNITY_EDITOR
			??= Resources.Load<GameObject>("AdManager").GetComponent<AdManager>()
#endif
		;
		private static AdManager _instance;

		public void LoadAds()
		{
			if (_isAdBlocked)
				return;

			_levelPlayBannerAd.LoadAd();
			_levelPlayInterstitialAd.LoadAd();
			_levelPlayRewardedAd.LoadAd();
		}

		public bool isRewardAdShowing;

		public void ShowRewardAd(UnityAction onAdSuccess)
		{
			if (_isAdBlocked)
			{
				onAdSuccess?.Invoke();
				return;
			}

			if (isRewardAdShowing)
			{
				Debug.LogWarning("Reward ad is already showing.");
				return;
			}

			isRewardAdShowing = true;
			_onAdSuccess = onAdSuccess;

			if (_levelPlayRewardedAd.IsAdReady())
			{
				_isShowingRewardAd = true;
				_levelPlayRewardedAd.OnAdLoaded -= RewardAdLoadedAndShow;
				isRewarded = false;
				_levelPlayRewardedAd.ShowAd();
			}
			else
			{
				_levelPlayRewardedAd.OnAdLoaded += RewardAdLoadedAndShow;
				_levelPlayRewardedAd.LoadAd();
				_isShowingRewardAd = false;
				Invoke(nameof(CancelShowRewardAd), 3f);
			}
		}

		private bool _isShowingRewardAd = false;

		private void CancelShowRewardAd()
		{
			if (_isShowingRewardAd == false)
			{
				_levelPlayRewardedAd.OnAdLoaded -= RewardAdLoadedAndShow;
				_onAdSuccess = null;
				isRewardAdShowing = false;
			}
		}

		public void ShowAd(UnityAction onAdSuccess = null)
		{
			if (_isAdBlocked)
				return;

			if (_levelPlayInterstitialAd.IsAdReady())
			{
				_levelPlayInterstitialAd.OnAdLoaded -= AdLoadedAndShow;
				isRewarded = true;
				_onAdSuccess = onAdSuccess;
				_levelPlayInterstitialAd.ShowAd();
			}
			else
			{
				_levelPlayInterstitialAd.OnAdLoaded += AdLoadedAndShow;
				_levelPlayInterstitialAd.LoadAd();
			}
		}

		public void AdLoadedAndShow(LevelPlayAdInfo info)
		{
			ShowAd();
		}

		public void RewardAdLoadedAndShow(LevelPlayAdInfo info)
		{
			ShowRewardAd(_onAdSuccess);
		}

#if UNITY_ANDROID
		private const string _appKey = "2294823c5";
#elif UNITY_IPHONE
		private const string _appKey = "1fad4069d";
#endif

		private bool _isAdBlocked;
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

			CheckAdBlock();

			if (_isAdBlocked)
				return;

			LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
			LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

			_levelPlayBannerAd = new("9kkh0ks13rv7r8ov", com.unity3d.mediation.LevelPlayAdSize.BANNER, LevelPlayBannerPosition.BottomCenter, "Startup", respectSafeArea: true);
			_levelPlayBannerAd.OnAdLoaded += OnShowBannerAd;

			_levelPlayInterstitialAd = new("qfvaerrxoa4actcz");
			_levelPlayInterstitialAd.OnAdClosed += OnRewardedAdClosed;

			/*
			_levelPlayRewardedAd = new("8937rlb9efrx3270");
			_levelPlayRewardedAd.OnAdRewarded += OnAdRewarded;
			_levelPlayRewardedAd.OnAdClosed += OnRewardedAdClosed;
			*/
			
#if DEVELOPMENT_BUILD
			LevelPlay.SetMetaData("is_test_suite", "enable");
#endif

			LevelPlay.Init(_appKey);
		}

		private void CheckAdBlock()
		{
#if UNITY_EDITOR
			_isAdBlocked = true;
			return;
#endif

			_isAdBlocked = PlayerPrefs.HasKey("isAdBlocked");

			if (_isAdBlocked)
				return;

			var product = CodelessIAPStoreListener.Instance.GetProduct("1");
			_isAdBlocked = product != null && string.IsNullOrEmpty(product.receipt) == false;

			if (_isAdBlocked)
			{
				PlayerPrefs.SetInt("isAdBlocked", 1);
				PlayerPrefs.Save();
				UIManager.Get.btAdBlock?.SetActive(false);
			}
		}

		public void OnAdBlockBought()
		{
			_isAdBlocked = true;
			_levelPlayBannerAd.DestroyAd();
			PlayerPrefs.SetInt("isAdBlocked", 1);
			PlayerPrefs.Save();
			Debug.Log("Ads are blocked.");
		}

		private void OnShowBannerAd(LevelPlayAdInfo obj)
		{
			Debug.Log("OnShowBannerAd");
			_levelPlayBannerAd.ShowAd();
		}

		private void SdkInitializationCompletedEvent(LevelPlayConfiguration obj)
		{
			LoadAds();
			Debug.Log("IronSource SDK initialized successfully.");
		}

		private void SdkInitializationFailedEvent(LevelPlayInitError error)
		{
			Debug.LogError($"IronSource SDK initialized failed.\n{error.ErrorMessage}");
		}

		private UnityAction _onAdSuccess;
		private bool isRewarded;

		private void OnAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
		{
			isRewarded = true;
		}

		private void OnRewardedAdClosed(LevelPlayAdInfo obj)
		{
			isRewardAdShowing = false;

			if (isRewarded)
				_onAdSuccess?.Invoke();

			_onAdSuccess = null;
		}
	}
}