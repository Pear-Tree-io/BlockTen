using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapEditor : MonoBehaviour
{
	public GridManager grid;
	public GameObject objCell;
	public string mapName;
	public InputAction input;

	private void Start()
	{
		input.Enable();
		input.performed += OnInput;
	}

	private List<GameObject> _cells = new();
	
	private void OnInput(InputAction.CallbackContext context)
	{
		if (int.TryParse(context.control.name, out var value))
		{
			var go = Instantiate(objCell, transform);
			var nb = go.GetComponentInChildren<NumberBlock>();
			if (GridManager.Instance.SetBlockData(nb, out var pos))
			{
				go.transform.position = GridManager.Instance.GetCellCenter(pos);
				nb.EditorValue = value;
			}
		}
	}

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