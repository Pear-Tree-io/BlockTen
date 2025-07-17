using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class StageEditor : MonoBehaviour
{
	public GridManager grid;
	public string stageName;

#if UNITY_EDITOR
	public GameObject objCell;
	public InputAction input;

	private readonly Dictionary<Vector2Int, NumberBlock> _cells = new();

	private void Start()
	{
		input.Enable();
		input.performed += OnInput;
	}

	private void OnInput(InputAction.CallbackContext context)
	{
		if (string.Equals(context.control.name, "-"))
		{
		}
		else if (int.TryParse(context.control.name, out var value))
		{
			var mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
			mouseWorldPos.z = 0f;

			var local = mouseWorldPos - grid.origin;
			var pos = new Vector2Int(
				(int)(Mathf.RoundToInt(local.x / grid.cellSize) * grid.cellSize),
				(int)(Mathf.RoundToInt(local.y / grid.cellSize) * grid.cellSize)
			);

			if (_cells.TryGetValue(pos, out var nb) == false)
			{
				var draggable = Instantiate(objCell, transform).GetComponent<DraggableCompositeBlock>();
				draggable.placed = true;
				nb = draggable.children[0];

				if (grid.SetBlockData(nb, pos))
				{
					_cells[pos] = nb;
					nb.transform.parent.transform.position = grid.GetCellCenter(pos);
				}
			}

			if (value == 0)
				grid.SetBlockRemove(pos);
			else
				nb.Value = value;
		}
	}
#endif

	[Button("Save Block Data", ButtonSizes.Large)]
	public void SaveBlock()
	{
		var path = $"Assets/Resources/{stageName}_MapData.asset";
		if (File.Exists(path))
			AssetDatabase.DeleteAsset(path);

		var asset = ScriptableObject.CreateInstance<MapData>();
		asset.SetBlocks(grid.GetBlocks());
		asset.SetUpcomingBlocks(FindObjectsByType<DraggableCompositeBlock>(FindObjectsSortMode.None).Where(i => i.placed == false).ToArray());

		AssetDatabase.CreateAsset(asset, path);
		EditorUtility.SetDirty(asset);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log("Save Block Data");
	}
}