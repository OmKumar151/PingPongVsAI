using UnityEngine;
using TMPro;

public class ScoreboardUI : MonoBehaviour
{
    public TextMeshProUGUI[] nameTexts;
    public TextMeshProUGUI[] timeTexts;
    public TextMeshProUGUI[] rankTexts;

    void OnEnable()
    {
        RefreshBoard();
    }

    public void RefreshBoard()
    {
        var scores = ScoreboardManager.Instance.scores;

        for (int i = 0; i < 10; i++)
        {
            if (i < scores.Count)
            {
                int minutes =
                    Mathf.FloorToInt(scores[i].time / 60f);

                int seconds =
                    Mathf.FloorToInt(scores[i].time % 60f);

                string formattedTime =
                    minutes.ToString("00") + ":" +
                    seconds.ToString("00");

                nameTexts[i].text =
                    scores[i].playerName;

                timeTexts[i].text =
                    formattedTime;

                rankTexts[i].text = (i + 1).ToString();
            }
            else
            {
                nameTexts[i].text = "N/A";
                timeTexts[i].text = "--:--";
            }
        }
    }
}