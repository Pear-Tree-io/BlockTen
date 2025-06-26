using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : ModeManager
{
	protected override void Awake()
	{
		base.Awake();

		if (ClassicModeManager.CheckTutorial() == false)
			LoadScene();
	}

	public void EnterClassicMode()
	{
		AudioManager.Instance.PlaySFX(SFXType.Button);
		LoadScene();
	}

	private void LoadScene()
	{
		SceneManager.LoadScene(1);
	}
}