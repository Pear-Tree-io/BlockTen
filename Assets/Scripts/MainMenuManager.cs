using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : ModeManager
{
	[SerializeField]
	private CanvasGroup canvasGroup;
	
	protected override void Awake()
	{
		base.Awake();
		
		Application.targetFrameRate = 240;

		if (ClassicModeManager.CheckTutorial() == false)
			LoadScene();
	}

	public void EnterClassicMode()
	{
		canvasGroup.DOFade(0f, .3456f).OnComplete(LoadScene);
	}

	private void LoadScene()
	{
		SceneManager.LoadScene(1);
	}
}