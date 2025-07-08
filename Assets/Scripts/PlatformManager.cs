using UnityEngine;

public class PlatformManager : MonoBehaviour
{
	private static PlatformManager _instance;
	public static PlatformManager Get => _instance;

	private PlatformBase targetPlatform;

	private void Awake()
	{
		_instance = this;
		
#if UNITY_ANDROID
		targetPlatform = new AndroidPlatform();
#elif UNITY_IOS
		targetPlatform = new IOSPlatform();
#endif
	}

	public void ReportScore(int score) => targetPlatform.ReportScore(score);
	public void OnLeaderboard() => targetPlatform.OnLeaderboard();
}

public abstract class PlatformBase
{
	public abstract void ReportScore(int score);
	public abstract void OnLeaderboard();
}