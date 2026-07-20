using UnityEngine;
using System.Collections.Generic;

public enum StepType
{
    CleaningOrScraping,
    GlueApplication,
    TrashBin,
    CustomMinigame
}

[System.Serializable]
public class LevelStep
{
    public string stepName = "Step";
    public StepType stepType = StepType.CleaningOrScraping;

    [Header("If Cleaning/Scraping Step")]
    public CleaningObjectData cleaningObjectData; // Aapka Football_Data asset

    [Header("If Minigame Step (Glue/Dustbin)")]
    public GameObject stepPrefab; // Minigame Prefab
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int levelID;
    public string levelName;
    public Sprite levelIcon;
    public List<LevelStep> stepList = new List<LevelStep>();
}