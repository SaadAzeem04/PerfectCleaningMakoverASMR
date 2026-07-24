using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MaskEraser : MonoBehaviour
{
    [Header("Data Source")]
    public CleaningObjectData objectData;
    public Transform levelParentAnchor;

    [Header("References")]
    public ToolData currentToolData;
    public ToolFollower toolFollower;
    public TMP_Text percentText;
    public Image progressFill;
    //public AudioSource audioSource;

    //  TOOL ANIMATION VARIABLES
    private Vector3 originalToolLocalPos;
    private Quaternion originalToolRotation;
    private bool isToolPosSaved = false;

    [Header("Particles")]
    public Transform effectAnchor;
    public Transform eraseAnchor;
    public GameObject currentParticle;

    // Naya private reference list taake dono active particles control ho sakein
    private List<GameObject> activeParticlesList = new List<GameObject>();

    [Header("Celebration")]
    public GameObject celebrationPrefab;
    public AudioClip celebrationSound;

    [Header("UI Panels")]
    public GameObject levelCompletePanel;
    public Image backgroundImage;

    [Tooltip("Gameplay me jo Pause Button hai use yahan drag karein")]
    public GameObject pauseButton;

    [Header("Coin UI Settings")]
    [Tooltip("Gameplay ke dauran jo Coin UI bar dikhana hai use yahan drag karein")]
    public GameObject gameplayCoinPanel;
    [Tooltip("Coin bar ke andar ka Text Mesh Pro (TMP_Text) component yahan drag karein")]
    public TMP_Text gameplayCoinText;

    [Header("--- Ref Video Tool Variant UI ---")]
    public GameObject variantMainPanel;
    // Niche wala poora UI Dabba
    public Transform variantButtonsContainer;
    // Jahan 3 buttons lagenge (Horizontal Layout Group)
    public GameObject variantButtonPrefab;
    // Button ka Prefab
    private List<ToolVariantButton> spawnedVariantButtons = new List<ToolVariantButton>();

    private Coroutine panelAnimCoroutine;

    private GameObject activeCelebrationInstance;
    // NAYA VARIABLE: Pehle se active variant ko track karne ke liye
    private ToolVariant currentEquippedVariant;

    [Header("Tool UI Panel")]
    public Image previousToolUIImage;
    public Image currentToolUIImage;
    public Image upcomingToolUIImage;

    [Header("Background Reference")]
    public SpriteRenderer backgroundRenderer; // Inspector mein BG wale GameObject ka SpriteRenderer drag karein

    [Header("Tool UI Sizes & Spacing")]
    public float activeToolScale = 2f;
    public float inactiveToolScale = 1.5f;
    // Yeh line add karein:
    public float toolSpacing = 100f;

    [Header("Upcoming Objects Panel")]
    public Image[] upcomingIcons;

    [Header("End Game Settings")]
    [Tooltip("Level complete hone par window kitni der baad aaye")]
    public float levelCompleteDelay = 3.0f;
    // Window aane mein delay
    [Tooltip("Sari layers clean hone ke baad camera ka zoom size kya ho")]
    public float levelCompleteZoomSize = 4.5f;
    // Thora zoom karne ke liye (kam value = zoom in, zyada = zoom out)

    [Header("Eraser Smoothness Settings")]
    [Range(0.01f, 1.0f)]
    [Tooltip("Kam value se edges soft aur smooth honge. Zyada se sharp honge.")]
    public float brushHardness = 0.15f;
    [Range(0.01f, 1.0f)]
    [Tooltip("Kam value se mitti dheere aur smoothly saaf hogi (Intensity kam hogi).")]
    public float eraserIntensityMultiplier = 0.1f;

    [Header("Level Completion Settings")]
    [Range(0f, 100f)]
    [Tooltip("Kitne percent mitti saaf hone par level complete mana jaye (e.g., 95% ya 98%)")]
    public float cleaningThreshold = 95f;

    [Header("Camera Settings")]
    [Tooltip("Camera zoom hone ki speed/intensity. Default 4 hai, jitna zyada karenge utna fast zoom hoga.")]
    public float cameraTransitionIntensity = 3f;

    [Header("Camera Parallax Settings")]
    public float cameraMoveIntensity = 0.2f;

    [Header("Camera Zoom Settings")]
    [Tooltip("Default camera size jab koi tool active na ho ya game start ho.")]
    public float defaultCameraSize = 5f;

    [Header("Level Completed UI Settings")]
    [Tooltip("Is level ke khatam hone par jo ALAG ya SHINY photo dikhani hai.")]
    public Sprite completedLevelSprite;
    //  Har level ki special win photo ke liye

    [SerializeField] private UnityEngine.UI.Image levelCompleteIconImage; // Hierarchy se LevelCompleteIconImage drag karein

    [Tooltip("Completed Window ke andar wala UI Image dabba jahan photo dikhegi.")]
    public UnityEngine.UI.Image winPanelIconImage;
    public GameObject progressBarMainPanel;

    [Header("Scraper Progress Variables")]
    private int totalScraperChunks = 0;
    private int remainingScraperChunks = 0;
    private bool isScraperActive = false;

    [Header("UI Animation References")]
    public SmoothUIAnimate pauseButtonAnim;
    public SmoothUIAnimate coinCounterAnim;

    private List<GameObject> stepGameObjects = new List<GameObject>();


    // Runtime Generated Layers
    private List<SpriteRenderer> layersList = new List<SpriteRenderer>();
    private List<ToolData> layerRequiredTools = new List<ToolData>();

    int currentLayer = 0;
    Texture2D texture;
    int totalOpaquePixels = 0;

    float targetFill;
    private bool isLayerClearSoundPlayed = false;
    float currentFill;
    bool gameCompleted = false;
    bool textureNeedsApply = false;

    float progressTimer = 0f;
    bool needsProgressCheck = false;

    Vector2 prevPos, currPos, upPos;
    bool positionsSaved = false;

    bool layerFinishedWaitingRelease = false;
    bool isTransitioningTool = false;
    float targetCameraSize = 5f;
    float effectGraceTimer = 0f;


    public GameObject scraperTriggerEdge;
    void Start()
    {
        PlayerPrefs.SetInt("Coins", 100);
        PlayerPrefs.Save();

        // Gameplay UI panels ko default par active karna start me
        if (gameplayCoinPanel != null)
        {
            gameplayCoinPanel.SetActive(true);
        }
        if (pauseButton != null)
        {
            pauseButton.SetActive(true);
        }
        UpdateGameplayCoinsUI();

        if (AudioManager.Instance != null && AudioManager.Instance.gameSceneMusic != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.gameSceneMusic);
        }
        if (Camera.main != null)
        {
            defaultCameraSize = Camera.main.orthographic ?
                Camera.main.orthographicSize : Camera.main.fieldOfView;
            targetCameraSize = defaultCameraSize;
        }

        // --- DYNAMIC DATA FETCH WITH FALLBACK ---
        if (LevelManager.SelectedObject != null)
        {
            objectData = LevelManager.SelectedObject;
        }

        // Safety Check: Agar LevelManager se Data nahi aaya to Inspector wala fallback chalega
        if (objectData == null)
        {
            Debug.LogWarning("MaskEraser: SelectedObject was NULL! Please assign a default objectData in Inspector or select a level from HomeScene.");
            return;
        }

        // Purani Generated Layers saaf karein
        ClearOldGeneratedLayers();

        SetupGenericLevel();
        if (layersList.Count > 0)
        {
            PrepareLayer();
            SelectTool(layerRequiredTools[currentLayer], false);

            if (layerRequiredTools[currentLayer] != null && layerRequiredTools[currentLayer].cameraZoomSize > 0.1f)
            {
                targetCameraSize = layerRequiredTools[currentLayer].cameraZoomSize;
            }
            else
            {
                targetCameraSize = defaultCameraSize;
            }
        }

        UpdateUpcomingIconsPanel(true);

        percentText.text = "0%";
        progressFill.fillAmount = 0f;
        currentFill = 0f;
        targetFill = 0f;

        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);

        // Tools animate kar ke screen par laane ke liye
        StartCoroutine(AnimateFirstToolOnStartup());
    }

    public void UpdateGameplayCoinsUI()
    {
        if (gameplayCoinText != null)
        {
            int currentCoins = PlayerPrefs.GetInt("Coins", 100);
            gameplayCoinText.text = currentCoins.ToString();
        }
    }

    void SetupGenericLevel()
    {

        if (levelParentAnchor == null)
        {
            Debug.LogError("MaskEraser: Please assign Level Parent Anchor!");
            return;
        }

        // 1. Purane sabhi generated objects clear karein
        for (int i = levelParentAnchor.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(levelParentAnchor.GetChild(i).gameObject);
        }

        layersList.Clear();
        layerRequiredTools.Clear();

        // 2. Background setup
        if (objectData != null)
        {
            if (backgroundRenderer != null && objectData.levelBackgroundSprite != null)
            {
                backgroundRenderer.sprite = objectData.levelBackgroundSprite;
                backgroundRenderer.sortingOrder = -10;
                backgroundRenderer.gameObject.SetActive(true);
            }

            if (backgroundImage != null && objectData.backgroundSprite != null)
            {
                backgroundImage.sprite = objectData.backgroundSprite;
            }

            cameraMoveIntensity = objectData.cameraMovementIntensity;
        }

        if (objectData != null && levelParentAnchor != null)
        {
            // ScriptableObject se position offset apply karein
            levelParentAnchor.localPosition = objectData.levelPositionOffset;
            levelParentAnchor.rotation = Quaternion.identity;
        }
        levelParentAnchor.rotation = Quaternion.identity;

        if (objectData == null)
        {
            Debug.LogError("MaskEraser: ObjectData is missing!");
            return;
        }

        // 3. Base Clean Object Create Karein
        GameObject cleanObj = new GameObject("Base_Clean_Object");
        cleanObj.transform.SetParent(levelParentAnchor, false);
        cleanObj.transform.localPosition = Vector3.zero;
        cleanObj.transform.localRotation = Quaternion.identity;
        cleanObj.transform.localScale = Vector3.one;

        SpriteRenderer baseCleanSR = cleanObj.AddComponent<SpriteRenderer>();

        if (objectData.cleanSprite != null)
        {
            baseCleanSR.sprite = objectData.cleanSprite;
            baseCleanSR.sortingOrder = 0;
            baseCleanSR.maskInteraction = SpriteMaskInteraction.None;
            baseCleanSR.material = new Material(Shader.Find("Sprites/Default"));
            baseCleanSR.enabled = true;
            cleanObj.SetActive(true);
        }

        // 4. Step-Based Dynamic Setup
        // 4. Step-Based Dynamic Setup
        if (objectData.cleaningSteps != null && objectData.cleaningSteps.Count > 0)
        {
            int totalSteps = objectData.cleaningSteps.Count;
            stepGameObjects.Clear(); // Step objects tracking list

            for (int i = 0; i < totalSteps; i++)
            {
                CleaningStep step = objectData.cleaningSteps[i];
                if (step == null) continue;

                GameObject stepObj = new GameObject($"Step_{i}_{step.stepName}");
                stepObj.transform.SetParent(levelParentAnchor, false);
                stepObj.transform.localPosition = Vector3.zero;
                stepObj.transform.localRotation = Quaternion.identity;
                stepObj.transform.localScale = Vector3.one;

                stepGameObjects.Add(stepObj); // Index 0, 1, 2 tracking

                switch (step.stepType)
                {
                    case CleaningStepType.PixelEraser:
                        SpriteRenderer sr = stepObj.AddComponent<SpriteRenderer>();
                        sr.sprite = step.dirtySprite;
                        sr.sortingOrder = (totalSteps + 5) - i;
                        layersList.Add(sr); // Index match (e.g. Index 1 for Step 1)
                        break;

                    case CleaningStepType.ChunkScraper:
                        if (step.stepPrefab != null)
                        {
                            GameObject instantiatedChunks = Instantiate(step.stepPrefab, stepObj.transform);
                            instantiatedChunks.transform.localPosition = Vector3.zero;
                            instantiatedChunks.transform.localRotation = Quaternion.identity;
                            instantiatedChunks.transform.localScale = Vector3.one;

                            MudChunk[] allChunks = instantiatedChunks.GetComponentsInChildren<MudChunk>(true);
                            totalScraperChunks = allChunks.Length;
                            remainingScraperChunks = totalScraperChunks;
                        }
                        layersList.Add(null); // Placeholder taake index out-of-sync na ho
                        break;

                    case CleaningStepType.GlueApply:
                        if (step.stepPrefab != null)
                        {
                            GameObject instantiatedGlue = Instantiate(step.stepPrefab, stepObj.transform);
                            instantiatedGlue.transform.localPosition = Vector3.zero;
                            instantiatedGlue.transform.localRotation = Quaternion.identity;
                            instantiatedGlue.transform.localScale = Vector3.one;
                        }
                        layersList.Add(null); // Placeholder taake index out-of-sync na ho
                        break;
                }

                CleaningLayer cleaningLayerComponent = stepObj.AddComponent<CleaningLayer>();
                cleaningLayerComponent.requiredTool = step.requiredTool;
                layerRequiredTools.Add(step.requiredTool);

                // Sirf pehli 2 layers active rahengi
                stepObj.SetActive(i == 0 || i == 1);
            }
        }
    }

    void Update()
    {
        if (scraperTriggerEdge != null && currentToolData != null)
        {
            bool isScraper = currentToolData.toolType == ToolType.Scraper || currentToolData.name.Contains("Scraper");
            scraperTriggerEdge.SetActive(isScraper);
        }
        if (PauseManager.IsGamePaused)
        {
            StopToolEffects();
            return;
        }

        // BUG FIX: Jab tak mouse ya finger UI/Buttons ke upar hai, follower ko stop rakhein
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            StopToolEffects();
            if (toolFollower != null && toolFollower.enabled && !isTransitioningTool)
            {
                toolFollower.enabled = false;
            }
            return;
        }
        else
        {
            if (toolFollower != null && !toolFollower.enabled && !isTransitioningTool)
            {
                toolFollower.enabled = true;
            }
        }

        // UNIFIED LOGIC: Touch down/up par panels ko hide aur show karna
        if (!gameCompleted && !isTransitioningTool)
        {
            if (Input.GetMouseButtonDown(0))
            {
                ToggleGameplayUI(true);  // Press par sab hide ho jayega
            }
            else if (Input.GetMouseButtonUp(0))
            {
                ToggleGameplayUI(false); // Touch chorne par sab show ho jayega
            }
        }

        if (Camera.main != null)
        {
            if (Camera.main.orthographic)
                Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, targetCameraSize, Time.deltaTime * cameraTransitionIntensity);
            else
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetCameraSize, Time.deltaTime * cameraTransitionIntensity);
        }

        // Camera Intensity setup (Dynamic from ObjectData or Fallback)
        float currentIntensity = (objectData != null && objectData.cameraMovementIntensity > 0)
            ? objectData.cameraMovementIntensity
            : cameraMoveIntensity;

        Vector3 currentMousePos = Input.mousePosition;
        Vector3 targetCameraPos = new Vector3(0, 0, Camera.main.transform.position.z);

        if (!gameCompleted && !isTransitioningTool && layersList.Count > 0 && Input.GetMouseButton(0))
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mouseOffset = new Vector2(
                (currentMousePos.x - screenCenter.x) / screenCenter.x,
                (currentMousePos.y - screenCenter.y) / screenCenter.y
            );

            // --- Y-AXIS OPTIONAL MOVEMENT CHECK ---
            float targetY = (objectData != null && objectData.enableYAxisMovement)
                ? mouseOffset.y * currentIntensity
                : 0f;

            targetCameraPos = new Vector3(mouseOffset.x * currentIntensity, targetY, Camera.main.transform.position.z);
        }

        if (Camera.main != null)
        {
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetCameraPos, Time.deltaTime * 5f);
        }

        if (gameCompleted || isTransitioningTool || layersList.Count == 0) return;
        if (layerFinishedWaitingRelease)
        {
            if (!Input.GetMouseButton(0))
            {
                layerFinishedWaitingRelease = false;
                StartCoroutine(TransitionToNextLayerRoutine());
            }
            return;
        }

        if (currentLayer >= layersList.Count) return;
        if (Input.GetMouseButton(0) && currentToolData != null && toolFollower.CanClean)
        {
            Vector3 world;
            if (eraseAnchor != null) world = eraseAnchor.position;
            else
            {
                float cameraDistance = Mathf.Abs(Camera.main.transform.position.z);
                world = Camera.main.ScreenToWorldPoint(new Vector3(currentMousePos.x, currentMousePos.y, cameraDistance));
            }

            world.z = 0;
            bool isOverLayer = EraseAtWorldPosition(world);
            bool shouldPlay = currentToolData.soundOnlyOnHit ? isOverLayer : true;

            if (shouldPlay) effectGraceTimer = 0.15f;
        }

        if (effectGraceTimer > 0)
        {
            effectGraceTimer -= Time.deltaTime;
            PlayToolEffects();
        }
        else
        {
            StopToolEffects();
        }

        if (textureNeedsApply)
        {
            texture.Apply(false);
            textureNeedsApply = false;
            needsProgressCheck = true;
        }

        if (needsProgressCheck)
        {
            progressTimer += Time.deltaTime;
            if (progressTimer > 0.15f || !Input.GetMouseButton(0))
            {
                UpdateProgress();
                progressTimer = 0f;
                needsProgressCheck = false;
            }
        }

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 15f);
        progressFill.fillAmount = currentFill;

        if (objectData != null && objectData.scraperChunksPrefab != null)
        {
            // Agar list mein layers hain, toh pehli layer (Index 0) ko hamesha band rakho
            if (layersList != null && layersList.Count > 0 && layersList[0] != null)
            {
                if (layersList[0].gameObject.activeSelf)
                {
                    layersList[0].gameObject.SetActive(false);
                }
            }

            // Backup Check: Agar Hierarchy mein direct child pada hai bina list ke, use bhi band karo
            if (levelParentAnchor != null)
            {
                Transform oldLayer = levelParentAnchor.Find("Dirty_Layer_0");
                if (oldLayer != null && oldLayer.gameObject.activeSelf)
                {
                    oldLayer.gameObject.SetActive(false);
                }
            }
        }
    }

    // UNIFIED FUNCTION: Teeno panels ko hide/show karne ke liye ek single controller
    [Header("Slide UI Animations")]
    private Coroutine topUISlideCoroutine;
    private Vector2 pauseBasePos;
    private Vector2 coinBasePos;
    private Vector3 pauseBaseScale = Vector3.one;
    private Vector3 coinBaseScale = Vector3.one;
    private bool isBasePosSaved = false;

    // UNIFIED FUNCTION: Controls Slide Animations for Side Panels
    public void ToggleGameplayUI(bool hide)
    {
        // 1. Variant Panel (Smart Check)
        if (currentToolData != null && currentToolData.hasVariants && currentToolData.toolVariants.Count > 0 && variantMainPanel != null)
        {
            if (panelAnimCoroutine != null) StopCoroutine(panelAnimCoroutine);

            // Agar touch chora gaya hai (!hide) lekin layer abhi complete hui hai (!layerFinishedWaitingRelease), 
            // toh variant panel ko WAPAS MAT DIKHAO.
            bool shouldShowVariant = !hide && !layerFinishedWaitingRelease;
            panelAnimCoroutine = StartCoroutine(AnimateVariantPanelVideoStyle(shouldShowVariant));
        }

        // 2. Pause Button & Coin Bar (Yeh HAR BAAR normal slide-in/out honge!)
        if (topUISlideCoroutine != null) StopCoroutine(topUISlideCoroutine);
        topUISlideCoroutine = StartCoroutine(SlideSideUIRoutine(hide));
    }

    private IEnumerator SlideSideUIRoutine(bool hide)
    {
        RectTransform pauseRect = pauseButton != null ? pauseButton.GetComponent<RectTransform>() : null;
        RectTransform coinRect = gameplayCoinPanel != null ? gameplayCoinPanel.GetComponent<RectTransform>() : null;

        // Pehli dafa Original Positions aur Scales safe store karein
        if (!isBasePosSaved)
        {
            if (pauseRect != null)
            {
                pauseBasePos = pauseRect.anchoredPosition;
                pauseBaseScale = pauseRect.localScale;
            }
            if (coinRect != null)
            {
                coinBasePos = coinRect.anchoredPosition;
                coinBaseScale = coinRect.localScale;
            }
            isBasePosSaved = true;
        }

        float duration = 0.5f; // Animation duration
        float time = 0f;

        // Start Positions & Scales
        Vector2 pauseStartPos = pauseRect != null ? pauseRect.anchoredPosition : Vector2.zero;
        Vector2 coinStartPos = coinRect != null ? coinRect.anchoredPosition : Vector2.zero;

        Vector3 pauseStartScale = pauseRect != null ? pauseRect.localScale : Vector3.one;
        Vector3 coinStartScale = coinRect != null ? coinRect.localScale : Vector3.one;

        // Target Positions & Scales:
        // Hide = True  -> Position Outside (+350 / -350) & Scale = ZERO (Small to disappear)
        // Hide = False -> Position Original Base & Scale = FULL (Base Scale)
        Vector2 pauseTargetPos = hide ? new Vector2(pauseBasePos.x + 350f, pauseBasePos.y) : pauseBasePos;
        Vector2 coinTargetPos = hide ? new Vector2(coinBasePos.x - 350f, coinBasePos.y) : coinBasePos;

        Vector3 pauseTargetScale = hide ? Vector3.zero : pauseBaseScale;
        Vector3 coinTargetScale = hide ? Vector3.zero : coinBaseScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t); // Smooth interpolation

            // Position Lerp (Slide)
            if (pauseRect != null)
                pauseRect.anchoredPosition = Vector2.Lerp(pauseStartPos, pauseTargetPos, smoothT);

            if (coinRect != null)
                coinRect.anchoredPosition = Vector2.Lerp(coinStartPos, coinTargetPos, smoothT);

            // Scale Lerp (Small to Full / Full to Small)
            if (pauseRect != null)
                pauseRect.localScale = Vector3.Lerp(pauseStartScale, pauseTargetScale, smoothT);

            if (coinRect != null)
                coinRect.localScale = Vector3.Lerp(coinStartScale, coinTargetScale, smoothT);

            yield return null;
        }

        // Direct Exact Targets Lock
        if (pauseRect != null)
        {
            pauseRect.anchoredPosition = pauseTargetPos;
            pauseRect.localScale = pauseTargetScale;
        }
        if (coinRect != null)
        {
            coinRect.anchoredPosition = coinTargetPos;
            coinRect.localScale = coinTargetScale;
        }
    }
    void PlayToolEffects()
    {
        AnimateTool();
        foreach (GameObject activePart in activeParticlesList)
        {
            if (activePart != null)
            {
                ParticleSystem[] allParticles = activePart.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in allParticles) if (!ps.isPlaying) ps.Play(true);
            }
        }

        if (AudioManager.Instance != null && currentToolData != null && currentToolData.toolSound != null)
        {
            AudioManager.Instance.PlayLoopingSFX(currentToolData.toolSound, true);
        }
    }

    void StopToolEffects()
    {
        ResetToolPosition();
        foreach (GameObject activePart in activeParticlesList)
        {
            if (activePart != null)
            {
                ParticleSystem[] allParticles = activePart.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in allParticles)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopToolSFX();
        }
    }

    void LoadToolEffect(ToolData tool)
    {
        if (currentParticle != null) Destroy(currentParticle);
        foreach (GameObject go in activeParticlesList) if (go != null) Destroy(go);
        activeParticlesList.Clear();

        if (effectAnchor == null || tool == null) return;
        if (eraseAnchor != null) eraseAnchor.localPosition = tool.eraseOffset;

        if (tool.useParticles && tool.particlePrefab != null)
        {
            currentParticle = Instantiate(tool.particlePrefab, effectAnchor);
            currentParticle.transform.localPosition = tool.particleOffset;

            ParticleSystem[] allParticles = currentParticle.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                var main = ps.main;
                main.playOnAwake = false;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            activeParticlesList.Add(currentParticle);
        }

        if (tool.useSecondParticles && tool.secondParticlePrefab != null)
        {
            GameObject secondParticle = Instantiate(tool.secondParticlePrefab, effectAnchor);
            secondParticle.transform.localPosition = tool.secondParticleOffset;

            ParticleSystem[] allParticles = secondParticle.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                var main = ps.main;
                main.playOnAwake = false;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            activeParticlesList.Add(secondParticle);
        }
    }

    void PrepareLayer()
    {
        if (currentLayer >= layersList.Count) return;

        // Chunk / Glue step par SpriteRenderer null hoga, isliye texture slicing skip karein
        if (layersList[currentLayer] == null) return;

        // =========================================================================
        // AAPKA ORIGINAL CODE (NO CHANGES AT ALL BELOW THIS POINT)
        // =========================================================================
        Sprite originalSprite = layersList[currentLayer].sprite;
        Texture2D sheetTexture = originalSprite.texture;

        Rect sliceRect = originalSprite.rect;

        int x = Mathf.RoundToInt(sliceRect.x);
        int y = Mathf.RoundToInt(sliceRect.y);
        int width = Mathf.RoundToInt(sliceRect.width);
        int height = Mathf.RoundToInt(sliceRect.height);

        Color[] slicePixels = sheetTexture.GetPixels(x, y, width, height);
        totalOpaquePixels = 0;
        foreach (Color c in slicePixels)
        {
            if (c.a > 0.25f) totalOpaquePixels++;
        }

        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels(slicePixels);
        texture.Apply();
        Vector2 exactPivot = new Vector2(originalSprite.pivot.x / width, originalSprite.pivot.y / height);
        layersList[currentLayer].sprite = Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            exactPivot,
            originalSprite.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect
        );
    }

    public bool EraseAtWorldPosition(Vector3 world)
    {
        // ================================================================
        //  ISOLATED SCRAPER BYPASS RULE (BILKUL TOP PAR)
        // ================================================================
        if (currentToolData != null && (currentToolData.toolType == ToolType.Scraper || currentToolData.name.Contains("Scraper")))
        {
            // Scraper ke liye pixel manipulation skip karein aur true return kar dein
            return true;
        }

        // --- Baki Saari Purani Logic Bilkul Same Rahegi (Brush/Water ke liye) ---
        if (currentLayer >= layersList.Count) return false;
        Vector3 local = layersList[currentLayer].transform.InverseTransformPoint(world);
        SpriteRenderer sr = layersList[currentLayer];

        float width = sr.sprite.bounds.size.x;
        float height = sr.sprite.bounds.size.y;
        float xp = (local.x + width / 2) / width;
        float yp = (local.y + height / 2) / height;
        int x = Mathf.RoundToInt(xp * texture.width);
        int y = Mathf.RoundToInt(yp * texture.height);
        int size = currentToolData.brushSize;

        float brushHardness = 0.1f;
        bool actualCleaningDone = false;
        for (int i = -size; i < size; i++)
        {
            for (int j = -size; j < size; j++)
            {
                float distance = Mathf.Sqrt(i * i + j * j);
                if (distance < size)
                {
                    int px = x + i;
                    int py = y + j;

                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        Color c = texture.GetPixel(px, py);
                        if (c.a <= 0.05f) continue;
                        float alphaReduction = Mathf.Clamp01(1f - (distance / size));
                        alphaReduction = Mathf.Pow(alphaReduction, brushHardness);
                        c.a -= alphaReduction * eraserIntensityMultiplier;
                        if (c.a < 0f) c.a = 0f;

                        texture.SetPixel(px, py, c);
                        textureNeedsApply = true;
                        actualCleaningDone = true;
                    }
                }
            }
        }
        return actualCleaningDone;
    }

    void UpdateProgress()
    {
        // Safety Check: Agar game finish ho chuki hai ya step list empty hai
        if (gameCompleted || layerFinishedWaitingRelease || objectData == null || objectData.cleaningSteps == null || objectData.cleaningSteps.Count == 0) return;
        if (currentLayer >= objectData.cleaningSteps.Count) return;

        CleaningStep currentStep = objectData.cleaningSteps[currentLayer];

        float percent = 0f;
        bool isLayerFullyCleaned = false;

        // --- STEP TYPE KE MUTAABIQ LOGIC SWITCH ---
        switch (currentStep.stepType)
        {
            case CleaningStepType.ChunkScraper:
                // AGAR TOTAL CHUNKS ZERO HAIN, TO RE-COUNT KAREIN (Aapki Original Safety Logic)
                if (totalScraperChunks <= 0)
                {
                    if (levelParentAnchor != null)
                    {
                        MudChunk[] allChunks = levelParentAnchor.GetComponentsInChildren<MudChunk>(true);
                        totalScraperChunks = allChunks.Length;
                        remainingScraperChunks = totalScraperChunks;
                    }
                }

                if (totalScraperChunks == 0) totalScraperChunks = 1;

                int removedChunks = totalScraperChunks - remainingScraperChunks;
                percent = ((float)removedChunks / totalScraperChunks) * 100f;

                // FORCE CHECK: Jab tak aakhri chunk baqi hai, percent 100% nahi ho sakta!
                if (remainingScraperChunks > 0 && percent >= 99f)
                {
                    percent = 99f;
                }

                isLayerFullyCleaned = (remainingScraperChunks <= 0);
                break;

            case CleaningStepType.PixelEraser:
                // Normal tools ke liye pixels count karein (Aapki Original Logic)
                if (texture != null)
                {
                    Color[] pixels = texture.GetPixels();
                    int currentOpaque = 0;
                    foreach (Color c in pixels)
                    {
                        if (c.a > 0.25f) currentOpaque++;
                    }

                    if (totalOpaquePixels == 0) totalOpaquePixels = 1;
                    int removed = totalOpaquePixels - currentOpaque;
                    percent = ((float)removed / totalOpaquePixels) * 100f;

                    // Aapka original script-level cleaningThreshold variable hi istemal hoga
                    isLayerFullyCleaned = (percent >= cleaningThreshold);
                }
                break;

            case CleaningStepType.GlueApply:
                // Future Glue application completion logic (Aap yahan bad mein add kar sakte hain)
                break;
        }

        float visualPercent = percent;
        if (visualPercent > 100f) visualPercent = 100f;

        // UI UPDATE (Aapka Original UI System)
        targetFill = visualPercent / 100f;
        progressFill.fillAmount = targetFill;
        percentText.text = Mathf.RoundToInt(visualPercent) + "%";

        // LAYER COMPLETION LOGIC
        if (isLayerFullyCleaned)
        {
            Debug.Log("LAYER TARGET ACHIEVED COMPLETELY!");

            if (!isLayerClearSoundPlayed)
            {
                if (AudioManager.Instance != null && AudioManager.Instance.layerClearSFX != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.layerClearSFX);
                }
                isLayerClearSoundPlayed = true;
            }

            effectGraceTimer = 0f;
            StopToolEffects();

            ClearRemainingLayer();
            if (variantMainPanel != null)
            {
                variantMainPanel.SetActive(false);
            }

            if (currentLayer >= objectData.cleaningSteps.Count - 1)
            {
                CompleteGame();
            }
            else
            {
                layerFinishedWaitingRelease = true;
            }
        }
    }
    public void ScraperChunkDestroyed()
    {
        remainingScraperChunks--;
        if (remainingScraperChunks < 0)
        {
            remainingScraperChunks = 0;
        }

        // Chunk kam hone par instant progress calculation update karein
        UpdateProgress();
    }

    void ClearRemainingLayer()
    {
        // 1. Current Step Check (Step-based logic + Fallback isScraperActive check)
        bool isChunkStep = false;
        if (objectData != null && objectData.cleaningSteps != null && currentLayer < objectData.cleaningSteps.Count)
        {
            CleaningStep currentStep = objectData.cleaningSteps[currentLayer];
            if (currentStep != null && currentStep.stepType == CleaningStepType.ChunkScraper)
            {
                isChunkStep = true;
            }
        }

        // 2. AGAR SCRAPER / CHUNK STEP HAI: Saare remaining MudChunks ko hide karein
        if (isChunkStep || isScraperActive)
        {
            remainingScraperChunks = 0;
            if (levelParentAnchor != null)
            {
                MudChunk[] remainingList = levelParentAnchor.GetComponentsInChildren<MudChunk>(true);
                foreach (MudChunk chunk in remainingList)
                {
                    if (chunk != null && chunk.gameObject.activeSelf)
                    {
                        chunk.gameObject.SetActive(false);
                    }
                }
            }
        }
        else
        {
            // 3. AGAR PIXEL / TEXTURE ERASER STEP HAI (Texture Null Safety Guard ke saath)
            if (texture != null)
            {
                Color[] pixels = texture.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].a = 0f;
                }
                texture.SetPixels(pixels);
                texture.Apply(false);
            }
        }

        // 4. UI KO FORCE 100% KAREIN
        targetFill = 1f;
        currentFill = 1f;
        if (progressFill != null) progressFill.fillAmount = 1f;
        if (percentText != null) percentText.text = "100%";
    }

    IEnumerator TransitionToNextLayerRoutine()
    {
        isTransitioningTool = true;
        if (toolFollower != null) toolFollower.enabled = false;

        targetCameraSize = defaultCameraSize;

        yield return new WaitForSeconds(0.5f);

        Vector3 startPos = toolFollower.transform.position;
        Vector3 leftTarget = startPos + Vector3.left * 15f;

        float time = 0;
        float durationOut = 0.5f;
        while (time < durationOut)
        {
            time += Time.deltaTime;
            float t = time / durationOut;
            toolFollower.transform.position = Vector3.Lerp(startPos, leftTarget, t * t);
            yield return null;
        }

        // 1. Purani complete hone wali layer ko hide karein
        if (stepGameObjects != null && currentLayer < stepGameObjects.Count && stepGameObjects[currentLayer] != null)
        {
            stepGameObjects[currentLayer].SetActive(false);
        }

        currentLayer++;

        // 2. Game Finish Check
        if (currentLayer >= objectData.cleaningSteps.Count)
        {
            CompleteGame();
            isTransitioningTool = false;
            yield break;
        }

        // 3. Nayi layer ko active karein
        if (stepGameObjects != null)
        {
            if (currentLayer < stepGameObjects.Count && stepGameObjects[currentLayer] != null)
                stepGameObjects[currentLayer].SetActive(true);

            if (currentLayer + 1 < stepGameObjects.Count && stepGameObjects[currentLayer + 1] != null)
                stepGameObjects[currentLayer + 1].SetActive(true);
        }

        PrepareLayer();

        ToolData nextTool = null;
        if (layerRequiredTools != null && currentLayer < layerRequiredTools.Count)
        {
            nextTool = layerRequiredTools[currentLayer];
        }

        SelectTool(nextTool, true);
        targetCameraSize = (nextTool != null && nextTool.cameraZoomSize > 0.1f) ? nextTool.cameraZoomSize : defaultCameraSize;

        float camZ = Mathf.Abs(Camera.main.transform.position.z);

        isLayerClearSoundPlayed = false;

        currentFill = 0f;
        targetFill = 0f;
        progressFill.fillAmount = 0f;
        percentText.text = "0%";

        yield return new WaitForSeconds(0.4f);
        time = 0;
        float durationIn = 0.6f;
        while (time < durationIn)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / durationIn);

            Vector3 currentRestTarget = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.2f, camZ));
            currentRestTarget.z = 0f;
            Vector3 currentRightStart = currentRestTarget + Vector3.right * 15f;

            toolFollower.transform.position = Vector3.Lerp(currentRightStart, currentRestTarget, t);
            yield return null;
        }

        Vector3 finalRestTarget = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.2f, camZ));
        finalRestTarget.z = 0f;
        toolFollower.transform.position = finalRestTarget;

        UpdateUpcomingIconsPanel(true);
        if (toolFollower != null) toolFollower.enabled = true;
        isTransitioningTool = false;
    }

    void CompleteGame()
    {
        Debug.Log("CompleteGame() START HUA HAI!");
        gameCompleted = true;
        percentText.text = "100%";
        progressFill.fillAmount = 1f;
        targetCameraSize = levelCompleteZoomSize;

        if (celebrationPrefab != null && activeCelebrationInstance == null)
        {
            activeCelebrationInstance = Instantiate(celebrationPrefab, Vector3.zero, Quaternion.identity);
        }
        if (toolFollower != null) toolFollower.gameObject.SetActive(false);
        if (previousToolUIImage != null) previousToolUIImage.gameObject.SetActive(false);
        if (currentToolUIImage != null) currentToolUIImage.gameObject.SetActive(false);
        if (upcomingToolUIImage != null) upcomingToolUIImage.gameObject.SetActive(false);
        UpdateUpcomingIconsPanel(false);

        // Variant aur pause panel ko hide karein
        if (variantMainPanel != null) variantMainPanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(false);

        // =========================================================================
        // FIX 1: COIN BAR KO ACTIVE KAREIN AUR SLIDE-IN ANIMATION TRIGGER KAREIN
        // =========================================================================
        if (gameplayCoinPanel != null)
        {
            gameplayCoinPanel.SetActive(true);
            gameplayCoinPanel.transform.SetAsLastSibling(); // Top layer par laayein

            // Agar aapki script mein side slide animation hai toh trigger karein:
            if (topUISlideCoroutine != null) StopCoroutine(topUISlideCoroutine);
            topUISlideCoroutine = StartCoroutine(SlideSideUIRoutine(false));
        }

        // --- DYNAMIC WIN PANEL ICON ASSIGNMENT ---
        if (winPanelIconImage != null)
        {
            if (objectData != null && objectData.levelCompleteIcon != null)
            {
                winPanelIconImage.sprite = objectData.levelCompleteIcon;
            }
            else if (objectData != null && objectData.cleanSprite != null)
            {
                winPanelIconImage.sprite = objectData.cleanSprite;
            }
            else if (completedLevelSprite != null)
            {
                winPanelIconImage.sprite = completedLevelSprite;
            }
        }

        if (celebrationSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(celebrationSound);
        }

        // =========================================================================
        // FIX 2: COIN SEQUENCE START KAREIN (Progress bar ko baad mein hide hone dein)
        // =========================================================================
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.StartCoroutine(
                CoinManager.Instance.PlayCoinSequenceRoutine(gameplayCoinPanel, levelCompletePanel, levelCompleteDelay)
            );
        }
        else
        {
            StartCoroutine(ShowDelayedUIAndCoinsRoutine());
        }

        // Progress bar ko sab se aakhir mein hide karein taake spawn point position break na ho
        if (progressBarMainPanel != null) progressBarMainPanel.SetActive(false);
    }


    void SnapToolUI()
    {
        if (previousToolUIImage != null)
        {
            previousToolUIImage.rectTransform.anchoredPosition = prevPos;
            previousToolUIImage.transform.localScale = new Vector3(inactiveToolScale, inactiveToolScale, 1f); SetImageAlpha(previousToolUIImage, 0.6f);
        }
        if (currentToolUIImage != null)
        {
            currentToolUIImage.rectTransform.anchoredPosition = currPos;
            currentToolUIImage.transform.localScale = new Vector3(activeToolScale, activeToolScale, 1f); SetImageAlpha(currentToolUIImage, 1f);
        }
        if (upcomingToolUIImage != null)
        {
            upcomingToolUIImage.rectTransform.anchoredPosition = upPos;
            upcomingToolUIImage.transform.localScale = new Vector3(inactiveToolScale, inactiveToolScale, 1f); SetImageAlpha(upcomingToolUIImage, 0.6f);
        }
    }

    void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color; c.a = alpha; img.color = c;
    }

    IEnumerator SlideToolUI()
    {
        float duration = 0.5f;
        float time = 0;
        Vector3 smallScale = new Vector3(inactiveToolScale, inactiveToolScale, 1f);
        Vector3 largeScale = new Vector3(activeToolScale, activeToolScale, 1f);
        if (previousToolUIImage != null && previousToolUIImage.gameObject.activeSelf)
        {
            previousToolUIImage.rectTransform.anchoredPosition = currPos;
            previousToolUIImage.transform.localScale = largeScale;
            SetImageAlpha(previousToolUIImage, 1f);
        }
        if (currentToolUIImage != null && currentToolUIImage.gameObject.activeSelf)
        {
            currentToolUIImage.rectTransform.anchoredPosition = upPos;
            currentToolUIImage.transform.localScale = smallScale;
            SetImageAlpha(currentToolUIImage, 0.6f);
        }
        if (upcomingToolUIImage != null && upcomingToolUIImage.gameObject.activeSelf)
        {
            upcomingToolUIImage.rectTransform.anchoredPosition = upPos + new Vector2(toolSpacing, 0);
            upcomingToolUIImage.transform.localScale = smallScale;
            SetImageAlpha(upcomingToolUIImage, 0f);
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t);
            if (previousToolUIImage != null && previousToolUIImage.gameObject.activeSelf)
            {
                previousToolUIImage.rectTransform.anchoredPosition = Vector2.Lerp(currPos, prevPos, smoothT);
                previousToolUIImage.transform.localScale = Vector3.Lerp(largeScale, smallScale, smoothT);
                SetImageAlpha(previousToolUIImage, Mathf.Lerp(1f, 0.6f, smoothT));
            }
            if (currentToolUIImage != null && currentToolUIImage.gameObject.activeSelf)
            {
                currentToolUIImage.rectTransform.anchoredPosition = Vector2.Lerp(upPos, currPos, smoothT);
                currentToolUIImage.transform.localScale = Vector3.Lerp(smallScale, largeScale, smoothT);
                SetImageAlpha(currentToolUIImage, Mathf.Lerp(0.6f, 1f, smoothT));
            }
            if (upcomingToolUIImage != null && upcomingToolUIImage.gameObject.activeSelf)
            {
                upcomingToolUIImage.rectTransform.anchoredPosition = Vector2.Lerp(upPos + new Vector2(toolSpacing, 0), upPos, smoothT);
                SetImageAlpha(upcomingToolUIImage, Mathf.Lerp(0f, 0.6f, smoothT));
            }
            yield return null;
        }
        SnapToolUI();
    }

    void UpdateToolUI(bool animate = false)
    {
        if (!positionsSaved)
        {
            if (currentToolUIImage != null)
            {
                currPos = currentToolUIImage.rectTransform.anchoredPosition;
                prevPos = currPos + new Vector2(-toolSpacing, 0);
                upPos = currPos + new Vector2(toolSpacing, 0);
            }
            positionsSaved = true;
        }

        if (previousToolUIImage != null)
        {
            if (currentLayer > 0 && layersList[currentLayer - 1] != null)
            {
                ToolData prevTool = layerRequiredTools[currentLayer - 1];
                if (prevTool != null && prevTool.panelIcon != null)
                {
                    previousToolUIImage.sprite = prevTool.panelIcon;
                    previousToolUIImage.gameObject.SetActive(true);
                }
                else previousToolUIImage.gameObject.SetActive(false);
            }
            else previousToolUIImage.gameObject.SetActive(false);
        }

        if (currentToolUIImage != null && currentToolData != null && currentToolData.panelIcon != null)
        {
            currentToolUIImage.sprite = currentToolData.panelIcon;
            currentToolUIImage.gameObject.SetActive(true);
        }

        if (upcomingToolUIImage != null)
        {
            int nextLayerIndex = currentLayer + 1;
            if (nextLayerIndex < layersList.Count && layersList[nextLayerIndex] != null)
            {
                ToolData nextTool = layerRequiredTools[nextLayerIndex];
                if (nextTool != null && nextTool.panelIcon != null)
                {
                    upcomingToolUIImage.sprite = nextTool.panelIcon;
                    upcomingToolUIImage.gameObject.SetActive(true);
                }
                else upcomingToolUIImage.gameObject.SetActive(false);
            }
            else upcomingToolUIImage.gameObject.SetActive(false);
        }

        if (animate) StartCoroutine(SlideToolUI());
        else SnapToolUI();
    }

    IEnumerator AnimateUIPopup(Image img, float delay)
    {
        if (img == null || !img.gameObject.activeSelf) yield break;
        img.transform.localScale = Vector3.zero; yield return new WaitForSeconds(delay);
        float time = 0; float duration = 0.3f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scale = Mathf.Lerp(0, 1, t * t * (3f - 2f * t));
            img.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
        img.transform.localScale = Vector3.one;
    }

    void UpdateUpcomingIconsPanel(bool animate = false)
    {
        if (upcomingIcons == null || upcomingIcons.Length == 0) return;
        float delay = 0f;
        for (int i = 0; i < upcomingIcons.Length; i++)
        {
            int layerIndex = currentLayer + i + 1;
            if (layerIndex < layersList.Count && layersList[layerIndex] != null)
            {
                upcomingIcons[i].gameObject.SetActive(true);
                upcomingIcons[i].sprite = layersList[layerIndex].sprite; if (animate)
                {
                    StartCoroutine(AnimateUIPopup(upcomingIcons[i], delay));
                    delay += 0.1f;
                }
            }
            else upcomingIcons[i].gameObject.SetActive(false);
        }
    }

    public void SelectTool(ToolData tool, bool animateUI = false)
    {
        currentToolData = tool;
        toolFollower.SetTool(tool);
        LoadToolEffect(tool);
        UpdateToolUI(animateUI);

        isToolPosSaved = false;
        currentEquippedVariant = null;

        SetupToolVariantsPanel(tool);
    }
   public void GoToHome()
    {
        if (activeCelebrationInstance != null)
        {
            Destroy(activeCelebrationInstance);
        }
        PlayerPrefs.Save();
        SceneManager.LoadScene("HomeScene");
    }
    IEnumerator ShowDelayedUIAndCoinsRoutine()
    {
        Debug.Log("<color=yellow>1. ShowDelayedUIAndCoinsRoutine Start Hua!</color>");

        yield return new WaitForSeconds(levelCompleteDelay);

        // 1. PEHLE COIN BAR SLIDE-IN KAREIN
        if (gameplayCoinPanel != null)
        {
            gameplayCoinPanel.SetActive(true);
            gameplayCoinPanel.transform.SetAsLastSibling();

            if (topUISlideCoroutine != null) StopCoroutine(topUISlideCoroutine);
            topUISlideCoroutine = StartCoroutine(SlideSideUIRoutine(false));
            Debug.Log("<color=yellow>2. Coin Bar Slide-In Started</color>");
        }

        // Coin Bar ko screen par aane ka time dein
        yield return new WaitForSeconds(0.4f);

        // 2. COIN ANIMATION TRIGGER KAREIN
        if (CoinManager.Instance != null)
        {
            Debug.Log("<color=green>3. TriggerCoinSwoopAnimation Call Ho Raha Hai!</color>");
            CoinManager.Instance.TriggerCoinSwoopAnimation(20);
        }
        else
        {
            Debug.LogError("CoinManager.Instance NULL hai! Check karein Scene mein CoinManager exist karta hai ya nahi.");
        }

        // Coins fly hone ka wait karein
        yield return new WaitForSeconds(1.2f);

        // 3. WIN PANEL SHOW KAREIN
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            Debug.Log("<color=yellow>4. Level Complete Panel Active Hua!</color>");
        }

        // 4. AAKHIR MEIN GAME CONTROLLER KO NOTIFY KAREIN
        if (GameStepController.Instance != null)
        {
            Debug.Log("<color=yellow>5. GameStepController Notified!</color>");
            GameStepController.Instance.OnStepFinishedFromMinigame();
        }
    }
    Transform GetToolTransformToAnimate()
    {
        if (toolFollower == null) return null;
        return toolFollower.transform.childCount > 0 ?
            toolFollower.transform.GetChild(0) : toolFollower.transform;
    }


    // MaskEraser.cs ke andar is function ko public banayein:
    

    void AnimateTool()
    {
        if (currentToolData == null || toolFollower == null) return;
        Transform toolObj = GetToolTransformToAnimate();
        if (toolObj == null) return;

        if (!isToolPosSaved)
        {
            originalToolLocalPos = toolObj.localPosition;
            originalToolRotation = toolObj.localRotation;
            isToolPosSaved = true;
        }

        if (currentToolData.movementType == ToolMovementType.Scrubbing)
        {
            float shake = Mathf.Sin(Time.time * currentToolData.scrubSpeed) * currentToolData.scrubAmount;
            toolObj.localPosition = originalToolLocalPos + new Vector3(shake, 0, 0);
            toolObj.localRotation = originalToolRotation;
        }
        else if (currentToolData.movementType == ToolMovementType.Spraying)
        {
            float vibration = Random.Range(-0.05f, 0.05f);
            toolObj.localPosition = originalToolLocalPos + new Vector3(vibration, vibration, 0);
            toolObj.localRotation = originalToolRotation;
        }
        else if (currentToolData.movementType == ToolMovementType.Rotation)
        {
            float angle = Mathf.Sin(Time.time * currentToolData.rotationSpeed) * currentToolData.rotationAmount;
            toolObj.localRotation = originalToolRotation * Quaternion.Euler(0, 0, angle);
            toolObj.localPosition = originalToolLocalPos;
        }
    }

    void ResetToolPosition()
    {
        if (toolFollower == null || !isToolPosSaved) return;
        Transform toolObj = GetToolTransformToAnimate();
        if (toolObj != null)
        {
            toolObj.localPosition = originalToolLocalPos;
            toolObj.localRotation = originalToolRotation;
        }
    }

    void SetupToolVariantsPanel(ToolData tool)
    {
        // 1. INSTANT CLEANUP
        if (variantButtonsContainer != null)
        {
            foreach (Transform child in variantButtonsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        spawnedVariantButtons.Clear();

        // 2. Hide panel if no variants
        if (tool == null || !tool.hasVariants || tool.toolVariants.Count == 0)
        {
            if (variantMainPanel != null)
            {
                variantMainPanel.SetActive(false);
            }
            return;
        }

        // 3. Naye Tool ke Variant Buttons Spawn Karein
        foreach (ToolVariant varData in tool.toolVariants)
        {
            GameObject btnObj = Instantiate(variantButtonPrefab, variantButtonsContainer);
            ToolVariantButton varBtnScript = btnObj.GetComponent<ToolVariantButton>();
            if (varBtnScript != null)
            {
                varBtnScript.SetupButton(varData, tool, this);
                spawnedVariantButtons.Add(varBtnScript);
            }
        }

        // =========================================================================
        // 4. ALWAYS FORCE ORIGINAL (INDEX 0) VARIANT ON PLAY / STEP START
        // =========================================================================
        if (tool.toolVariants.Count > 0)
        {
            ToolVariant originalVariant = tool.toolVariants[0]; // Hamesha Original Variant (Index 0)

            // PlayerPrefs mein hamesha Original ko as 'Equipped' save karein taake UI "Equipped" show kare
            PlayerPrefs.SetString(tool.name + "_Equipped", originalVariant.variantName);
            PlayerPrefs.Save();

            // Original Variant skin apply karein
            ApplyVariantSkin(tool, originalVariant, false);
        }

        // 5. Panel Animation Start Karein
        if (variantMainPanel != null)
        {
            variantMainPanel.transform.localScale = new Vector3(0.5f, 0f, 1f);
            variantMainPanel.SetActive(true);

            StartCoroutine(AnimateVariantPanelVideoStyle(true));
        }
    }

    // Variant Panel Pop-up / Hide Coroutine
    // Variant Panel Pop-up / Hide Coroutine (Fixed without unwanted delays during gameplay)
    private IEnumerator AnimateVariantPanelVideoStyle(bool show)
    {
        if (variantMainPanel == null) yield break;

        // Agar show kar rahe hain to panel ko turant Active karein
        if (show)
        {
            variantMainPanel.SetActive(true);
        }

        Vector3 startScale = variantMainPanel.transform.localScale;
        Vector3 targetScale = show ? Vector3.one : new Vector3(0.5f, 0f, 1f);

        float time = 0f;
        float duration = 0.6f; // Quick and smooth response for cleaning stop/start

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t);
            variantMainPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            yield return null;
        }

        variantMainPanel.transform.localScale = targetScale;

        // Agar hide hua hai to animation khatam hone par SetActive(false) karein
        if (!show)
        {
            variantMainPanel.SetActive(false);
        }
    }
    public void ApplyVariantSkin(ToolData tool, ToolVariant variant, bool animate = false)
    {
        if (toolFollower == null || variant == null) return;

        if (animate && variant == currentEquippedVariant)
        {
            Debug.Log("Variant is already equipped. Animation skipped.");
            return;
        }

        currentEquippedVariant = variant;

        //  BRUSH SIZE APPLY (Type Casting Added):
        if (tool != null && variant.brushSize > 0)
        {
            // (int) se float value int mein convert ho jayegi aur error khatam ho jayega
            tool.brushSize = (int)variant.brushSize;
            Debug.Log("Brush size updated to: " + tool.brushSize);
        }

        // Visual animation / Sprite replacement
        if (animate)
        {
            StartCoroutine(AnimateVariantSkinRoutine(variant));
        }
        else
        {
            SpriteRenderer toolSR = toolFollower.GetComponentInChildren<SpriteRenderer>();
            if (toolSR != null && variant.toolSprite != null)
            {
                toolSR.sprite = variant.toolSprite;
            }
        }

        // UI Buttons Update
        foreach (ToolVariantButton btn in spawnedVariantButtons)
        {
            if (btn != null) btn.UpdateUI();
        }
    }

    // Variant Skin Change Animation Coroutine
    private IEnumerator AnimateVariantSkinRoutine(ToolVariant variant)
    {
        if (toolFollower == null) yield break;

        toolFollower.enabled = false;
        Vector3 startPos = toolFollower.transform.position;
        Vector3 outTarget = startPos + Vector3.left * 15f;
        float time = 0;
        float durationOut = 0.3f;

        // Tool screen se bahar jayega
        while (time < durationOut)
        {
            time += Time.deltaTime;
            float t = time / durationOut;
            toolFollower.transform.position = Vector3.Lerp(startPos, outTarget, t * t);
            yield return null;
        }

        // Sprite update
        SpriteRenderer toolSR = toolFollower.GetComponentInChildren<SpriteRenderer>();
        if (toolSR != null && variant.toolSprite != null)
        {
            toolSR.sprite = variant.toolSprite;
        }

        // Tool screen par wapas aayega
        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 bottomScreenTarget = new Vector3(Screen.width / 2f, Screen.height * 0.3f, camZ);
        Vector3 restTarget = Camera.main.ScreenToWorldPoint(bottomScreenTarget);
        restTarget.z = 0;
        Vector3 inStart = restTarget + Vector3.right * 15f;

        toolFollower.transform.position = inStart;

        time = 0;
        float durationIn = 0.4f;
        while (time < durationIn)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / durationIn);
            toolFollower.transform.position = Vector3.Lerp(inStart, restTarget, t);
            yield return null;
        }

        toolFollower.enabled = true;
    }

    IEnumerator AnimateFirstToolOnStartup()
    {
        isTransitioningTool = true;
        yield return new WaitForSeconds(0.15f);

        if (toolFollower != null && Camera.main != null)
        {
            toolFollower.gameObject.SetActive(true);
            Vector3 restTarget = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.2f, 10f));
            restTarget.z = 0f;

            Vector3 startPos = restTarget + Vector3.right * 15f;
            toolFollower.transform.position = startPos;

            if (currentToolData != null)
            {
                SetupToolVariantsPanel(currentToolData);
            }

            float time = 0f;
            float duration = 0.4f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, time / duration);
                toolFollower.transform.position = Vector3.Lerp(startPos, restTarget, t);
                yield return null;
            }

            toolFollower.transform.position = restTarget;
        }

        isTransitioningTool = false;
    }

    void ClearOldGeneratedLayers()
    {
        if (levelParentAnchor == null) return;

        // Purani mitti ke chunks, dynamic layers aur base object ko remove karein
        foreach (Transform child in levelParentAnchor)
        {
            if (child.gameObject.name.Contains("Dirty_Layer") ||
                child.gameObject.name.Contains("Base_Clean_Object") ||
                child.gameObject.name.Contains("Chunk"))
            {
                Destroy(child.gameObject);
            }
        }
    }
    public void InitializeCleaningObject(CleaningObjectData newObjectData)
    {
        if (newObjectData == null) return;

        // 1. Data replace karein
        objectData = newObjectData;
        LevelManager.SelectedObject = newObjectData;

        // 2. Clear old generated objects/layers
        ClearOldGeneratedLayers();

        // 3. Reset level states
        currentLayer = 0;
        gameCompleted = false;

        // 4. Fresh level setup
        SetupGenericLevel();

        if (layersList.Count > 0)
        {
            PrepareLayer();
            SelectTool(layerRequiredTools[currentLayer], false);
        }
    }

}