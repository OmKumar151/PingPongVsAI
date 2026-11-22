using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Neon arcade look, generated entirely in code (no image files needed).
//
// It adds itself automatically when the game starts. To tweak colours in the
// Inspector, add this component to any GameObject in the scene yourself —
// the automatic one is skipped when one already exists.
//
// What it does:
//  - dark background, glowing grid, centre line, neon walls, goal-side tints
//  - glowing paddles (cyan = you, magenta = AI) that flash when they hit
//  - glowing ball whose colour follows speed/spin (same as the trail)
//  - spark bursts on hits and goals, bloom/vignette post-processing, goal pulse
//  - restyles the UI: dark panels with neon outlines, glowing text
public class NeonTheme : MonoBehaviour
{
    [Header("Colours")]
    public Color playerColor = new Color(0f, 0.9f, 1f);
    public Color aiColor = new Color(1f, 0.2f, 0.8f);
    public Color wallColor = new Color(0.6f, 0.4f, 1f);
    public Color gridColor = new Color(0.45f, 0.25f, 0.9f, 0.16f);
    public Color backgroundColor = new Color(0.02f, 0.01f, 0.05f);

    [Header("Post Processing")]
    public float bloomIntensity = 1.3f;
    public float bloomThreshold = 0.75f;
    public float vignetteIntensity = 0.35f;

    public static NeonTheme Instance { get; private set; }

    // Fallbacks so other scripts can use the colours even if the theme is off
    public static Color PlayerColor => Instance != null ? Instance.playerColor : new Color(0f, 0.9f, 1f);
    public static Color AiColor => Instance != null ? Instance.aiColor : new Color(1f, 0.2f, 0.8f);

    // ---------------------------------------------------------------
    // Sorting orders (back to front)
    const int OrderBackground = -20, OrderGrid = -19, OrderGoalTint = -18, OrderCenterLine = -15;
    const int OrderWallGlow = -6, OrderWall = -5, OrderGlow = 1, OrderCore = 2, OrderBall = 3, OrderSparks = 5;

    // Generated sprites
    Sprite roundRect;   // sliced, for paddles / walls / dashes
    Sprite rectGlow;    // sliced, soft halo around rectangles
    Sprite circle;      // ball core
    Sprite radialGlow;  // ball halo, sparks, background haze
    Sprite gridTile;    // tiled background grid
    Sprite edgeFade;    // goal-side tint

    const float GlowRadius = 0.35f; // world units the halo extends past a shape

    class PaddleVis
    {
        public SpriteRenderer core, glow;
        public Color color;
        public float flash;
    }
    readonly Dictionary<GameObject, PaddleVis> paddles = new Dictionary<GameObject, PaddleVis>();

    Ball ball;
    SpriteRenderer ballCore, ballGlow;

    SpriteRenderer leftTint, rightTint;
    float leftFlash, rightFlash;

    Bloom bloom;
    ChromaticAberration chroma;
    float fxPulse;

    float courtTop = 5f, courtBottom = -5f, halfWidth = 9f;

    // ---------------------------------------------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindFirstObjectByType<NeonTheme>() == null)
            new GameObject("NeonTheme").AddComponent<NeonTheme>();
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        BuildSprites();
        ReadCourt();

