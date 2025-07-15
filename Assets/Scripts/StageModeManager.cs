using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class StageModeManager : MonoBehaviour
{
	public string stageName;
	
	private ModeManager @base;

	private void Start()
	{
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
		
		return data;
	}
}
