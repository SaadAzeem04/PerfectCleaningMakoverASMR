using UnityEngine;
using System.Collections;

public class SmoothUIAnimate : MonoBehaviour
{
    [Header("Scale Animation Settings")]
    public float animDuration = 0.25f; // Kitni der me chhupe/dikh ga

    private Vector3 originalScale;
    private Coroutine activeCoroutine;

    private void Awake()
    {
        // Shuru me iska jo bhi scale inspector me set hai (e.g. 1, 1, 1), use save kar lo
        originalScale = transform.localScale;
    }

    public void ShowUI()
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);

        gameObject.SetActive(true); // Animation chalne ke liye object ka active hona zaroori hai
        activeCoroutine = StartCoroutine(ScaleRoutine(originalScale, false));
    }

    public void HideUI()
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);

        // Check karein ke GameObject active hai aur Hierarchy mein activeInHierarchy true hai ya nahi
        if (gameObject.activeInHierarchy)
        {
            // FIX: 'HideRoutine' ki jagah ScaleRoutine(Vector3.zero, true) call kiya hai
            activeCoroutine = StartCoroutine(ScaleRoutine(Vector3.zero, true));
        }
        else
        {
            // Agar pehle se inactive hai, to Coroutine chalanay ki zaroorat nahi hai
            gameObject.SetActive(false);
        }
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale, bool disableAtEnd)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;

            // Easing effect (Smooth transition bina Animator ke)
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;

        // Agar hide ho chuka hai, to smoothly zero hone ke baad hi SetActive(false) karo
        if (disableAtEnd)
        {
            gameObject.SetActive(false);
        }
    }
}