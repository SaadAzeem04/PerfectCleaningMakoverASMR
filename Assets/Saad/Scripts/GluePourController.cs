using UnityEngine;
using UnityEngine.UI; // UI Image ke liye
using TMPro;          // TextMeshPro ke liye

public class GluePourController : MonoBehaviour
{
    [Header("References")]
    public ToolFollower toolFollower;
    public Transform toolVisualTransform; // Glue Bottle ka Sprite / Child Transform

    [Header("Audio Settings")]
    [Tooltip("Glue pour sound effect (Optional: Agar empty rakhenge tou ToolData wala sound play hoga)")]
    public AudioClip gluePourSound;

    [Header("UI Progress Bar Settings")]
    public Image progressImage;             // Inspector mein Fill Image drag karein
    public TMP_Text progressText;           // Inspector mein TextMeshPro text drag karein
    public bool autoHideProgressBar = true; // Pouring start hone par dikhe, finish hone par hide ho

    [Header("Glue Liquid Trail Settings")]
    public Material glueMaterial;         // White material
    public float glueWidth = 0.25f;       // Patch par glue ki thickness
    public TrailRenderer glueTrail;

    [Header("Sorting Settings")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 10;

    [Header("Target & Distance")]
    public Transform patchTarget;             // Patch target
    public float triggerDistance = 2.0f;      // Distance trigger

    [Header("Rotation Settings")]
    public float pourAngle = 100f;            // Tilt angle
    public float rotationSpeed = 8f;

    [Header("Fixed Animation Settings")]
    public bool enableWaveMotion = true;
    public Vector3 fixedStartOffset = new Vector3(-0.8f, 0f, 0f);
    public float strokeSpeed = 1.0f;
    public float maxDistanceX = 1.6f;
    public float waveHeightY = 0.1f;
    public float waveFrequency = 3.0f;

    [Header("Slide Away Settings (Next Step Trigger)")]
    public Vector3 slideAwayOffset = new Vector3(6f, -3f, 0f); // Slide direction
    public float slideSpeed = 4f;                              // Slide speed

    private Quaternion defaultRotation;
    private Quaternion pourRotation;
    private Vector3 defaultLocalPos;
    private float strokeProgress = 0f;
    private Transform glueTipTransform;

    private bool isCompleted = false;
    private bool isSlidingAway = false;
    private Vector3 slideTargetPos;
    private bool isUIControlledByGlue = false;
    private bool isPlayingGlueSound = false; // Sound tracking flag

    void Start()
    {
        if (toolFollower == null)
        {
            toolFollower = GetComponent<ToolFollower>();
        }

        if (toolVisualTransform == null && toolFollower != null && toolFollower.toolSprite != null)
        {
            toolVisualTransform = toolFollower.toolSprite.transform;
        }
        else if (toolVisualTransform == null)
        {
            toolVisualTransform = transform;
        }

        defaultRotation = toolVisualTransform.localRotation;
        pourRotation = Quaternion.Euler(0, 0, pourAngle);
        defaultLocalPos = toolVisualTransform.localPosition;
    }

    void OnDisable()
    {
        // Safety check: Agar object deactivate ho jaye tou sound zaroor stop ho
        StopGlueSound();
    }

    void Update()
    {
        // 1. Agar completion slide animation chal rahi hai
        if (isSlidingAway)
        {
            StopGlueSound();
            HandleSlideAway();
            return;
        }

        // 2. Target auto find
        if (patchTarget == null)
        {
            GameObject targetObj = GameObject.FindWithTag("PatchTarget");
            if (targetObj != null)
            {
                patchTarget = targetObj.transform;
            }
        }

        // 3. Glue Tool Check
        bool isGlueTool = false;
        ToolData data = (toolFollower != null) ? toolFollower.CurrentToolData : null;

        if (patchTarget == null || isCompleted)
        {
            StopGlueSound();
            ResetVisualTransform();
            return;
        }

        if (data != null)
        {
            string toolName = data.name.ToLower();
            if (toolName.Contains("glue_data") || toolName.Contains("glue"))
            {
                isGlueTool = true;
            }
        }

        if (!isGlueTool || patchTarget == null || isCompleted)
        {
            StopGlueSound();
            ResetVisualTransform();
            return;
        }

        // 4. Dynamic Tip Setup
        SetupDynamicGlueTip(data);

        // 5. Distance Check
        float distance = Vector3.Distance(transform.position, patchTarget.position);
        bool isNearTarget = (distance <= triggerDistance);

        if (isNearTarget)
        {
            // Touch Freeze: Range mein aate hi player control disable ho jayega
            if (toolFollower != null)
            {
                toolFollower.IsInputLocked = true;
            }

            // Object ko EXACT Patch Target Transform ki position par lock/snap karein
            transform.position = Vector3.Lerp(transform.position, patchTarget.position, Time.deltaTime * 15f);

            // --- STEP 1: ROTATION ---
            toolVisualTransform.localRotation = Quaternion.Lerp(
                toolVisualTransform.localRotation,
                pourRotation,
                Time.deltaTime * rotationSpeed
            );

            float angleDiff = Quaternion.Angle(toolVisualTransform.localRotation, pourRotation);
            bool isRotationComplete = (angleDiff <= 5f);

            // --- STEP 2: WAVE MOTION, SOUND & UI UPDATE ---
            if (isRotationComplete && enableWaveMotion)
            {
                isUIControlledByGlue = true;

                // GLUE POUR SOUND START
                if (strokeProgress < 1f)
                {
                    StartGlueSound(data);
                }

                // UI Show
                if (autoHideProgressBar)
                {
                    if (progressImage != null && !progressImage.gameObject.activeSelf)
                        progressImage.gameObject.SetActive(true);

                    if (progressText != null && !progressText.gameObject.activeSelf)
                        progressText.gameObject.SetActive(true);
                }

                strokeProgress += Time.deltaTime * strokeSpeed;
                strokeProgress = Mathf.Clamp01(strokeProgress);

                // Image Fill & Text Update
                if (progressImage != null) progressImage.fillAmount = strokeProgress;
                if (progressText != null) progressText.text = Mathf.RoundToInt(strokeProgress * 100f) + "%";

                // Wave Movement
                Vector3 startPos = defaultLocalPos + fixedStartOffset;
                float posX = startPos.x + (strokeProgress * maxDistanceX);
                float posY = startPos.y + (Mathf.Sin(strokeProgress * Mathf.PI * 2f * waveFrequency) * waveHeightY);

                Vector3 targetWavePos = new Vector3(posX, posY, startPos.z);

                toolVisualTransform.localPosition = Vector3.Lerp(
                    toolVisualTransform.localPosition,
                    targetWavePos,
                    Time.deltaTime * 12f
                );

                // Trail Emission
                if (glueTrail != null)
                {
                    glueTrail.emitting = (strokeProgress > 0.05f && strokeProgress < 1.0f);
                }

                // --- STEP 3: 100% COMPLETION & TRIGGER NEXT STEP ---
                if (strokeProgress >= 1f && !isCompleted)
                {
                    isCompleted = true;
                    isSlidingAway = true;
                    slideTargetPos = toolVisualTransform.localPosition + slideAwayOffset;

                    // STOP SOUND ON COMPLETION
                    StopGlueSound();

                    if (glueTrail != null) glueTrail.emitting = false;
                }
            }
            else
            {
                StopGlueSound();
                if (glueTrail != null) glueTrail.emitting = false;
            }
        }
        else
        {
            // Target se door hone par Touch Unlock karein
            if (toolFollower != null && !isCompleted)
            {
                toolFollower.IsInputLocked = false;
            }

            StopGlueSound();
            ResetVisualTransform();
        }
    }

    private void HandleSlideAway()
    {
        // Slide out hote waqt strictly trail OFF & Sound OFF rahegi
        StopGlueSound();
        if (glueTrail != null) glueTrail.emitting = false;

        // Bottle Screen se bahar slide karegi
        toolVisualTransform.localPosition = Vector3.MoveTowards(
            toolVisualTransform.localPosition,
            slideTargetPos,
            Time.deltaTime * slideSpeed * 5f
        );

        // Slide poori hone par NEXT STEP trigger hoga
        if (Vector3.Distance(toolVisualTransform.localPosition, slideTargetPos) < 0.1f)
        {
            isSlidingAway = false;

            ResetUIState();

            // Next Tool call karein
            if (toolFollower != null)
            {
                toolFollower.HideTool();
            }

            // MaskEraser ko Next Step par bhejein
            MaskEraser maskEraser = Object.FindFirstObjectByType<MaskEraser>();
            if (maskEraser != null)
            {
                maskEraser.OnCurrentStepCompleted();
            }
        }
    }

    private void SetupDynamicGlueTip(ToolData data)
    {
        if (data == null || toolVisualTransform == null) return;

        Vector3 tipOffset = data.eraseOffset;

        if (glueTipTransform == null)
        {
            Transform existingTip = toolVisualTransform.Find("DynamicGlueTip");
            if (existingTip != null)
            {
                glueTipTransform = existingTip;
            }
            else
            {
                GameObject tipObj = new GameObject("DynamicGlueTip");
                tipObj.transform.SetParent(toolVisualTransform, false);
                glueTipTransform = tipObj.transform;
            }
        }

        glueTipTransform.localPosition = tipOffset;

        if (glueTrail == null)
        {
            glueTrail = glueTipTransform.GetComponent<TrailRenderer>();
            if (glueTrail == null)
            {
                glueTrail = glueTipTransform.gameObject.AddComponent<TrailRenderer>();
                glueTrail.time = 100f;
                glueTrail.startWidth = glueWidth;
                glueTrail.endWidth = glueWidth;
                glueTrail.minVertexDistance = 0.02f;
                glueTrail.numCornerVertices = 5;
                glueTrail.numCapVertices = 5;
                glueTrail.alignment = LineAlignment.TransformZ;

                if (glueMaterial != null)
                {
                    glueTrail.material = glueMaterial;
                }
                else
                {
                    glueTrail.material = new Material(Shader.Find("Sprites/Default"));
                    glueTrail.startColor = Color.white;
                    glueTrail.endColor = Color.white;
                }
                glueTrail.emitting = false;
            }
        }

        if (glueTrail != null)
        {
            glueTrail.sortingLayerName = sortingLayerName;
            glueTrail.sortingOrder = sortingOrder;
        }
    }

    private void ResetVisualTransform()
    {
        if (isSlidingAway) return;

        StopGlueSound();

        if (glueTrail != null) glueTrail.emitting = false;

        if (isUIControlledByGlue)
        {
            ResetUIState();
        }

        toolVisualTransform.localRotation = Quaternion.Lerp(
            toolVisualTransform.localRotation,
            defaultRotation,
            Time.deltaTime * rotationSpeed
        );

        toolVisualTransform.localPosition = Vector3.Lerp(
            toolVisualTransform.localPosition,
            defaultLocalPos,
            Time.deltaTime * 10f
        );
    }

    private void ResetUIState()
    {
        isUIControlledByGlue = false;
        strokeProgress = 0f;

        if (progressImage != null)
        {
            progressImage.fillAmount = 0f;
            if (autoHideProgressBar) progressImage.gameObject.SetActive(false);
        }

        if (progressText != null)
        {
            progressText.text = "0%";
            if (autoHideProgressBar) progressText.gameObject.SetActive(false);
        }
    }

    // AUDIO HELPER METHODS WITH DEBUG LOGS
    private void StartGlueSound(ToolData data)
    {
        if (isPlayingGlueSound) return;

        AudioClip sfxToPlay = (gluePourSound != null) ? gluePourSound : (data != null ? data.toolSound : null);

        if (sfxToPlay != null && AudioManager.Instance != null && AudioManager.Instance.isSoundOn && AudioManager.Instance.sfxSource != null)
        {
            AudioManager.Instance.sfxSource.clip = sfxToPlay;
            AudioManager.Instance.sfxSource.loop = true;
            AudioManager.Instance.sfxSource.Play();
            isPlayingGlueSound = true;
        }
    }

    private void StopGlueSound()
    {
        if (!isPlayingGlueSound) return;

        if (AudioManager.Instance != null && AudioManager.Instance.sfxSource != null)
        {
            AudioManager.Instance.sfxSource.Stop();
            AudioManager.Instance.sfxSource.loop = false;
        }
        isPlayingGlueSound = false;
    }

    public void ClearGlueTrail()
    {
        if (glueTrail != null)
        {
            glueTrail.emitting = false;
            glueTrail.Clear(); // Screen par draw hui lines ko instant erase kar deta hai
        }

        if (glueTipTransform != null)
        {
            Destroy(glueTipTransform.gameObject); // Dynamic tip transform ko destroy kar dein
            glueTipTransform = null;
            glueTrail = null;
        }
    }
}