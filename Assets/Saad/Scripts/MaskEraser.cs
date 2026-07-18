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

    // NAYE COROUTINE & POSITION VARIABLES: Coin aur Pause button animation ke liye
    private Coroutine cornerButtonsCoroutine;
    private Vector2 coinPanelOriginalPos;
    private Vector2 pauseButtonOriginalPos;
    private bool areCornerPosSaved = false;

    // NAYA VARIABLE: Pehle se active variant ko track karne ke liye
    private ToolVariant currentEquippedVariant;

    [Header("Tool UI Panel")]
    public Image previousToolUIImage;
    public Image currentToolUIImage;
    public Image upcomingToolUIImage;

    [Header("Tool UI Sizes & Spacing")]
    public float activeToolScale = 2f;
    public float inactiveToolScale = 1.5f;
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

    [Tooltip("Completed Window ke andar wala UI Image dabba jahan photo dikhegi.")]
    public UnityEngine.UI.Image winPanelIconImage;
    public GameObject progressBarMainPanel;

    // Runtime Generated Layers
    private List<SpriteRenderer> layersList = new List<SpriteRenderer>();
    private List<ToolData> layerRequiredTools = new List<ToolData>();

    int currentLayer = 0;
    Texture2D texture;
    int totalOpaquePixels = 0;

    // Inko class ke top par declare karein
    private float totalChunksCount = 0f;
    private float remainingChunksCount = 0f;

   // private int totalChunksCount = 0; // Chunks ki kul tadad save karne ke liye

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

    private int totalChunksInLayer = 0;
    private int removedChunksCount = 0;

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

        if (LevelManager.SelectedObject != null)
        {
            objectData = LevelManager.SelectedObject;
        }

        if (objectData == null)
        {
            Debug.LogError("MaskEraser: No CleaningObjectData assigned!");
            return;
        }

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
        if (backgroundImage != null && objectData.backgroundSprite != null)
            backgroundImage.sprite = objectData.backgroundSprite;

        if (levelParentAnchor == null)
        {
            Debug.LogError("MaskEraser: Please assign Level Parent Anchor!");
            return;
        }

        Vector3 currentPos = levelParentAnchor.position;
        levelParentAnchor.position = new Vector3(currentPos.x, currentPos.y, 0f);
        levelParentAnchor.rotation = Quaternion.identity;

        layersList.Clear();
        layerRequiredTools.Clear();

        SpriteRenderer[] existingRenderers = levelParentAnchor.GetComponentsInChildren<SpriteRenderer>(true);

        if (existingRenderers.Length > 0)
        {
            int totalLayers = 0;
            foreach (SpriteRenderer sr in existingRenderers)
            {
                if (sr.gameObject.name.Contains("Dirty_Layer") || System.Char.IsDigit(sr.gameObject.name[0]))
                {
                    totalLayers++;
                }
            }

            int dirtyIndex = 0;
            foreach (SpriteRenderer sr in existingRenderers)
            {
                Vector3 childLocalPos = sr.transform.localPosition;
                childLocalPos.z = 0f;
                sr.transform.localPosition = childLocalPos;
                sr.transform.localRotation = Quaternion.identity;

                if (sr.gameObject.name.Contains("Base_Clean_Object") || sr.gameObject.name.Contains("07"))
                {
                    sr.sprite = objectData.cleanSprite;
                    sr.sortingOrder = 0;

                    // --- NAYA CODE: Clean object agar pehle se hierarchy me ho to uski position/scale ---
                    sr.transform.localPosition = objectData.cleanObjectOffset;
                    sr.transform.localScale = objectData.cleanObjectScale;
                }
                else
                {
                    if (dirtyIndex < objectData.dirtyLayers.Length)
                    {
                        sr.sprite = objectData.dirtyLayers[dirtyIndex];
                        sr.sortingOrder = totalLayers - dirtyIndex;

                        if (objectData.layerOffsets != null && dirtyIndex < objectData.layerOffsets.Length)
                        {
                            sr.transform.localPosition = objectData.layerOffsets[dirtyIndex];
                        }

                        if (objectData.layerScales != null && dirtyIndex < objectData.layerScales.Length)
                        {
                            sr.transform.localScale = objectData.layerScales[dirtyIndex];
                        }

                        layersList.Add(sr);

                        ToolData assignedTool = null;
                        if (objectData.requiredTools != null && dirtyIndex < objectData.requiredTools.Length)
                            assignedTool = objectData.requiredTools[dirtyIndex];
                        CleaningLayer cleaningLayerComponent = sr.GetComponent<CleaningLayer>();
                        if (cleaningLayerComponent == null) cleaningLayerComponent = sr.gameObject.AddComponent<CleaningLayer>();
                        cleaningLayerComponent.requiredTool = assignedTool;

                        layerRequiredTools.Add(assignedTool);
                        sr.gameObject.SetActive(true);
                        dirtyIndex++;
                    }
                }
            }
        }
        else
        {
            if (objectData.cleanSprite != null)
            {
                GameObject cleanObj = new GameObject("Base_Clean_Object");
                cleanObj.transform.SetParent(levelParentAnchor);
                cleanObj.transform.localRotation = Quaternion.identity;

                // --- NAYA CODE: Runtime par banne wale clean object ki position/scale ---
                cleanObj.transform.localPosition = objectData.cleanObjectOffset;
                cleanObj.transform.localScale = objectData.cleanObjectScale;

                SpriteRenderer sr = cleanObj.AddComponent<SpriteRenderer>();
                sr.sprite = objectData.cleanSprite;
                sr.sortingOrder = 0;
            }

            if (objectData.dirtyLayers != null && objectData.dirtyLayers.Length > 0)
            {
                int totalLayers = objectData.dirtyLayers.Length;
                for (int i = 0; i < totalLayers; i++)
                {
                    if (objectData.dirtyLayers[i] == null) continue;
                    GameObject dirtyObj = new GameObject("Dirty_Layer_" + i);
                    dirtyObj.transform.SetParent(levelParentAnchor);

                    if (objectData.layerOffsets != null && i < objectData.layerOffsets.Length)
                    {
                        dirtyObj.transform.localPosition = objectData.layerOffsets[i];
                    }
                    else
                    {
                        dirtyObj.transform.localPosition = Vector3.zero;
                    }

                    if (objectData.layerScales != null && i < objectData.layerScales.Length)
                    {
                        dirtyObj.transform.localScale = objectData.layerScales[i];
                    }
                    else
                    {
                        dirtyObj.transform.localScale = Vector3.one;
                    }

                    SpriteRenderer sr = dirtyObj.AddComponent<SpriteRenderer>();
                    sr.sprite = objectData.dirtyLayers[i];
                    sr.sortingOrder = totalLayers - i;

                    layersList.Add(sr);

                    ToolData assignedTool = null;
                    if (objectData.requiredTools != null && i < objectData.requiredTools.Length)
                        assignedTool = objectData.requiredTools[i];
                    CleaningLayer cleaningLayerComponent = dirtyObj.AddComponent<CleaningLayer>();
                    cleaningLayerComponent.requiredTool = assignedTool;

                    layerRequiredTools.Add(assignedTool);
                    dirtyObj.SetActive(true);
                }
            }
        }
    }

    void Update()
    {
        if (PauseManager.IsGamePaused)
        {
            StopToolEffects();
            return;
        }

        // Jab tak mouse ya finger UI/Buttons ke upar hai, follower ko stop rakhein
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
                ToggleGameplayUI(true);  // Sab hide ho jayega
            }
            else if (Input.GetMouseButtonUp(0))
            {
                ToggleGameplayUI(false); // Sab wapas show ho jayega
            }
        }

        if (Camera.main != null)
        {
            if (Camera.main.orthographic)
                Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, targetCameraSize, Time.deltaTime * cameraTransitionIntensity);
            else
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetCameraSize, Time.deltaTime * cameraTransitionIntensity);
        }

        Vector3 currentMousePos = Input.mousePosition;
        Vector3 targetCameraPos = new Vector3(0, 0, Camera.main.transform.position.z);
        if (!gameCompleted && !isTransitioningTool && layersList.Count > 0 && Input.GetMouseButton(0))
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mouseOffset = new Vector2(
                (currentMousePos.x - screenCenter.x) / screenCenter.x,
                (currentMousePos.y - screenCenter.y) / screenCenter.y
            );
            targetCameraPos = new Vector3(mouseOffset.x * cameraMoveIntensity, 0, Camera.main.transform.position.z);
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

        // --- MOUSE INPUT FOR ANIMATION AND SCRUBBING ---
        if (Input.GetMouseButton(0) && currentToolData != null && toolFollower != null && toolFollower.CanClean)
        {
            Vector3 world;
            if (eraseAnchor != null) world = eraseAnchor.position;
            else
            {
                float cameraDistance = Mathf.Abs(Camera.main.transform.position.z);
                world = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));
            }
            world.z = 0;

            // Har tool ke liye mitti kaatna lazmi karein
            bool isOverLayer = EraseAtWorldPosition(world);

            //  Hawa mein click block fix: Progress check tabhi chalega jab mouse click dabba hua ho!
            needsProgressCheck = true;
            textureNeedsApply = true;

            if (currentToolData.name.Contains("Scraper") || currentToolData.toolName.Contains("Scraper"))
            {
                AnimateTool(); // Shake karega
                effectGraceTimer = 0.15f;
            }
            else
            {
                bool shouldPlay = currentToolData.soundOnlyOnHit ? isOverLayer : true;
                if (shouldPlay) effectGraceTimer = 0.15f;
            }
        }
        else if (!Input.GetMouseButton(0) && currentToolData != null)
        {
            ResetToolPosition();
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
        }

        // --- RESET AND REALTIME PROGRESS CALCULATION ---
        if (needsProgressCheck)
        {
            progressTimer += Time.deltaTime;

            if (!Input.GetMouseButton(0) || progressTimer > 0.15f)
            {
                // Automatic Chunks System Finder:
                Transform existingChunks = levelParentAnchor.Find("Scraper_Chunks_Runtime");

                // 1. Agar scene mein chunks object active aur maujood hai
                if (existingChunks != null && existingChunks.gameObject.activeInHierarchy)
                {
                    int currentLeftChunks = 0;
                    foreach (Transform child in existingChunks)
                    {
                        // Sirf un chunks ko gino jo abhi tak nahi gire/gayab nahi hue (active hain)
                        if (child.gameObject.activeSelf)
                        {
                            currentLeftChunks++;
                        }
                    }

                    if (totalChunksCount > 0)
                    {
                        // Progress = Kitne percent gir chuke hain
                        float progress = 1f - ((float)currentLeftChunks / totalChunksCount);
                        targetFill = Mathf.Clamp01(progress);
                    }
                }
                else
                {
                    // 2. Agar chunks nahi hain (Brush/Water tool chal raha hai), to pixels se calculation karo
                    UpdateProgress();
                }

                progressTimer = 0f;
                needsProgressCheck = false;
            }
        }

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 15f);
        progressFill.fillAmount = currentFill;
    }



    // UNIFIED FUNCTION: Teeno panels ko smooth animation ke sath hide/show karne ke liye
    public void ToggleGameplayUI(bool hide)
    {
        // 1. Variant Panel (Sirf tabhi slide hoga jab tool ke paas variants hon)
        if (currentToolData != null && currentToolData.hasVariants && currentToolData.toolVariants.Count > 0 && variantMainPanel != null)
        {
            if (panelAnimCoroutine != null) StopCoroutine(panelAnimCoroutine);
            panelAnimCoroutine = StartCoroutine(AnimateVariantPanelVideoStyle(!hide));
        }

        // 2 & 3. Coin Bar aur Pause Button ki smooth animation
        if (gameplayCoinPanel != null || pauseButton != null)
        {
            if (cornerButtonsCoroutine != null) StopCoroutine(cornerButtonsCoroutine);
            cornerButtonsCoroutine = StartCoroutine(AnimateCornerButtonsRoutine(!hide));
        }
    }

    // NAYI COROUTINE: Coin aur Pause button ko smooth slide/scale karane ke liye
    IEnumerator AnimateCornerButtonsRoutine(bool show)
    {
        RectTransform coinRect = gameplayCoinPanel != null ? gameplayCoinPanel.GetComponent<RectTransform>() : null;
        RectTransform pauseRect = pauseButton != null ? pauseButton.GetComponent<RectTransform>() : null;

        // Start me dono ki original positions save karna taake animation hamesha accurate rahe
        if (!areCornerPosSaved)
        {
            if (coinRect != null) coinPanelOriginalPos = coinRect.anchoredPosition;
            if (pauseRect != null) pauseButtonOriginalPos = pauseRect.anchoredPosition;
            areCornerPosSaved = true;
        }

        if (show)
        {
            if (gameplayCoinPanel != null) gameplayCoinPanel.SetActive(true);
            if (pauseButton != null) pauseButton.SetActive(true);
        }

        Vector3 startCoinScale = coinRect != null ? coinRect.localScale : Vector3.one;
        Vector3 startPauseScale = pauseRect != null ? pauseRect.localScale : Vector3.one;

        Vector3 targetScale = show ? Vector3.one : new Vector3(0f, 0f, 1f);

        Vector2 startCoinPos = coinRect != null ? coinRect.anchoredPosition : Vector2.zero;
        Vector2 startPausePos = pauseRect != null ? pauseRect.anchoredPosition : Vector2.zero;

        // Hide hote waqt coin bar left ki taraf aur pause button right ki taraf slide hoga
        Vector2 targetCoinPos = show ? coinPanelOriginalPos : coinPanelOriginalPos + new Vector2(-150f, 0f);
        Vector2 targetPausePos = show ? pauseButtonOriginalPos : pauseButtonOriginalPos + new Vector2(150f, 0f);

        float time = 0f;
        float duration = 0.25f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t);

            if (coinRect != null)
            {
                coinRect.localScale = Vector3.Lerp(startCoinScale, targetScale, smoothT);
                coinRect.anchoredPosition = Vector2.Lerp(startCoinPos, targetCoinPos, smoothT);
            }

            if (pauseRect != null)
            {
                pauseRect.localScale = Vector3.Lerp(startPauseScale, targetScale, smoothT);
                pauseRect.anchoredPosition = Vector2.Lerp(startPausePos, targetPausePos, smoothT);
            }

            yield return null;
        }

        if (coinRect != null)
        {
            coinRect.localScale = targetScale;
            coinRect.anchoredPosition = targetCoinPos;
        }

        if (pauseRect != null)
        {
            pauseRect.localScale = targetScale;
            pauseRect.anchoredPosition = targetPausePos;
        }

        if (!show)
        {
            if (gameplayCoinPanel != null) gameplayCoinPanel.SetActive(false);
            if (pauseButton != null) pauseButton.SetActive(false);
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
        layersList[currentLayer].gameObject.SetActive(true);

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
        if (gameCompleted || layerFinishedWaitingRelease || layersList.Count == 0) return;
        Color[] pixels = texture.GetPixels();

        int currentOpaque = 0;
        foreach (Color c in pixels)
        {
            if (c.a > 0.25f) currentOpaque++;
        }

        if (totalOpaquePixels == 0) totalOpaquePixels = 1;
        int removed = totalOpaquePixels - currentOpaque;

        float percent = ((float)removed / totalOpaquePixels) * 100f;

        float visualPercent = percent;
        if (visualPercent > 100f) visualPercent = 100f;

        targetFill = visualPercent / 100f;
        percentText.text = Mathf.RoundToInt(visualPercent) + "%";
        if (percent >= cleaningThreshold)
        {
            Debug.Log("LAYER TARGET ACHIEVED!");
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
            if (currentLayer >= layersList.Count - 1)
            {
                CompleteGame();
            }
            else
            {
                layerFinishedWaitingRelease = true;
            }
        }
    }

    void ClearRemainingLayer()
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++) pixels[i].a = 0f;
        texture.SetPixels(pixels);
        texture.Apply(false);

        targetFill = 1f;
        currentFill = 1f;
        progressFill.fillAmount = 1f;
        percentText.text = "100%";
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

        if (currentLayer < layersList.Count && layersList[currentLayer] != null)
        {
            layersList[currentLayer].gameObject.SetActive(false);
        }

        currentLayer++;

        if (currentLayer >= layersList.Count)
        {
            CompleteGame();
            isTransitioningTool = false;
            yield break;
        }

        PrepareLayer();
        ToolData nextTool = layerRequiredTools[currentLayer];

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

        if (celebrationPrefab != null) Instantiate(celebrationPrefab, Vector3.zero, Quaternion.identity);
        if (toolFollower != null) toolFollower.gameObject.SetActive(false);
        if (previousToolUIImage != null) previousToolUIImage.gameObject.SetActive(false);
        if (currentToolUIImage != null) currentToolUIImage.gameObject.SetActive(false);
        if (upcomingToolUIImage != null) upcomingToolUIImage.gameObject.SetActive(false);
        UpdateUpcomingIconsPanel(false);

        // Level complete hone par variant aur pause panel ko permanent hide karein
        if (variantMainPanel != null) variantMainPanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(false);

        // GAMEPLAY COIN BAR: Jab tak win panel delayed chal raha hai, tab tak isko hide rakhein
        if (gameplayCoinPanel != null) gameplayCoinPanel.SetActive(false);

        StartCoroutine(ShowDelayedUIAndCoinsRoutine());
        if (progressBarMainPanel != null) progressBarMainPanel.SetActive(false);
        if (celebrationSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(celebrationSound);
        }

        if (winPanelIconImage != null && completedLevelSprite != null)
        {
            winPanelIconImage.sprite = completedLevelSprite;
        }
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

        // =================================================================
        // --- HYBRID SYSTEM (SIRF CLICK HONE PAR CHUNKS DETECT KAREGA) ---
        // =================================================================
        if (currentToolData != null)
        {
            // Check karein ke jo tool select hua hai kya wo Scraper hai?
            bool isScraper = currentToolData.name.Contains("Scraper") || currentToolData.toolName.Contains("Scraper");

            if (isScraper)
            {
                // 1. Agar Scraper hai, to purani plain mud layer photo ko chupa do
                if (currentLayer < layersList.Count && layersList[currentLayer] != null)
                {
                    layersList[currentLayer].gameObject.SetActive(false);
                }

                // 2. Chunks wala prefab dhoondhein ya spawn karein
                Transform existingChunks = levelParentAnchor.Find("Scraper_Chunks_Runtime");
                if (existingChunks == null && objectData.scraperChunksPrefab != null)
                {
                    // Purani layer ki position aur transform nikalna
                    Transform originalLayerTransform = layersList[currentLayer].transform;

                    // Chunks ko usi exact position aur parent par spawn karein jahan purani mitti thi
                    GameObject chunksInstance = Instantiate(objectData.scraperChunksPrefab, originalLayerTransform.parent);

                    MudChunk[] allChunks = chunksInstance.GetComponentsInChildren<MudChunk>();
                    InitializeChunksCount(allChunks.Length);

                    chunksInstance.name = "Scraper_Chunks_Runtime";

                    // Exact position, rotation aur scale match karna taake ball ke upar fit aaye
                    chunksInstance.transform.localPosition = originalLayerTransform.localPosition;
                    chunksInstance.transform.localRotation = originalLayerTransform.localRotation;
                    chunksInstance.transform.localScale = originalLayerTransform.localScale;

                    // LIVE FIX 1: Chunks ki Z position ko 0 par force karein taake collision intersect ho
                    Vector3 chunkPos = chunksInstance.transform.localPosition;
                    chunkPos.z = 0;
                    chunksInstance.transform.localPosition = chunkPos;

                    // Rigidbodies ko ignore collision dena taake tool freeze na ho
                    Collider2D[] childColliders = chunksInstance.GetComponentsInChildren<Collider2D>();
                    InitializeChunksCount(childColliders.Length);
                }
                else if (existingChunks != null)
                {
                    existingChunks.gameObject.SetActive(true);

                    // Z-position alignment check for existing chunks
                    Vector3 chunkPos = existingChunks.localPosition;
                    chunkPos.z = 0;
                    existingChunks.localPosition = chunkPos;
                }
            }
            else
            {
                // 3. Agar koi doosra tool select hua (Brush/Water), to chunks ko chupa do aur purani photo wapas lao
                Transform existingChunks = levelParentAnchor.Find("Scraper_Chunks_Runtime");
                if (existingChunks != null)
                {
                    existingChunks.gameObject.SetActive(false);
                }

                if (currentLayer < layersList.Count && layersList[currentLayer] != null)
                {
                    layersList[currentLayer].gameObject.SetActive(true);
                }
            }
        }
        // =================================================================

        // Tool follower par automatic collider lagana taake mitti ko gira sake
        if (toolFollower != null)
        {
            //  LIVE FIX 2: Tool ki apni local Z position ko bhi zero karein
            Vector3 toolPos = toolFollower.transform.localPosition;
            toolPos.z = 0;
            toolFollower.transform.localPosition = toolPos;

            Collider2D toolCol = toolFollower.GetComponent<Collider2D>();
            if (toolCol == null)
            {
                // Agar tool par collider nahi hai to CircleCollider2D laga do
                CircleCollider2D circleCol = toolFollower.gameObject.AddComponent<CircleCollider2D>();
                // Radius thoda bada rakhein taake ragadte waqt aaram se touch ho
                circleCol.radius = 0.5f;
                toolCol = circleCol;
            }
            toolCol.isTrigger = true; // Is ka trigger hona lazmi hai

            Rigidbody2D toolRb = toolFollower.GetComponent<Rigidbody2D>();
            if (toolRb == null)
            {
                toolRb = toolFollower.gameObject.AddComponent<Rigidbody2D>();
            }
            toolRb.bodyType = RigidbodyType2D.Kinematic; // Taake tool khud neeche na gire
            toolRb.simulated = true;
        }
    }

    public void GoToHome()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene("HomeScene");
    }

    IEnumerator ShowDelayedUIAndCoinsRoutine()
    {
        yield return new WaitForSeconds(levelCompleteDelay);
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        else
        {
            Debug.LogError("MaskEraser: levelCompletePanel reference missing in Inspector!");
        }

        // COIN BAR RE-ACTIVATION: Level Complete screen aate hi coin counter wapas ON ho jayega
        if (gameplayCoinPanel != null)
        {
            gameplayCoinPanel.SetActive(true);
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.TriggerCoinSwoopAnimation(20);
        }

        UpdateGameplayCoinsUI();
        // Tool load hone ke baad, use collider aur trigger banayein:
        if (toolFollower != null)
        {
            // Agar pehle se collider nahi hai to add karein
            Collider2D toolCollider = toolFollower.GetComponent<Collider2D>();
            if (toolCollider == null)
            {
                toolCollider = toolFollower.gameObject.AddComponent<CircleCollider2D>();
            }
            toolCollider.isTrigger = true; // Is ko check rakhna zaroori hai

            // Rigidbody2D zaroori hai triggers ke takrane ke liye
            Rigidbody2D toolRb = toolFollower.GetComponent<Rigidbody2D>();
            if (toolRb == null)
            {
                toolRb = toolFollower.gameObject.AddComponent<Rigidbody2D>();
            }
            toolRb.bodyType = RigidbodyType2D.Kinematic; // Taake tool gravity se neeche na gire
        }
        // --- HYBRID SYSTEM LOGIC ---
        // --- HYBRID SYSTEM LOGIC (FIXED) ---
        if (currentToolData != null && (currentToolData.name.Contains("Scraper") || currentToolData.toolName.Contains("Scraper")))
        {
            // 1. Agar Scraper select hua hai, to purani sprite layer ko hide kar do
            if (currentLayer < layersList.Count && layersList[currentLayer] != null)
            {
                layersList[currentLayer].gameObject.SetActive(false);
            }

            // 2. Chunks wala prefab spawn karo agar pehle se nahi hua verna use active karo
            GameObject chunksInstance = levelParentAnchor.Find("Scraper_Chunks_Runtime")?.gameObject;

            if (chunksInstance == null && objectData.scraperChunksPrefab != null)
            {
                chunksInstance = Instantiate(objectData.scraperChunksPrefab, levelParentAnchor);
                chunksInstance.name = "Scraper_Chunks_Runtime";

                chunksInstance.transform.localPosition = objectData.cleanObjectOffset;
                chunksInstance.transform.localScale = objectData.cleanObjectScale;

                InitializeChunksCount(chunksInstance.GetComponentsInChildren<MudChunk>().Length);
            }
            else if (chunksInstance != null)
            {
                chunksInstance.SetActive(true);
            }
        }
        else
        {
            // 3. Agar koi DOOSRA tool select hua hai, to chunks ko hide karo aur purani sprite dikhao
            GameObject chunksInstance = levelParentAnchor.Find("Scraper_Chunks_Runtime")?.gameObject;
            if (chunksInstance != null)
            {
                chunksInstance.SetActive(false);
            }

            if (currentLayer < layersList.Count && layersList[currentLayer] != null)
            {
                layersList[currentLayer].gameObject.SetActive(true);
            }
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

        // --- FIXED: Brackets aur Duplicate Code Saaf Kar Diya ---
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
        foreach (ToolVariantButton btn in spawnedVariantButtons) if (btn != null) Destroy(btn.gameObject);
        spawnedVariantButtons.Clear();

        if (tool == null || !tool.hasVariants || tool.toolVariants.Count == 0)
        {
            if (variantMainPanel != null && variantMainPanel.activeSelf)
                StartCoroutine(AnimateVariantPanelVideoStyle(false));
            return;
        }

        if (variantMainPanel != null)
        {
            variantMainPanel.SetActive(true);
            StartCoroutine(AnimateVariantPanelVideoStyle(true));
        }

        if (tool.toolVariants.Count > 0)
        {
            PlayerPrefs.SetString(tool.name + "_Equipped", tool.toolVariants[0].variantName);
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
            ApplyVariantSkin(tool, tool.toolVariants[0], false);
        }
    }

    public void ApplyVariantSkin(ToolData tool, ToolVariant variant, bool animate = false)
    {
        if (toolFollower == null || variant == null) return;

        if (animate && variant == currentEquippedVariant)
        {
            Debug.Log("Variant is already equipped. Animation and execution skipped.");
            return;
        }

        currentEquippedVariant = variant;

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

    IEnumerator AnimateVariantSkinRoutine(ToolVariant variant)
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

    IEnumerator AnimateVariantPanelVideoStyle(bool show)
    {
        if (variantMainPanel == null) yield break;
        if (show)
        {
            variantMainPanel.transform.localScale = new Vector3(0.5f, 0f, 1f);
            variantMainPanel.SetActive(true);
            yield return new WaitForSeconds(0.8f);
        }

        Vector3 startScale = variantMainPanel.transform.localScale;
        Vector3 targetScale = show ? Vector3.one : new Vector3(0.5f, 0f, 1f);

        float time = 0f;
        float duration = 0.25f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t);
            variantMainPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            yield return null;
        }

        variantMainPanel.transform.localScale = targetScale;
        if (!show) variantMainPanel.SetActive(false);
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
    public void InitializeChunksCount(int count)
    {
        totalChunksCount = count;
        remainingChunksCount = count;

        // Agar pehle se hi progress 0 set karni ho
        targetFill = 0f;
    }

    // Naya function jo MudChunk.cs direct call karega jab chunk girega
    public void ChunkFallen()
    {
        if (totalChunksCount > 0)
        {
            remainingChunksCount--;
            if (remainingChunksCount < 0) remainingChunksCount = 0;

            // Progress calculate karein: Jitne chunks gir gaye hain uske mutabik
            float progress = 1f - (remainingChunksCount / totalChunksCount);
            targetFill = Mathf.Clamp01(progress);
        }
    }

    // Har tukda girne par yeh call hoga
    public void ChunkRemoved()
    {
        removedChunksCount++;

        // Progress calculation (0 se 1 ke beech fill amount)
        if (totalChunksInLayer > 0)
        {
            targetFill = (float)removedChunksCount / totalChunksInLayer;
            percentText.text = Mathf.RoundToInt(targetFill * 100f) + "%";

            // Agar saare chunks gir gaye to next layer par jao
            if (removedChunksCount >= totalChunksInLayer)
            {
                // Aapka next layer par jaane ka purana function
                StartCoroutine(TransitionToNextLayerRoutine());
            }
        }
    }
}