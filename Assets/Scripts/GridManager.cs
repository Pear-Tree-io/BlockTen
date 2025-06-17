using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
	public static GridManager Instance { get; private set; }

	[Header("Grid Settings")]
	public int rows = 8;
	public int columns = 8;
	public float cellSize = 1f;
	public GameObject cellPrefab;

	private NumberBlock[,] gridBlocks;
	private bool[,] occupied;
	private Vector3 origin;

	private void Awake()
	{
		if (Instance != null && Instance != this)
			Destroy(gameObject);
		else
			Instance = this;

		occupied = new bool[columns, rows];
		gridBlocks = new NumberBlock[columns, rows];
	}

	private void Start()
	{
		float w = columns * cellSize;
		float h = rows * cellSize;
		origin = transform.position
		         - new Vector3(w / 2f - cellSize / 2f,
			         h / 2f - cellSize / 2f,
			         0f);

		for (int x = 0; x < columns; x++)
		for (int y = 0; y < rows; y++)
		{
			occupied[x, y] = false;
			gridBlocks[x, y] = null;
			Instantiate(cellPrefab,
				origin + new Vector3(x * cellSize, y * cellSize, 0f),
				Quaternion.identity,
				transform);
		}
	}

    public bool TryPlaceCell(Vector3 worldPos, out Vector3 cellCenter, out int gx, out int gy)
	{
		Vector3 local = worldPos - origin;
		int x = Mathf.RoundToInt(local.x / cellSize);
		int y = Mathf.RoundToInt(local.y / cellSize);
		gx = x;
		gy = y;

		if (x >= 0 && x < columns && y >= 0 && y < rows && !occupied[x, y])
		{
			occupied[x, y] = true;
			cellCenter = origin + new Vector3(x * cellSize, y * cellSize, 0f);
			return true;
		}

		cellCenter = Vector3.zero;
		return false;
	}

	public void RegisterBlock(NumberBlock block, int x, int y)
	{
		gridBlocks[x, y] = block;
		occupied[x, y] = true;
	}

	public void FreeCell(int x, int y)
	{
		if (x >= 0 && x < columns && y >= 0 && y < rows)
		{
			occupied[x, y] = false;
			gridBlocks[x, y] = null;
		}
	}

	public bool HasFreeSlots(int count)
	{
		int freeCount = 0;
		for (int x = 0; x < columns; x++)
		for (int y = 0; y < rows; y++)
			if (!occupied[x, y] && ++freeCount >= count)
				return true;
		return false;
	}

	/// <summary>
	/// Scans horizontal & vertical straight segments of 2+ blocks where
	/// non-joker sum + jokers-as-[1��9] can reach exactly 10, and removes them.
	/// </summary>
	public void CheckAndDestroyMatches()
	{
		var toRemove = new HashSet<Vector2Int>();
		var parentTrack = new HashSet<Transform>();

		// HORIZONTAL
		for (int y = 0; y < rows; y++)
		for (int xs = 0; xs < columns; xs++)
		{
			int sumNonJ = 0;
			int jokerCnt = 0;

			for (int x = xs; x < columns; x++)
			{
				var b = gridBlocks[x, y];
				if (b == null) break;

				if (b.IsJoker) jokerCnt++;
				else sumNonJ += b.Value;

				int length = x - xs + 1;
				int minSum = sumNonJ + jokerCnt * 1;
				int maxSum = sumNonJ + jokerCnt * 9;

				if (minSum > 10)
					break;

				if (length >= 2 && minSum <= 10 && maxSum >= 10)
				{
					for (int k = xs; k <= x; k++)
						toRemove.Add(new Vector2Int(k, y));
					break;
				}
			}
		}

		// VERTICAL
		for (int x = 0; x < columns; x++)
		for (int ys = 0; ys < rows; ys++)
		{
			int sumNonJ = 0;
			int jokerCnt = 0;

			for (int y = ys; y < rows; y++)
			{
				var b = gridBlocks[x, y];
				if (b == null) break;

				if (b.IsJoker) jokerCnt++;
				else sumNonJ += b.Value;

				int length = y - ys + 1;
				int minSum = sumNonJ + jokerCnt * 1;
				int maxSum = sumNonJ + jokerCnt * 9;

				if (minSum > 10)
					break;

				if (length >= 2 && minSum <= 10 && maxSum >= 10)
				{
					for (int k = ys; k <= y; k++)
						toRemove.Add(new Vector2Int(x, k));
					break;
				}
			}
		}

		// DESTROY MARKED BLOCKS
		foreach (var p in toRemove)
		{
			var b = gridBlocks[p.x, p.y];
			if (b == null) continue;
			parentTrack.Add(b.transform.parent);
			occupied[p.x, p.y] = false;
			gridBlocks[p.x, p.y] = null;
			Destroy(b.gameObject);
		}

		// ���� CALL SCORING ����
		// ClassicModeManager.Instance.OnBlocksDestroyed(toRemove.Count);

		// DESTROY ANY EMPTY COMPOSITE PARENTS
		foreach (var parent in parentTrack)
			if (parent != null && parent.GetComponentInChildren<NumberBlock>() == null)
				Destroy(parent.gameObject);
	}

	public bool CanPlaceCompositeBlockAnywhere(DraggableCompositeBlock block)
	{
		var children = block.children;
		var localOffsets = new List<Vector2Int>();

		foreach (var child in children)
		{
			var local = child.transform.localPosition;
			int dx = Mathf.RoundToInt((local.x + 0.0001f) / cellSize);
			var dy = Mathf.RoundToInt((local.y + 0.0001f) / cellSize);
			localOffsets.Add(new(dx, dy));
		}

		for (var x = 0; x < columns; x++)
		{
			for (var y = 0; y < rows; y++)
			{
				var canPlace = true;

				foreach (var offset in localOffsets)
				{
					var tx = x + offset.x;
					var ty = y + offset.y;

					if (tx < 0 || tx >= columns || ty < 0 || ty >= rows || occupied[tx, ty])
					{
						canPlace = false;
						break;
					}
				}

				if (canPlace)
					return true;
			}
		}

		return false;
	}
}