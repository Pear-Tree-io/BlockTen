using UnityEngine;

public static class Utility
{
	public static void SetActive(this Component component, bool active)
	{
		component.gameObject.SetActive(active);
	}
}
