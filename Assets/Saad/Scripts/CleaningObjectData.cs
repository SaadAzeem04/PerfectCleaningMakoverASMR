using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCleaningObject", menuName = "Cleaning Game/Object Data")]
public class CleaningObjectData : ScriptableObject
{
    public string objectName;
    public Sprite backgroundSprite;
    public Sprite cleanSprite;

    [Header("Background Settings")]
    public Sprite levelBackgroundSprite;

    [Header("Dynamic Positioning Settings")]
    [Tooltip("Is object/level ke liye layers container ki Y/X position offset")]
    public Vector3 levelPositionOffset = Vector3.zero;

    [Header("Camera Settings")]
    public float cameraMovementIntensity = 1.0f;
    public float customCameraZoomSize = 5.0f;

    [Tooltip("Level complete hone par camera ka zoom size kitna hona chahiye")]
    public float levelCompleteZoomSize = 4.5f;

    public bool enableYAxisMovement = false;

    [Header("Dynamic Cleaning Sub-Steps")]
    public List<CleaningStep> cleaningSteps = new List<CleaningStep>();

    [Header("Layer Transform Overrides (Optional)")]
    public Vector3[] customLayerOffsets;
    public Vector3[] customLayerScales;

    [Header("UI Settings")]
    public Sprite levelCompleteIcon;
}