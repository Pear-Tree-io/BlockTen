using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ClassicModeManager : ModeManager
{
    public static ClassicModeManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private GameObject comboTextPrefab;
    private Canvas _canvas;

    private float highScore = 0f;
    private int currentScore;
    private int comboMultiplier = 1;

    private string HighScoreKey = "HighScore";

    public GameObject gameOverPanel;
    public TMP_Text goScoreText;
    public TMP_Text goHighScoreText;
    
    public GameObject highScorePanel;
    public TMP_Text bestHighScoreText;

    public GameObject noSpaceLeft;

    protected override void Awake()
    {
        LoadGame();
        gameOverPanel.SetActive(false);
        highScorePanel.SetActive(false);
        noSpaceLeft.SetActive(false);
    }

    protected override void Start()
    {
        // Cache the canvas for world-to-screen conversions
        _canvas = scoreText.GetComponentInParent<Canvas>();
        highScoreText.text = ""+highScore;
        ResetMode();
    }

    /// <summary>
    /// Call this whenever blocks are destroyed.
    /// </summary>
    public override void OnBlocksDestroyed(int destroyedCount)
    {
        if (destroyedCount <= 0)
        {
            comboMultiplier = 1;
        }
        else
        {
            currentScore += CalculateScore(destroyedCount);
            if (scoreText != null)
                scoreText.text = currentScore.ToString();

            if (comboMultiplier >= 2)
                ShowComboPopup(comboTextPrefab,_canvas,comboMultiplier);

            comboMultiplier++;

            if(currentScore >= highScore)
            {
                highScoreText.text = "" + currentScore;
            }
        }
    }

    protected override int CalculateScore(int destroyedCount)
    {
        return destroyedCount * comboMultiplier;
    }

    /// <summary>
    /// Resets score & combo—call at level start or on restart.
    /// </summary>
    public override void ResetMode()
    {
        currentScore = 0;
        comboMultiplier = 1;
        scoreText.text = "0";
    }

    public override void GameOver()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            bestHighScoreText.text = highScore.ToString();
            //socialObserver.SetLeaderboardScore((int)highScore);
            StartCoroutine(GameOverPlay(highScorePanel));
        }
        else
        {
            goHighScoreText.text = highScore.ToString();
            goScoreText.text = currentScore.ToString();
            StartCoroutine(GameOverPlay(gameOverPanel));
        }

        base.GameOver();
    }
    protected override void SaveGame()
    {
        PlayerPrefs.SetFloat(HighScoreKey, highScore);
        PlayerPrefs.Save();
    }

    protected override void LoadGame()
    {
        highScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);
    }

    public void LoadClassicScene()
    {
        SceneManager.LoadScene("Classic");
    }

    private IEnumerator GameOverPlay(GameObject objectToActive)
    {
        noSpaceLeft.SetActive(true);

        yield return new WaitForSeconds(3f);

        objectToActive.SetActive(true);
    }

    public void ToMenu()
    {
        SceneManager.LoadScene(0);
    }
}
