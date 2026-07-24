using UnityEngine;

// 1. NAYA ENUM ADD KYA HE: Is se hum Inspector me drop-down se movement select kar sakenge
public enum ToolMovementType
{
    StandardFollow,
    Scrubbing,      // Left-Right move hone ke liye
    Spraying,       // Vibration ke liye
    Rotation
}

[System.Serializable]
public class ToolVariant
{
    public string variantName;    // e.g., "Brown Brush", "Pink Brush"
    public Sprite toolSprite;     // Jo screen par tool safai karega (Actual Tool Sprite)
    public Sprite iconSprite;     // Jo niche button me chota icon dikhega (UI Icon)
    public int coinPrice;
    public float brushSize = 30f;// 0 likhenge to "Free" likha aayega
}

[CreateAssetMenu(
fileName = "New Tool",
menuName = "Cleaning Game/Tool Data"
)]
public class ToolData : ScriptableObject
{

    [Header("--- Variant Settings ---")]
    public bool hasVariants = false; // Is tool ke naye options on/off karne ke liye
    public System.Collections.Generic.List<ToolVariant> toolVariants = new System.Collections.Generic.List<ToolVariant>();

    public string toolName;
    public float cameraZoomSize = 5f;

    public ToolType toolType;

    public Sprite toolSprite;
    public bool soundOnlyOnHit = true; // Agar true hoga, toh sound tabhi aayegi jab safai ho rahi ho.
    public float cleaningSpeed = 1f;

    public int brushSize = 35;

    // 2. NAYA SECTION ADD KYA HE: Tool ki Animation / Movement ke liye
    [Header("Animation & Movement Settings")]
    [Tooltip("Check karein ke tool screen par kaise move karega (Scrubbing, Spraying, ya Normal)")]
    public ToolMovementType movementType = ToolMovementType.StandardFollow;
    [Tooltip("Agar Brush ya Towel hai to ragadne ki speed kitni ho? (Normal = 15)")]
    public float scrubSpeed = 15f;
    [Tooltip("Ragadte waqt kitna left-right hile? (Normal = 0.3 to 0.5)")]
    public float scrubAmount = 0.4f;

    [Tooltip("Rotation ki speed kitni ho? (Normal = 10)")]
    public float rotationSpeed = 10f;
    [Tooltip("Kitne degree tak aage-piche rotate kare? (Normal = 15 ya 20 degree)")]
    public float rotationAmount = 15f;

    // Effects
    [Header("Purana Particle System")]
    public bool useParticles;
    public GameObject particlePrefab;
    public Vector3 particleOffset; // Agar pehle particle ka offset chahiye

    [Header("Naya (Second) Particle System")]
    public bool useSecondParticles;          // Kya naya particle chalana hai? (True/False)
    public GameObject secondParticlePrefab;  // Naye particle ka Prefab yahan aayega
    public Vector3 secondParticleOffset;
    public Vector3 eraseOffset;
    public AudioClip toolSound;
    public Vector3 toolOffset;

    [Header("UI Display Settings")]
    [Tooltip("Yeh woh picture hai jo Top Panel mein saaf aur clean nazar aayegi.")]
    public Sprite panelIcon; //  Yeh nayi line add kar dein

    
}