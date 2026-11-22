using UnityEngine;

public class PlayerPaddle : MonoBehaviour, IPaddle
{
    public float speed = 10f;

    public float minY = -5.5f;
    public float maxY = 5.5f;

    public float VelocityY { get; private set; }

    void Update()
    {
        float move = Input.GetAxis("Vertical");

        Vector3 pos = transform.position;
        float oldY = pos.y;
        pos.y += move * speed * Time.deltaTime;

        // 🔒 Clamp movement
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;

        // Actual movement this frame (0 when pinned against the clamp)
        VelocityY = Time.deltaTime > 0f ? (pos.y - oldY) / Time.deltaTime : 0f;
    }
}
