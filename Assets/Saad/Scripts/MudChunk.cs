using UnityEngine;
using System.Collections;

public class MudChunk : MonoBehaviour
{
    private bool isFallen = false;
    private MaskEraser manager;
    private Vector3 customVelocity;
    private float customGravity = -9.8f * 1.5f;
    private bool isManualFalling = false;
    private SpriteRenderer spriteRen;

    public Sprite newMudChunkSprite;

    void Start()
    {
        if (spriteRen != null && newMudChunkSprite != null)
        {
            spriteRen.sprite = newMudChunkSprite;
        }

        manager = FindFirstObjectByType<MaskEraser>();
        spriteRen = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Agar chunk physics ke zariye gir raha hai to uski movement handle karein
        if (isManualFalling)
        {
            customVelocity.y += customGravity * Time.deltaTime;
            transform.localPosition += customVelocity * Time.deltaTime;
            transform.Rotate(0, 0, Random.Range(30f, 60f) * Time.deltaTime);
        }
    }

    // NEW ISOLATED COLLISION SYSTEM
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Jaise hi Scraper ka child edge isse takrayega, yeh gir jayega
        if (collision.CompareTag("ScraperEdge"))
        {
            FallDown();
        }
    }

    public void FallDown()
    {
        if (isFallen) return;
        isFallen = true;

        if (spriteRen != null)
        {
            spriteRen.sortingOrder = 50; // Z-order upar karein taake baqi layers ke piche na chupe
        }

        // Random direction me girne ki velocity
        customVelocity = new Vector3(Random.Range(-2f, 2f), Random.Range(0.5f, 1.5f), 0);
        isManualFalling = true;

        // Collider disable karein taake dobara kisi cheez se na takraye girte hue
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Manager ko report karna ke ek chunk gir gaya hai
        if (manager != null)
        {
            manager.ChunkFallen();
        }

        StartCoroutine(FadeOutAndDestroyRoutine());
    }

    IEnumerator FadeOutAndDestroyRoutine()
    {
        yield return new WaitForSeconds(0.7f);

        float fadeDuration = 0.3f;
        float currentTime = 0f;

        if (spriteRen != null)
        {
            Color originalColor = spriteRen.color;

            while (currentTime < fadeDuration)
            {
                currentTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, currentTime / fadeDuration);
                spriteRen.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}