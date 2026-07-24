using System.Collections;
using UnityEngine;

public class MudChunk : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private bool isFalling = false;
    private Color originalColor;

    [Header("Level / Ground Settings")]
    [Tooltip("Is Y-position par chunk ruk kar fade out ho jayega.")]
    [SerializeField] private float floorYThreshold = -3.5f;

    [Header("Chunk Animation Settings")]
    [SerializeField] private float rotateDuration = 0.5f;   // Wobble duration
    [SerializeField] private float rotateAngle = 10f;       // Wobble angle
    [SerializeField] private float rotateSpeed = 1f;       // Wobble speed
    [SerializeField] private float fadeDuration = 0.5f;     // Fade duration

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isFalling) return;

        // Dynamic Check: Compare Tag ya Name contain check (Case insensitive)
        bool isScraper = collision.CompareTag("ScraperEdge") ||
                         collision.gameObject.name.ToLower().Contains("scraper");

        if (isScraper)
        {
            isFalling = true;

            // MaskEraser notification
            MaskEraser eraser = Object.FindFirstObjectByType<MaskEraser>();
            if (eraser != null)
            {
                eraser.ScraperChunkDestroyed();
            }

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            StartCoroutine(ChunkCompleteSequenceRoutine());
        }
    }

    private IEnumerator ChunkCompleteSequenceRoutine()
    {
        Quaternion initialRotation = transform.rotation;
        float elapsed = 0f;
        float direction = Random.value > 0.5f ? 1f : -1f;

        // STEP 1: WOBBLE / ROTATE IN PLACE
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
        rb.gravityScale = 3.5f;

        // Bouncing force & random rotation
        rb.linearVelocity = new Vector2(Random.Range(-3f, 3f), Random.Range(2f, 4f));
        rb.angularVelocity = Random.Range(-90f, 90f);

        // Wait until chunk drops below the Floor Y Threshold
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