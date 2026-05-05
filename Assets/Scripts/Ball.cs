using UnityEngine;

public class Ball : MonoBehaviour
{
    public float speed = 8f;
    public float speedIncrease = 0.5f;

    public AudioClip paddleHit;
    public AudioClip wallHit;

    private Rigidbody2D rb;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        Launch();
    }

    public void Launch()
    {
        float x = Random.value > 0.5f ? 1 : -1;
        float y = Random.Range(-0.5f, 0.5f);

        Vector2 dir = new Vector2(x, y).normalized;
        rb.linearVelocity = dir * speed;
    }

    public void ResetBall()
    {
        rb.linearVelocity = Vector2.zero;
        transform.position = Vector2.zero;
        speed = 8f; // reset difficulty
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Paddle bounce
        if (collision.gameObject.CompareTag("Paddle"))
        {
            audioSource.PlayOneShot(paddleHit);

            float paddleY = collision.transform.position.y;
            float ballY = transform.position.y;
            float height = collision.collider.bounds.size.y;

            float offset = (ballY - paddleY) / height;

            Vector2 dir = new Vector2(
                rb.linearVelocity.x > 0 ? -1 : 1,
                offset
            );

            speed += speedIncrease;
            rb.linearVelocity = dir.normalized * speed;
        }

        // Wall bounce sound only
        else if (collision.gameObject.CompareTag("Wall"))
        {
            audioSource.PlayOneShot(wallHit);
        }
    }
}