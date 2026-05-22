using UnityEngine;

public class AiPaddle : MonoBehaviour
{
    public Transform ball;
    public float speed = 5f;

    public float minY = -5.5f;
    public float maxY = 5.5f;

    void Update()
    {
        if (ball == null) return;

        Vector3 target = new Vector3(
            transform.position.x,
            ball.position.y,
            transform.position.z
        );

        Vector3 pos = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        // 🔒 Clamp movement
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}