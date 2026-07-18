using UnityEngine;

[CreateAssetMenu(fileName = "NewCleaningObject", menuName = "Cleaning Game/Object Data")]
public class CleaningObjectData : ScriptableObject
{
    public string objectName;
    public Sprite backgroundSprite;
    public Sprite cleanSprite;

    [Header("Base Clean Object Settings")]
    [Tooltip("Clean sprite ki position set karne ke liye offset")]
    public Vector3 cleanObjectOffset = Vector3.zero;

    [Tooltip("Clean sprite ka size set karne ke liye scale (Default 1, 1, 1 rakhein)")]
    public Vector3 cleanObjectScale = Vector3.one;

    [Header("Layer Sprites")]
    public Sprite[] dirtyLayers;

    [Header("Position Settings")]
    [Tooltip("Har layer ki photo ko upar/neeche fit karne ke liye X aur Y offset")]
    public Vector3[] layerOffsets;

    [Tooltip("Har layer ka size chota/bada karne ke liye scale offset (Default 1, 1, 1 hona chahiye)")]
    public Vector3[] layerScales;

    public ToolData[] requiredTools;

    [Header("Scraper Special Settings")]
    [Tooltip("Scraper tool ke liye chunks wala prefab (Is me MudChunk scripts lagi hongi)")]
    public GameObject scraperChunksPrefab;
}