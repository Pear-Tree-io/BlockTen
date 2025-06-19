using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

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

    public GameObject destroyVFX;
    [Header("Destroy Highlight")]
    [Tooltip("Color to flash on blocks just before they pop-and-destroy")]
    public Color destroyHighlightColor = Color.red;
    public SpawnManager spawnManager;
    public GameObject currentModeManager;

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

    public void RegisterBlock(NumberBlock block, int x, int y)
    {
        gridBlocks[x, y] = block;
        occupied[x, y] = true;
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
    /// sum can reach exactly 10, and removes them.
    /// </summary>
    public void CheckAndDestroyMatches()
    {
        var runs = new List<List<Vector2Int>>();

        // ── HORIZONTAL ──
        for (int y = 0; y < rows; y++)
        {
            for (int xs = 0; xs < columns; xs++)
            {
                int sum = 0;
                for (int x = xs; x < columns; x++)
                {
                    var b = gridBlocks[x, y];
                    if (b == null) break;

                    sum += b.Value;
                    int len = x - xs + 1;

                    if (len >= 2 && sum == 10)
                    {
                        var run = Enumerable.Range(xs, len)
                                            .Select(i => new Vector2Int(i, y))
                                            .ToList();
                        runs.Add(run);
                        break;
                    }
                    else if (sum > 10)
                    {
                        break;
                    }
                }
            }
        }

        // ── VERTICAL ──
        for (int x = 0; x < columns; x++)
        {
            for (int ys = 0; ys < rows; ys++)
            {
                int sum = 0;
                for (int y = ys; y < rows; y++)
                {
                    var b = gridBlocks[x, y];
                    if (b == null) break;

                    sum += b.Value;
                    int len = y - ys + 1;

                    if (len >= 2 && sum == 10)
                    {
                        var run = Enumerable.Range(ys, len)
                                            .Select(i => new Vector2Int(x, i))
                                            .ToList();
                        runs.Add(run);
                        break;
                    }
                    else if (sum > 10)
                    {
                        break;
                    }
                }
            }
        }


        // 2) Flatten & dedupe to count unique blocks
        var allCoords = new HashSet<Vector2Int>();
        foreach (var run in runs)
            foreach (var coord in run)
                allCoords.Add(coord);

        // <-- use the Count property here:
        int destroyedCount = allCoords.Count;

        // 3) scoring/combo
        currentModeManager.GetComponent<ModeManager>().OnBlocksDestroyed(destroyedCount);

        if (destroyedCount > 0)
        {
            // play your existing pop/VFX coroutine…
            StartCoroutine(PlayDestroySequence(runs));
        }
        else
        {
            // ← re-insert this so that "no clear" also advances the wave
            spawnManager.NotifyBlockPlaced();
        }
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

        var allBlocks = new HashSet<NumberBlock>();

        // 1) Animate each run in order
        foreach (var run in allRuns)
        {
            foreach (var b in run)
            {
                allBlocks.Add(b);
                StartCoroutine(PopOne(b, 0.2f));
            }
            yield return new WaitForSeconds(0.3f);
        }

        // 2) Final simultaneous pop
        /*foreach (var b in allBlocks)
            StartCoroutine(PopOne(b, 0.2f));
        yield return new WaitForSeconds(0.25f);*/

        // 3) Spawn VFX, free cells & destroy objects
        foreach (var b in allBlocks)
        {
            Instantiate(destroyVFX, b.transform.position, Quaternion.identity);
            var coord = FindBlockCoords(b);
            occupied[coord.x, coord.y] = false;
            gridBlocks[coord.x, coord.y] = null;
            Destroy(b.gameObject);
        }

        spawnManager.NotifyBlockPlaced();
    }

    /// <summary>
    /// Scales up then back down over totalDuration seconds.
    /// </summary>
    private IEnumerator PopOne(NumberBlock b, float totalDuration)
    {
        // 0) Flash to the destroy color immediately
        b.SetColor(destroyHighlightColor);
        b.spriteRenderer.sortingOrder = 3;
        b.ValueText.sortingOrder = 4;

        float half = totalDuration * 0.5f;
        // scale up
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            b.transform.localScale = Vector3.one * Mathf.Lerp(1f, 2f, t / half);
            yield return null;
        }
        // scale back
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            b.transform.localScale = Vector3.one * Mathf.Lerp(2f, 1f, t / half);
            yield return null;
        }
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

    /// <summary>
    /// Tests whether the world-space position hits an empty cell,
    /// without modifying occupancy. Returns the grid coords in gx, gy.
    /// </summary>
    public bool CanPlaceCell(Vector3 worldPos, out int gx, out int gy)
    {
        Vector3 local = worldPos - origin;
        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.y / cellSize);
        gx = x; gy = y;

        return x >= 0 && x < columns
            && y >= 0 && y < rows
            && !occupied[x, y];
    }

    /// <summary>
    /// Returns the world-space center of the cell at grid coords (x,y).
    /// </summary>
    public Vector3 GetCellCenter(int x, int y)
    {
        return origin + new Vector3(x * cellSize, y * cellSize, 0f);
    }

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
        NumberBlock root = blocks[0];

        // try every free cell for the root
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
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
        Vector2Int[] cardinals = {
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
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                if (occupied[x, y]) return true;
        return false;
    }

    public int CountFreeCells()
    {
        int free = 0;
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                if (!occupied[x, y]) free++;
        return free;
    }

    /// <summary>
    /// Given a temporary placement of some NumberBlocks (mapping block→grid coord),
    /// returns all the horizontal/vertical runs (length≥2) that would sum to 10.
    /// </summary>
    public List<List<Vector2Int>> GetPreviewRuns(
        Dictionary<NumberBlock, Vector2Int> tempPlacement)
    {
        // 1) copy current occupied & values
        int[,] vals = new int[columns, rows];
        bool[,] occ = new bool[columns, rows];
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                if (gridBlocks[x, y] != null)
                {
                    vals[x, y] = gridBlocks[x, y].Value;
                    occ[x, y] = true;
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
        for (int y = 0; y < rows; y++)
        {
            for (int xs = 0; xs < columns; xs++)
            {
                int sum = 0;
                for (int x = xs; x < columns; x++)
                {
                    if (!occ[x, y]) break;
                    sum += vals[x, y];
                    int len = x - xs + 1;
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
        for (int x = 0; x < columns; x++)
        {
            for (int ys = 0; ys < rows; ys++)
            {
                int sum = 0;
                for (int y = ys; y < rows; y++)
                {
                    if (!occ[x, y]) break;
                    sum += vals[x, y];
                    int len = y - ys + 1;
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
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                if (GetBlockAt(x, y) is NumberBlock b)
                    b.StopPreview();
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
}