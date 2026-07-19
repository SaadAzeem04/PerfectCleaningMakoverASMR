using UnityEngine;

[CreateAssetMenu(fileName = "NewCleaningObject", menuName = "Cleaning Game/Object Data")]
public class CleaningObjectData : ScriptableObject
{
    public string objectName;
    public Sprite backgroundSprite;
    public Sprite cleanSprite;
    [Header("Naya Custom Scraper System")]
    public GameObject scraperChunksPrefab;

    public Sprite[] dirtyLayers;
    // NAYA: Har layer ke samne uska tool assign karne ke liye array
    public ToolData[] requiredTools;

}