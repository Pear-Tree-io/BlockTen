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
		private const string _gameId = "5675513";
		private const string _appKey = "1f4f5ebbd";
#elif UNITY_IPHONE
		private const string _gameId = "";
		private const string _appKey = "1fad4069d";
#endif

		private LevelPlayRewardedAd _levelPlayRewardedAd;

		private void Awake()
		{
			_instance = this;
			
			LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
			LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
			LevelPlay.Init(_appKey);
			_levelPlayRewardedAd.OnAdRewarded += OnAdRewarded;
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
	}
}