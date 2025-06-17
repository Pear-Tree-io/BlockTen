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
        var centers = new Vector3[n];
        var gx = new int[n];
        var gy = new int[n];
        bool ok = true;

        // 1) Try to reserve each target cell
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
            transform.localScale = Vector3.one * 0.8f;
            return;
        }

        // 2) Align the parent so all children land flush
        var localOffset = children[0].transform.localPosition;
        transform.position = centers[0] - localOffset;

        // 3) Register each child in the grid
        for (int i = 0; i < n; i++)
            GridManager.Instance.RegisterBlock(children[i], gx[i], gy[i]);

        children.ForEach(i => i.OnDragEnd());
        placed = true;

        // 4) Run your match‐and‐clear logic
        GridManager.Instance.CheckAndDestroyMatches();

        // 5) Detach children so they live independently on the grid
        foreach (var nb in children)
        {
            nb.transform.SetParent(GridManager.Instance.transform, true);
        }

        // 6) Destroy the now‐empty composite parent
        Destroy(gameObject);

        // 7) Tell the spawner we placed one
        spawnManager.NotifyBlockPlaced();
    }
}
