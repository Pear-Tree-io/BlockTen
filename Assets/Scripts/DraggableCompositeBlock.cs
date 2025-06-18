using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
public class DraggableCompositeBlock : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Color Settings")]
    public Color[] availableColors;

    [Header("Drag Preview")]
    [Tooltip("Color to tint the composite when over a valid drop spot")]
    public Color validDropTint = new Color(0f, 1f, 0f, 0.5f);

    [HideInInspector] public SpawnManager spawnManager;
    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public List<NumberBlock> children;
    [HideInInspector] public bool placed;

    private Camera cam;
    private float screenZ;
    private Vector3 offset;
    
    private bool _isOverValidSpot = false;
    private Color[] _originalColors;

    private void OnEnable()
    {
        // 1) Grab your NumberBlock children
        children = GetComponentsInChildren<NumberBlock>().ToList();

        // 2) Assign numbers & then tint the entire composite
        AssignValidRandomNumbers();
        if (availableColors != null && availableColors.Length > 0)
        {
            var tint = availableColors[Random.Range(0, availableColors.Length)];
            foreach (var nb in children)
                nb.spriteRenderer.color = tint;
        }

        // 3) NOW cache those post-tint colors as your “original” base
        _originalColors = children
            .Select(nb => nb.spriteRenderer.color)
            .ToArray();
    }

    private void Start()
    {
        cam = Camera.main;
        screenZ = cam.WorldToScreenPoint(transform.position).z;
    }

    /// <summary>
    /// Public so SpawnManager can invoke it too.
    /// Keeps re-rolling 1–9 until no internal run (len≥2)
    /// sums exactly to 10.
    /// </summary>
    public void AssignValidRandomNumbers()
    {
        bool invalidRun;
        do
        {
            invalidRun = false;
            // fresh randoms
            foreach (var nb in children)
                nb.AssignRandom();

            // check horizontals
            var seenH = new HashSet<NumberBlock>();
            foreach (var nb in children)
            {
                var head = nb;
                while (head.neighborLeft != null) head = head.neighborLeft;
                if (!seenH.Add(head)) continue;

                var chain = new List<NumberBlock>();
                var cur = head;
                while (cur != null)
                {
                    chain.Add(cur);
                    cur = cur.neighborRight;
                }
                if (ChainHasInvalidSegment(chain))
                {
                    invalidRun = true;
                    break;
                }
            }
            if (invalidRun) continue;

            // check verticals
            var seenV = new HashSet<NumberBlock>();
            foreach (var nb in children)
            {
                var head = nb;
                while (head.neighborDown != null) head = head.neighborDown;
                if (!seenV.Add(head)) continue;

                var chain = new List<NumberBlock>();
                var cur = head;
                while (cur != null)
                {
                    chain.Add(cur);
                    cur = cur.neighborUp;
                }
                if (ChainHasInvalidSegment(chain))
                {
                    invalidRun = true;
                    break;
                }
            }
        } while (invalidRun);
    }

    private bool ChainHasInvalidSegment(List<NumberBlock> chain)
    {
        for (int s = 0; s < chain.Count - 1; s++)
        {
            int sum = 0;
            for (int e = s; e < chain.Count; e++)
            {
                sum += chain[e].Value;
                int len = e - s + 1;
                if (len >= 2 && sum == 10)
                    return true;
                if (sum > 10)
                    break;
            }
        }
        return false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (placed) return;

        // Grow back to full size on grab
        transform.localScale = Vector3.one;
        children.ForEach(i => i.OnDragStart());

        var ps = new Vector3(e.position.x, e.position.y, screenZ);
        offset = transform.position - cam.ScreenToWorldPoint(ps);
    }

    public void OnDrag(PointerEventData e)
    {
        // 1) Move the composite with the cursor
        var screenPt = new Vector3(e.position.x, e.position.y, screenZ);
        transform.position = cam.ScreenToWorldPoint(screenPt) + offset;

        // 2) Build a temp-placement to test “canPlace”
        var temp = new Dictionary<NumberBlock, Vector2Int>();
        bool canPlace = true;
        foreach (var nb in children)
        {
            if (!GridManager.Instance.CanPlaceCell(
                    nb.transform.position, out int gx, out int gy))
            {
                canPlace = false;
                break;
            }
            temp[nb] = new Vector2Int(gx, gy);
        }

        // 3) Always clear previous highlights/tints
        if (_isOverValidSpot)
        {
            // restore composite blocks to their base colors
            for (int i = 0; i < children.Count; i++)
                children[i].spriteRenderer.color = _originalColors[i];

            // clear any grid‐block previews
            GridManager.Instance.ClearAllPreviews();
            _isOverValidSpot = false;
        }

        // 4) If over a valid spot, do both previews
        if (canPlace)
        {
            // 1) GRID PREVIEW: as before
            GridManager.Instance.ClearAllPreviews();
            var runs = GridManager.Instance.GetPreviewRuns(temp);
            foreach (var run in runs)
                foreach (var cell in run)
                    GridManager.Instance.GetBlockAt(cell.x, cell.y)
                                ?.PlayPreview();

            // 2) COMPOSITE PREVIEW: find all cells that would clear
            var matchedCells = new HashSet<Vector2Int>();
            foreach (var run in runs)
                foreach (var cell in run)
                    matchedCells.Add(cell);

            // 3) Tint only those children whose target cell is in matchedCells
            foreach (var nb in children)
            {
                var cell = temp[nb];
                if (matchedCells.Contains(cell))
                    nb.spriteRenderer.color = validDropTint;
                else
                    nb.spriteRenderer.color = _originalColors[children.IndexOf(nb)];
            }

            _isOverValidSpot = true;
        }
    }


    public void OnPointerUp(PointerEventData e)
    {
        // on drop, make sure composite is back to its base tint
        if (_isOverValidSpot)
        {
            for (int i = 0; i < children.Count; i++)
                children[i].spriteRenderer.color = _originalColors[i];
            _isOverValidSpot = false;
        }
        GridManager.Instance.ClearAllPreviews();

        if (placed) return;

        int n = children.Count;
        var gx = new int[n];
        var gy = new int[n];
        bool ok = true;
        var centers = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            if (!GridManager.Instance.CanPlaceCell(
                    children[i].transform.position,
                    out gx[i], out gy[i]))
            {
                ok = false;
                break;
            }
            centers[i] = GridManager.Instance.GetCellCenter(gx[i], gy[i]);
        }

        if (!ok)
        {
            // Failed: snap back and shrink
            transform.position = startPosition;
            transform.localScale = Vector3.one * 0.7f;
            return;
        }

        // 2) ALIGN parent so all children land exactly on their centers
        var localOffset = children[0].transform.localPosition;
        transform.position = centers[0] - localOffset;

        // 3) Register each child into the grid
        for (int i = 0; i < n; i++)
            GridManager.Instance.RegisterBlock(children[i], gx[i], gy[i]);

        // finish drag visuals
        children.ForEach(i => i.OnDragEnd());

        // ——— mark as placed & notify spawner ———
        placed = true;

        // remember this drop pos for the combo popup:
        GridManager.Instance.LastPlacedPosition = transform.position;

        // 4) Clear matches
        GridManager.Instance.CheckAndDestroyMatches();

        // detach blocks and destroy this composite
        foreach (var nb in children)
            nb.transform.SetParent(GridManager.Instance.transform, true);
        Destroy(gameObject);
    }

}
