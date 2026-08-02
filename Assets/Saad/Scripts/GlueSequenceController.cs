/*using System.Collections;
using UnityEngine;

public class GlueSequenceController : MonoBehaviour
{
    [Header("Glue References")]
    public GameObject glueContainer;
    public Transform glueCap;

    [Header("Purse Images (Before & After)")]
    [Tooltip("Patch lagne se pehle wali Purse (Damaged)")]
    public GameObject purseBeforePatch;

    [Tooltip("Patch lagne ke baad wali Purse (Repaired)")]
    public GameObject purseAfterPatch;

    [Header("Hand Tutorials")]
    public Transform handTutorialGlue;
    public Transform handTutorialPatch;

    private MaskEraser maskEraser;
    private bool isCapOpened = false;
    private bool isGlueApplied = false;
    private bool isPatchPlaced = false;

    void Start()
    {
        // Scene se MaskEraser auto-find karein
        maskEraser = FindFirstObjectByType<MaskEraser>();

        // Initial Setup
        if (glueContainer != null) glueContainer.SetActive(true);
        if (glueCap != null) glueCap.gameObject.SetActive(true);

        // Pehle wali Purse dikhegi, After wali hidden rahegi
        if (purseBeforePatch != null) purseBeforePatch.SetActive(true);
        if (purseAfterPatch != null) purseAfterPatch.SetActive(false);

        if (handTutorialGlue != null) handTutorialGlue.gameObject.SetActive(true);
        if (handTutorialPatch != null) handTutorialPatch.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Debug.Log("<color=green>--- GlueSequenceController Active Ho Gaya! ---</color>");

        if (handTutorialGlue != null)
        {
            Debug.Log("<color=yellow>Hand Tutorial Found: " + handTutorialGlue.name + "</color>");
            handTutorialGlue.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(AnimateHandTutorial(handTutorialGlue));
        }
        else
        {
            Debug.LogError("Hand Tutorial Glue Reference MISSING in Inspector!");
        }
    }

    // Cap par Tap hone par call karein
    public void OnCapTapped()
    {
        if (!isCapOpened)
        {
            isCapOpened = true;
            if (handTutorialGlue != null) handTutorialGlue.gameObject.SetActive(false);
            StartCoroutine(AnimateCapOff());
        }
    }

    // Cap Unscrew Animation
    private IEnumerator AnimateCapOff()
    {
        float duration = 0.8f;
        float elapsed = 0f;

        Vector3 startPos = glueCap.localPosition;
        Vector3 targetPos = startPos + new Vector3(0f, 1.5f, 0f);
        Quaternion startRot = glueCap.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0f, 0f, 360f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            glueCap.localPosition = Vector3.Lerp(startPos, targetPos, t);
            glueCap.localRotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        glueCap.gameObject.SetActive(false);
    }

    // Glue Application poori hone par call hoga
    public void OnGlueAppliedComplete()
    {
        if (isGlueApplied) return;
        isGlueApplied = true;

        if (glueContainer != null) glueContainer.SetActive(false);

        // Patch tutorial active karein
        if (handTutorialPatch != null)
        {
            handTutorialPatch.gameObject.SetActive(true);
            StartCoroutine(AnimateHandTutorial(handTutorialPatch));
        }
    }

    // Player jab Screen / Purse par Tap karega Patch lagane ke liye
    public void OnPatchPlaced()
    {
        if (isPatchPlaced) return;
        isPatchPlaced = true;

        if (handTutorialPatch != null) handTutorialPatch.gameObject.SetActive(false);
        StartCoroutine(AnimatePatchTransition());
    }

    // Transition: Before Purse hide hogi aur After Purse Scale-In hoke aayegi
    private IEnumerator AnimatePatchTransition()
    {
        if (purseAfterPatch != null)
        {
            purseAfterPatch.SetActive(true);

            // Pop-In / Scale-In Animation
            float duration = 0.4f;
            float elapsed = 0f;
            Vector3 targetScale = purseAfterPatch.transform.localScale;
            purseAfterPatch.transform.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = t * t * (3f - 2f * t);

                purseAfterPatch.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, smoothT);
                yield return null;
            }

            purseAfterPatch.transform.localScale = targetScale;
        }

        // Purani Damaged Purse ko Hide kar dein
        if (purseBeforePatch != null) purseBeforePatch.SetActive(false);

        yield return new WaitForSeconds(0.3f);
        CompleteSequence();
    }

    private void CompleteSequence()
    {
        Debug.Log("Glue & Patch Sequence Complete!");
        if (maskEraser != null)
        {
            maskEraser.OnCurrentStepCompleted();
        }
    }

    // Hand Tutorial Idle Floating Animation
    private IEnumerator AnimateHandTutorial(Transform handTransform)
    {
        Vector3 basePos = handTransform.localPosition;
        while (handTransform != null && handTransform.gameObject.activeInHierarchy)
        {
            // Canvas scaling ke liye amplitude 30f rakha hai, Time.unscaledTime pause fix karta hai
            float offset = Mathf.Sin(Time.unscaledTime * 5f) * 30f;
            handTransform.localPosition = basePos + new Vector3(0f, offset, 0f);
            yield return null;
        }
    }
}*/