using System.Collections;
using UnityEngine;

// Screen shake + hit-stop. Lives on the Main Camera.
// You don't need to add it by hand: GameFeel.Get() adds it automatically.
public class GameFeel : MonoBehaviour
{
    private static GameFeel instance;

    // Time scale used during a hit-stop. Deliberately not 0, so we can tell
    // "frozen by hit-stop" apart from "frozen by the pause menu" (which uses 0).
    private const float HitStopScale = 0.05f;

    private Vector3 originalPos;
    private float shakeTimeLeft;
    private float shakeDuration;
    private float shakeStrength;
    private Coroutine hitStopRoutine;

    public static GameFeel Get()
    {
        if (instance == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return null;

            instance = cam.GetComponent<GameFeel>();
            if (instance == null)
                instance = cam.gameObject.AddComponent<GameFeel>();
        }
        return instance;
    }

    void Awake()
    {
        instance = this;
        originalPos = transform.localPosition;
    }

    // strength = how far the camera moves (world units), duration = seconds
    public void Shake(float strength, float duration)
    {
        // Don't let a small shake cut a big one short
        if (strength >= shakeStrength * (shakeTimeLeft / Mathf.Max(shakeDuration, 0.0001f)))
        {
            shakeStrength = strength;
            shakeDuration = duration;
            shakeTimeLeft = duration;
        }
    }

    // Briefly slows time so a hit feels heavy
    public void HitStop(float seconds)
    {
        // Only hit-stop while the game is actually running at normal speed
        if (Time.timeScale != 1f) return;

        if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
        hitStopRoutine = StartCoroutine(HitStopRoutine(seconds));
    }

    IEnumerator HitStopRoutine(float seconds)
    {
        Time.timeScale = HitStopScale;
        yield return new WaitForSecondsRealtime(seconds);

        // If the player paused (timeScale 0) or the game ended meanwhile, leave it alone
        if (Mathf.Approximately(Time.timeScale, HitStopScale))
            Time.timeScale = 1f;

        hitStopRoutine = null;
    }

    void LateUpdate()
    {
        if (shakeTimeLeft > 0f)
        {
            // unscaled so the shake still plays during hit-stop / game over
            shakeTimeLeft -= Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01(shakeTimeLeft / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * shakeStrength * fade;
            transform.localPosition = originalPos + (Vector3)offset;
        }
        else
        {
            shakeStrength = 0f;
            transform.localPosition = originalPos;
        }
    }
}
