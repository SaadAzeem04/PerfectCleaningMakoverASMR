using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    // Backward compatibility for MaskEraser
    public static CleaningObjectData SelectedObject;

    [Header("Multi-Step System")]
    public LevelData currentLevelData;
    public int currentStepIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadLevel(LevelData level)
    {
        currentLevelData = level;
        currentStepIndex = 0;
        SceneManager.LoadScene("GameplayScene");
    }

    public LevelStep GetCurrentStep()
    {
        if (currentLevelData != null && currentStepIndex < currentLevelData.stepList.Count)
        {
            return currentLevelData.stepList[currentStepIndex];
        }
        return null;
    }

    public void AdvanceToNextStep()
    {
        currentStepIndex++;
        if (GameStepController.Instance != null)
        {
            GameStepController.Instance.ExecuteCurrentStep();
        }
    }
}