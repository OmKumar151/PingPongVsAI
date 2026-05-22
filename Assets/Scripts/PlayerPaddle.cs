using UnityEngine;

public class PlayerPaddle : MonoBehaviour
{
    public float speed = 10f;

    public float minY = -5.5f;
    public float maxY = 5.5f;

    void Update()
    {
        float move = Input.GetAxis("Vertical");

        Vector3 pos = transform.position;
        pos.y += move * speed * Time.deltaTime;

        // 🔒 Clamp movement
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}