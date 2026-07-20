using UnityEngine;

public class GameStepController : MonoBehaviour
{
    public static GameStepController Instance { get; private set; }

    [Header("References")]
    public MaskEraser maskEraser;
    public Transform minigameSpawnAnchor; // Point jahan Glue/Dustbin prefabs load honge
    public GameObject winPanelUI;         // Level Complete Popup UI

    private GameObject activeMinigameObject;

    private void Awake()
    {
        Instance = this;

        // Safety: Start hote hi Win Panel ko hide rakhein
        if (winPanelUI != null)
        {
            winPanelUI.SetActive(false);
        }
    }
    // Top variables check karein ke level data variable ka naam kya hai (e.g., levelData ya objectData)
    // Agar variable ka naam "levelData" ya "cleaningObjectData" hai, to niche check karein:

    void Start()
    {
        // Agar LevelManager se SelectedObject mil jaye to use pick karein
        if (LevelManager.SelectedObject != null)
        {
            // Safe check: MaskEraser ko pass kar de
            MaskEraser eraser = FindFirstObjectByType<MaskEraser>();
            if (eraser != null)
            {
                eraser.InitializeCleaningObject(LevelManager.SelectedObject);
            }
        }

        ExecuteCurrentStep();
    }

    public void ExecuteCurrentStep()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.currentLevelData == null)
        {
            Debug.LogWarning("No Level Data assigned!");
            return;
        }

        LevelStep step = LevelManager.Instance.GetCurrentStep();

        // 1. Purana Minigame Prefab destroy karein
        if (activeMinigameObject != null)
        {
            Destroy(activeMinigameObject);
        }

        // 2. Agar Saare Steps Finish Ho Gaye Hain:
        if (step == null)
        {
            OnAllStepsCompleted();
            return;
        }

        // 3. Current Step Load Karein
        switch (step.stepType)
        {
            case StepType.CleaningOrScraping:
                if (maskEraser != null)
                {
                    maskEraser.gameObject.SetActive(true);
                    if (step.cleaningObjectData != null)
                    {
                        // LevelManager me SelectedObject assign karein
                        LevelManager.SelectedObject = step.cleaningObjectData;

                        // IMPORTANT: MaskEraser ko force-refresh karein naye Object (Trophy) ke data se
                        maskEraser.InitializeCleaningObject(step.cleaningObjectData);
                    }
                }
                break;

            case StepType.GlueApplication:
            case StepType.TrashBin:
            case StepType.CustomMinigame:
                if (maskEraser != null) maskEraser.gameObject.SetActive(false);

                if (step.stepPrefab != null && minigameSpawnAnchor != null)
                {
                    activeMinigameObject = Instantiate(step.stepPrefab, minigameSpawnAnchor);
                    activeMinigameObject.transform.localPosition = Vector3.zero;
                    activeMinigameObject.transform.localScale = Vector3.one;
                }
                break;
        }
    }

    public void OnStepFinishedFromMinigame()
    {
        LevelManager.Instance.AdvanceToNextStep();
    }

    private void OnAllStepsCompleted()
    {
        if (maskEraser != null) maskEraser.gameObject.SetActive(false);
        if (winPanelUI != null) winPanelUI.SetActive(true);
    }
}