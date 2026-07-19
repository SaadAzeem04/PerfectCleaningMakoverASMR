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

    [Header("Layer Transform Overrides (Optional)")]
    [Tooltip("Agar aap kisi specific layer ki position badalna chahte hain, to yahan vector offset dein. (Element 0 = Layer 1)")]
    public Vector3[] customLayerOffsets;

    [Tooltip("Agar aap kisi specific layer ka size badalna chahte hain, to yahan scale dein. Default (1,1,1) rahega.")]
    public Vector3[] customLayerScales;

}