        StyleCamera();
        BuildBackground();
        BuildWalls();
        BuildGoalTints();
        StylePaddles();
        StyleBall();
        StyleUI();
    }

    // ===============================================================
    // EVENTS (called by Ball / Goal)
    // ===============================================================

    public static void OnPaddleHit(Vector2 point, GameObject paddle)
    {
        if (Instance == null) return;

        Color c = Color.white;
        if (Instance.paddles.TryGetValue(paddle, out PaddleVis vis))
        {
            vis.flash = 1f;
            c = vis.color;
        }

        // sparks fly away from the paddle
        float away = Mathf.Sign(point.x - paddle.transform.position.x);
        Instance.Burst(point, c, 14, 6f, 0.35f, 0.25f, new Vector2(away * 0.8f, 0f));
    }

    public static void OnWallHit(Vector2 point)
    {
        if (Instance == null) return;
        float away = point.y > 0 ? -1f : 1f;
        Instance.Burst(point, Instance.wallColor, 6, 4f, 0.25f, 0.18f, new Vector2(0f, away * 0.8f));
    }

    public static void OnGoal(Vector2 point, bool leftGoal)
    {
        if (Instance == null) return;

        // Left goal = AI scored, right goal = player scored
        Color scorer = leftGoal ? Instance.aiColor : Instance.playerColor;

        if (leftGoal) Instance.leftFlash = 1f;
        else Instance.rightFlash = 1f;

        Instance.fxPulse = 1f;
        Instance.Burst(point, scorer, 40, 10f, 0.8f, 0.35f, Vector2.zero);
        Instance.Burst(point, Color.white, 12, 5f, 0.5f, 0.25f, Vector2.zero);
    }

    // ===============================================================
    // PER-FRAME ANIMATION
    // ===============================================================

    void Update()
    {
        // unscaled so effects keep animating during hit-stop / game over
        float dt = Time.unscaledDeltaTime;
        float breathe = 0.05f * Mathf.Sin(Time.unscaledTime * 2.5f);

        foreach (PaddleVis p in paddles.Values)
        {
            p.flash = Mathf.MoveTowards(p.flash, 0f, dt * 6f);
            p.core.color = Color.Lerp(p.color, Color.white, p.flash * 0.8f);
            p.glow.color = WithAlpha(p.color, 0.45f + breathe + 0.5f * p.flash);
        }

        if (ball != null && ballGlow != null)
        {
            Color g = ball.GlowColor;
            ballGlow.color = WithAlpha(g, 0.65f);
            ballCore.color = Color.Lerp(Color.white, WithAlpha(g, 1f), 0.25f);
        }

        leftFlash = Mathf.MoveTowards(leftFlash, 0f, dt * 2f);
        rightFlash = Mathf.MoveTowards(rightFlash, 0f, dt * 2f);
        if (leftTint != null)
            leftTint.color = WithAlpha(Color.Lerp(playerColor, aiColor, leftFlash), 0.12f + 0.6f * leftFlash);
        if (rightTint != null)
            rightTint.color = WithAlpha(Color.Lerp(aiColor, playerColor, rightFlash), 0.12f + 0.6f * rightFlash);

        fxPulse = Mathf.MoveTowards(fxPulse, 0f, dt * 2.5f);
        if (bloom != null) bloom.intensity.value = bloomIntensity + fxPulse * 2f;
        if (chroma != null) chroma.intensity.value = 0.12f + fxPulse * 0.6f;

        UpdateSparks(dt);
    }

    // ===============================================================
    // SCENE SETUP
    // ===============================================================

    void ReadCourt()
    {
        foreach (GameObject w in GameObject.FindGameObjectsWithTag("Wall"))
        {
            Collider2D c = w.GetComponent<Collider2D>();
            if (c == null) continue;
            if (c.bounds.center.y > 0f) courtTop = c.bounds.min.y;
            else courtBottom = c.bounds.max.y;
        }

        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
            halfWidth = cam.orthographicSize * cam.aspect;
    }

    void StyleCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;

        // Post-processing must be switched on per camera in URP
        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        if (data != null) data.renderPostProcessing = true;

        GameObject go = new GameObject("NeonPostFX");
        go.transform.SetParent(transform, false);
        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(bloomThreshold);
        bloom.intensity.Override(bloomIntensity);
        bloom.scatter.Override(0.7f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(0.5f);

        chroma = profile.Add<ChromaticAberration>(true);
        chroma.intensity.Override(0.12f);

        volume.sharedProfile = profile;
    }

    void BuildBackground()
    {
        float h = courtTop - courtBottom;
        float midY = (courtTop + courtBottom) * 0.5f;

        // Soft purple haze in the middle for depth
        SpriteRenderer haze = MakeSprite("BG Haze", transform, radialGlow, new Color(0.35f, 0.15f, 0.7f, 0.35f), OrderBackground);
        haze.transform.position = new Vector3(0f, midY, 0f);
        haze.transform.localScale = new Vector3(halfWidth * 2f / 2f, h / 2f, 1f); // radialGlow is 2 units wide

        // Grid
        SpriteRenderer grid = MakeSprite("BG Grid", transform, gridTile, gridColor, OrderGrid);
        grid.drawMode = SpriteDrawMode.Tiled;
        grid.size = new Vector2(halfWidth * 2f + 2f, h + 2f);
        grid.transform.position = new Vector3(0f, midY, 0f);

        // Dashed centre line
        const float dash = 0.35f, gap = 0.3f;
        for (float y = courtBottom + gap; y + dash <= courtTop; y += dash + gap)
        {
            SpriteRenderer d = MakeSprite("Center Dash", transform, roundRect, WithAlpha(wallColor, 0.35f), OrderCenterLine);
            d.drawMode = SpriteDrawMode.Sliced;
            d.size = new Vector2(0.08f, dash);
            d.transform.position = new Vector3(0f, y + dash * 0.5f, 0f);
        }
    }

    void BuildWalls()
    {
        foreach (float y in new[] { courtTop, courtBottom })
        {
            Vector2 size = new Vector2(halfWidth * 2f + 1f, 0.07f);

            SpriteRenderer glow = MakeSprite("Wall Glow", transform, rectGlow, WithAlpha(wallColor, 0.5f), OrderWallGlow);
            glow.drawMode = SpriteDrawMode.Sliced;
            glow.size = size + Vector2.one * GlowRadius * 2f;
            glow.transform.position = new Vector3(0f, y, 0f);

            SpriteRenderer line = MakeSprite("Wall Line", transform, roundRect, wallColor, OrderWall);
            line.drawMode = SpriteDrawMode.Sliced;
            line.size = size;
            line.transform.position = new Vector3(0f, y, 0f);
        }
    }

    void BuildGoalTints()
    {
        float h = courtTop - courtBottom;
        float midY = (courtTop + courtBottom) * 0.5f;
        const float width = 1.6f; // edgeFade sprite is 1 x 1 world unit

        leftTint = MakeSprite("Left Goal Tint", transform, edgeFade, WithAlpha(playerColor, 0.12f), OrderGoalTint);
        leftTint.transform.position = new Vector3(-halfWidth + width * 0.5f, midY, 0f);
        leftTint.transform.localScale = new Vector3(width, h, 1f);

        rightTint = MakeSprite("Right Goal Tint", transform, edgeFade, WithAlpha(aiColor, 0.12f), OrderGoalTint);
        rightTint.flipX = true;
        rightTint.transform.position = new Vector3(halfWidth - width * 0.5f, midY, 0f);
        rightTint.transform.localScale = new Vector3(width, h, 1f);
    }

    void StylePaddles()
    {
        PlayerPaddle player = FindFirstObjectByType<PlayerPaddle>();
        AiPaddle ai = FindFirstObjectByType<AiPaddle>();
        if (player != null) StylePaddle(player.gameObject, playerColor);
        if (ai != null) StylePaddle(ai.gameObject, aiColor);
    }

    void StylePaddle(GameObject paddle, Color color)
    {
        Collider2D col = paddle.GetComponent<Collider2D>();
        if (col == null) return;

        HideOriginal(paddle);

        Vector2 size = col.bounds.size;
        Vector3 centerOffset = col.bounds.center - paddle.transform.position;

        PaddleVis vis = new PaddleVis { color = color };

        vis.glow = MakeSprite("Neon Glow", paddle.transform, rectGlow, WithAlpha(color, 0.45f), OrderGlow);
        vis.glow.drawMode = SpriteDrawMode.Sliced;
        vis.glow.size = size + Vector2.one * GlowRadius * 2f;
        vis.glow.transform.position = paddle.transform.position + centerOffset;

        vis.core = MakeSprite("Neon Core", paddle.transform, roundRect, color, OrderCore);
        vis.core.drawMode = SpriteDrawMode.Sliced;
        vis.core.size = size;
        vis.core.transform.position = paddle.transform.position + centerOffset;

        paddles[paddle] = vis;
    }

    void StyleBall()
    {
        ball = FindFirstObjectByType<Ball>();
        if (ball == null) return;

        Collider2D col = ball.GetComponent<Collider2D>();
        float diameter = col != null ? col.bounds.size.x : 0.4f;

        HideOriginal(ball.gameObject);

        // radialGlow / circle are 2 units / 1 unit wide at scale 1
        ballGlow = MakeSprite("Neon Glow", ball.transform, radialGlow, Color.white, OrderGlow);
        ballGlow.transform.localScale = Vector3.Scale(ballGlow.transform.localScale, Vector3.one * diameter * 1.6f);

        ballCore = MakeSprite("Neon Core", ball.transform, circle, Color.white, OrderBall);
        ballCore.transform.localScale = Vector3.Scale(ballCore.transform.localScale, Vector3.one * diameter);
    }

    // ===============================================================
    // UI
    // ===============================================================

    void StyleUI()
    {
        Color panelBg = new Color(0.03f, 0.02f, 0.08f, 0.88f);
        Color buttonBg = new Color(0.07f, 0.04f, 0.16f, 0.95f);

        // Text: soft coloured underlay = glow. One material per (font material, colour).
        var glowMats = new Dictionary<(Material, Color), Material>();
        foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool onButton = t.GetComponentInParent<Button>(true) != null;
            bool inInput = t.GetComponentInParent<TMP_InputField>(true) != null;
            string n = t.gameObject.name;

            Color glowColor = playerColor;
            if (onButton) t.color = playerColor;
            else if (n.Contains("Title")) t.color = playerColor;
            else if (n.Contains("Header")) { t.color = aiColor; glowColor = aiColor; }
            else if (n.Contains("Timer")) { t.color = new Color(0.8f, 0.75f, 1f); glowColor = wallColor; }
            else if (!inInput) t.color = Color.white;

            Material src = t.fontSharedMaterial;
            if (src == null) continue;

            if (!glowMats.TryGetValue((src, glowColor), out Material m))
            {
                m = new Material(src);
                m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                m.SetColor(ShaderUtilities.ID_UnderlayColor, WithAlpha(glowColor, 0.55f));
                m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.8f);
                m.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.4f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                glowMats[(src, glowColor)] = m;
            }
            t.fontSharedMaterial = m;
        }

        // Buttons, input fields, panels
        foreach (Image img in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Selectable sel = img.GetComponent<Selectable>();

            if (sel is Button btn)
            {
                img.color = Color.white; // tint comes from the ColorBlock below
                ColorBlock cb = btn.colors;
                cb.normalColor = buttonBg;
                cb.selectedColor = buttonBg;
                cb.highlightedColor = new Color(0f, 0.35f, 0.45f, 1f);
                cb.pressedColor = new Color(0f, 0.6f, 0.75f, 1f);
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.08f;
                btn.colors = cb;
                AddOutline(img.gameObject, playerColor);
            }
            else if (sel is TMP_InputField)
            {
                img.color = buttonBg;
                AddOutline(img.gameObject, aiColor);
            }
            else if (sel == null && img.GetComponentInParent<Selectable>(true) == null)
            {
                Rect r = img.rectTransform.rect;
                bool isPanel = r.width >= 250f && r.height >= 150f;

                img.color = isPanel ? panelBg : new Color(1f, 1f, 1f, 0.04f); // rows etc: faint strip
                if (isPanel) AddOutline(img.gameObject, WithAlpha(aiColor, 0.7f));
            }
        }
    }

    static void AddOutline(GameObject go, Color c)
    {
        Outline o = go.GetComponent<Outline>();
        if (o == null) o = go.AddComponent<Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(2f, -2f);
        o.useGraphicAlpha = false;
    }

    // ===============================================================
    // SPARKS (tiny pooled particle system using sprites)
    // ===============================================================

    class Spark
    {
        public SpriteRenderer sr;
        public Vector2 vel;
        public float life, maxLife, size;
        public Color color;
    }
    readonly List<Spark> sparks = new List<Spark>();
    readonly Stack<Spark> sparkPool = new Stack<Spark>();
    const int MaxSparks = 200;

    void Burst(Vector2 pos, Color color, int count, float speed, float life, float size, Vector2 bias)
    {
        for (int i = 0; i < count && sparks.Count < MaxSparks; i++)
        {
            Spark s = sparkPool.Count > 0 ? sparkPool.Pop() : NewSpark();
            s.sr.gameObject.SetActive(true);
            s.sr.transform.position = pos;

            Vector2 dir = (Random.insideUnitCircle.normalized + bias).normalized;
            s.vel = dir * speed * Random.Range(0.35f, 1f);
            s.maxLife = s.life = life * Random.Range(0.6f, 1f);
            s.size = size * Random.Range(0.6f, 1.2f);
            s.color = color;
            sparks.Add(s);
        }
    }

    Spark NewSpark()
    {
        SpriteRenderer sr = MakeSprite("Spark", transform, radialGlow, Color.white, OrderSparks);
        return new Spark { sr = sr };
    }

    void UpdateSparks(float dt)
    {
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            Spark s = sparks[i];
            s.life -= dt;
            if (s.life <= 0f)
            {
                s.sr.gameObject.SetActive(false);
                sparks.RemoveAt(i);
                sparkPool.Push(s);
                continue;
            }

            float k = s.life / s.maxLife;
            s.vel *= 1f - Mathf.Min(1f, 4f * dt); // drag
            s.sr.transform.position += (Vector3)(s.vel * dt);
            s.sr.transform.localScale = Vector3.one * s.size * k;
            s.sr.color = WithAlpha(Color.Lerp(Color.white, s.color, 1f - k * 0.5f), k);
        }
    }

    // ===============================================================
    // HELPERS
    // ===============================================================

    // Creates a sprite object. If parented, cancels the parent's scale so
    // sizes are in world units (paddles are scaled 0.5 x 3, ball 0.5).
    SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, Color color, int order)
    {
        GameObject go = new GameObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
            Vector3 s = parent.lossyScale;
            go.transform.localScale = new Vector3(
                s.x != 0f ? 1f / s.x : 1f,
                s.y != 0f ? 1f / s.y : 1f,
                1f);
        }

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static void HideOriginal(GameObject go)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
    }

    static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    // ===============================================================
    // PROCEDURAL TEXTURES
    // ===============================================================

    void BuildSprites()
    {
        // Rounded rectangle: 32px, corner radius 15px -> sliced border 15px.
        // PPU chosen so the corner radius is 0.25 world units (half a paddle width).
        {
            const int size = 32; const float r = 15f;
            Texture2D tex = NewTex(size, size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedBoxDistance(x + 0.5f, y + 0.5f, size, size, r);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
                }
            tex.Apply();
            roundRect = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                r / 0.25f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        // Soft halo around a rectangle: falls off over 'R' pixels = GlowRadius world units
        {
            const int R = 32; int size = R * 2 + 2;
            Texture2D tex = NewTex(size, size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size * 0.5f) - 1f);
                    float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size * 0.5f) - 1f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / R;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            tex.Apply();
            rectGlow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                R / GlowRadius, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
        }

        // Circle (1 world unit wide)
        {
            const int size = 128;
            Texture2D tex = NewTex(size, size);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(c - 1f - d)));
                }
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Radial glow (2 world units wide)
        {
            const int size = 128;
            Texture2D tex = NewTex(size, size);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a * a));
                }
            tex.Apply();
            radialGlow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f);
        }

        // Grid tile: 1 world unit cell with thin lines and a brighter dot at crossings
        {
            const int size = 64;
            Texture2D tex = NewTex(size, size);
            tex.wrapMode = TextureWrapMode.Repeat;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool lineX = x < 2, lineY = y < 2;
                    float a = (lineX || lineY) ? 0.6f : 0f;
                    if (lineX && lineY) a = 1f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            gridTile = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                size, 0, SpriteMeshType.FullRect);
        }

        // Horizontal fade: opaque at the left edge, transparent at the right (1 unit wide)
        {
            const int w = 64, h = 64;
            Texture2D tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = 1f - (x + 0.5f) / w;
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            tex.Apply();
            edgeFade = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }
    }

    static Texture2D NewTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    // Signed distance (pixels) from point to a rounded box filling a w x h texture
    static float RoundedBoxDistance(float px, float py, float w, float h, float r)
    {
        float qx = Mathf.Abs(px - w * 0.5f) - (w * 0.5f - r);
        float qy = Mathf.Abs(py - h * 0.5f) - (h * 0.5f - r);
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }
}
