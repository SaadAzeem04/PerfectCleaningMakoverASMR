using System.Collections;
using UnityEngine;

public class MudChunk : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private bool isFalling = false;
    private Color originalColor;

    // ==========================================
    //  SCRIPT CONTROLLED SETTINGS
    // ==========================================
    private const float FLOOR_Y_THRESHOLD = -3.5f; // Zameen ki Y-position jahan chunk rukega
    private const float ROTATE_DURATION = 0.5f;    // Kitni der tak apni jagah rotate karega
    private const float ROTATE_ANGLE = 10f;        // Rotation ka angle (degrees)
    private const float ROTATE_SPEED = 1f;         // Pre-fall rotation speed
    private const float FADE_DURATION = 0.5f;      // Zameen par rukne ke kitni der baad fade out hoga
    // ==========================================

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. COLLIDE: Trigger on touch
        if (isFalling) return;

        if (collision.CompareTag("ScraperEdge") || collision.gameObject.name == "Scraper_Trigger_Edge")
        {
            isFalling = true;

            // MaskEraser ko notify karein
            MaskEraser eraser = Object.FindFirstObjectByType<MaskEraser>();
            if (eraser == null) eraser = FindObjectOfType<MaskEraser>();

            if (eraser != null)
            {
                eraser.ScraperChunkDestroyed();
            }

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            // Inactive check to prevent coroutine error
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            // Complete Sequence Start
            StartCoroutine(ChunkCompleteSequenceRoutine());
        }
    }

    private IEnumerator ChunkCompleteSequenceRoutine()
    {
        Quaternion initialRotation = transform.rotation;
        float elapsed = 0f;
        float direction = Random.value > 0.5f ? 1f : -1f;

        // ----------------------------------------------------
        // STEP 2: ROTATE FOR SOME SECONDS (Apni jagah wobble)
        // ----------------------------------------------------
        while (elapsed < ROTATE_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ROTATE_DURATION;

            float zAngle = Mathf.Sin(t * Mathf.PI * ROTATE_SPEED) * ROTATE_ANGLE * direction;
            transform.rotation = initialRotation * Quaternion.Euler(0f, 0f, zAngle);
            yield return null;
        }

        // ----------------------------------------------------
        // STEP 3: FALL (Gravity & Physics Enable)
        // ----------------------------------------------------
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
        rb.gravityScale = 3.5f;

        // Bouncing force & rotation while falling
        rb.linearVelocity = new Vector2(Random.Range(-3f, 3f), Random.Range(2f, 4f));
        rb.angularVelocity = Random.Range(-90f, 90f);

        // Zameen tak girne ka wait karein
        while (transform.position.y > FLOOR_Y_THRESHOLD)
        {
            yield return null;
        }

        // ----------------------------------------------------
        // STEP 4: STOP IN GROUND (Zameen par rukna aur lock hona)
        // ----------------------------------------------------
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // ----------------------------------------------------
        // STEP 5: FADE OUT & DESTROY
        // ----------------------------------------------------
        if (spriteRenderer != null)
        {
            elapsed = 0f;
            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsed / FADE_DURATION));
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}