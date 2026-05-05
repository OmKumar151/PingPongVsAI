using UnityEngine;

public class PlayerPaddle : MonoBehaviour
{
    public float speed = 10f;

    void Update()
    {
        float move = Input.GetAxis("Vertical");

        transform.Translate(Vector2.up * move * speed * Time.deltaTime);
    }
}