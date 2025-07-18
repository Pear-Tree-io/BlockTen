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
		
		Application.targetFrameRate = 120;

		if (PlayerData.ShouldTutorial())
			LoadStageScene();
	}

	public void EnterClassicMode()
	{
		canvasGroup.DOFade(0f, .3456f).OnComplete(LoadClassicScene);
	}

    public void EnterTimelimitMode()
    {
        canvasGroup.DOFade(0f, .3456f).OnComplete(LoadTimeLimitScene);
    }

    private void LoadClassicScene()
	{
		SceneManager.LoadScene(1);
	}
    
    private void LoadTimeLimitScene()
    {
        SceneManager.LoadScene(2);
    }
    
    private void LoadStageScene()
    {
	    PlayerData.CompleteTutorial();
		SceneManager.LoadScene("Stage");
	}
}