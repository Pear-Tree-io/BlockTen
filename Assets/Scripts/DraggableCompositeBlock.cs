using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class DraggableCompositeBlock : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector] public SpawnManager spawnManager;
    [HideInInspector] public Vector3 startPosition;

    private Camera cam;
    private float screenZ;
    private Vector3 offset;
    private bool placed;
    private List<NumberBlock> children;

    private void Start()
    {
        cam = Camera.main;
        screenZ = cam.WorldToScreenPoint(transform.position).z;
        children = new List<NumberBlock>(GetComponentsInChildren<NumberBlock>());

        // 2) random values on spawn
        foreach (var nb in children)
            nb.AssignRandom();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (placed) return;
        // 3) snap up to full size when you grab it
        transform.localScale = Vector3.one;

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

        // calculate each child’s target cell
        for (int i = 0; i < n; i++)
        {
            if (!GridManager.Instance.TryPlaceCell(
                    children[i].transform.position,
                    out centers[i],
                    out gx[i],
                    out gy[i]
                ))
            {
                ok = false;
                break;
            }
        }

        if (!ok)
        {
            // 4) reset both position AND scale if placement failed
            transform.position = startPosition;
            transform.localScale = Vector3.one * 0.8f;
            return;
        }

        // align the parent based on its first child’s local offset
        var localOffset = children[0].transform.localPosition;
        transform.position = centers[0] - localOffset;

        // register them and lock them in
        for (int i = 0; i < n; i++)
            GridManager.Instance.RegisterBlock(children[i], gx[i], gy[i]);

        placed = true;
        GridManager.Instance.CheckAndDestroyMatches();
        spawnManager.NotifyBlockPlaced();
    }
}
