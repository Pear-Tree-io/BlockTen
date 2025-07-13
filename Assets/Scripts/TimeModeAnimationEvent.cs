using UnityEngine;

public class TimeModeAnimationEvent : MonoBehaviour
{
    public SpawnManager spawnManager;
    public GameObject timerOverPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void TimeOver()
    {
        spawnManager.SetGameOver();
        timerOverPanel.SetActive(true);
    }
}
