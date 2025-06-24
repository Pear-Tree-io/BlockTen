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
    public Camera cam;
    [Tooltip("World-space Transform to track")]
    public Transform worldTarget;

    void Start()
    {
        Vector3 pos = cam.WorldToScreenPoint(worldTarget.position);
        this.transform.position = pos;
    }
}
