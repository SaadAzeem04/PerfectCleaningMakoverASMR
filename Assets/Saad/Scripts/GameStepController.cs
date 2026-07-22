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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Safety: Start hote hi Win Panel ko hide rakhein
        if (winPanelUI != null)
        {
            winPanelUI.SetActive(false);
        }
    }

    void Start()
    {
        // Agar LevelManager se SelectedObject mil jaye to use pick karein
        if (LevelManager.SelectedObject != null)
        {
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
            Debug.LogWarning("No Level Data or LevelManager assigned!");
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
                        LevelManager.SelectedObject = step.cleaningObjectData;
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
        // SAFE CHECK: LevelManager.Instance null hone par crash hone se bachayein
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.AdvanceToNextStep();
            ExecuteCurrentStep(); // Agla step execute karein ya complete win trigger karein
        }
        else
        {
            Debug.LogWarning("LevelManager.Instance Scene mein missing hai! Direct Win panel trigger kar rahe hain.");
            OnAllStepsCompleted(); // Fallback taake game stuck na ho
        }
    }

    private void OnAllStepsCompleted()
    {
        if (maskEraser != null) maskEraser.gameObject.SetActive(false);
        if (winPanelUI != null) winPanelUI.SetActive(true);
        if (winPanelUI != null) winPanelUI.SetActive(true);
    }


}