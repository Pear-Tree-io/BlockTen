using UnityEditor;
using UnityEngine;

public class StageModeManager : MonoBehaviour
{
	public enum StageModeType
	{
		None,
		Tutorial,
		Classic,
		TimeLimit,
		Clear,
	}
	
	public StageModeType stageModeType;
	private bool isRevivable => stageModeType is StageModeType.Classic;
	private bool isInfinity => stageModeType is StageModeType.Classic or StageModeType.TimeLimit;
	public string stageName;
	public ModeManager @base;
	public SpawnManager spawn;

	private void Start()
	{
		if (string.IsNullOrEmpty(stageName))
			return;
		
		@base.isRevivable = isRevivable;
		spawn.isInfinityMode = isInfinity;
		@base.SetModeType(stageModeType);
		UIManager.Get.SetModeType(stageModeType);
		
		LoadMapData();
	}
	
	public MapData LoadMapData()
	{
		var path = $"Assets/Resources/{stageName}_MapData.asset";
		var data = AssetDatabase.LoadAssetAtPath<MapData>(path);
		if (data == null)
		{
			Debug.LogError($"MapData를 찾을 수 없습니다: {path}");
			return null;
		}
		
		GridManager.Instance.SetMapData(data);
		@base.spawnManager.SetUpcomingBlocks(data.upcomingBlocks);
		return data;
	}
}
