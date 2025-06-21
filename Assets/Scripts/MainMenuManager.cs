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
        SceneManager.LoadScene(1);
    }
}
