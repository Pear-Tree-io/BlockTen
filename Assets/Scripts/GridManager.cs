// GridManager.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages an NxM grid, tracks block placement, frees cells, checks for matches, and handles game over.
/// </summary>
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

        // Initialize grid arrays early so HasFreeSlots can be called safely
        occupied = new bool[columns, rows];
        gridBlocks = new NumberBlock[columns, rows];
    }

    private void Start()
    {
        // Center the grid around this GameObject
        float w = columns * cellSize;
        float h = rows * cellSize;
        origin = transform.position - new Vector3(
            w / 2f - cellSize / 2f,
            h / 2f - cellSize / 2f,
            0f
        );

        // Initialize grid state and instantiate cell placeholders
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                occupied[x, y] = false;
                gridBlocks[x, y] = null;
                Instantiate(
                    cellPrefab,
                    origin + new Vector3(x * cellSize, y * cellSize, 0f),
                    Quaternion.identity,
                    transform
                );
            }
        }
    }

    /// <summary>
    /// Attempts to occupy the nearest cell to worldPos.
    /// </summary>
    public bool TryPlaceCell(Vector3 worldPos, out Vector3 cellCenter, out int gx, out int gy)
    {
        Vector3 local = worldPos - origin;
        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.y / cellSize);
        gx = x; gy = y;

        if (x >= 0 && x < columns && y >= 0 && y < rows && !occupied[x, y])
        {
            occupied[x, y] = true;
            cellCenter = origin + new Vector3(x * cellSize, y * cellSize, 0f);
            return true;
        }

        cellCenter = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Registers a block in the grid at (x, y).
    /// </summary>
    public void RegisterBlock(NumberBlock block, int x, int y)
    {
        gridBlocks[x, y] = block;
        occupied[x, y] = true;
    }

    /// <summary>
    /// Frees the cell at (x, y).
    /// </summary>
    public void FreeCell(int x, int y)
    {
        if (x >= 0 && x < columns && y >= 0 && y < rows)
        {
            occupied[x, y] = false;
            gridBlocks[x, y] = null;
        }
    }

    /// <summary>
    /// Returns true if at least 'count' slots are free.
    /// </summary>
    public bool HasFreeSlots(int count)
    {
        int freeCount = 0;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!occupied[x, y] && ++freeCount >= count)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Logs game over if there aren't enough free slots.
    /// </summary>
    public void CheckGameOver(int requiredSlots)
    {
        if (!HasFreeSlots(requiredSlots))
        {
            Debug.Log("Game Over: No more space to place blocks.");
            // TODO: Trigger your Game Over sequence here
        }
    }

    /// <summary>
    /// Clears any horizontal or vertical sequences summing to 10 and removes empty parents.
    /// </summary>
    public void CheckAndDestroyMatches()
    {
        var toRemove = new HashSet<Vector2Int>();
        var parentTrack = new HashSet<Transform>();

        // Horizontal
        for (int y = 0; y < rows; y++)
        {
            for (int xs = 0; xs < columns; xs++)
            {
                int sum = 0;
                for (int x = xs; x < columns; x++)
                {
                    var nb = gridBlocks[x, y];
                    if (nb == null) break;
                    sum += nb.Value;
                    if (sum == 10 && x > xs)
                    {
                        for (int k = xs; k <= x; k++)
                            toRemove.Add(new Vector2Int(k, y));
                    }
                    if (sum >= 10) break;
                }
            }
        }

        // Vertical
        for (int x = 0; x < columns; x++)
        {
            for (int ys = 0; ys < rows; ys++)
            {
                int sum = 0;
                for (int y = ys; y < rows; y++)
                {
                    var nb = gridBlocks[x, y];
                    if (nb == null) break;
                    sum += nb.Value;
                    if (sum == 10 && y > ys)
                    {
                        for (int k = ys; k <= y; k++)
                            toRemove.Add(new Vector2Int(x, k));
                    }
                    if (sum >= 10) break;
                }
            }
        }

        // Destroy blocks
        foreach (var p in toRemove)
        {
            var block = gridBlocks[p.x, p.y];
            if (block != null)
            {
                if (block.transform.parent != null)
                    parentTrack.Add(block.transform.parent);
                occupied[p.x, p.y] = false;
                gridBlocks[p.x, p.y] = null;
                Destroy(block.gameObject);
            }
        }

        // Remove empty parents
        foreach (var parent in parentTrack)
        {
            if (parent != null && parent.GetComponentInChildren<NumberBlock>() == null)
                Destroy(parent.gameObject);
        }
    }
}
