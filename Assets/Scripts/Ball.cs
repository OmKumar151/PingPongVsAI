using UnityEngine;

public class Ball : MonoBehaviour
{
    public float speed = 8f;
    public float speedIncrease = 0.5f;

    public AudioClip paddleHit;
    public AudioClip wallHit;

    [Header("Game Feel")]
    [Tooltip("Ball speed at which the trail is brightest / shake is strongest")]
    public float maxFeelSpeed = 20f;
    [Tooltip("How much the paddle-hit pitch rises per hit in a rally")]
    public float pitchStepPerHit = 0.04f;
    public float maxPitch = 1.6f;

    [Header("Spin")]
    [Tooltip("Spin gained per unit of paddle speed at the moment of contact")]
    public float spinPerPaddleSpeed = 0.3f;
    [Tooltip("Strongest possible curve (sideways acceleration, units/sec²)")]
    public float maxSpin = 3f;
    [Tooltip("How fast spin fades (per second). Higher = shorter curves.")]
    public float spinDecay = 0.6f;
    [Tooltip("Stops the ball curving into an almost vertical path")]
    [Range(0.1f, 0.9f)] public float minHorizontal = 0.4f;
    public Color spinTrailColor = new Color(0.3f, 0.9f, 1f, 0.9f);

    // Read by the AI
    public float Spin { get; private set; }
    public float StartSpeed => startSpeed;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private TrailRenderer trail;
    private float startSpeed;
    private int rallyHits;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        startSpeed = speed; // remember the Inspector value for resets
        SetupTrail();
    }

    void Start()
    {
        Launch();
    }

    public void Launch()
    {
        float x = Random.value > 0.5f ? 1 : -1;
        float y = Random.Range(-0.5f, 0.5f);

        Vector2 dir = new Vector2(x, y).normalized;
        rb.linearVelocity = dir * speed;
        Spin = 0f;
    }

    public void ResetBall()
    {
        rb.linearVelocity = Vector2.zero;
        transform.position = Vector2.zero;
        speed = startSpeed; // reset difficulty
        rallyHits = 0;
        Spin = 0f;
        trail.Clear(); // otherwise the trail draws a line from the goal to the centre
    }

    void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.01f || Mathf.Abs(Spin) < 0.01f) return;

        // Spin bends the ball's path up or down; speed stays the same
        v.y += Spin * Time.fixedDeltaTime;
        Vector2 dir = v.normalized;

        // Never let it curve into a near-vertical path
        if (Mathf.Abs(dir.x) < minHorizontal)
        {
            dir.x = Mathf.Sign(v.x) * minHorizontal;
            dir.y = Mathf.Sign(dir.y) * Mathf.Sqrt(1f - minHorizontal * minHorizontal);
        }

        rb.linearVelocity = dir * speed;

        // Spin fades over time
        Spin = Mathf.MoveTowards(Spin, 0f, spinDecay * Mathf.Abs(Spin) * Time.fixedDeltaTime + 0.01f);
    }

    void Update()
    {
        // Trail gets longer and brighter as the ball speeds up
        float t = Mathf.InverseLerp(startSpeed, maxFeelSpeed, speed);
        trail.time = Mathf.Lerp(0.08f, 0.25f, t);
        Color c = Color.Lerp(new Color(1f, 1f, 1f, 0.35f), new Color(1f, 0.85f, 0.3f, 0.9f), t);

        // ...and turns blue when the ball is spinning, so the player can read it
        c = Color.Lerp(c, spinTrailColor, maxSpin > 0f ? Mathf.Abs(Spin) / maxSpin : 0f);
        trail.startColor = c;
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Paddle bounce
        if (collision.gameObject.CompareTag("Paddle"))
        {
            rallyHits++;

            float paddleY = collision.transform.position.y;
            float ballY = transform.position.y;
            float height = collision.collider.bounds.size.y;

            float offset = (ballY - paddleY) / height;

            // Send the ball away from whichever side the paddle is on.
            // (Using velocity here is unreliable: the bouncy physics material
            // has usually already flipped it by the time this runs.)
            float dirX = transform.position.x > collision.transform.position.x ? 1 : -1;
            Vector2 dir = new Vector2(dirX, offset);

            speed += speedIncrease;
            rb.linearVelocity = dir.normalized * speed;

            // Moving paddle = spin. Swipe up while hitting and the ball curves upward.
            IPaddle paddle = collision.gameObject.GetComponent<IPaddle>();
            Spin = paddle != null
                ? Mathf.Clamp(paddle.VelocityY * spinPerPaddleSpeed, -maxSpin, maxSpin)
                : 0f;

            // Rising pitch: long rallies sound more and more tense
            audioSource.pitch = Mathf.Min(1f + rallyHits * pitchStepPerHit, maxPitch);
            audioSource.PlayOneShot(paddleHit);

            // Shake + hit-stop scale with ball speed
            float t = Mathf.InverseLerp(startSpeed, maxFeelSpeed, speed);
            GameFeel feel = GameFeel.Get();
            if (feel != null)
            {
                feel.Shake(Mathf.Lerp(0.05f, 0.15f, t), 0.12f);
                feel.HitStop(Mathf.Lerp(0.02f, 0.06f, t));
            }
        }

        // Wall bounce sound only
        else if (collision.gameObject.CompareTag("Wall"))
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(wallHit);

            // Hitting a wall flips and weakens the curve, so it bends away from the wall
            Spin *= -0.5f;
        }
    }

    void SetupTrail()
    {
        trail = GetComponent<TrailRenderer>();
        if (trail != null) return; // you added/tuned one yourself in the Inspector

        trail = gameObject.AddComponent<TrailRenderer>();

        // Reuse the ball's own sprite material so it renders in this pipeline
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            trail.sharedMaterial = sr.sharedMaterial;
            trail.sortingLayerID = sr.sortingLayerID;
            trail.sortingOrder = sr.sortingOrder - 1; // behind the ball
        }

        float width = GetComponent<Collider2D>() != null
            ? GetComponent<Collider2D>().bounds.size.x
            : 0.3f;
        trail.startWidth = width * 0.9f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 4;
    }
}
