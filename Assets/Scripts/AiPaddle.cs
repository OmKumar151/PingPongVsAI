using UnityEngine;

// An AI that plays like a person: it predicts where the ball will arrive,
// but it only re-thinks a few times per second, it aims imperfectly, and it
// gets worse against fast or spinning balls. Between shots it returns to centre.
public class AiPaddle : MonoBehaviour, IPaddle
{
    public Transform ball;
    public float speed = 5f;

    public float minY = -5.5f;
    public float maxY = 5.5f;

    [Header("Imperfection")]
    [Tooltip("Seconds between AI decisions. Higher = slower reactions.")]
    public float reactionTime = 0.2f;

    [Tooltip("Aim error with a slow, straight ball. 1 = may aim at the very edge of the paddle.")]
    public float baseError = 0.5f;

    [Tooltip("Extra aim error at top ball speed.")]
    public float errorAtMaxSpeed = 0.6f;

    [Tooltip("Extra aim error against maximum spin.")]
    public float errorAtMaxSpin = 0.4f;

    [Tooltip("How quickly the paddle drifts back to centre when the ball is moving away (0-1 of speed).")]
    public float returnSpeedFactor = 0.5f;

    public float VelocityY { get; private set; }

    private Ball ballScript;
    private Rigidbody2D ballRb;
    private float ballRadius = 0.2f;
    private float paddleHalfHeight = 0.5f;
    private float courtTop = 5f;
    private float courtBottom = -5f;

    private float targetY;
    private float decisionTimer;
    private bool chasing;

    void Start()
    {
        if (ball != null)
        {
            ballScript = ball.GetComponent<Ball>();
            ballRb = ball.GetComponent<Rigidbody2D>();
            Collider2D bc = ball.GetComponent<Collider2D>();
            if (bc != null) ballRadius = bc.bounds.extents.y;
        }

        Collider2D pc = GetComponent<Collider2D>();
        if (pc != null) paddleHalfHeight = pc.bounds.extents.y;

        FindCourtEdges();
    }

    void Update()
    {
        if (ball == null || ballRb == null) return;

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = reactionTime;
            Decide();
        }

        // Move toward the current target
        float moveSpeed = chasing ? speed : speed * returnSpeedFactor;
        Vector3 pos = transform.position;
        float oldY = pos.y;

        pos.y = Mathf.MoveTowards(pos.y, targetY, moveSpeed * Time.deltaTime);

        // 🔒 Clamp movement
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
        VelocityY = Time.deltaTime > 0f ? (pos.y - oldY) / Time.deltaTime : 0f;
    }

    void Decide()
    {
        Vector2 ballPos = ball.position;
        Vector2 vel = ballRb.linearVelocity;

        // Is the ball coming toward us?
        float towardUs = Mathf.Sign(transform.position.x - ballPos.x);
        bool approaching = vel.sqrMagnitude > 0.01f && Mathf.Sign(vel.x) == towardUs;

        if (!approaching)
        {
            // Ball going away (or waiting to launch): relax back to the middle
            chasing = false;
            targetY = 0f;
            return;
        }

        chasing = true;

        // Where will it cross our line? (ignores spin, so curving shots fool us)
        float predictedY = PredictArrivalY(ballPos, vel, transform.position.x);

        // How bad is our aim right now?
        float speedT = ballScript != null
            ? Mathf.InverseLerp(ballScript.StartSpeed, ballScript.maxFeelSpeed, vel.magnitude)
            : 0f;
        float spinT = ballScript != null && ballScript.maxSpin > 0f
            ? Mathf.Abs(ballScript.Spin) / ballScript.maxSpin
            : 0f;

        float errorAmount = baseError + errorAtMaxSpeed * speedT + errorAtMaxSpin * spinT;
        float error = Random.Range(-1f, 1f) * errorAmount * paddleHalfHeight;

        targetY = predictedY + error;
    }

    // Straight-line prediction with wall bounces folded in
    float PredictArrivalY(Vector2 pos, Vector2 vel, float targetX)
    {
        if (Mathf.Abs(vel.x) < 0.001f) return pos.y;

        float t = (targetX - pos.x) / vel.x;
        if (t < 0f) return pos.y;

        float y = pos.y + vel.y * t;

        float low = courtBottom + ballRadius;
        float high = courtTop - ballRadius;
        float range = high - low;
        if (range <= 0f) return y;

        // Mirror the path back into the court for every wall bounce
        return low + Mathf.PingPong(y - low, range);
    }

    void FindCourtEdges()
    {
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        bool foundTop = false, foundBottom = false;

        foreach (GameObject w in walls)
        {
            Collider2D c = w.GetComponent<Collider2D>();
            if (c == null) continue;

            if (c.bounds.center.y > 0f)
            {
                courtTop = foundTop ? Mathf.Min(courtTop, c.bounds.min.y) : c.bounds.min.y;
                foundTop = true;
            }
            else
            {
                courtBottom = foundBottom ? Mathf.Max(courtBottom, c.bounds.max.y) : c.bounds.max.y;
                foundBottom = true;
            }
        }
    }
}
