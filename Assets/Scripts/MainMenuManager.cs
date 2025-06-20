using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    //settings related
    public GameObject Settings;
    


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

    public void OpenSettings()
    {
        Settings.SetActive(true);
    }

    public void CloseSettings()
    {
        Settings.SetActive(false);
    }
}
