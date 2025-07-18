using System;
using UnityEngine;
using UnityEngine.SceneManagement;

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
	public static StageModeManager Get { get; private set; }
	public string stageName;
	private int _stageIndex;
	public ModeManager @base;
	public SpawnManager spawn;

	private void Awake()
	{
		Get = this;
	}

	private void Start()
	{
		if (string.IsNullOrEmpty(stageName))
			return;

		@base.isRevivable = isRevivable;
		spawn.isInfinityMode = isInfinity;
		UIManager.Get.SetModeType(stageModeType);

		LoadMapData();
	}

	private bool _isFinding = false;

	public MapData LoadMapData()
	{
		while (true)
		{
			var data = Resources.Load<MapData>($"{stageName}_{_stageIndex}_MapData");
			if (data == null)
			{
				if (_isFinding == false)
				{
					_isFinding = true;
					_stageIndex++;
					Debug.LogError($"MapData를 찾을 수 없습니다: {stageName}");
					continue;
				}

				return null;
			}

			GridManager.Instance.SetMapData(data);
			@base.spawnManager.SetUpcomingBlocks(data.upcomingBlocks);
			@base.SetModeType(stageModeType);
			return data;
		}
	}

	public void NextStage()
	{
		_stageIndex++;
		_isFinding = true;

		if (LoadMapData() == null)
			SceneManager.LoadScene("Menu");
	}

	public void ResetStage()
	{
		LoadMapData();
	}
}