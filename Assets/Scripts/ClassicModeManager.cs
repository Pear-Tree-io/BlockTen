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
    [SerializeField] private GameObject scoreTextPrefab;
    private Canvas _canvas;

    private float highScore = 0f;
    private int currentScore;
    private int comboMultiplier = 1;

    private readonly string HighScoreKey = "HighScore";

    public GameObject gameOverPanel;
    public TMP_Text goScoreText;
    public TMP_Text goHighScoreText;
    
    public GameObject highScorePanel;
    public TMP_Text bestHighScoreText;

    public GameObject noSpaceLeft;

    public GameObject revivePanel;
    public bool isRevived = false;
    [SerializeField] private int reviveCountDown;

    protected override void Awake()
    {
        base.Awake();
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
        base.Start();
    }

    /// <summary>
    /// Call this whenever blocks are destroyed.
    /// </summary>
    public override void OnMatchBlocksDestroyed(int matchCount, int blockCount)
    {
        //comboMultiplier = matchCount;

        var pointsGained = blockCount * matchCount * 10; //CalculateScore(matchCount);
        int oldScore = currentScore;
        int newScore = oldScore + pointsGained;
        currentScore = newScore;

        if (scoreText != null)
            scoreText.text = currentScore.ToString();

        // Animate score increment
        StartCoroutine(AnimateScore(scoreText, oldScore, newScore, 0.5f));

        if (matchCount >= 1)
        {
            StartCoroutine(PlayScoreTexts(matchCount, pointsGained));
        }
 
        if(currentScore >= highScore)
        {
            highScoreText.text = "" + currentScore;
        }
    }

    protected override int CalculateScore(int destroyedCount)
    {
        return destroyedCount * 10 * comboMultiplier;
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
            StartCoroutine(GameOverPlay(highScorePanel, bestHighScoreText));
        }
        else
        {
            goHighScoreText.text = highScore.ToString();
            goScoreText.text = currentScore.ToString();
            StartCoroutine(GameOverPlay(gameOverPanel, goScoreText));
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

    private IEnumerator GameOverPlay(GameObject objectToActive,TMP_Text text)
    {
        noSpaceLeft.SetActive(true);

        yield return new WaitForSeconds(2f);

        objectToActive.SetActive(true);
        StartCoroutine(AnimateScore(text, 0, currentScore, 1));
    }

    private IEnumerator PlayScoreTexts(int count, int score)
    {
        var waitPopup = 0f;
        var waitTime = (count * 0.4f) + 0.2f;

        if (count >= 2)
        {
            waitPopup = count * 0.3f + 0.2f;
            waitTime = (count * 0.4f) + 0.75f - waitPopup;
            yield return new WaitForSeconds(waitPopup);
            ShowTextOnCanvas(comboTextPrefab, _canvas, count);
        }
        
        yield return new WaitForSeconds(waitTime);
        ShowTextOnCanvas(scoreTextPrefab, _canvas, score);
    }

    /// <summary>
    /// Smoothly animates the scoreText from 'start' to 'end' over 'duration' seconds.
    /// </summary>
    private IEnumerator AnimateScore(TMP_Text text, int start, int end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int displayValue = Mathf.RoundToInt(Mathf.Lerp(start, end, elapsed / duration));
            text.text = displayValue.ToString();
            yield return null;
        }
        text.text = end.ToString();
    }

    public void ToMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void Replay()
    {
        SceneManager.LoadScene("Classic");
    }

    public IEnumerator StartReviveCountdown()
    {
        isRevived = true;
        revivePanel.SetActive(true);
        while (reviveCountDown > 0)
        {           
            yield return new WaitForSeconds(1);
            reviveCountDown--;
        }
        if(reviveCountDown == 0)
        {
            revivePanel.SetActive(false);
        }
    }

    public int Score => currentScore;
}
