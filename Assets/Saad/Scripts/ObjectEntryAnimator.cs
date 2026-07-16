using UnityEngine;
using System.Collections;

public class ObjectEntryAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float animationDuration = 0.5f;
    public float delayBeforeStart = 0.1f;

    void Start()
    {
        // Script start hote hi animation shuru kar do
        StartCoroutine(AnimateEntry());
    }

    IEnumerator AnimateEntry()
    {
        // Object ka asli size save kar lo
        Vector3 finalScale = transform.localScale;

        // Shuru mein object ko bilkul chupa do (size 0)
        transform.localScale = Vector3.zero;

        // Thora sa intezar (taake level load hote hi fauran na ho)
        yield return new WaitForSeconds(delayBeforeStart);

        float time = 0;
        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;

            // Yeh math formula (EaseOutBack) ek khoobsurat pop-up/bounce effect deta hai
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float easedT = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

            transform.localScale = finalScale * easedT;
            yield return null;
        }

        // Aakhir mein size ko bilkul perfect final scale par set kar do
        transform.localScale = finalScale;
    }
}