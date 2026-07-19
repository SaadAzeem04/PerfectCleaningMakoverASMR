using UnityEngine;

[CreateAssetMenu(fileName = "NewCleaningObject", menuName = "Cleaning Game/Object Data")]
public class CleaningObjectData : ScriptableObject
{
    public string objectName;
    public Sprite backgroundSprite;
    public Sprite cleanSprite;

    public Sprite[] dirtyLayers;
    // NAYA: Har layer ke samne uska tool assign karne ke liye array
    public ToolData[] requiredTools;
    public GameObject scraperChunksPrefab;


    
}