using System.Collections;
using UnityEngine;

public class MudChunk : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private bool isFalling = false;
    private bool isFading = false;
    private float fadeTimer = 0.5f; // Kitni der me fade out hoga
    private Color originalColor;

    [Header("Floor & Natural Feel Settings")]
    [SerializeField] private float floorYThreshold = -3.8f; // Floor limit jahan chunks ikhatte honge
    [SerializeField] private float shakeAmount = 0.03f;     // Scraping break shake distance
    [SerializeField] private float shakeFrequency = 20f;    // Shake ki speed (LOWER = SLOWER / SMOOTHER)

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Agar chunk pehle hi gir raha hai to dobara collision register na ho
        if (isFalling) return;

        // Jaise hi scraper ka edge is chunk se takrayega
        if (collision.CompareTag("ScraperEdge") || collision.gameObject.name == "Scraper_Trigger_Edge")
        {
            StartCoroutine(ShakeAndFallRoutine(collision));
        }
    }

    private IEnumerator ShakeAndFallRoutine(Collider2D collision)
    {
        // 1. Smooth & Slow Shake Logic (Sine Wave Motion)
        Vector3 initialPos = transform.position;
        float shakeDuration = 0.12f; // Time kitni der tak shake chalega
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Frequency se smooth speed control aur Sine wave se slow shake
            float offsetX = Mathf.Sin(elapsed * shakeFrequency) * shakeAmount;
            float offsetY = Mathf.Cos(elapsed * shakeFrequency * 0.8f) * (shakeAmount * 0.5f);

            transform.position = initialPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }

        // Position reset taake detachment natural lage
        transform.position = initialPos;

        isFalling = true;

        // 1. MaskEraser ko count kam karne ka signal bhejein
        MaskEraser eraser = Object.FindFirstObjectByType<MaskEraser>();
        if (eraser == null) eraser = FindObjectOfType<MaskEraser>();

        if (eraser != null)
        {
            eraser.ScraperChunkDestroyed();
        }

        // 2. Trigger ko normal physical collider me badlein taake gravity se niche gire
        Collider2D chunkCollider = GetComponent<Collider2D>();
        if (chunkCollider != null)
        {
            chunkCollider.isTrigger = false;
        }

        // 3. Rigidbody add/setup karein taake weight ke sath niche gire
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2.5f;

        // Random force aur torque (rotation) taake natural look aaye
        rb.linearVelocity = new Vector2(Random.Range(-2f, 2f), Random.Range(1f, 3f));
        rb.angularVelocity = Random.Range(-150f, 150f);

        // Color save kar lete hain fade out ke liye
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        if (!isFalling) return;

        // Ground / Floor Check: Jab threshold hit ho jaye
        if (transform.position.y <= floorYThreshold && !isFading)
        {
            isFading = true;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f; // <--- Spinning (rotate hona) roke ga
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            // REMOVED: transform.rotation = Quaternion.identity; 
            // Is line ko hata diya hai taake chunk jis angle par zameen par gira hai, usi position/orientation me rahe!
        }

        // Ground hit hone ke baad smooth fade out
        if (isFading && spriteRenderer != null)
        {
            fadeTimer -= Time.deltaTime;

            float alpha = Mathf.Clamp01(fadeTimer / 0.5f);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            if (fadeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}