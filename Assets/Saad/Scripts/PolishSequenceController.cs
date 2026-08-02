using System.Collections;
using UnityEngine;

public class PolishSequenceController : MonoBehaviour
{
    public static PolishSequenceController Instance;

    [Header("Polish Container Reference")]
    public Transform polishContainer;

    [Header("Audio Settings")]
    [Tooltip("Container par rub hotey waqt specific rubbing sound effect yahan drag karein")]
    public AudioClip containerRubSound;

    [Header("Animation Settings")]
    public Vector3 slideInOffset = new Vector3(12f, 0f, 0f);
    public float slideDuration = 0.5f;
    public float rubDuration = 1.2f;
    public float rubSpeed = 18f;
    public float rubAmount = 0.35f;

    private Vector3 originalContainerPos;
    private bool isPosSaved = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (polishContainer != null)
        {
            originalContainerPos = polishContainer.position;
            isPosSaved = true;
            polishContainer.gameObject.SetActive(false);
        }
    }

    public IEnumerator PlayPolishSequenceRoutine(ToolFollower toolFollower, ToolData currentToolData)
    {
        if (polishContainer == null || toolFollower == null) yield break;

        toolFollower.enabled = false;

        if (!isPosSaved)
        {
            originalContainerPos = polishContainer.position;
            isPosSaved = true;
        }

        Vector3 targetContainerPos = originalContainerPos;
        Vector3 containerStartPos = targetContainerPos + slideInOffset;

        // 1. CONTAINER SLIDE-IN
        polishContainer.position = containerStartPos;
        polishContainer.gameObject.SetActive(true);

        float time = 0f;
        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / slideDuration);
            polishContainer.position = Vector3.Lerp(containerStartPos, targetContainerPos, t);
            yield return null;
        }
        polishContainer.position = targetContainerPos;

        // 2. BRUSH MOVES TO CONTAINER
        Vector3 brushStartPos = toolFollower.transform.position;
        Vector3 brushRubSpot = targetContainerPos + new Vector3(0f, 0.4f, 0f);

        time = 0f;
        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / slideDuration);
            toolFollower.transform.position = Vector3.Lerp(brushStartPos, brushRubSpot, t);
            yield return null;
        }
        toolFollower.transform.position = brushRubSpot;
        
        // 3. RUBBING ANIMATION & SOUND
        AudioClip sfxToPlay = (containerRubSound != null) ? containerRubSound : (currentToolData != null ? currentToolData.toolSound : null);

        // Added "isSoundOn" check taake settings se sound OFF hone par ye sound na chale
        if (sfxToPlay != null && AudioManager.Instance != null && AudioManager.Instance.isSoundOn && AudioManager.Instance.sfxSource != null)
        {
            AudioManager.Instance.sfxSource.clip = sfxToPlay;
            AudioManager.Instance.sfxSource.loop = true;
            AudioManager.Instance.sfxSource.Play();
        }

        float rubTimer = 0f;
        while (rubTimer < rubDuration)
        {
            rubTimer += Time.deltaTime;
            float offsetX = Mathf.Sin(rubTimer * rubSpeed) * rubAmount;
            toolFollower.transform.position = brushRubSpot + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }

        // Rubbing animation khatam hote hi sound stop kar dein
        if (AudioManager.Instance != null && AudioManager.Instance.sfxSource != null)
        {
            AudioManager.Instance.sfxSource.Stop();
            AudioManager.Instance.sfxSource.loop = false;
        }

        // 4. CONTAINER SLIDE-OUT
        time = 0f;
        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / slideDuration);
            polishContainer.position = Vector3.Lerp(targetContainerPos, containerStartPos, t);
            yield return null;
        }
        polishContainer.gameObject.SetActive(false);
        polishContainer.position = targetContainerPos;

        // 5. BRUSH MOVES TO GAMEPLAY POSITION
        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 finalRestTarget = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.3f, camZ));
        finalRestTarget.z = 0f;

        time = 0f;
        Vector3 brushCurrentPos = toolFollower.transform.position;
        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / slideDuration);
            toolFollower.transform.position = Vector3.Lerp(brushCurrentPos, finalRestTarget, t);
            yield return null;
        }
        toolFollower.transform.position = finalRestTarget;

        toolFollower.enabled = true;
    }
}