using UnityEngine;

public enum CleaningStepType
{
    PixelEraser,   // Standard Texture Erasing (Brush, Water, Spray)
    ChunkScraper,  // Mud Chunks Prefab (Scraper)
    GlueApply      // Future Glue Spray
}

[System.Serializable]
public class CleaningStep
{
    public string stepName = "Step Name";
    public CleaningStepType stepType;
    public ToolData requiredTool;

    [Header("Step Assets")]
    public Sprite dirtySprite;       // Pixel Eraser logic ke liye Texture Sprite
    public GameObject stepPrefab;    // Chunk Scraper / Glue ke liye Prefab

   // [Range(1f, 100f)]
   // public float completionThreshold = 95f;
}