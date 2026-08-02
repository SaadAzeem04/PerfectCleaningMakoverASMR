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
    private float placementTimer = 0f;
    private ToolData lastData;

    void Start()
    {
        if (toolFollower == null)
        {
            toolFollower = GetComponent<ToolFollower>();
        }

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
            placementTimer = 0f;

            ShowAndResetUI();
            // STEP SHURU HOTEY HI UI INITIALIZE KAREIN
            ResetUIState();
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

        // 4. AREA DETECTION CHECK: Target ke paas aane par lock aur snap trigger
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

        // 2. Progress fill: 0% se 100%
        placementTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(placementTimer / placementDuration);

        if (progressImage != null) progressImage.fillAmount = progress;
        if (progressText != null) progressText.text = Mathf.RoundToInt(progress * 100f) + "%";

        // 3. Complete & Next Step Signal
        if (progress >= 1.0f)
        {
            isAnimating = false;
            isCompleted = true;

            transform.position = patchTarget.position;
            transform.rotation = patchTarget.rotation;

            // FIX: Next steps (Polish/Towel) ke liye UI ko ACTIVE rakhein
            EnsureUIActiveForNextStep();

            HideUI();
            // Tool Hide
            if (toolFollower != null)
            {
                toolFollower.HideTool();
            }

            // NEXT STEP TRIGGER
            MaskEraser maskEraser = FindObjectOfType<MaskEraser>();
            if (maskEraser != null)
            {
                maskEraser.OnCurrentStepCompleted();
            }
        }
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

    // NEW HELPER: Patch khatam hote hi progress UI ko active rakhta hai
    private void EnsureUIActiveForNextStep()
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
    // 1. Step Start hone par (Tool select hotey hi UI Show + 0%)
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

    // 2. Step 100% Complete hone par UI Hide
    private void HideUI()
    {
        if (progressImage != null) progressImage.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
    }
}