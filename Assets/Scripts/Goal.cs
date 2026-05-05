using UnityEngine;

public class Goal : MonoBehaviour
{
    public bool isLeftGoal;
    public GameManager gm;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Ball")) return;

        if (isLeftGoal)
            gm.AIScored();
        else
            gm.PlayerScored();
    }
}