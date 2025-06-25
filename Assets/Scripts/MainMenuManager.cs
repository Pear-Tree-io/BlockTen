using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : ModeManager
{
    protected override void Awake()
    {
        base.Awake();
    }

    public void EnterClassicMode()
    {
        AudioManager.Instance.PlaySFX(SFXType.Button);
        SceneManager.LoadScene(1);
    }
}
