using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
	public static GridManager Instance { get; private set; }

	[Header("Grid Settings")]
	public int rows = 8;
	public int columns = 8;
	public float cellSize = 1f;
	public GameObject cellPrefab;
	public GameObject blockPrefab;

	private NumberBlock[,] gridBlocks;
	private bool[,] occupied;
	public Vector3 origin { get; private set; }

	public GameObject destroyVFX;
	[Header("Destroy Highlight")]
	[Tooltip("Color to flash on blocks just before they pop-and-destroy")]
	public Color destroyHighlightColor = Color.red;
	public SpawnManager spawnManager;
	public ModeManager currentModeManager;
	[SerializeField]
	private GameObject inputBlocker;

	public bool isPopup = false;

    [Header("Preview Shadows")]
    public GameObject shadowPrefab;             // assign a semi-transparent sprite prefab
    private SpriteRenderer[,] shadows;          // one per cell

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
		var w = columns * cellSize;
		var h = rows * cellSize;
		origin = transform.position - new Vector3(w / 2f - cellSize / 2f, h / 2f - cellSize / 2f, 0f);

        // 1) Pre-spawn all shadows
        shadows = new SpriteRenderer[columns, rows];
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                var go = Instantiate(
                    shadowPrefab,
                    GetCellCenter(x, y),
                    Quaternion.identity,
                    transform        // parent under your grid
                );
                var sr = go.GetComponent<SpriteRenderer>();
                sr.enabled = false;              // start hidden
                shadows[x, y] = sr;
            }

        // for (int x = 0; x < columns; x++)
        //     for (int y = 0; y < rows; y++)
        //     {
        //         occupied[x, y] = false;
        //         gridBlocks[x, y] = null;
        //         Instantiate(cellPrefab,
        //             origin + new Vector3(x * cellSize, y * cellSize, 0f),
        //             Quaternion.identity,
        //             transform);
        //     }
    }

	public void RegisterBlock(NumberBlock block, int x, int y)
	{
		gridBlocks[x, y] = block;
		occupied[x, y] = true;
	}

	public bool HasFreeSlots(int count)
	{
		var freeCount = 0;
		for (var x = 0; x < columns; x++)
		{
			for (var y = 0; y < rows; y++)
			{
				if (!occupied[x, y] && ++freeCount >= count)
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Scans horizontal & vertical straight segments of 2+ blocks where
	/// sum can reach exactly 10, and removes them.
	/// </summary>
	public void CheckAndDestroyMatches()
	{
		inputBlocker.SetActive(true);

		var runs = new List<List<Vector2Int>>();

		// ── HORIZONTAL ──
		for (var y = 0; y < rows; y++)
		{
			for (var xs = 0; xs < columns; xs++)
			{
				var sum = 0;
				for (var x = xs; x < columns; x++)
				{
					var b = gridBlocks[x, y];
					if (b == null) break;

					sum += b.Value;
					var len = x - xs + 1;

					if (len >= 2 && sum == 10)
					{
						var run = Enumerable.Range(xs, len)
							.Select(i => new Vector2Int(i, y))
							.ToList();
						runs.Add(run);
						break;
					}

					if (sum > 10)
					{
						break;
					}
				}
			}
		}

		// ── VERTICAL ──
		for (var x = 0; x < columns; x++)
		{
			for (var ys = 0; ys < rows; ys++)
			{
				var sum = 0;
				for (var y = ys; y < rows; y++)
				{
					var b = gridBlocks[x, y];
					if (b == null) break;

					sum += b.Value;
					var len = y - ys + 1;

					if (len >= 2 && sum == 10)
					{
						var run = Enumerable.Range(ys, len)
							.Select(i => new Vector2Int(x, i))
							.ToList();
						runs.Add(run);
						break;
					}

					if (sum > 10)
					{
						break;
					}
				}
			}
		}

		// 2) Flatten & dedupe to count unique blocks
		var allCoords = new HashSet<Vector2Int>();
		foreach (var run in runs)
		{
			foreach (var coord in run)
			{
				allCoords.Add(coord);
			}
		}

		// <-- use the Count property here:
		var destroyedCount = allCoords.Count;
		var matchedCount = runs.Count;

		//currentModeManager.GetComponent<ModeManager>().OnBlocksDestroyed(destroyedCount);

		if (destroyedCount > 0)
		{
			// play your existing pop/VFX coroutine…
			StartCoroutine(PlayDestroySequence(runs));
		}
		else
		{
			// ← re-insert this so that "no clear" also advances the wave
			spawnManager.NotifyBlockPlaced();
			inputBlocker.SetActive(false);
		}

		// 3) scoring/combo
		currentModeManager.OnMatchBlocksDestroyed(matchedCount, destroyedCount);
	}

	private IEnumerator PlayDestroySequence(List<List<Vector2Int>> runs)
	{
		// Convert runs of coords → runs of NumberBlock
		var allRuns = runs
			.Select(run => run
				.Select(p => gridBlocks[p.x, p.y])
				.Where(b => b != null)
				.ToList()
			)
			.ToList();

		// Flatten into a unique set
		var allBlocks = new HashSet<NumberBlock>();
		foreach (var run in allRuns)
		foreach (var b in run)
			allBlocks.Add(b);

		// 0) **Compute the geometric center** of all the blocks to be destroyed:
		var center = Vector3.zero;
		foreach (var b in allBlocks)
		{
			center += b.transform.position;
		}
		center /= allBlocks.Count;
		// stash it so ModeManager.ShowTextOnCanvas uses THIS position
		LastPlacedPosition = center;

		if (isPopup)
		{
			// 1) Animate each run in order
			foreach (var run in allRuns)
			{
				foreach (var b in run)
				{
					StartCoroutine(PopOne(b, 0.3f));
				}

				yield return new WaitForSeconds(0.4f);
			}
		}
		else
		{
            // 1) Animate each run in order
            foreach (var run in allRuns)
            {
                foreach (var b in run)
                {
                    StartCoroutine(PopOne(b, 0.2f));
                }
            }
            yield return new WaitForSeconds(0.3f);
        }

		// 2) Spawn VFX, free cells & destroy objects
		foreach (var b in allBlocks)
		{
			Instantiate(destroyVFX, b.transform.position, Quaternion.identity);
			var coord = FindBlockCoords(b);
			occupied[coord.x, coord.y] = false;
			gridBlocks[coord.x, coord.y] = null;
			Destroy(b.gameObject);
		}
		AudioManager.Instance.PlaySFX(SFXType.brickBreak);

		spawnManager.NotifyBlockPlaced();
		inputBlocker.SetActive(false);
	}

	/// <summary>
	/// Scales up then back down over totalDuration seconds.
	/// </summary>
	private IEnumerator PopOne(NumberBlock b, float totalDuration)
	{
		if (b?.gameObject == false)
			yield break;

		b.SetColor(destroyHighlightColor);
		b.spriteRenderer.sortingOrder = 3;
		b.ValueText.sortingOrder = 4;

		var half = totalDuration * 0.5f;

		for (float t = 0; t < half; t += Time.deltaTime)
		{
			if (b.gameObject == false)
				yield break;

			b.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, t / half);
			yield return null;
		}

		for (float t = 0; t < half; t += Time.deltaTime)
		{
			if (b.gameObject == false)
				yield break;

			b.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, t / half);
			yield return null;
		}

		if (b.gameObject == false)
			b.transform.localScale = Vector3.one;
	}

    // Helper to find a block's grid coords
    private Vector2Int FindBlockCoords(NumberBlock b)
	{
		for (int x = 0; x < columns; x++)
		for (int y = 0; y < rows; y++)
			if (gridBlocks[x, y] == b)
				return new Vector2Int(x, y);
		return Vector2Int.zero;
	}

	/// <summary>
	/// Returns true if this composite (wired up via neighbor links)
	/// can be placed somewhere on the grid without overlap.
	/// </summary>
	public bool CanPlaceCompositeBlockAnywhere(DraggableCompositeBlock comp)
	{
		var blocks = comp.GetComponentsInChildren<NumberBlock>();
		foreach (var root in blocks)
		{
			for (int x = 0; x < columns; x++)
			for (int y = 0; y < rows; y++)
			{
				if (occupied[x, y]) continue;

				var placed = new Dictionary<NumberBlock, Vector2Int>();
				if (TryPlaceRec(root, x, y, placed))
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Recursively attempts to place 'node' at (gx,gy) and all its neighbors.
	/// Returns false immediately if any neighbor link would land out of bounds
	/// or on an already occupied cell.
	/// </summary>
	private bool TryPlaceRec(
		NumberBlock node,
		int gx,
		int gy,
		Dictionary<NumberBlock, Vector2Int> placed
	)
	{
		// if already placed, ensure it's consistent
		if (placed.TryGetValue(node, out var prev))
			return prev.x == gx && prev.y == gy;

		// out of bounds or occupied?
		if (gx < 0 || gx >= columns ||
		    gy < 0 || gy >= rows ||
		    occupied[gx, gy])
			return false;

		// mark this block in our trial map
		placed[node] = new Vector2Int(gx, gy);

		// follow each neighbor link, aborting on failure
		if (node.neighborRight != null &&
		    !TryPlaceRec(node.neighborRight, gx + 1, gy, placed))
			return false;
		if (node.neighborLeft != null &&
		    !TryPlaceRec(node.neighborLeft, gx - 1, gy, placed))
			return false;
		if (node.neighborUp != null &&
		    !TryPlaceRec(node.neighborUp, gx, gy + 1, placed))
			return false;
		if (node.neighborDown != null &&
		    !TryPlaceRec(node.neighborDown, gx, gy - 1, placed))
			return false;

		return true;
	}

	public bool TryPlaceCompositeAt(
		DraggableCompositeBlock comp,
		out Dictionary<NumberBlock, Vector2Int> placed)
	{
		placed = new Dictionary<NumberBlock, Vector2Int>();

		// grab all the child blocks & pick a “root”
		var blocks = comp.GetComponentsInChildren<NumberBlock>();
		if (blocks.Length == 0) return false;
		var root = blocks[0];

		// figure out which cell the root is over
		if (!CanPlaceCell(root.transform.position, out int gx, out int gy))
			return false;

		// try the recursive placement
		if (!TryPlaceRec(root, gx, gy, placed))
		{
			placed = null;
			return false;
		}
		return true;
	}

	/// <summary>
	/// Tests whether the world-space position hits an empty cell,
	/// without modifying occupancy. Returns the grid coords in gx, gy.
	/// </summary>
	private bool CanPlaceCell(Vector3 worldPos, out int gx, out int gy)
	{
		var local = worldPos - origin;
		var x = Mathf.RoundToInt(local.x / cellSize);
		var y = Mathf.RoundToInt(local.y / cellSize);
		gx = x;
		gy = y;

		return x >= 0 && x < columns
		              && y >= 0 && y < rows
		              && !occupied[x, y];
	}

	/// <summary>
	/// Returns the world-space center of the cell at grid coords (x,y).
	/// </summary>
	///
	
	public Vector3 GetCellCenter(Vector2Int pos) => GetCellCenter(pos.x, pos.y);
	public Vector3 GetCellCenter(int x, int y) => origin + new Vector3(x * cellSize, y * cellSize, 0f);

	/// <summary>
	/// Looks for a placement of this composite (by walking neighbor links)
	/// that would also create at least one horizontal or vertical sum-10 match
	/// against the existing grid. Returns true if found.
	/// </summary>
	public bool TryFindPlacementThatMatchesSum10(DraggableCompositeBlock comp)
	{
		// get fresh children list and root
		var blocks = comp.GetComponentsInChildren<NumberBlock>();
		if (blocks.Length == 0) return false;
		var root = blocks[0];

		// try every free cell for the root
		for (var x = 0; x < columns; x++)
		{
			for (var y = 0; y < rows; y++)
			{
				if (occupied[x, y]) continue;
				var placed = new Dictionary<NumberBlock, Vector2Int>();
				if (!TryPlaceRec(root, x, y, placed)) continue;

				// if that placement mapping yields a sum-10, we're done
				if (PlacementHasMatch(placed))
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns true if placing these new blocks would create any adjacent
	/// pair whose values sum exactly to 10.  Only checks up/right/down/left.
	/// </summary>
	private bool PlacementHasMatch(Dictionary<NumberBlock, Vector2Int> placed)
	{
		// Only these four directions—no diagonals!
		Vector2Int[] cardinals =
		{
			Vector2Int.up,
			Vector2Int.right,
			Vector2Int.down,
			Vector2Int.left
		};

		foreach (var kv in placed)
		{
			var nb = kv.Key;
			var p = kv.Value;

			foreach (var d in cardinals)
			{
				var np = p + d;
				// bounds check
				if (np.x < 0 || np.x >= columns ||
				    np.y < 0 || np.y >= rows)
					continue;

				var neighbor = gridBlocks[np.x, np.y];
				if (neighbor == null) continue;

				if (nb.Value + neighbor.Value == 10)
					return true;
			}
		}

		return false;
	}

	public bool HasPlacedBlocks()
	{
		for (var x = 0; x < columns; x++)
		{
			for (var y = 0; y < rows; y++)
			{
				if (occupied[x, y])
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Given a temporary placement of some NumberBlocks (mapping block→grid coord),
	/// returns all the horizontal/vertical runs (length≥2) that would sum to 10.
	/// </summary>
	public List<List<Vector2Int>> GetPreviewRuns(Dictionary<NumberBlock, Vector2Int> tempPlacement)
	{
		// 1) copy current occupied & values
		var vals = new int[columns, rows];
		var occ = new bool[columns, rows];
		for (var x = 0; x < columns; x++)
		{
			for (var y = 0; y < rows; y++)
			{
				if (gridBlocks[x, y] != null)
				{
					vals[x, y] = gridBlocks[x, y].Value;
					occ[x, y] = true;
				}
			}
		}

		// 2) overlay tempPlacement
		foreach (var kv in tempPlacement)
		{
			var pos = kv.Value;
			vals[pos.x, pos.y] = kv.Key.Value;
			occ[pos.x, pos.y] = true;
		}

		// 3) scan exactly like CheckAndDestroyMatches but collect runs
		var runs = new List<List<Vector2Int>>();

		// horiz
		for (var y = 0; y < rows; y++)
		{
			for (var xs = 0; xs < columns; xs++)
			{
				var sum = 0;
				for (var x = xs; x < columns; x++)
				{
					if (!occ[x, y]) break;
					sum += vals[x, y];
					var len = x - xs + 1;
					if (len >= 2 && sum == 10)
					{
						runs.Add(Enumerable.Range(xs, len)
							.Select(i => new Vector2Int(i, y))
							.ToList());
						break;
					}
					else if (sum > 10) break;
				}
			}
		}

		// vert
		for (var x = 0; x < columns; x++)
		{
			for (var ys = 0; ys < rows; ys++)
			{
				var sum = 0;
				for (var y = ys; y < rows; y++)
				{
					if (!occ[x, y]) break;
					sum += vals[x, y];
					var len = y - ys + 1;
					if (len >= 2 && sum == 10)
					{
						runs.Add(Enumerable.Range(ys, len)
							.Select(i => new Vector2Int(x, i))
							.ToList());
						break;
					}
					else if (sum > 10) break;
				}
			}
		}

		return runs;
	}

	public void ClearAllPreviews()
	{
		for (var x = 0; x < columns; x++)
		{
			for (var y = 0; y < rows; y++)
			{
				if (GetBlockAt(x, y) is NumberBlock b)
					b.StopPreview();
			}
		}
	}

	/// <summary>
	/// Set by DraggableCompositeBlock just before we run match‐checking.
	/// </summary>
	public Vector3 LastPlacedPosition { get; set; }

	/// <summary>
	/// Returns the NumberBlock at (x,y), or null if out of bounds or empty.
	/// </summary>
	public NumberBlock GetBlockAt(int x, int y)
	{
		if (x < 0 || x >= columns || y < 0 || y >= rows)
			return null;
		return gridBlocks[x, y];
	}

    /// <summary>
    /// Turns on the shadow at each given cell, and hides the rest.
    /// </summary>
    public void ShowShadows(IEnumerable<Vector2Int> cells)
    {
        ClearShadows();
        foreach (var c in cells)
        {
            if (c.x >= 0 && c.x < columns && c.y >= 0 && c.y < rows)
                shadows[c.x, c.y].enabled = true;
        }
    }

    /// <summary>
    /// Hide all preview shadows.
    /// </summary>
    public void ClearShadows()
    {
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                shadows[x, y].enabled = false;
    }

    [Header("End Grid Initialization Settings")]
	[SerializeField]
	private float initEndDelay = 0.05f;
	[SerializeField]
	private GameObject gameOverTile;

	/// <summary>
	/// Initializes the grid with an animated fill from bottom to top.
	/// </summary>
	public void InitializeEndGrid()
	{
		StartCoroutine(InitializeEndGridRoutine());
		AudioManager.Instance.PlaySFX(currentModeManager.isHighScore ? SFXType.GameOverHighScore : SFXType.GameOver);
	}

	private IEnumerator InitializeEndGridRoutine()
	{
		// Delay between instantiating each cell
		for (var y = 0; y < rows; y++)
		{
			for (var x = 0; x < columns; x++)
			{
				var worldPos = origin + new Vector3(x * cellSize, y * cellSize, 0f);
				Instantiate(gameOverTile, worldPos, Quaternion.identity, transform);
			}
			yield return new WaitForSeconds(initEndDelay);
		}
	}

	public NumberBlock[,] GetBlocks() => gridBlocks;

	public bool SetBlockData(NumberBlock nb, Vector2Int pos)
	{
		if (pos.x < 0 || pos.x >= columns || pos.y < 0 || pos.y >= rows)
			return false;
		
		RegisterBlock(nb, pos.x, pos.y);
		return true;
	}

	public void SetBlockRemove(Vector2Int pos)
	{
		if (pos.x < 0 || pos.x >= columns || pos.y < 0 || pos.y >= rows)
			return;
		
		gridBlocks[pos.x, pos.y] = null;
		occupied[pos.x, pos.y] = false;
	}

	public void SetMapData(MapData data)
	{
		foreach (var block in data.blocks)
		{
			if (block is not { value: > 0 })
				continue;
			
			var obj = Instantiate(blockPrefab, GetCellCenter(block.x, block.y), Quaternion.identity);
			obj.GetComponent<DraggableCompositeBlock>().placed = true;
			var nb = obj.GetComponentInChildren<NumberBlock>();
			nb.EditorValue = block.value;
			SetBlockData(nb, new(block.x, block.y));
		}
	}
}