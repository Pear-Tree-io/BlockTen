using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a UI element to position it over a world-space target.
/// Converts the target's world position to screen position and
/// places this UI element there on a screen-space Canvas.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIPlacer : MonoBehaviour
{
    public Canvas canvas;
    public Camera cam;
    [Tooltip("World-space Transform to track")]
    public Transform worldTarget;

    void Start()
    {
        /*Vector3 pos = cam.WorldToViewportPoint(worldTarget.position);
        this.transform.position = pos;*/


        // 1) get your Canvas’s RectTransform
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();

        // 2) convert world → screen
        Vector3 screenPos = cam.WorldToScreenPoint(worldTarget.position);

        // 3) convert screen → local point in the canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,                // the parent you want coords in
            screenPos,               // the point in screen‐space
            canvas.worldCamera,    // your Canvas’s camera
            out Vector2 localPoint
        );

        // 4) assign to your UI element’s anchoredPosition
        this.GetComponent<RectTransform>().anchoredPosition = localPoint;
    }
}
