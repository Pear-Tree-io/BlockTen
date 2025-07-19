using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using ManagersSpace;
using DG.Tweening;

public class TimeModeManager : ModeManager
{
    public static TimeModeManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private GameObject scoreTextPrefab;
    private Canvas _canvas;

    private int highScore = 0;
    private int currentScore;

    private const string HighScoreKey = "TimeHighScore";
    private const string AdKey = "Ad";

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

    public Animator timerAnimation;

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
            Camera.main.DOShakePosition(0.2f, 0.2f, 50, 180, true);
            // AudioManager.Instance.PlaySFX(SFXType.brickBreak);
        }
        else if (blockCount > 4 || matchCount >= 2)
        {
            StartCoroutine(PrintStamp(greatStamp, SFXType.greatStamp));
            Camera.main.DOShakePosition(0.2f, 0.1f, 30, 180, true);   
        }
        else if (blockCount > 2)
        {
            StartCoroutine(PrintStamp(goodStamp, SFXType.goodStamp));
        }

        //comboMultiplier = matchCount;

        var pointsGained = blockCount;
        
        int oldScore = currentScore;
        int newScore = oldScore + pointsGained;
        currentScore = newScore;

        if (scoreText != null)
            scoreText.text = currentScore.ToString();

        // Animate score increment
        StartCoroutine(AnimateScore(scoreText, oldScore, newScore, 0.5f));

        if (matchCount >= 1)
        {
            StartCoroutine(PlayScoreTexts(pointsGained));
        }

        if (currentScore > highScore)
        {
	        base.isHighScore = true;
            highScoreText.text = "" + currentScore;
        }
    }

    /// <summary>
    /// Resets score & combo—call at level start or on restart.
    /// </summary>
    public override void ResetMode()
    {
	    base.ResetMode();
	    
        currentScore = 0;
        scoreText.text = "0";
        base.isHighScore = false;
    }

    public override void GameOver()
    {
        timerAnimation.enabled = false;

        if (currentScore > highScore)
        {
	        highScore = currentScore;
	        bestHighScoreText.text = highScore.ToString();
	        PlatformManager.Get.ReportScore(highScore);
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
        base.SaveGame();
    }

    protected override void LoadGame()
    {
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        base.LoadGame();
    }

    public void LoadTimeLimitScene()
    {
        AudioManager.Instance.PlaySFX(SFXType.Button);
        SceneManager.LoadScene("TimeLimit");
    }

    private IEnumerator GameOverPlay(GameObject objectToActive, TMP_Text text, SFXType type)
    {
        SetNoSpaceLeftMessage(true);
        yield return new WaitForSeconds(2f);

        objectToActive.SetActive(true);
        StartCoroutine(AnimateScore(text, 0, currentScore, 1));
        AudioManager.Instance.PlaySFX(type);
    }

    public override void SetNoSpaceLeftMessage(bool active)
    {
	    noSpaceLeft.SetActive(active);
    }

    private IEnumerator PlayScoreTexts(int score)
    {
        yield return new WaitForSeconds(0.5f);
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

    public void Replay()
    {
        base.Replay();
        SceneManager.LoadScene("TimeLimit");
    }

    private IEnumerator PrintStamp(GameObject stamp ,SFXType type)
    {
        stamp.SetActive(true);
        stamp.GetComponent<Animator>().Play("StampAnimation");

        //yield return new WaitForSeconds(0.3456f);
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
        
        yield return new WaitForSeconds(0.5f);
        stamp.SetActive(false);
    }

    public int Score => currentScore;
}
