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

    [HideInInspector] public SpawnManager spawnManager;
    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public List<NumberBlock> children;
    [HideInInspector] public bool placed;

    private Camera cam;
    private float screenZ;
    private Vector3 offset;
    private float dragStartWorldY = 0;
    private float yOffsetFactor = 0.75f;

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
            {
                nb.spriteRenderer.color = tint;
                nb.OriginalColor = tint;
            }      
        }
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

        AudioManager.Instance.PlaySFX(SFXType.pickUp);
        // Grow back to full size on grab
        transform.localScale = Vector3.one;

        transform.position += new Vector3(0, 3, 0);
        children.ForEach(i => i.OnDragStart());

        var ps = new Vector3(e.position.x, e.position.y, screenZ);
        offset = transform.position - cam.ScreenToWorldPoint(ps);

        // store the starting world Y so we know our baseline
        dragStartWorldY = transform.position.y;
    }

    public void OnDrag(PointerEventData e)
    {
        // 1) Move the composite with the cursor
        Vector3 screenPt = new Vector3(e.position.x, e.position.y, screenZ);
        Vector3 worldPt = cam.ScreenToWorldPoint(screenPt);

        // 2) dynamic extra Y based on how far above the start Y we are
        float extraY = Mathf.Max(0f, worldPt.y - dragStartWorldY) * yOffsetFactor;
        Vector3 dynamicOffset = offset + Vector3.up * extraY;

        // 3) move the composite
        transform.position = worldPt + dynamicOffset;

        // 1) clear any old shadows
        GridManager.Instance.ClearShadows();

        // 2) Try placing the whole composite in one shot
        if (!GridManager.Instance.TryPlaceCompositeAt(this, out var placement))
            return;

        // 3) show shadows at each spot this composite would occupy
        var landingCells = placement.Values;        // each Vector2Int(x,y)
        GridManager.Instance.ShowShadows(landingCells);


        /*// 3) Figure out which cells would clear if dropped here
        var runs = GridManager.Instance.GetPreviewRuns(placement);
        var matchedCells = new HashSet<Vector2Int>(runs.SelectMany(r => r));

        // 4) Clear any old previews
        GridManager.Instance.ClearAllPreviews();
        foreach (var nb in children)
            nb.StopPreview();

        // 5) Show new previews:
        //  • Grid cells in any run
        foreach (var run in runs)
            foreach (var cell in run)
                GridManager.Instance.GetBlockAt(cell.x, cell.y)
                                    ?.PlayPreview();
        //  • Composite blocks whose target cell is in a run
        foreach (var nb in children)
        {
            var cell = placement[nb];
            if (matchedCells.Contains(cell))
                nb.PlayPreview();
            else
                nb.StopPreview();
        }*/
    }

    public void OnPointerUp(PointerEventData e)
    {
        // Clear any running preview animations on the grid
        GridManager.Instance.ClearAllPreviews();
        // 1) clear any old shadows
        GridManager.Instance.ClearShadows();

        // 0) First: stop *all* play‐preview on the dragged blocks
        foreach (var nb in children)
            nb.StopPreview();

        if (placed) return;

        // new:
        if (!GridManager.Instance.TryPlaceCompositeAt(this, out var placedGrid))
        {
            // snap back on failure
            transform.position = startPosition;
            transform.localScale = Vector3.one * 0.7f;
            return;
        }

        // align the composite so its first child hits exactly its cell center
        var first = children[0];
        var firstGrid = placedGrid[first];
        var targetPos = GridManager.Instance.GetCellCenter(firstGrid.x, firstGrid.y);
        transform.position = targetPos - first.transform.localPosition;

        // register every block in their mapped cell
        foreach (var kv in placedGrid)
            GridManager.Instance.RegisterBlock(kv.Key, kv.Value.x, kv.Value.y);

        // finish the drop
        children.ForEach(i => i.OnDragEnd());
        placed = true;
        GridManager.Instance.LastPlacedPosition = transform.position;
        GridManager.Instance.CheckAndDestroyMatches();
        foreach (var nb in children)
            nb.transform.SetParent(GridManager.Instance.transform, true);
        Destroy(gameObject);

        AudioManager.Instance.PlaySFX(SFXType.PlaceBlock);
    }
}
