using UnityEngine;

public class Goal : MonoBehaviour
{
    public bool isLeftGoal;
    public GameManager gm;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Ball")) return;

        // Big shake on a goal
        GameFeel feel = GameFeel.Get();
        if (feel != null) feel.Shake(0.3f, 0.35f);

        if (isLeftGoal)
            gm.AIScored();
        else
            gm.PlayerScored();
    }
}