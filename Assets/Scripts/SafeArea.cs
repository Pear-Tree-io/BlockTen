using UnityEngine;

public class SafeArea : MonoBehaviour
{
	public void Awake()
	{
		var tra = (RectTransform)transform;

		tra.offsetMin = new(tra.offsetMin.x, -Screen.safeArea.yMin);
		tra.offsetMax = new(tra.offsetMax.x, Screen.safeArea.yMax - Screen.height);
	}
}