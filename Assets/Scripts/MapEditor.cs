using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapEditor : MonoBehaviour
{
	public GridManager grid;
	public string mapName;
	public InputAction input;

	[Button("Save Block Data", ButtonSizes.Large)]
	public void SaveBlock()
	{
#if UNITY_EDITOR
		var asset = ScriptableObject.CreateInstance<MapData>();
		asset.blocks = grid.GetBlocks();

		var path = $"Assets/Resources/{mapName}_MapData.asset";
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		AssetDatabase.CreateAsset(asset, path);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Save Block Data");
#endif
	}
}