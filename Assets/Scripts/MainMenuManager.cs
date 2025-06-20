using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : ModeManager
{
    private void Awake()
    {
        base.Awake();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnterClassicMode()
    {
        SceneManager.LoadScene(1);
    }
}
