using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Scores")]
    public int playerScore;
    public int aiScore;
    public int maxScore = 5;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI winText;
    public TextMeshProUGUI timerText;

    public GameObject startMenu;
    public GameObject pauseMenu;
    public GameObject namePanel;
    public GameObject scoreboardPanel;

    [Header("Name Entry")]
    public TMP_InputField nameInput;

    [Header("Game")]
    public Ball ball;

    private float gameTime = 0f;

    private bool isGameRunning = false;
    private bool isPaused = false;
    private bool playerWon = false;

    void Start()
    {
        Time.timeScale = 0f;

        startMenu.SetActive(true);
        pauseMenu.SetActive(false);
        namePanel.SetActive(false);
        scoreboardPanel.SetActive(false);

        winText.gameObject.SetActive(false);

        UpdateScore();
        UpdateTimerUI();
    }

    void Update()
    {
        HandleEscapeKey();

        // TIMER
        if (isGameRunning)
        {
            gameTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    // =====================================================
    // ESC KEY SYSTEM
    // =====================================================

    void HandleEscapeKey()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        // SCOREBOARD OPEN
        if (scoreboardPanel.activeSelf)
        {
            CloseScoreboard();
        }

        // NAME PANEL OPEN
        else if (namePanel.activeSelf)
        {
            CancelNameEntry();
        }

        // GAME PAUSED
        else if (isPaused)
        {
            ResumeGame();
        }

        // GAME RUNNING
        else if (isGameRunning)
        {
            PauseGame();
        }

        // MAIN MENU
        else
        {
            // Do nothing
        }
    }

    // =====================================================
    // START GAME
    // =====================================================

    public void StartGame()
    {
        // Cancel any pending "launch after delay" from a previous round
        StopAllCoroutines();

        startMenu.SetActive(false);
        pauseMenu.SetActive(false);
        scoreboardPanel.SetActive(false);
        namePanel.SetActive(false);

        winText.gameObject.SetActive(false);

        Time.timeScale = 1f;

        isPaused = false;
        isGameRunning = true;

        playerScore = 0;
        aiScore = 0;

        gameTime = 0f;

        UpdateScore();
        UpdateTimerUI();

        ball.ResetBall();

        StartCoroutine(StartDelay());
    }

    IEnumerator StartDelay()
    {
        yield return new WaitForSeconds(1f);
        ball.Launch();
    }

    // =====================================================
    // PAUSE SYSTEM
    // =====================================================

    public void PauseGame()
    {
        isPaused = true;
        isGameRunning = false;

        pauseMenu.SetActive(true);

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        isGameRunning = true;

        pauseMenu.SetActive(false);

        Time.timeScale = 1f;
    }

    // =====================================================
    // RESTART
    // =====================================================

    public void RestartGame()
    {
        // Cancel any pending "launch after delay" from a previous round
        StopAllCoroutines();

        Time.timeScale = 1f;

        isPaused = false;
        isGameRunning = true;

        pauseMenu.SetActive(false);
        namePanel.SetActive(false);
        scoreboardPanel.SetActive(false);

        winText.gameObject.SetActive(false);

        playerScore = 0;
        aiScore = 0;

        gameTime = 0f;

        UpdateScore();
        UpdateTimerUI();

        ball.ResetBall();

        StartCoroutine(RestartDelay());
    }

    IEnumerator RestartDelay()
    {
        yield return new WaitForSeconds(1f);
        ball.Launch();
    }

    // =====================================================
    // SCOREBOARD
    // =====================================================

    public void OpenScoreboard()
    {
        startMenu.SetActive(false);
        scoreboardPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void CloseScoreboard()
    {
        scoreboardPanel.SetActive(false);
        startMenu.SetActive(true);

        Time.timeScale = 0f;
    }

    // =====================================================
    // SCORING
    // =====================================================

    public void PlayerScored()
    {
        playerScore++;

        UpdateScore();

        CheckWin();

        if (isGameRunning)
        {
            StartCoroutine(ResetAndLaunch());
        }
    }

    public void AIScored()
    {
        aiScore++;

        UpdateScore();

        CheckWin();

        if (isGameRunning)
        {
            StartCoroutine(ResetAndLaunch());
        }
    }

    IEnumerator ResetAndLaunch()
    {
        ball.ResetBall();

        yield return new WaitForSeconds(1.5f);

        ball.Launch();
    }

    void UpdateScore()
    {
        scoreText.text = playerScore + " : " + aiScore;
    }

    // =====================================================
    // TIMER
    // =====================================================

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);

        timerText.text =
            minutes.ToString("00") +
            ":" +
            seconds.ToString("00");
    }

    // =====================================================
    // WIN SYSTEM
    // =====================================================

    void CheckWin()
    {
        if (playerScore >= maxScore)
        {
            playerWon = true;
            EndGame("YOU WIN");
        }
        else if (aiScore >= maxScore)
        {
            playerWon = false;
            EndGame("AI WINS");
        }
    }

    void EndGame(string result)
    {
        isGameRunning = false;

        winText.gameObject.SetActive(true);
        winText.text = result;

        Time.timeScale = 0f;

        namePanel.SetActive(true);
    }

    // =====================================================
    // NAME ENTRY
    // =====================================================

    public void SubmitName()
    {
        string playerName = nameInput.text;

        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "N/A";
        }

        // SAVE ONLY WINNERS
        if (playerWon)
        {
            ScoreboardManager.Instance.AddScore(
                playerName,
                gameTime
            );
        }

        // REFRESH SCOREBOARD UI
        ScoreboardUI ui =
            FindObjectOfType<ScoreboardUI>();

        if (ui != null)
        {
            ui.RefreshBoard();
        }

        nameInput.text = "";

        namePanel.SetActive(false);

        ExitToMenu();
    }

    public void CancelNameEntry()
    {
        nameInput.text = "";

        namePanel.SetActive(false);

        ExitToMenu();
    }

    // =====================================================
    // EXIT TO MENU
    // =====================================================

    public void ExitToMenu()
    {
        // Cancel any pending "launch after delay" from a previous round
        StopAllCoroutines();

        Time.timeScale = 0f;

        isPaused = false;
        isGameRunning = false;

        playerScore = 0;
        aiScore = 0;

        gameTime = 0f;

        UpdateScore();
        UpdateTimerUI();

        pauseMenu.SetActive(false);
        namePanel.SetActive(false);
        scoreboardPanel.SetActive(false);

        winText.gameObject.SetActive(false);

        startMenu.SetActive(true);

        ball.ResetBall();
    }
}