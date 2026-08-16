using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PatchPlacementController : MonoBehaviour
{
    [Header("References")]
    public ToolFollower toolFollower;

    [Header("Target & Distance Settings")]
    public Transform patchTarget;             // Target purse patch position
    public float triggerDistance = 1.8f;      // Area detection distance threshold
    public float autoSnapSpeed = 6.0f;        // Target tak slide hone ki speed

    [Header("UI Progress Bar Settings")]
    public Image progressImage;               // Progress fill image
    public TMP_Text progressText;             // Progress text (0% to 100%)

    [Tooltip("Check = Sirf target par aane par dikhega | Uncheck = Step start hote hi 0% dikhega")]
    public bool autoHideProgressBar = false;  // Step start hote hi 0% dikhane ke liye isay false rakhein

    public float placementDuration = 1.2f;   // 0% se 100% complete hone ka time (Seconds)

    private bool isAnimating = false;
    private bool isCompleted = false;
    private bool hasTriggeredStep = false;   // Double trigger rokne ke liye
    private float placementTimer = 0f;
    private ToolData lastData;

    void Start()
    {
        if (toolFollower == null)
        {
            toolFollower = GetComponent<ToolFollower>();
        }

        // Order 100 set kar diya taake Glue (90) aur bakki layers ke uper aaye
        SetPatchSortingOrder(100);

        ResetUIState();
    }

    void Update()
    {
        // 1. Tool Data Check
        ToolData data = (toolFollower != null) ? toolFollower.CurrentToolData : null;
        if (data == null) return;

        string toolName = data.name.ToLower();
        if (!toolName.Contains("patch")) return;

        // Reset states when Patch tool starts
        if (lastData != data)
        {
            lastData = data;
            isCompleted = false;
            isAnimating = false;
            hasTriggeredStep = false; // Reset trigger flag
            placementTimer = 0f;

            SetPatchSortingOrder(100); // Step start hote hi order ensure karein
            ShowAndResetUI();
        }

        if (isCompleted) return;

        // 2. Target Auto-Find
        if (patchTarget == null)
        {
            GameObject targetObj = GameObject.FindWithTag("PatchTarget");
            if (targetObj != null)
            {
                patchTarget = targetObj.transform;
            }
        }

        if (patchTarget == null) return;

        // 3. Snap & Fill Animation Handling
        if (isAnimating)
        {
            HandlePlacementAnimation();
            return;
        }

        // 4. AREA DETECTION CHECK
        float distance = Vector3.Distance(transform.position, patchTarget.position);

        if (distance <= triggerDistance)
        {
            isAnimating = true;
            placementTimer = 0f;

            // TOUCH LOCK
            if (toolFollower != null)
            {
                toolFollower.IsInputLocked = true;
            }

            // Progress UI Enable
            if (progressImage != null) progressImage.gameObject.SetActive(true);
            if (progressText != null) progressText.gameObject.SetActive(true);
        }
    }

    private void HandlePlacementAnimation()
    {
        // 1. Target Position ki taraf move
        transform.position = Vector3.Lerp(
            transform.position,
            patchTarget.position,
            Time.deltaTime * autoSnapSpeed
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            patchTarget.rotation,
            Time.deltaTime * autoSnapSpeed
        );

        // 2. Progress fill
        placementTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(placementTimer / placementDuration);

        if (progressImage != null) progressImage.fillAmount = progress;
        if (progressText != null) progressText.text = Mathf.RoundToInt(progress * 100f) + "%";

        // 3. Complete & Next Step Signal
        if (progress >= 1.0f && !hasTriggeredStep)
        {
            hasTriggeredStep = true; // Guard against multiple calls
            isAnimating = false;
            isCompleted = true;

            GluePourController glueController = Object.FindFirstObjectByType<GluePourController>();
            if (glueController != null)
            {
                glueController.ClearGlueTrail();
            }

            transform.position = patchTarget.position;
            transform.rotation = patchTarget.rotation;

            // Tool Hide
            if (toolFollower != null)
            {
                toolFollower.HideTool();
            }

            // NEXT STEP TRIGGER
            MaskEraser maskEraser = Object.FindFirstObjectByType<MaskEraser>();
            if (maskEraser != null)
            {
                maskEraser.OnCurrentStepCompleted();
            }
        }
    }

    private void SetPatchSortingOrder(int newOrder)
    {
        // 1. Tool / Current Object ka Order
        ApplyOrderAndLayer(gameObject, newOrder);

        // 2. Agar Patch target scene me alag object hai, to uska bhi Order badlein
        if (patchTarget != null)
        {
            ApplyOrderAndLayer(patchTarget.gameObject, newOrder);
        }
    }
    private void ApplyOrderAndLayer(GameObject obj, int order)
{
    // Layer aur Order dono enforce karein
    SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
    foreach (SpriteRenderer sr in renderers)
    {
        if (sr != null)
        {
            sr.sortingLayerName = "Default"; // Same Layer Ensure Karein
            sr.sortingOrder = order;
        }
    }

    var sortingGroup = obj.GetComponent<UnityEngine.Rendering.SortingGroup>();
    if (sortingGroup != null)
    {
        sortingGroup.sortingLayerName = "Default";
        sortingGroup.sortingOrder = order;
    }

    // Z-Position ko Camera ke qareeb karein taake 3D space me bhi Glue ke uper aaye
    Vector3 pos = obj.transform.position;
    pos.z = -0.1f; 
    obj.transform.position = pos;
}

    private void ResetUIState()
    {
        if (progressImage != null)
        {
            progressImage.fillAmount = 0f;
            progressImage.gameObject.SetActive(!autoHideProgressBar);
        }

        if (progressText != null)
        {
            progressText.text = "0%";
            progressText.gameObject.SetActive(!autoHideProgressBar);
        }
    }

    private void ShowAndResetUI()
    {
        if (progressImage != null)
        {
            progressImage.fillAmount = 0f;
            progressImage.gameObject.SetActive(true);
        }

        if (progressText != null)
        {
            progressText.text = "0%";
            progressText.gameObject.SetActive(true);
        }
    }
}