using System.Collections.Generic;
using UnityEngine;

public class ScoreboardManager : MonoBehaviour
{
    public static ScoreboardManager Instance;

    public List<ScoreEntry> scores = new List<ScoreEntry>();

    private int maxEntries = 10;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadScores();
    }

    // ✅ ADD NEW SCORE
    public void AddScore(string playerName, float time)
    {
        ScoreEntry entry = new ScoreEntry
        {
            playerName = playerName,
            time = time
        };

        scores.Add(entry);

        // sort fastest time first
        scores.Sort((a, b) => a.time.CompareTo(b.time));

        // keep only top 10
        if (scores.Count > maxEntries)
        {
            scores.RemoveAt(scores.Count - 1);
        }

        SaveScores();
    }

    // ✅ SAVE
    public void SaveScores()
    {
        PlayerPrefs.SetInt("ScoreCount", scores.Count);

        for (int i = 0; i < scores.Count; i++)
        {
            PlayerPrefs.SetString("PlayerName_" + i, scores[i].playerName);
            PlayerPrefs.SetFloat("PlayerTime_" + i, scores[i].time);
        }

        PlayerPrefs.Save();
    }

    // ✅ LOAD
    public void LoadScores()
    {
        scores.Clear();

        int count = PlayerPrefs.GetInt("ScoreCount", 0);

        for (int i = 0; i < count; i++)
        {
            ScoreEntry entry = new ScoreEntry();

            entry.playerName =
                PlayerPrefs.GetString("PlayerName_" + i, "N/A");

            entry.time =
                PlayerPrefs.GetFloat("PlayerTime_" + i, 9999f);

            scores.Add(entry);
        }
    }
}