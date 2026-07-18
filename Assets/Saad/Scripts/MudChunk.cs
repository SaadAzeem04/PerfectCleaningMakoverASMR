using UnityEngine;
using System.Collections;

public class MudChunk : MonoBehaviour
{
    private bool isFallen = false;
    private MaskEraser manager;
    private Vector3 customVelocity;

    // --- LIVE FIX 1: Gravity aur vertical drop control karne ke liye ---
    private float customGravity = -7f; // Gravity kam kar di taake chunks tezi se neeche na bhaagein
    private bool isManualFalling = false;

    private Transform toolTransform;
    private float triggerDistance = 0.6f;

    private SpriteRenderer spriteRen;

    void Start()
    {
        manager = FindFirstObjectByType<MaskEraser>();
        spriteRen = GetComponent<SpriteRenderer>();

        if (manager != null && manager.toolFollower != null)
        {
            toolTransform = manager.toolFollower.transform;
        }
        else
        {
            GameObject toolFollowerObj = GameObject.Find("ToolFollower");
            if (toolFollowerObj != null)
            {
                toolTransform = toolFollowerObj.transform;
            }
        }
    }

    void Update()
    {
        if (!isFallen && toolTransform != null && Input.GetMouseButton(0))
        {
            float dist = Vector3.Distance(transform.position, toolTransform.position);
            if (dist <= triggerDistance)
            {
                FallDown();
            }
        }

        if (isManualFalling)
        {
            // Slow gravity fall animation
            customVelocity.y += customGravity * Time.deltaTime;
            transform.localPosition += customVelocity * Time.deltaTime;
            transform.Rotate(0, 0, Random.Range(30f, 60f) * Time.deltaTime);
        }
    }

    public void FallDown()
    {
        if (isFallen) return;
        isFallen = true;

        if (spriteRen != null)
        {
            spriteRen.sortingOrder = 50;
        }

        customVelocity = new Vector3(Random.Range(-2f, 2f), Random.Range(0.5f, 1.5f), 0);
        isManualFalling = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // --- FIXED: Manager ko report karo ke chunk gir gaya taake progress bar chalay ---
        if (manager != null)
        {
            manager.ChunkFallen();
        }

        StartCoroutine(FadeOutAndDestroyRoutine());
    }

    // ---  LIVE FIX 4: Coroutine Jo Chunk Ko Screen Par Hi Smoothly Fade Out Karegi ---
    IEnumerator FadeOutAndDestroyRoutine()
    {
        //  LIVE FIX: Chunks ko zameen tak pahunchne ke liye zyada time dein (e.g., 0.6 ya 0.8 seconds)
        // Is time ko aap check karke badha sakte hain jab tak wo zameen tak na pahunch jaye
        yield return new WaitForSeconds(0.7f);

        float fadeDuration = 0.3f; // Gayab hone ki speed (0.3 seconds mein smoothly fade hoga)
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