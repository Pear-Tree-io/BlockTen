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

    private void OnEnable()
    {
        // gather children and do one “valid” roll
        children = GetComponentsInChildren<NumberBlock>().ToList();
        AssignValidRandomNumbers();

        // tint
        if (availableColors != null && availableColors.Length > 0)
        {
            var col = availableColors[Random.Range(0, availableColors.Length)];
            foreach (var nb in children)
                nb.SetColor(col);
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

        // Grow back to full size on grab
        transform.localScale = Vector3.one;
        children.ForEach(i => i.OnDragStart());

        var ps = new Vector3(e.position.x, e.position.y, screenZ);
        offset = transform.position - cam.ScreenToWorldPoint(ps);
    }

    public void OnDrag(PointerEventData e)
    {
        if (placed) return;

        var ps = new Vector3(e.position.x, e.position.y, screenZ);
        var world = cam.ScreenToWorldPoint(ps);
        var np = world + offset;
        np.z = startPosition.z;
        transform.position = np;
    }

    public void OnPointerUp(PointerEventData e)
    {
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

        // 4) Clear matches
        GridManager.Instance.CheckAndDestroyMatches();

        // detach blocks and destroy this composite
        foreach (var nb in children)
            nb.transform.SetParent(GridManager.Instance.transform, true);
        Destroy(gameObject);
    }

}
