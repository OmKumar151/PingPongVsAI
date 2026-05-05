using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public int playerScore;
    public int aiScore;

    public int maxScore = 5;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI winText;

    public Ball ball;

    void Start()
    {
        UpdateScore();
        winText.gameObject.SetActive(false);
    }

    public void PlayerScored()
    {
        playerScore++;
        UpdateScore();
        CheckWin();
        StartCoroutine(ResetAndLaunch());
    }

    public void AIScored()
    {
        aiScore++;
        UpdateScore();
        CheckWin();
        StartCoroutine(ResetAndLaunch());
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

    void CheckWin()
    {
        if (playerScore >= maxScore)
        {
            winText.gameObject.SetActive(true);
            winText.text = "YOU WIN";
            Time.timeScale = 0f;
        }
        else if (aiScore >= maxScore)
        {
            winText.gameObject.SetActive(true);
            winText.text = "AI WINS";
            Time.timeScale = 0f;
        }
    }
}