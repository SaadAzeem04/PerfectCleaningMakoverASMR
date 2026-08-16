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

    [Header("Camera Permission")]
    public bool allowCameraMovement = true;

    // NAYA ADD: Is layer ka specific Camera Zoom Size
    public float cameraZoomSize = 7.0f;

    [Header("Step Assets")]
    public Sprite dirtySprite;       // Pixel Eraser logic ke liye Texture Sprite
    public GameObject stepPrefab;    // Chunk Scraper / Glue ke liye Prefab

    // [Range(1f, 100f)]
    // public float completionThreshold = 95f;
}