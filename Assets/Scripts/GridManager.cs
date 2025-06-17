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
    /// non-joker sum + jokers-as-[1��9] can reach exactly 10, and removes them.
    /// </summary>
    public void CheckAndDestroyMatches()
    {
        var toRemove = new HashSet<Vector2Int>();
        var parentTrack = new HashSet<Transform>();

        // ── HORIZONTAL ──
        for (int y = 0; y < rows; y++)
        {
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
                    if (length < 2) continue;

                    if (jokerCnt == 0)
                    {
                        // 조커 없을 때는 실제 합이 10일 때만
                        if (sumNonJ == 10)
                        {
                            for (int k = xs; k <= x; k++)
                                toRemove.Add(new Vector2Int(k, y));
                        }
                    }
                    else
                    {
                        // 조커 있을 때는 기존 범위 검사
                        int minSum = sumNonJ + jokerCnt * 1;
                        int maxSum = sumNonJ + jokerCnt * 9;
                        if (minSum <= 10 && maxSum >= 10)
                        {
                            for (int k = xs; k <= x; k++)
                                toRemove.Add(new Vector2Int(k, y));
                        }
                    }

                    // 범위를 넘으면 더 이상 연장 불필요
                    if (sumNonJ > 10) break;
                }
            }
        }

        // ── VERTICAL ──
        for (int x = 0; x < columns; x++)
        {
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
                    if (length < 2) continue;

                    if (jokerCnt == 0)
                    {
                        if (sumNonJ == 10)
                        {
                            for (int k = ys; k <= y; k++)
                                toRemove.Add(new Vector2Int(x, k));
                        }
                    }
                    else
                    {
                        int minSum = sumNonJ + jokerCnt * 1;
                        int maxSum = sumNonJ + jokerCnt * 9;
                        if (minSum <= 10 && maxSum >= 10)
                        {
                            for (int k = ys; k <= y; k++)
                                toRemove.Add(new Vector2Int(x, k));
                        }
                    }

                    if (sumNonJ > 10) break;
                }
            }
        }

        // ── DESTROY & SCORE ──
        int destroyedCount = toRemove.Count;
        if (destroyedCount > 0)
            ClassicModeManager.Instance.OnBlocksDestroyed(destroyedCount);

        foreach (var p in toRemove)
        {
            var b = gridBlocks[p.x, p.y];
            if (b == null) continue;

            // 1) free the cell
            occupied[p.x, p.y] = false;

            // 2) remove from our lookup
            gridBlocks[p.x, p.y] = null;

            // 3) destroy the GameObject
            Destroy(b.gameObject);
        }

        // DESTROY ANY EMPTY COMPOSITE PARENTS
        foreach (var parent in parentTrack)
            if (parent != null && parent.GetComponentInChildren<NumberBlock>() == null)
                Destroy(parent.gameObject);
    }

    /// <summary>
    /// Returns true if this composite (wired up via neighbor links)
    /// can be placed somewhere on the grid without overlap.
    /// </summary>
    public bool CanPlaceCompositeBlockAnywhere(DraggableCompositeBlock comp)
    {
        // grab whatever NumberBlock instances are under this composite right now
        var blocks = comp.GetComponentsInChildren<NumberBlock>();
        if (blocks == null || blocks.Length == 0)
            return false;

        // choose the first one as our “root” (any will do, since neighbors link the rest)
        NumberBlock root = blocks[0];

        // try every free cell for that root
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (occupied[x, y]) continue;

                // track placements in this trial
                var placed = new Dictionary<NumberBlock, Vector2Int>();
                if (TryPlaceRec(root, x, y, placed))
                    return true;  // fits somewhere!
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
    /// Given a successful placement mapping (block→grid coords),
    /// returns true if adding those blocks would create at least one
    /// adjacent pair summing to 10 (treating jokers as wildcards).
    /// </summary>
    private bool PlacementHasMatch(Dictionary<NumberBlock, Vector2Int> placed)
    {
        // for each newly placed block...
        foreach (var kv in placed)
        {
            var nb = kv.Key;
            var p = kv.Value;

            // check its four neighbors on the EXISTING grid
            foreach (var d in new[]{ Vector2Int.up,
                                 Vector2Int.right,
                                 Vector2Int.down,
                                 Vector2Int.left })
            {
                var np = p + d;
                if (np.x < 0 || np.x >= columns ||
                    np.y < 0 || np.y >= rows) continue;

                var neighbor = gridBlocks[np.x, np.y];
                if (neighbor == null) continue;

                // get fixed sums
                int v1 = nb.IsJoker ? 0 : nb.Value;
                int v2 = neighbor.IsJoker ? 0 : neighbor.Value;
                int jokers = (nb.IsJoker ? 1 : 0) + (neighbor.IsJoker ? 1 : 0);

                // if there's ANY way to reach 10 with jokers as [1..9], we have a match
                int min = v1 + v2 + jokers * 1;
                int max = v1 + v2 + jokers * 9;
                if (min <= 10 && max >= 10)
                    return true;
            }
        }
        return false;
    }

    /// <summary> Returns true if there is at least one block already placed on the grid. </summary>
    public bool HasPlacedBlocks()
    {
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                if (occupied[x, y])
                    return true;
        return false;
    }
}