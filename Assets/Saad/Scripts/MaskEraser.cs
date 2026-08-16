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
    public GameObject progressFillBg;

    // TOOL ANIMATION VARIABLES
    private Vector3 originalToolLocalPos;
    private Quaternion originalToolRotation;
    private bool isToolPosSaved = false;

    [Header("Polish Tool Settings")]
    [Tooltip("Hierarchy se Polish Box/Dabi ka Transform yahan drag karein")]
    public Transform polishSpotTarget;

    [Header("Tool Sorting Settings")]
    public SpriteRenderer toolSpriteRenderer;
    public int defaultToolOrder = 10;

    [Header("Particles")]
    public Transform effectAnchor;
    public Transform eraseAnchor;
    public GameObject currentParticle;

    private List<GameObject> activeParticlesList = new List<GameObject>();

    [Header("Celebration")]
    public GameObject celebrationPrefab;
    public AudioClip celebrationSound;

    [Header("UI Panels")]
    public GameObject levelCompletePanel;
    public Image backgroundImage;

    private Vector3 lastEraseWorldPos;
    private bool hasLastErasePos = false;

    [Tooltip("Gameplay me jo Pause Button hai use yahan drag karein")]
    public GameObject pauseButton;

    [Header("Coin UI Settings")]
    public GameObject gameplayCoinPanel;
    public TMP_Text gameplayCoinText;

    [Header("Diamond UI Settings")]
    public GameObject gameplayDiamondPanel;
    private Vector2 diamondBasePos;
    private Vector3 diamondBaseScale = Vector3.one;

    [Header("--- Ref Video Tool Variant UI ---")]
    public GameObject variantMainPanel;
    public Transform variantButtonsContainer;
    public GameObject variantButtonPrefab;
    private List<ToolVariantButton> spawnedVariantButtons = new List<ToolVariantButton>();

    private Coroutine panelAnimCoroutine;
    private GameObject activeCelebrationInstance;
    private ToolVariant currentEquippedVariant;

    [Header("UI Delay Hide/Show Settings")]
    public float holdToHideDelay = 2.0f;
    public float idleToShowDelay = 2.0f;
    private float touchTimer = 0f;
    private float idleTimer = 0f;
    private bool isUIHiddenByTimer = false;

    [Header("Tool UI Panel")]
    public Image previousToolUIImage;
    public Image currentToolUIImage;
    public Image upcomingToolUIImage;

    [Header("Background Reference")]
    public SpriteRenderer backgroundRenderer;

    [Header("Tool UI Sizes & Spacing")]
    public float activeToolScale = 2f;
    public float inactiveToolScale = 1.5f;
    public float toolSpacing = 100f;

    [Header("Upcoming Objects Panel")]
    public Image[] upcomingIcons;

    [Header("End Game Settings")]
    public float levelCompleteDelay = 3.0f;

    [Header("Eraser Smoothness Settings")]
    [Range(0.01f, 1.0f)] public float brushHardness = 0.15f;
    [Range(0.01f, 1.0f)] public float eraserIntensityMultiplier = 0.1f;

    [Header("Level Completion Settings")]
    [Range(0f, 100f)] public float cleaningThreshold = 95f;

    [Header("Camera Settings")]
    public float cameraTransitionIntensity = 3f;
    public float cameraMoveIntensity = 0.2f;
    public float defaultCameraSize = 5f;

    [Header("Level Completed UI Settings")]
    public Sprite completedLevelSprite;
    [SerializeField] private UnityEngine.UI.Image levelCompleteIconImage;
    public UnityEngine.UI.Image winPanelIconImage;
    public GameObject progressBarMainPanel;

    [Header("Scraper Progress Variables")]
    private int totalScraperChunks = 0;
    private int remainingScraperChunks = 0;
    private bool isScraperActive = false;

    [Header("UI Animation References")]
    public SmoothUIAnimate pauseButtonAnim;
    public SmoothUIAnimate coinCounterAnim;
    public SmoothUIAnimate diamondCounterAnim;

    [Header("Chunk Hint / Idle Glow Settings")]
    [Tooltip("Kitne seconds player touch na kare to green glow shuru ho (Default: 2.5s)")]
    public float chunkIdleThreshold = 2.5f;
    private float chunkIdleTimer = 0f;
    private bool isChunksGlowing = false;

    private List<GameObject> stepGameObjects = new List<GameObject>();

    // Runtime Generated Layers
    private List<SpriteRenderer> layersList = new List<SpriteRenderer>();
    private List<ToolData> layerRequiredTools = new List<ToolData>();

    int currentLayer = 0;
    Texture2D texture;
    int totalOpaquePixels = 0;

    private int cachedChunkOrder = -1;
    private string cachedChunkLayer = "";

    float targetFill;
    private bool isLayerClearSoundPlayed = false;
    float currentFill;
    bool gameCompleted = false;
    bool textureNeedsApply = false;

    private GameObject baseCleanObjRef;
    public DynamicToolSorting toolSorter;
    float progressTimer = 0f;
    bool needsProgressCheck = false;

    Vector2 prevPos, currPos, upPos;
    bool positionsSaved = false;

    bool layerFinishedWaitingRelease = false;
    bool isTransitioningTool = false;
    float targetCameraSize = 5f;
    float effectGraceTimer = 0f;

    public GameObject scraperTriggerEdge;

    private bool isThresholdReached = false;

    void Start()
    {
        PlayerPrefs.SetInt("Coins", 100);
        PlayerPrefs.Save();

        if (gameplayCoinPanel != null) gameplayCoinPanel.SetActive(true);
        if (pauseButton != null) pauseButton.SetActive(true);
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

        if (LevelManager.SelectedObject != null)
        {
            objectData = LevelManager.SelectedObject;
        }

        if (objectData == null)
        {
            Debug.LogWarning("MaskEraser: SelectedObject was NULL!");
            return;
        }

        ClearOldGeneratedLayers();
        SetupGenericLevel();

        if (layersList.Count > 0)
        {
            PrepareLayer();
            SelectTool(layerRequiredTools[currentLayer], false);
            targetCameraSize = defaultCameraSize;
        }

        UpdateUpcomingIconsPanel(true);

        if (progressBarMainPanel != null)
        {
            progressBarMainPanel.SetActive(true);
        }
        percentText.text = "0%";
        progressFill.fillAmount = 0f;
        currentFill = 0f;
        targetFill = 0f;

        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);

        ToggleGameplayUI(false);
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
        if (levelParentAnchor == null) return;

        for (int i = levelParentAnchor.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(levelParentAnchor.GetChild(i).gameObject);
        }

        layersList.Clear();
        layerRequiredTools.Clear();

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
            levelParentAnchor.localPosition = objectData.levelPositionOffset;
            levelParentAnchor.rotation = Quaternion.identity;
        }

        if (objectData == null) return;

        GameObject cleanObj = new GameObject("Base_Clean_Object");
        cleanObj.transform.SetParent(levelParentAnchor, false);
        cleanObj.transform.localPosition = Vector3.zero;
        cleanObj.transform.localRotation = Quaternion.identity;
        cleanObj.transform.localScale = Vector3.one;

        SpriteRenderer baseCleanSR = cleanObj.AddComponent<SpriteRenderer>();

        if (objectData.cleanSprite != null)
        {
            baseCleanSR.sprite = objectData.cleanSprite;
            baseCleanSR.sortingOrder = 0; // BASE CLEAN OBJECT IS ALWAYS 0 (BACKGROUND)
            baseCleanSR.maskInteraction = SpriteMaskInteraction.None;
            baseCleanSR.material = new Material(Shader.Find("Sprites/Default"));
            baseCleanSR.enabled = true;
            cleanObj.SetActive(true);

            baseCleanObjRef = cleanObj;

            bool isOnlyOneStep = (objectData.cleaningSteps != null && objectData.cleaningSteps.Count <= 1);
            cleanObj.SetActive(isOnlyOneStep);
        }

        if (objectData.cleaningSteps != null && objectData.cleaningSteps.Count > 0)
        {
            int totalSteps = objectData.cleaningSteps.Count;
            stepGameObjects.Clear();

            for (int i = 0; i < totalSteps; i++)
            {
                CleaningStep step = objectData.cleaningSteps[i];
                if (step == null) continue;

                GameObject stepObj = new GameObject($"Step_{i}_{step.stepName}");
                stepObj.transform.SetParent(levelParentAnchor, false);
                stepObj.transform.localPosition = Vector3.zero;
                stepObj.transform.localRotation = Quaternion.identity;
                stepObj.transform.localScale = Vector3.one;

                stepGameObjects.Add(stepObj);

                // TOP-TO-BOTTOM AUTO-SORTING (Step 0 = Highest Order)
                int autoSortingOrder = (totalSteps - i) * 10;

                switch (step.stepType)
                {
                    case CleaningStepType.PixelEraser:
                        SpriteRenderer sr = stepObj.AddComponent<SpriteRenderer>();
                        sr.sprite = step.dirtySprite;
                        sr.sortingOrder = autoSortingOrder;
                        layersList.Add(sr);
                        break;

                    case CleaningStepType.ChunkScraper:
                        if (step.stepPrefab != null)
                        {
                            GameObject instantiatedChunks = Instantiate(step.stepPrefab, stepObj.transform);
                            instantiatedChunks.transform.localPosition = Vector3.zero;
                            instantiatedChunks.transform.localRotation = Quaternion.identity;
                            instantiatedChunks.transform.localScale = Vector3.one;

                            SetObjectSortingOrder(instantiatedChunks, autoSortingOrder);

                            MudChunk[] allChunks = instantiatedChunks.GetComponentsInChildren<MudChunk>(true);
                            totalScraperChunks = allChunks.Length;
                            remainingScraperChunks = totalScraperChunks;
                        }
                        layersList.Add(null);
                        break;

                    case CleaningStepType.GlueApply:
                        if (step.stepPrefab != null)
                        {
                            GameObject instantiatedGlue = Instantiate(step.stepPrefab, stepObj.transform);
                            instantiatedGlue.transform.localPosition = Vector3.zero;
                            instantiatedGlue.transform.localRotation = Quaternion.identity;
                            instantiatedGlue.transform.localScale = Vector3.one;

                            SetObjectSortingOrder(instantiatedGlue, autoSortingOrder);
                        }
                        layersList.Add(null);
                        break;
                }

                CleaningLayer cleaningLayerComponent = stepObj.AddComponent<CleaningLayer>();
                cleaningLayerComponent.requiredTool = step.requiredTool;
                layerRequiredTools.Add(step.requiredTool);

                stepObj.SetActive(i == 0 || i == 1);
            }
        }
    }

    void Update()
    {
        // IDLE SCRAPER CHUNKS GLOW LOGIC
        bool isScraperStep = false;
        if (objectData != null && objectData.cleaningSteps != null && currentLayer < objectData.cleaningSteps.Count)
        {
            CleaningStep currentStep = objectData.cleaningSteps[currentLayer];
            if (currentStep != null && currentStep.stepType == CleaningStepType.ChunkScraper)
            {
                isScraperStep = true;
            }
        }

        // --- TOP-TO-BOTTOM DYNAMIC TOOL & LAYER SORTING LOGIC ---
        if (toolSpriteRenderer != null)
        {
            int totalSteps = (objectData != null && objectData.cleaningSteps != null) ? objectData.cleaningSteps.Count : 1;
            int activeStepOrder = (totalSteps - currentLayer) * 10;

            GameObject currentStepObj = (stepGameObjects != null && currentLayer < stepGameObjects.Count) ? stepGameObjects[currentLayer] : null;
            SpriteRenderer stepRenderer = (currentStepObj != null) ? currentStepObj.GetComponentInChildren<SpriteRenderer>() : null;
            string targetLayer = (stepRenderer != null) ? stepRenderer.sortingLayerName : toolSpriteRenderer.sortingLayerName;

            // Tool hamesha active step layer ke UPAR (+50) rahega
            ApplyToolSorting(targetLayer, activeStepOrder + 50);

            if (currentStepObj != null)
            {
                SetObjectSortingOrder(currentStepObj, activeStepOrder);
            }
        }

        bool isHalfOrMoreRemoved = (totalScraperChunks > 0) && (remainingScraperChunks <= totalScraperChunks * 0.5f);

        if (isScraperStep && remainingScraperChunks > 0 && isHalfOrMoreRemoved && !gameCompleted && !isTransitioningTool)
        {
            if (Input.GetMouseButton(0))
            {
                chunkIdleTimer = 0f;
                if (isChunksGlowing)
                {
                    SetRemainingChunksGlow(false);
                }
            }
            else
            {
                chunkIdleTimer += Time.deltaTime;
                if (chunkIdleTimer >= chunkIdleThreshold && !isChunksGlowing)
                {
                    SetRemainingChunksGlow(true);
                }
            }
        }
        else if (isChunksGlowing)
        {
            SetRemainingChunksGlow(false);
        }

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

        // UI HIDE LOGIC
        if (!gameCompleted && !isTransitioningTool)
        {
            bool isTouching = Input.GetMouseButton(0) || Input.touchCount > 0;

            if (isTouching)
            {
                idleTimer = 0f;
                touchTimer += Time.deltaTime;

                if (touchTimer >= holdToHideDelay && !isUIHiddenByTimer)
                {
                    ToggleGameplayUI(true);
                    isUIHiddenByTimer = true;
                }
            }
            else
            {
                touchTimer = 0f;
                idleTimer += Time.deltaTime;

                if (idleTimer >= idleToShowDelay && isUIHiddenByTimer)
                {
                    ToggleGameplayUI(false);
                    isUIHiddenByTimer = false;
                }
            }
        }

        // CAMERA ZOOM LOGIC
        if (Camera.main != null)
        {
            if (Camera.main.orthographic)
                Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, targetCameraSize, Time.deltaTime * cameraTransitionIntensity);
            else
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetCameraSize, Time.deltaTime * cameraTransitionIntensity);
        }

        // CAMERA MOVEMENT LOGIC
        if (Camera.main != null && Screen.width > 0 && Screen.height > 0)
        {
            float initialCamZ = Camera.main.transform.position.z;
            Vector3 targetCameraPos = new Vector3(0f, 0f, initialCamZ);

            bool canMoveCamera = !gameCompleted &&
                                 !isTransitioningTool &&
                                 !layerFinishedWaitingRelease &&
                                 layersList.Count > 0 &&
                                 Input.GetMouseButton(0);

            CleaningStep currentStep = (objectData != null && objectData.cleaningSteps != null && currentLayer < objectData.cleaningSteps.Count)
                ? objectData.cleaningSteps[currentLayer]
                : null;

            bool isCameraMovementActive = (currentStep != null) && currentStep.allowCameraMovement;

            if (canMoveCamera && isCameraMovementActive)
            {
                float mouseXOffset = ((Input.mousePosition.x / Screen.width) - 0.5f) * 2f;
                float mouseYOffset = ((Input.mousePosition.y / Screen.height) - 0.5f) * 2f;

                float targetX = mouseXOffset * cameraMoveIntensity;

                float targetY = 0f;
                if (objectData != null && objectData.enableYAxisMovement)
                {
                    targetY = mouseYOffset * cameraMoveIntensity * 2.5f;
                }

                targetCameraPos = new Vector3(targetX, targetY, initialCamZ);
            }

            if (!float.IsInfinity(targetCameraPos.x) && !float.IsNaN(targetCameraPos.x))
            {
                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetCameraPos, Time.deltaTime * 1f);
            }
        }

        if (gameCompleted || isTransitioningTool || layersList.Count == 0) return;

        if (isThresholdReached)
        {
            if (!Input.GetMouseButton(0))
            {
                isThresholdReached = false;
                effectGraceTimer = 0f;
                StopToolEffects();

                ClearRemainingLayer();

                if (variantMainPanel != null)
                {
                    variantMainPanel.SetActive(false);
                }

                StartCoroutine(TransitionToNextLayerRoutine());
                return;
            }
        }

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

        // --- TOUCH DOWN ---
        if (Input.GetMouseButtonDown(0))
        {
            hasLastErasePos = false;
        }

        if (Input.GetMouseButton(0) && currentToolData != null && toolFollower.CanClean && currentToolData.canRemove)
        {
            Vector3 world;
            if (eraseAnchor != null)
            {
                world = eraseAnchor.position;
            }
            else
            {
                float cameraDistance = Mathf.Abs(Camera.main.transform.position.z);
                world = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));
            }

            world.z = 0;
            bool isOverLayer = false;

            if (!hasLastErasePos)
            {
                isOverLayer = EraseAtWorldPosition(world);
                lastEraseWorldPos = world;
                hasLastErasePos = true;
            }
            else
            {
                float distance = Vector3.Distance(lastEraseWorldPos, world);
                float stepSize = 0.12f;

                if (distance > stepSize)
                {
                    int steps = Mathf.Min(Mathf.CeilToInt(distance / stepSize), 10);
                    for (int i = 1; i <= steps; i++)
                    {
                        Vector3 interpolatedPos = Vector3.Lerp(lastEraseWorldPos, world, (float)i / steps);
                        if (EraseAtWorldPosition(interpolatedPos))
                        {
                            isOverLayer = true;
                        }
                    }
                }
                else
                {
                    isOverLayer = EraseAtWorldPosition(world);
                }

                lastEraseWorldPos = world;
            }

            bool shouldPlay = currentToolData.soundOnlyOnHit ? isOverLayer : true;
            if (shouldPlay) effectGraceTimer = 0.15f;
        }

        // --- TOUCH UP ---
        if (Input.GetMouseButtonUp(0))
        {
            hasLastErasePos = false;
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

        if (objectData != null && objectData.scraperChunksPrefab != null && currentLayer > 0)
        {
            if (layersList != null && layersList.Count > 0 && layersList[0] != null)
            {
                if (layersList[0].gameObject.activeSelf)
                {
                    layersList[0].gameObject.SetActive(false);
                }
            }

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

    private void SetObjectSortingOrder(GameObject obj, int order)
    {
        if (obj == null) return;

        UnityEngine.Rendering.SortingGroup group = obj.GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (group != null)
        {
            group.sortingOrder = order;
            return;
        }

        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            sr.sortingOrder = order;
        }
    }

    [Header("Slide UI Animations")]
    private Coroutine topUISlideCoroutine;
    private Vector2 pauseBasePos;
    private Vector2 coinBasePos;
    private Vector3 pauseBaseScale = Vector3.one;
    private Vector3 coinBaseScale = Vector3.one;
    private bool isBasePosSaved = false;

    public void ToggleGameplayUI(bool hide)
    {
        if (currentToolData != null && currentToolData.hasVariants && currentToolData.toolVariants.Count > 0 && variantMainPanel != null)
        {
            if (panelAnimCoroutine != null) StopCoroutine(panelAnimCoroutine);

            bool shouldShowVariant = !hide && !layerFinishedWaitingRelease && !isTransitioningTool;
            if (shouldShowVariant)
            {
                variantMainPanel.SetActive(true);
            }
            panelAnimCoroutine = StartCoroutine(AnimateVariantPanelVideoStyle(shouldShowVariant));
        }

        if (topUISlideCoroutine != null) StopCoroutine(topUISlideCoroutine);
        topUISlideCoroutine = StartCoroutine(SlideSideUIRoutine(hide));
    }

    private IEnumerator SlideSideUIRoutine(bool hide)
    {
        RectTransform pauseRect = pauseButton != null ? pauseButton.GetComponent<RectTransform>() : null;
        RectTransform coinRect = gameplayCoinPanel != null ? gameplayCoinPanel.GetComponent<RectTransform>() : null;
        RectTransform diamondRect = gameplayDiamondPanel != null ? gameplayDiamondPanel.GetComponent<RectTransform>() : null;

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
            if (diamondRect != null)
            {
                diamondBasePos = diamondRect.anchoredPosition;
                diamondBaseScale = diamondRect.localScale;
            }
            isBasePosSaved = true;
        }

        float duration = 0.5f;
        float time = 0f;

        Vector2 pauseStartPos = pauseRect != null ? pauseRect.anchoredPosition : Vector2.zero;
        Vector2 coinStartPos = coinRect != null ? coinRect.anchoredPosition : Vector2.zero;
        Vector2 diamondStartPos = diamondRect != null ? diamondRect.anchoredPosition : Vector2.zero;

        Vector3 pauseStartScale = pauseRect != null ? pauseRect.localScale : Vector3.one;
        Vector3 coinStartScale = coinRect != null ? coinRect.localScale : Vector3.one;
        Vector3 diamondStartScale = diamondRect != null ? diamondRect.localScale : Vector3.one;

        Vector2 pauseTargetPos = hide ? new Vector2(pauseBasePos.x + 350f, pauseBasePos.y) : pauseBasePos;
        Vector2 coinTargetPos = hide ? new Vector2(coinBasePos.x - 350f, coinBasePos.y) : coinBasePos;
        Vector2 diamondTargetPos = hide ? new Vector2(diamondBasePos.x - 350f, diamondBasePos.y) : diamondBasePos;

        Vector3 pauseTargetScale = hide ? Vector3.zero : pauseBaseScale;
        Vector3 coinTargetScale = hide ? Vector3.zero : coinBaseScale;
        Vector3 diamondTargetScale = hide ? Vector3.zero : diamondBaseScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t);

            if (pauseRect != null) pauseRect.anchoredPosition = Vector2.Lerp(pauseStartPos, pauseTargetPos, smoothT);
            if (coinRect != null) coinRect.anchoredPosition = Vector2.Lerp(coinStartPos, coinTargetPos, smoothT);
            if (diamondRect != null) diamondRect.anchoredPosition = Vector2.Lerp(diamondStartPos, diamondTargetPos, smoothT);

            if (pauseRect != null) pauseRect.localScale = Vector3.Lerp(pauseStartScale, pauseTargetScale, smoothT);
            if (coinRect != null) coinRect.localScale = Vector3.Lerp(coinStartScale, coinTargetScale, smoothT);
            if (diamondRect != null) diamondRect.localScale = Vector3.Lerp(diamondStartScale, diamondTargetScale, smoothT);

            yield return null;
        }

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
        if (diamondRect != null)
        {
            diamondRect.anchoredPosition = diamondTargetPos;
            diamondRect.localScale = diamondTargetScale;
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

        // Current step ke tamaam renderers ka mask interaction normal (None) karein
        if (currentLayer < stepGameObjects.Count && stepGameObjects[currentLayer] != null)
        {
            SpriteRenderer[] currentRenderers = stepGameObjects[currentLayer].GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sr in currentRenderers)
            {
                sr.maskInteraction = SpriteMaskInteraction.None;
            }
        }

        if (layersList[currentLayer] == null) return;

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

        ApplyLayerMasking(originalSprite);
    }
    

    private void ApplyToolSorting(string layerName, int order)
    {
        if (toolSpriteRenderer == null) return;

        UnityEngine.Rendering.SortingGroup toolGroup = toolSpriteRenderer.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
        if (toolGroup != null)
        {
            toolGroup.sortingLayerName = layerName;
            toolGroup.sortingOrder = order;
        }
        else
        {
            SpriteRenderer[] toolRenderers = toolSpriteRenderer.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sr in toolRenderers)
            {
                sr.sortingLayerName = layerName;
                sr.sortingOrder = order;
            }
        }
    }

    public bool EraseAtWorldPosition(Vector3 world)
    {
        if (currentToolData != null && (currentToolData.toolType == ToolType.Scraper || currentToolData.name.Contains("Scraper")))
        {
            return true;
        }

        if (currentLayer >= layersList.Count || layersList[currentLayer] == null || texture == null) return false;

        SpriteRenderer sr = layersList[currentLayer];
        Vector3 local = sr.transform.InverseTransformPoint(world);

        float width = sr.sprite.bounds.size.x;
        float height = sr.sprite.bounds.size.y;
        float xp = (local.x + width / 2) / width;
        float yp = (local.y + height / 2) / height;
        int x = Mathf.RoundToInt(xp * texture.width);
        int y = Mathf.RoundToInt(yp * texture.height);

        bool actualCleaningDone = false;

        if (currentToolData != null && currentToolData.brushShape != null)
        {
            Texture2D customBrush = currentToolData.brushShape;

            int brushWidth = Mathf.Max(1, Mathf.RoundToInt(customBrush.width * currentToolData.brushWidthScale));
            int brushHeight = Mathf.Max(1, Mathf.RoundToInt(customBrush.height * currentToolData.brushHeightScale));

            int startX = x - (brushWidth / 2);
            int startY = y - (brushHeight / 2);

            int clampedStartX = Mathf.Clamp(startX, 0, texture.width);
            int clampedStartY = Mathf.Clamp(startY, 0, texture.height);
            int clampedEndX = Mathf.Clamp(startX + brushWidth, 0, texture.width);
            int clampedEndY = Mathf.Clamp(startY + brushHeight, 0, texture.height);

            int blockW = clampedEndX - clampedStartX;
            int blockH = clampedEndY - clampedStartY;

            if (blockW <= 0 || blockH <= 0) return false;

            Color[] targetPixels = texture.GetPixels(clampedStartX, clampedStartY, blockW, blockH);
            Color[] brushPixels = customBrush.GetPixels();
            int origBrushW = customBrush.width;
            int origBrushH = customBrush.height;

            float intensity = eraserIntensityMultiplier;

            for (int bh = 0; bh < blockH; bh++)
            {
                int py = clampedStartY + bh;
                int brushYIndex = py - startY;
                float sampleYNorm = (float)brushYIndex / brushHeight;
                int sampleY = Mathf.Clamp(Mathf.FloorToInt(sampleYNorm * origBrushH), 0, origBrushH - 1);

                for (int bw = 0; bw < blockW; bw++)
                {
                    int px = clampedStartX + bw;
                    int brushXIndex = px - startX;
                    float sampleXNorm = (float)brushXIndex / brushWidth;
                    int sampleX = Mathf.Clamp(Mathf.FloorToInt(sampleXNorm * origBrushW), 0, origBrushW - 1);

                    Color brushPixel = brushPixels[sampleY * origBrushW + sampleX];

                    if (brushPixel.a > 0.05f)
                    {
                        int targetIdx = bh * blockW + bw;
                        Color c = targetPixels[targetIdx];

                        if (c.a <= 0.05f) continue;

                        c.a -= brushPixel.a * intensity;
                        if (c.a < 0f) c.a = 0f;

                        targetPixels[targetIdx] = c;
                        actualCleaningDone = true;
                    }
                }
            }

            if (actualCleaningDone)
            {
                texture.SetPixels(clampedStartX, clampedStartY, blockW, blockH, targetPixels);
                textureNeedsApply = true;
            }
        }
        else
        {
            int size = currentToolData.brushSize;
            float localHardness = 0.1f;

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
                            alphaReduction = Mathf.Pow(alphaReduction, localHardness);
                            c.a -= alphaReduction * eraserIntensityMultiplier;
                            if (c.a < 0f) c.a = 0f;

                            texture.SetPixel(px, py, c);
                            textureNeedsApply = true;
                            actualCleaningDone = true;
                        }
                    }
                }
            }
        }

        return actualCleaningDone;
    }

    void UpdateProgress()
    {
        if (gameCompleted || layerFinishedWaitingRelease || objectData == null || objectData.cleaningSteps == null || objectData.cleaningSteps.Count == 0) return;
        if (currentLayer >= objectData.cleaningSteps.Count) return;

        CleaningStep currentStep = objectData.cleaningSteps[currentLayer];

        float percent = 0f;
        bool isLayerFullyCleaned = false;

        switch (currentStep.stepType)
        {
            case CleaningStepType.ChunkScraper:
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

                if (remainingScraperChunks > 0 && percent >= 99f)
                {
                    percent = 99f;
                }

                isLayerFullyCleaned = (remainingScraperChunks <= 0);
                break;

            case CleaningStepType.PixelEraser:
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

                    isLayerFullyCleaned = (percent >= cleaningThreshold);
                }
                break;

            case CleaningStepType.GlueApply:
                break;
        }

        float visualPercent = percent;
        if (visualPercent > 100f) visualPercent = 100f;

        targetFill = visualPercent / 100f;
        progressFill.fillAmount = targetFill;
        percentText.text = Mathf.RoundToInt(visualPercent) + "%";

        if (isLayerFullyCleaned)
        {
            Debug.Log("LAYER TARGET ACHIEVED!");
            isThresholdReached = true;
        }
    }

    void ApplyLayerMasking(Sprite originalSprite)
    {
        if (currentLayer >= stepGameObjects.Count || stepGameObjects[currentLayer] == null) return;

        // Current Step par SpriteMask add/enable karein
        GameObject currentStepObj = stepGameObjects[currentLayer];
        SpriteMask mask = currentStepObj.GetComponent<SpriteMask>();
        if (mask == null)
        {
            mask = currentStepObj.AddComponent<SpriteMask>();
        }

        mask.enabled = true;
        mask.sprite = originalSprite;

        // Agli Layer (Step) ke SARE child renderers par masking lagayein
        int nextIndex = currentLayer + 1;
        if (nextIndex < stepGameObjects.Count && stepGameObjects[nextIndex] != null)
        {
            SpriteRenderer[] nextRenderers = stepGameObjects[nextIndex].GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sr in nextRenderers)
            {
                sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
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

        UpdateProgress();
    }

    void ClearRemainingLayer()
    {
        bool isChunkStep = false;
        if (objectData != null && objectData.cleaningSteps != null && currentLayer < objectData.cleaningSteps.Count)
        {
            CleaningStep currentStep = objectData.cleaningSteps[currentLayer];
            if (currentStep != null && currentStep.stepType == CleaningStepType.ChunkScraper)
            {
                isChunkStep = true;
            }
        }

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

        targetFill = 1f;
        currentFill = 1f;
        if (progressFill != null) progressFill.fillAmount = 1f;
        if (percentText != null) percentText.text = "100%";
    }

    IEnumerator TransitionToNextLayerRoutine()
    {
        isTransitioningTool = true;
        layerFinishedWaitingRelease = false;

        ToggleGameplayUI(true);

        if (isUIHiddenByTimer)
        {
            isUIHiddenByTimer = false;
        }

        touchTimer = 0f;
        idleTimer = 0f;

        if (toolFollower != null) toolFollower.enabled = false;

        currentFill = 1f;
        targetFill = 1f;
        if (progressFill != null) progressFill.fillAmount = 1f;
        if (percentText != null) percentText.text = "100%";

        if (!isLayerClearSoundPlayed)
        {
            isLayerClearSoundPlayed = true;
            if (AudioManager.Instance != null && AudioManager.Instance.layerClearSFX != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.layerClearSFX);
            }
        }

        yield return new WaitForSeconds(0.2f);

        if (progressBarMainPanel != null) progressBarMainPanel.SetActive(true);
        if (percentText != null) percentText.gameObject.SetActive(false);
        if (progressFill != null) progressFill.gameObject.SetActive(false);
        if (progressFillBg != null) progressFillBg.SetActive(false);

        targetCameraSize = defaultCameraSize;

        yield return new WaitForSeconds(0.07f);

        Vector3 startPos = toolFollower.transform.position;
        Vector3 leftTarget = startPos + Vector3.left * 15f;

        SpriteRenderer currentLayerSR = (stepGameObjects != null && currentLayer < stepGameObjects.Count && stepGameObjects[currentLayer] != null)
            ? stepGameObjects[currentLayer].GetComponentInChildren<SpriteRenderer>()
            : null;

        Color originalColor = currentLayerSR != null ? currentLayerSR.color : Color.white;

        float time = 0;
        float durationOut = 0.6f;
        float patchFadeDelay = 0.3f;

        while (time < durationOut)
        {
            time += Time.deltaTime;
            float t = time / durationOut;

            if (toolFollower != null)
                toolFollower.transform.position = Vector3.Lerp(startPos, leftTarget, t * t);

            if (currentLayerSR != null)
            {
                Color c = originalColor;
                float fadeT = Mathf.Clamp01((time - patchFadeDelay) / (durationOut - patchFadeDelay));
                c.a = Mathf.Lerp(originalColor.a, 0f, fadeT);
                currentLayerSR.color = c;
            }

            yield return null;
        }

        if (stepGameObjects != null && currentLayer < stepGameObjects.Count && stepGameObjects[currentLayer] != null)
        {
            stepGameObjects[currentLayer].SetActive(false);
            if (currentLayerSR != null) currentLayerSR.color = originalColor;
        }

        if (currentLayer < layersList.Count && layersList[currentLayer] != null)
        {
            SpriteMask oldMask = layersList[currentLayer].GetComponent<SpriteMask>();
            if (oldMask != null)
            {
                oldMask.enabled = false;
            }
        }

        currentLayer++;

        if (currentLayer >= objectData.cleaningSteps.Count)
        {
            CompleteGame();
            isTransitioningTool = false;
            yield break;
        }

        if (stepGameObjects != null)
        {
            for (int i = 0; i < stepGameObjects.Count; i++)
            {
                if (stepGameObjects[i] != null)
                {
                    bool shouldBeActive = (i == currentLayer || i == currentLayer + 1);
                    stepGameObjects[i].SetActive(shouldBeActive);
                }
            }
        }

        if (baseCleanObjRef != null)
        {
            bool isLastStep = (currentLayer == objectData.cleaningSteps.Count - 1);
            baseCleanObjRef.SetActive(isLastStep);
        }

        PrepareLayer();

        ToolData nextTool = null;
        if (layerRequiredTools != null && currentLayer < layerRequiredTools.Count)
        {
            nextTool = layerRequiredTools[currentLayer];
        }

        SelectTool(nextTool, true);

        if (objectData != null && objectData.cleaningSteps != null && currentLayer < objectData.cleaningSteps.Count)
        {
            CleaningStep currentStep = objectData.cleaningSteps[currentLayer];

            if (currentStep != null && currentStep.cameraZoomSize > 0.1f)
            {
                targetCameraSize = currentStep.cameraZoomSize;
            }
            else if (objectData.customCameraZoomSize > 0.1f)
            {
                targetCameraSize = objectData.customCameraZoomSize;
            }
            else
            {
                targetCameraSize = defaultCameraSize;
            }
        }

        float camZ = Mathf.Abs(Camera.main.transform.position.z);

        isLayerClearSoundPlayed = false;

        currentFill = 0f;
        targetFill = 0f;
        if (progressFill != null) progressFill.fillAmount = 0f;
        if (percentText != null) percentText.text = "0%";

        yield return new WaitForSeconds(0.2f);

        time = 0;
        float durationIn = 0.6f;

        while (time < durationIn)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / durationIn);

            Vector3 currentRestTarget = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.3f, camZ));
            currentRestTarget.z = 0f;
            Vector3 currentRightStart = currentRestTarget + Vector3.right * 15f;

            if (toolFollower != null)
                toolFollower.transform.position = Vector3.Lerp(currentRightStart, currentRestTarget, t);

            yield return null;
        }

        Vector3 finalRestTarget = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.3f, camZ));
        finalRestTarget.z = 0f;
        if (toolFollower != null)
            toolFollower.transform.position = finalRestTarget;

        UpdateUpcomingIconsPanel(true);
        if (toolFollower != null) toolFollower.enabled = true;

        isTransitioningTool = false;

        ToggleGameplayUI(false);

        if (percentText != null) percentText.gameObject.SetActive(true);
        if (progressFill != null) progressFill.gameObject.SetActive(true);
        if (progressFillBg != null) progressFillBg.SetActive(true);

        if (toolFollower != null)
        {
            toolFollower.AnimateCapOff();
        }
    }

    void CompleteGame()
    {
        Debug.Log("CompleteGame() START HUA HAI!");
        gameCompleted = true;
        percentText.text = "100%";
        progressFill.fillAmount = 1f;

        if (objectData != null && objectData.levelCompleteZoomSize > 0.1f)
        {
            targetCameraSize = objectData.levelCompleteZoomSize;
        }
        else
        {
            targetCameraSize = defaultCameraSize;
        }

        if (celebrationPrefab != null && activeCelebrationInstance == null)
        {
            activeCelebrationInstance = Instantiate(celebrationPrefab, Vector3.zero, Quaternion.identity);
        }
        if (toolFollower != null) toolFollower.gameObject.SetActive(false);
        if (previousToolUIImage != null) previousToolUIImage.gameObject.SetActive(false);
        if (currentToolUIImage != null) currentToolUIImage.gameObject.SetActive(false);
        if (upcomingToolUIImage != null) upcomingToolUIImage.gameObject.SetActive(false);
        UpdateUpcomingIconsPanel(false);

        if (variantMainPanel != null) variantMainPanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(false);

        if (gameplayCoinPanel != null)
        {
            gameplayCoinPanel.SetActive(true);
            gameplayCoinPanel.transform.SetAsLastSibling();

            if (topUISlideCoroutine != null) StopCoroutine(topUISlideCoroutine);
            topUISlideCoroutine = StartCoroutine(SlideSideUIRoutine(false));
        }

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

        if (progressBarMainPanel != null) progressBarMainPanel.SetActive(false);
    }

    void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private Coroutine slideCoroutine;

    private RectTransform GetTargetRect(Image img)
    {
        if (img == null) return null;

        RectTransform parentRT = img.transform.parent as RectTransform;
        if (parentRT != null && parentRT.GetComponent<Canvas>() == null && parentRT.name.ToLower().Contains("slot"))
        {
            return parentRT;
        }
        return img.rectTransform;
    }

    void UpdateToolUI(bool animate = false)
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        RectTransform currTarget = GetTargetRect(currentToolUIImage);
        RectTransform prevTarget = GetTargetRect(previousToolUIImage);
        RectTransform upTarget = GetTargetRect(upcomingToolUIImage);

        if (!positionsSaved && currTarget != null)
        {
            currPos = currTarget.anchoredPosition;
            prevPos = prevTarget != null ? prevTarget.anchoredPosition : currPos - new Vector2(toolSpacing, 0);
            upPos = upTarget != null ? upTarget.anchoredPosition : currPos + new Vector2(toolSpacing, 0);
            positionsSaved = true;
        }

        if (animate)
        {
            slideCoroutine = StartCoroutine(SlideToolUI());
        }
        else
        {
            SnapToolUI();
        }
    }

    IEnumerator SlideToolUI()
    {
        float duration = 0.38f;
        float time = 0;

        RectTransform prevTarget = GetTargetRect(previousToolUIImage);
        RectTransform currTarget = GetTargetRect(currentToolUIImage);
        RectTransform upTarget = GetTargetRect(upcomingToolUIImage);

        if (currTarget == null)
        {
            SnapToolUI();
            yield break;
        }

        Vector2 offscreenLeft = prevPos - (upPos - currPos);
        Vector3 smallScale = new Vector3(inactiveToolScale, inactiveToolScale, 1f);
        Vector3 largeScale = new Vector3(activeToolScale, activeToolScale, 1f);

        if (currTarget != null)
        {
            currTarget.anchoredPosition = currPos;
            currTarget.localScale = largeScale;
            currTarget.gameObject.SetActive(true);
        }

        if (upTarget != null && upcomingToolUIImage != null && upcomingToolUIImage.gameObject.activeSelf)
        {
            upTarget.anchoredPosition = upPos;
            upTarget.localScale = smallScale;
            upTarget.gameObject.SetActive(true);
        }

        if (prevTarget != null && previousToolUIImage != null && previousToolUIImage.gameObject.activeSelf)
        {
            prevTarget.anchoredPosition = prevPos;
            prevTarget.localScale = smallScale;
            prevTarget.gameObject.SetActive(true);
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t);

            if (currTarget != null)
            {
                currTarget.anchoredPosition = Vector2.Lerp(currPos, prevPos, smoothT);
                currTarget.localScale = Vector3.Lerp(largeScale, smallScale, smoothT);
            }

            if (upTarget != null && upTarget.gameObject.activeSelf)
            {
                upTarget.anchoredPosition = Vector2.Lerp(upPos, currPos, smoothT);
                upTarget.localScale = Vector3.Lerp(smallScale, largeScale, smoothT);
            }

            if (prevTarget != null && prevTarget.gameObject.activeSelf)
            {
                prevTarget.anchoredPosition = Vector2.Lerp(prevPos, offscreenLeft, smoothT);
                prevTarget.localScale = smallScale;
            }

            yield return null;
        }

        SnapToolUI();
    }

    void SnapToolUI()
    {
        RectTransform prevTarget = GetTargetRect(previousToolUIImage);
        RectTransform currTarget = GetTargetRect(currentToolUIImage);
        RectTransform upTarget = GetTargetRect(upcomingToolUIImage);

        bool hasPrevTool = (currentLayer > 0 && currentLayer - 1 < layerRequiredTools.Count);
        if (previousToolUIImage != null)
        {
            if (hasPrevTool)
            {
                ToolData prevTool = layerRequiredTools[currentLayer - 1];
                if (prevTool != null && prevTool.panelIcon != null)
                {
                    previousToolUIImage.sprite = prevTool.panelIcon;
                    previousToolUIImage.gameObject.SetActive(true);
                    if (prevTarget != null) prevTarget.gameObject.SetActive(true);
                }
                else
                {
                    previousToolUIImage.gameObject.SetActive(false);
                    if (prevTarget != null) prevTarget.gameObject.SetActive(false);
                }
            }
            else
            {
                previousToolUIImage.gameObject.SetActive(false);
                if (prevTarget != null) prevTarget.gameObject.SetActive(false);
            }
        }

        if (currentToolUIImage != null)
        {
            if (currentToolData != null && currentToolData.panelIcon != null)
            {
                currentToolUIImage.sprite = currentToolData.panelIcon;
                currentToolUIImage.gameObject.SetActive(true);
                if (currTarget != null) currTarget.gameObject.SetActive(true);
            }
            else
            {
                currentToolUIImage.gameObject.SetActive(false);
                if (currTarget != null) currTarget.gameObject.SetActive(false);
            }
        }

        int nextLayerIndex = currentLayer + 1;
        bool hasUpTool = (nextLayerIndex < layerRequiredTools.Count);
        if (upcomingToolUIImage != null)
        {
            if (hasUpTool)
            {
                ToolData nextTool = layerRequiredTools[nextLayerIndex];
                if (nextTool != null && nextTool.panelIcon != null)
                {
                    upcomingToolUIImage.sprite = nextTool.panelIcon;
                    upcomingToolUIImage.gameObject.SetActive(true);
                    if (upTarget != null) upTarget.gameObject.SetActive(true);
                }
                else
                {
                    upcomingToolUIImage.gameObject.SetActive(false);
                    if (upTarget != null) upTarget.gameObject.SetActive(false);
                }
            }
            else
            {
                upcomingToolUIImage.gameObject.SetActive(false);
                if (upTarget != null) upTarget.gameObject.SetActive(false);
            }
        }

        if (prevTarget != null)
        {
            prevTarget.anchoredPosition = prevPos;
            prevTarget.localScale = new Vector3(inactiveToolScale, inactiveToolScale, 1f);
        }

        if (currTarget != null)
        {
            currTarget.anchoredPosition = currPos;
            currTarget.localScale = new Vector3(activeToolScale, activeToolScale, 1f);
        }

        if (upTarget != null)
        {
            upTarget.anchoredPosition = upPos;
            upTarget.localScale = new Vector3(inactiveToolScale, inactiveToolScale, 1f);
        }
    }

    IEnumerator AnimateUIPopup(Image img, float delay)
    {
        if (img == null || !img.gameObject.activeSelf) yield break;
        img.transform.localScale = Vector3.zero;
        yield return new WaitForSeconds(delay);
        float time = 0;
        float duration = 0.3f;
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
                upcomingIcons[i].sprite = layersList[layerIndex].sprite;
                if (animate)
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

        if (currentToolData != null && currentToolData.isPolishTool)
        {
            StartCoroutine(PlayPolishPickupAnimation());
        }
    }

    private IEnumerator PlayPolishPickupAnimation()
    {
        if (PolishSequenceController.Instance != null)
        {
            yield return StartCoroutine(PolishSequenceController.Instance.PlayPolishSequenceRoutine(toolFollower, currentToolData));
        }
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
        yield return new WaitForSeconds(levelCompleteDelay);

        if (gameplayCoinPanel != null)
        {
            gameplayCoinPanel.SetActive(true);
            gameplayCoinPanel.transform.SetAsLastSibling();

            if (topUISlideCoroutine != null) StopCoroutine(topUISlideCoroutine);
            topUISlideCoroutine = StartCoroutine(SlideSideUIRoutine(false));
        }

        yield return new WaitForSeconds(0.4f);

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.TriggerCoinSwoopAnimation(20);
        }

        yield return new WaitForSeconds(1.2f);

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        if (GameStepController.Instance != null)
        {
            GameStepController.Instance.OnStepFinishedFromMinigame();
        }
    }

    Transform GetToolTransformToAnimate()
    {
        if (toolFollower == null) return null;
        return toolFollower.transform.childCount > 0 ?
            toolFollower.transform.GetChild(0) : toolFollower.transform;
    }

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
        currentToolData = tool;

        if (variantButtonsContainer != null)
        {
            foreach (Transform child in variantButtonsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        spawnedVariantButtons.Clear();

        if (tool == null || !tool.hasVariants || tool.toolVariants == null || tool.toolVariants.Count == 0)
        {
            if (variantMainPanel != null) variantMainPanel.SetActive(false);
            return;
        }

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

        if (tool.toolVariants.Count > 0)
        {
            ToolVariant baseVariant = tool.toolVariants[0];

            PlayerPrefs.SetString(tool.name + "_Equipped", baseVariant.variantName);
            PlayerPrefs.Save();

            ApplyVariantSkin(tool, baseVariant, false);
        }

        if (variantMainPanel != null)
        {
            variantMainPanel.transform.localScale = new Vector3(0.5f, 0f, 1f);
            variantMainPanel.SetActive(true);
        }
    }

    private IEnumerator AnimateVariantPanelVideoStyle(bool show)
    {
        if (variantMainPanel == null) yield break;

        if (show)
        {
            variantMainPanel.SetActive(true);
        }

        Vector3 startScale = variantMainPanel.transform.localScale;
        Vector3 targetScale = show ? Vector3.one : new Vector3(0.5f, 0f, 1f);

        float time = 0f;
        float duration = 0.6f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t);
            variantMainPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            yield return null;
        }

        variantMainPanel.transform.localScale = targetScale;

        if (!show)
        {
            variantMainPanel.SetActive(false);
        }
    }

    public void ApplyVariantSkin(ToolData tool, ToolVariant variant, bool animate = false)
    {
        if (toolFollower == null || variant == null) return;

        if (animate && variant == currentEquippedVariant) return;

        currentEquippedVariant = variant;

        if (tool != null && variant.brushSize > 0)
        {
            tool.brushSize = (int)variant.brushSize;
        }

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

        foreach (ToolVariantButton btn in spawnedVariantButtons)
        {
            if (btn != null) btn.UpdateUI();
        }
    }

    private IEnumerator AnimateVariantSkinRoutine(ToolVariant variant)
    {
        if (toolFollower == null) yield break;

        toolFollower.enabled = false;
        Vector3 startPos = toolFollower.transform.position;
        Vector3 outTarget = startPos + Vector3.left * 15f;
        float time = 0;
        float durationOut = 0.3f;

        while (time < durationOut)
        {
            time += Time.deltaTime;
            float t = time / durationOut;
            toolFollower.transform.position = Vector3.Lerp(startPos, outTarget, t * t);
            yield return null;
        }

        SpriteRenderer toolSR = toolFollower.GetComponentInChildren<SpriteRenderer>();
        if (toolSR != null && variant.toolSprite != null)
        {
            toolSR.sprite = variant.toolSprite;
        }

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
            toolFollower.transform.rotation = Quaternion.identity;

            Vector3 restTarget = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.3f, 10f));
            restTarget.z = 0f;

            Vector3 startPos = restTarget + Vector3.right * 15f;
            toolFollower.transform.position = startPos;

            if (currentToolData != null)
            {
                SetupToolVariantsPanel(currentToolData);

                if (objectData != null && objectData.cleaningSteps != null && objectData.cleaningSteps.Count > 0)
                {
                    CleaningStep firstStep = objectData.cleaningSteps[0];
                    if (firstStep != null && firstStep.cameraZoomSize > 0.1f)
                    {
                        targetCameraSize = firstStep.cameraZoomSize;
                    }
                }
            }

            float time = 0f;
            float duration = 0.4f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, time / duration);

                toolFollower.transform.position = Vector3.Lerp(startPos, restTarget, t);
                toolFollower.transform.rotation = Quaternion.identity;

                yield return null;
            }

            toolFollower.transform.position = restTarget;
            toolFollower.transform.rotation = Quaternion.identity;

            if (toolFollower != null)
            {
                toolFollower.AnimateCapOff();
            }
        }

        isTransitioningTool = false;
        ToggleGameplayUI(false);
    }

    void ClearOldGeneratedLayers()
    {
        if (levelParentAnchor == null) return;

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

        objectData = newObjectData;
        LevelManager.SelectedObject = newObjectData;

        ClearOldGeneratedLayers();

        currentLayer = 0;
        gameCompleted = false;

        SetupGenericLevel();

        if (layersList.Count > 0)
        {
            PrepareLayer();
            SelectTool(layerRequiredTools[currentLayer], false);
        }
    }

    public void OnCurrentStepCompleted()
    {
        if (isUIHiddenByTimer)
        {
            isUIHiddenByTimer = false;
            ToggleGameplayUI(false);
        }
        touchTimer = 0f;
        idleTimer = 0f;

        effectGraceTimer = 0f;
        StopToolEffects();

        if (progressBarMainPanel != null) progressBarMainPanel.SetActive(false);
        if (progressFill != null) progressFill.gameObject.SetActive(false);
        if (percentText != null) percentText.gameObject.SetActive(false);

        if (currentLayer >= objectData.cleaningSteps.Count - 1)
        {
            CompleteGame();
        }
        else
        {
            StartCoroutine(TransitionToNextLayerRoutine());
        }
    }

    private void SetRemainingChunksGlow(bool enable)
    {
        isChunksGlowing = enable;
        if (levelParentAnchor == null) return;

        MudChunk[] activeChunks = levelParentAnchor.GetComponentsInChildren<MudChunk>(true);
        foreach (MudChunk chunk in activeChunks)
        {
            if (chunk != null && chunk.gameObject.activeSelf)
            {
                chunk.SetGlow(enable);
            }
        }
    }
}