using DG.Tweening;
using UnityEngine;

public class UIManager : MonoBehaviour
{
	[SerializeField]
	private SpriteRenderer initRenderer;

	private void Start()
	{
		initRenderer.DOFade(0f, .3456f).SetDelay(.3456f).OnComplete(() => Destroy(initRenderer.gameObject));
	}
}