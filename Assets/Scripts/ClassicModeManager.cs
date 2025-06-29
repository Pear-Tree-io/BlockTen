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

    private int highScore = 0;
    private int currentScore;
    private int comboMultiplier = 1;

    private const string HighScoreKey = "HighScore";
    private const string TutorialKey = "Tutorial";

    public GameObject gameOverPanel;
    public TMP_Text goScoreText;
    public TMP_Text goHighScoreText;

    public GameObject highScorePanel;
    public TMP_Text bestHighScoreText;

    public GameObject noSpaceLeft;

    public GameObject goodStamp;
    public GameObject greatStamp;
    public GameObject fantasticStamp;

    public ParticleSystem fantasticEffect;
    public ParticleSystem greatEffect;
    public ParticleSystem goodEffect;
    public bool isHighScore;

    protected override void Awake()
    {
	    Instance = this;
	    
        base.Awake();
        LoadGame();
        gameOverPanel.SetActive(false);
        highScorePanel.SetActive(false);
        noSpaceLeft.SetActive(false);
    }

    protected override void Start()
    {
        Debug.Log(PlayerPrefs.GetInt(TutorialKey));

        // Cache the canvas for world-to-screen conversions
        _canvas = scoreText.GetComponentInParent<Canvas>();
        highScoreText.text = "" + highScore;
        base.Start();

    }

    /// <summary>
    /// Call this whenever blocks are destroyed.
    /// </summary>
    public override void OnMatchBlocksDestroyed(int matchCount, int blockCount)
    {
        
        if (blockCount >= 6)
        {
            StartCoroutine(PrintStamp(fantasticStamp, SFXType.fantasticStamp));
            
            // AudioManager.Instance.PlaySFX(SFXType.brickBreak);
        }
        else if (blockCount > 4 || matchCount >= 2)
        {
            StartCoroutine(PrintStamp(greatStamp, SFXType.greatStamp));
        }
        else if (blockCount > 2)
        {
            StartCoroutine(PrintStamp(goodStamp, SFXType.goodStamp));
        }
        else if (blockCount > 0)
        {
            // AudioManager.Instance.PlaySFX(SFXType.noStamp);
        }


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

        if (currentScore > highScore)
        {
	        isHighScore = true;
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
        isHighScore = false;
    }

    public override void GameOver()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            bestHighScoreText.text = highScore.ToString();
            //socialObserver.SetLeaderboardScore((int)highScore);
            StartCoroutine(GameOverPlay(highScorePanel, bestHighScoreText, SFXType.bestScore));
        }
        else
        {
            goHighScoreText.text = highScore.ToString();
            goScoreText.text = currentScore.ToString();
            StartCoroutine(GameOverPlay(gameOverPanel, goScoreText, SFXType.goScore));
        }

        base.GameOver();
    }

    protected override void SaveGame()
    {
        PlayerPrefs.SetInt(HighScoreKey, highScore);
        PlayerPrefs.Save();
    }

    protected override void LoadGame()
    {
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public void TutorialPlayed()
    {
        PlayerPrefs.SetInt(TutorialKey, 1);
        PlayerPrefs.Save();
    }

    public static bool CheckTutorial()
    {
        if (PlayerPrefs.GetInt(TutorialKey) == 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void LoadClassicScene()
    {
        AudioManager.Instance.PlaySFX(SFXType.Button);
        SceneManager.LoadScene("Classic");
    }

    private IEnumerator GameOverPlay(GameObject objectToActive, TMP_Text text, SFXType type)
    {
        ToggleNoSpaceLeftMessage();
        yield return new WaitForSeconds(2f);

        objectToActive.SetActive(true);
        StartCoroutine(AnimateScore(text, 0, currentScore, 1));
        AudioManager.Instance.PlaySFX(type);
    }

    public void ToggleNoSpaceLeftMessage()
    {
        if (noSpaceLeft.activeSelf)
        {
            noSpaceLeft.SetActive(false);
        }
        else
        {
            noSpaceLeft.SetActive(true);
        }
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
            StartCoroutine(ShowTextOnCanvas(comboTextPrefab, _canvas, count, 0.5f));
        }

        yield return new WaitForSeconds(waitTime);
        StartCoroutine(ShowTextOnCanvas(scoreTextPrefab, _canvas, score, 0.5f));
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
        AudioManager.Instance.PlaySFX(SFXType.Button);
        SceneManager.LoadScene(0);
    }

    public void Replay()
    {
        AudioManager.Instance.PlaySFX(SFXType.Button);
        SceneManager.LoadScene("Classic");
    }

    private IEnumerator PrintStamp(GameObject stamp ,SFXType type)
    {
        stamp.SetActive(true);
        stamp.GetComponent<Animator>().Play("StampAnimation");

        yield return new WaitForSeconds(0.3456f);
        switch (type)
        {
	        case SFXType.fantasticStamp:
		        fantasticEffect.Play();
                AudioManager.Instance.PlaySFX(SFXType.fantastic);
		        break;
	        case SFXType.greatStamp:
		        greatEffect.Play();
                AudioManager.Instance.PlaySFX(SFXType.great);
                break;
	        case SFXType.goodStamp:
		        goodEffect.Play();
                AudioManager.Instance.PlaySFX(SFXType.good);
                break;
        }
        
        AudioManager.Instance.PlaySFX(type);
        
        yield return new WaitForSeconds(0.1f);
        stamp.SetActive(false);
    }

    public int Score => currentScore;
}
