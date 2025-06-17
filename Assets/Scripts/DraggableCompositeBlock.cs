using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using static Unity.VisualScripting.Metadata;
using Unity.VisualScripting;

[RequireComponent(typeof(Collider2D))]
public class DraggableCompositeBlock : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Color Settings")]
    [Tooltip("Pick one of these at random and tint the entire composite.")]
    public Color[] availableColors;
    private Color blockColor;

    [HideInInspector] public SpawnManager spawnManager;
    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public List<NumberBlock> children;
    [HideInInspector] public bool placed;

    private Camera cam;
    private float screenZ;
    private Vector3 offset;

    private List<Vector2Int> childOffsets;
    private Dictionary<Vector2Int, int> offsetToIndex;

    private void Start()
    {
        cam = Camera.main;
        screenZ = cam.WorldToScreenPoint(transform.position).z;
        children = new(GetComponentsInChildren<NumberBlock>());

        // compute each child's fixed grid offset
        childOffsets = new List<Vector2Int>();
        float cs = GridManager.Instance.cellSize;
        for (int i = 0; i < children.Count; i++)
        {
            var nb = children[i];
            var off = new Vector2Int(
                Mathf.RoundToInt(nb.transform.localPosition.x / cs),
                Mathf.RoundToInt(nb.transform.localPosition.y / cs)
            );
            childOffsets.Add(off);
        }

        // now roll numbers until no adjacent-pair can sum to 10
        AssignValidRandomNumbers();


        // 2) pick a uniform color for whole composite
        if (availableColors != null && availableColors.Length > 0)
            blockColor = availableColors[Random.Range(0, availableColors.Length)];
            foreach (var nb in children)
                nb.SetColor(blockColor);
    }

    /// <summary>
    /// Rolls all children and repeats until no horizontal or vertical
    /// contiguous run (length ≥2) could sum to 10 (jokers count as 1–9).
    /// </summary>
    private void AssignValidRandomNumbers()
    {
        bool invalidRun;
        do
        {
            invalidRun = false;

            // 1) Assign all children
            foreach (var nb in children)
                nb.AssignRandom();

            // 2) Check horizontal chains
            var checkedH = new HashSet<NumberBlock>();
            foreach (var nb in children)
            {
                // Find leftmost head
                var head = nb;
                while (head.neighborLeft != null) head = head.neighborLeft;
                if (!checkedH.Add(head)) continue;

                // Walk right to build chain
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

            // 3) Check vertical chains
            var checkedV = new HashSet<NumberBlock>();
            foreach (var nb in children)
            {
                // Find bottommost head
                var head = nb;
                while (head.neighborDown != null) head = head.neighborDown;
                if (!checkedV.Add(head)) continue;

                // Walk up to build chain
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

    /// <summary>
    /// Returns true if there is any contiguous sub-segment (length ≥2)
    /// made up *only of non-joker blocks* whose values sum to exactly 10.
    /// Segments containing any joker are always considered valid here.
    /// </summary>
    private bool ChainHasInvalidSegment(List<NumberBlock> chain)
    {
        for (int start = 0; start < chain.Count - 1; start++)
        {
            int sum = 0;
            for (int end = start; end < chain.Count; end++)
            {
                var b = chain[end];

                // if it’s a joker, skip this entire segment
                if (b.IsJoker)
                    break;

                sum += b.Value;
                int length = end - start + 1;
                if (length >= 2 && sum == 10)
                    return true;
                // if sum > 10 we can also break early
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

        // 1) TEST pass: can *all* children fit?
        for (int i = 0; i < n; i++)
        {
            if (!GridManager.Instance.TryPlaceCell(
                    children[i].transform.position,
                    out centers[i],
                    out gx[i],
                    out gy[i]))
            {
                ok = false;
                break;
            }
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

        // 3) RESERVE & REGISTER each child (marks occupied + stores ref)
        for (int i = 0; i < n; i++)
            GridManager.Instance.RegisterBlock(children[i], gx[i], gy[i]);

        children.ForEach(i => i.OnDragEnd());
        placed = true;

        // 4) Clear matches, detach, destroy parent, notify spawn manager…
        GridManager.Instance.CheckAndDestroyMatches();

        foreach (var nb in children)
            nb.transform.SetParent(GridManager.Instance.transform, true);
        Destroy(gameObject);

        spawnManager.NotifyBlockPlaced();
    }
}
