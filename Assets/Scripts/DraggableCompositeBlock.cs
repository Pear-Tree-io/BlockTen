using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class DraggableCompositeBlock : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector] public SpawnManager spawnManager;
    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public List<NumberBlock> children;
    [HideInInspector] public bool placed;

    private Camera cam;
    private float screenZ;
    private Vector3 offset;

    private void Start()
    {
        cam = Camera.main;
        screenZ = cam.WorldToScreenPoint(transform.position).z;
        children = new(GetComponentsInChildren<NumberBlock>());

        // Randomize child values on spawn
        foreach (var nb in children)
            nb.AssignRandom();
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
        var centers = new Vector3[n];

        // 1) TEST pass: can *all* children fit?
        for (int i = 0; i < n; i++)
        {
            if (!GridManager.Instance.CanPlaceCell(
                    children[i].transform.position,
                    out gx[i], out gy[i]))
            {
                // failure → snap back & shrink
                transform.position = startPosition;
                transform.localScale = Vector3.one * 0.8f;
                return;
            }
            // if they *can*, remember their would-be centers
            centers[i] = GridManager.Instance.GetCellCenter(gx[i], gy[i]);
        }

        // 2) ALIGN parent so all children land exactly on their centers
        var localOffset = children[0].transform.localPosition;
        transform.position = centers[0] - localOffset;

        // 3) RESERVE & REGISTER each child (marks occupied + stores ref)
        for (int i = 0; i < n; i++)
            GridManager.Instance.RegisterBlock(children[i], gx[i], gy[i]);

        placed = true;

        // 4) Clear matches, detach, destroy parent, notify spawn manager…
        GridManager.Instance.CheckAndDestroyMatches();

        foreach (var nb in children)
            nb.transform.SetParent(GridManager.Instance.transform, true);
        Destroy(gameObject);

        spawnManager.NotifyBlockPlaced();
    }
}
