using UnityEngine;

public class MudChunk : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private bool isFalling = false;
    private float fadeTimer = 0.5f; // Kitni der me fade out hoga
    private Color originalColor;

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
    }

    private void Update()
    {
        // Agar chunk gir raha hai, to har frame iska alpha kam (fade out) karte jao
        if (isFalling && spriteRenderer != null)
        {
            fadeTimer -= Time.deltaTime;

            // Alpha value calculate karein (0.5 se lekar 0 tak smoothly)
            float alpha = Mathf.Clamp01(fadeTimer / 0.5f);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            // Jab poori tarah transparent ho jaye, tab object ko destroy karein
            if (fadeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}