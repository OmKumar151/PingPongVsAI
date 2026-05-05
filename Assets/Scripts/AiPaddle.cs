using UnityEngine;

public class AiPaddle : MonoBehaviour
{
    public Transform ball;
    public float speed = 5f;
    public float reactionDelay = 0.1f;

    private float targetY;

    void Update()
    {
        if (ball == null) return;

        if (Time.time % reactionDelay < 0.02f)
        {
            targetY = ball.position.y + Random.Range(-0.5f, 0.5f);
        }

        Vector2 target = new Vector2(transform.position.x, targetY);

        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );
    }
}