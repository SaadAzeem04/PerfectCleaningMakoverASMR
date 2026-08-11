using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MudChunk : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private LineRenderer lineRenderer;
    private bool isFalling = false;
    private bool isGlowing = false;
    private Color originalColor;

    [Header("Level / Ground Settings")]
    [Tooltip("Agar dynamic floor calculate na ho paye to fallback value")]
    [SerializeField] private float floorYThreshold = -3.5f;

    [Header("Chunk Animation Settings")]
    [SerializeField] private float rotateDuration = 0.5f;   // Wobble duration
    [SerializeField] private float rotateAngle = 10f;       // Wobble angle
    [SerializeField] private float rotateSpeed = 1f;       // Wobble speed
    [SerializeField] private float fadeDuration = 0.5f;     // Fade duration

    [Header("Exact Sprite Edge Glow Settings")]
    [SerializeField] private Color greenGlowColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private float lineWidth = 0.08f; // Line thickness
    [SerializeField] private float glowPulseSpeed = 4f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        GenerateExactSpriteOutline();
    }

    private void Start()
    {
        // Camera Viewport ke hisaab se ground level calculate karein
        if (Camera.main != null)
        {
            float camZ = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 bottomScreenWorldPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.05f, camZ));
            floorYThreshold = bottomScreenWorldPos.y;
        }
    }

    private void GenerateExactSpriteOutline()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        Sprite sprite = spriteRenderer.sprite;
        List<Vector2> spriteEdgePoints = new List<Vector2>();

        if (sprite.GetPhysicsShapeCount() > 0)
        {
            sprite.GetPhysicsShape(0, spriteEdgePoints);
        }

        if (spriteEdgePoints.Count == 0) return;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        Shader lineShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (lineShader == null) lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null) lineShader = Shader.Find("Hidden/Internal-Colored");

        lineRenderer.material = new Material(lineShader);
        lineRenderer.startColor = greenGlowColor;
        lineRenderer.endColor = greenGlowColor;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;

        if (spriteRenderer != null)
        {
            lineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            lineRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        lineRenderer.positionCount = spriteEdgePoints.Count;
        for (int i = 0; i < spriteEdgePoints.Count; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(spriteEdgePoints[i].x, spriteEdgePoints[i].y, -0.1f));
        }

        lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (isGlowing && !isFalling && lineRenderer != null)
        {
            float pingPong = Mathf.PingPong(Time.time * glowPulseSpeed, 1f);
            Color pulsedColor = Color.Lerp(new Color(greenGlowColor.r, greenGlowColor.g, greenGlowColor.b, 0.2f), greenGlowColor, pingPong);

            lineRenderer.startColor = pulsedColor;
            lineRenderer.endColor = pulsedColor;
        }
    }

    public void SetGlow(bool glow)
    {
        if (isFalling) return;

        isGlowing = glow;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = glow;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isFalling) return;

        bool isScraper = collision.CompareTag("ScraperEdge") ||
                         collision.gameObject.name.ToLower().Contains("scraper");

        if (isScraper)
        {
            isFalling = true;
            SetGlow(false);

            // TOOL KE PEECHE BHEJNE KA ENHANCED LOGIC
            PushChunkBehindTool(collision.gameObject);

            MaskEraser eraser = Object.FindFirstObjectByType<MaskEraser>();
            if (eraser != null)
            {
                eraser.ScraperChunkDestroyed();
            }

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            StartCoroutine(ChunkCompleteSequenceRoutine());
        }
    }
    //// <summary>
    /// Tool ke visual SpriteRenderer ko trace karke chunk ko hamesha tool ke piche bheja jata hai.
    /// </summary>
    private void PushChunkBehindTool(GameObject toolObject)
    {
        // 1. Chunk ko Parent se Instant Detach karein taake parent iska order reset na kar sake
        transform.SetParent(null);

        SpriteRenderer toolSR = null;

        // Direct ToolFollower script se scene ka active Tool SpriteRenderer dhoondein (Sab se reliable tariqah)
        ToolFollower toolFollower = Object.FindFirstObjectByType<ToolFollower>();
        if (toolFollower != null)
        {
            toolSR = toolFollower.GetComponentInChildren<SpriteRenderer>();
        }

        // Backup Search: Agar ToolFollower na mile to collision object ki hierarchy check karein
        if (toolSR == null)
        {
            toolSR = toolObject.GetComponentInParent<SpriteRenderer>();
            if (toolSR == null) toolSR = toolObject.GetComponentInChildren<SpriteRenderer>();
        }

        // 2. Sorting Apply Karein
        if (toolSR != null)
        {
            int targetOrder = toolSR.sortingOrder - 5; // Direct Tool ke 5 points niche
            string targetLayer = toolSR.sortingLayerName;

            // Tamaam child sprites ka order change karein
            SpriteRenderer[] chunkSRs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var cSR in chunkSRs)
            {
                if (cSR == null) continue;
                cSR.sortingLayerName = targetLayer;
                cSR.sortingOrder = targetOrder;
            }

            // Agar SortingGroup component laga ho
            var sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (sortingGroup != null)
            {
                sortingGroup.sortingLayerName = targetLayer;
                sortingGroup.sortingOrder = targetOrder;
            }

            // 3. 3D World Z-Axis Par Bhi Tool Se Peeche Push Karein
            Vector3 pos = transform.position;
            pos.z = toolSR.transform.position.z + 0.5f; // +Z matlab Camera se door (Tool ke peeche)
            transform.position = pos;
        }
        else
        {
            // Safe Fallback (Agar Tool ka SR na mile to Order 50 kar do jo Tool ke order ~97 se bohot chota hai)
            SpriteRenderer[] chunkSRs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var cSR in chunkSRs)
            {
                if (cSR != null) cSR.sortingOrder = 50;
            }
        }
    }

    private IEnumerator ChunkCompleteSequenceRoutine()
    {
        Quaternion initialRotation = transform.rotation;
        float elapsed = 0f;
        float direction = Random.value > 0.5f ? 1f : -1f;

        // STEP 1: WOBBLE / ROTATE IN PLACE (Tool ke peeche)
        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateDuration;

            float zAngle = Mathf.Sin(t * Mathf.PI * rotateSpeed) * rotateAngle * direction;
            transform.rotation = initialRotation * Quaternion.Euler(0f, 0f, zAngle);
            yield return null;
        }

        // STEP 2: FALL WITH PHYSICS
        Collider2D chunkCollider = GetComponent<Collider2D>();
        if (chunkCollider != null)
        {
            chunkCollider.isTrigger = false;
        }

        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 4f;

        // Direct niche drop hone ke liye velocity
        rb.linearVelocity = new Vector2(Random.Range(-2f, 2f), Random.Range(-1f, -3f));
        rb.angularVelocity = Random.Range(-90f, 90f);

        while (transform.position.y > floorYThreshold)
        {
            yield return null;
        }

        // STEP 3: FREEZE ON GROUND
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // STEP 4: FADE OUT & DESTROY
        if (spriteRenderer != null)
        {
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}