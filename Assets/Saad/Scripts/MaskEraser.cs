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

    [Header("--- Ref Video Tool Variant UI ---")]
    public GameObject variantMainPanel;        // Niche wala poora UI Dabba
    public Transform variantButtonsContainer;  // Jahan 3 buttons lagenge (Horizontal Layout Group)
    public GameObject variantButtonPrefab;     // Button ka Prefab
    private List<ToolVariantButton> spawnedVariantButtons = new List<ToolVariantButton>();

    private Coroutine panelAnimCoroutine;

    [Header("Tool UI Panel")]
    public Image previousToolUIImage;
    public Image currentToolUIImage;
    public Image upcomingToolUIImage;

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

    [Tooltip("Completed Window ke andar wala UI Image dabba jahan photo dikhegi.")]
    public UnityEngine.UI.Image winPanelIconImage;
    public GameObject progressBarMainPanel;

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

    void Awake()
    {
        //  MOBILE ME SUPER SMOOTH 60 FPS LAG-FREE TOUCH KE LIYE:
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0; // VSync ko 0 karna zaroori hai tabhi 60 FPS chalega!

        // ... Aapka baqi purana code ...
    }

    void Start()
    {
        if (!PlayerPrefs.HasKey("Coins"))
        {
            PlayerPrefs.SetInt("Coins", 100);
            PlayerPrefs.Save();
        }

        if (AudioManager.Instance != null && AudioManager.Instance.gameSceneMusic != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.gameSceneMusic);
        }
        if (Camera.main != null)
        {
            defaultCameraSize = Camera.main.orthographic ? Camera.main.orthographicSize : Camera.main.fieldOfView;
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
                }
                else
                {
                    if (dirtyIndex < objectData.dirtyLayers.Length)
                    {
                        sr.sprite = objectData.dirtyLayers[dirtyIndex];
                        sr.sortingOrder = totalLayers - dirtyIndex;

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
                cleanObj.transform.localPosition = Vector3.zero;
                cleanObj.transform.localScale = Vector3.one;

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

                    dirtyObj.transform.localPosition = Vector3.zero;
                    dirtyObj.transform.localScale = Vector3.one;

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
        // 1. Pause Check (Sabse Pehle)
        if (PauseManager.IsGamePaused)
        {
            StopToolEffects();
            return;
        }

        // =========================================================================
        //  NATIVE MOBILE TOUCH & INPUT SYSTEM (Sensitivity Boost Fix!)
        // =========================================================================
        bool touchStarted = false;
        bool touchEnded = false;
        bool isTouching = false;
        Vector2 inputPosition = Vector2.zero;
        int pointerId = -1; // Default for PC Mouse

        // Mobile Direct Touch Check
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = touch.position;
            pointerId = touch.fingerId; // Mobile ki exact finger ID pakro

            if (touch.phase == TouchPhase.Began) touchStarted = true;
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) touchEnded = true;
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) isTouching = true;
        }
        // PC Editor Testing Backup (Mouse Click)
        else
        {
            inputPosition = Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) touchStarted = true;
            if (Input.GetMouseButtonUp(0)) touchEnded = true;
            if (Input.GetMouseButton(0)) isTouching = true;
        }
        // =========================================================================

        //  2. UI BLOCK CHECK (Mobile Safe Fixed! Pointer ID dena zaroori hai)
        if (isTouching && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
        {
            StopToolEffects();
            return;
        }

        // 3. Jaise hi player ne touch shuru kiya -> Panel Chupa do!
        if (touchStarted && !gameCompleted && !isTransitioningTool)
        {
            ToggleVariantPanelDuringCleaning(true); // Hide Panel
        }

        // 4. Jaise hi player ne touch choda aur layer complete NAHI hui -> Panel Wapas le aao!
        if (touchEnded && !gameCompleted && !isTransitioningTool && !layerFinishedWaitingRelease)
        {
            ToggleVariantPanelDuringCleaning(false); // Show Panel
        }

        // Camera Size Transition (Orthographic ya Field of View)
        if (Camera.main != null)
        {
            if (Camera.main.orthographic)
                Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, targetCameraSize, Time.deltaTime * cameraTransitionIntensity);
            else
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetCameraSize, Time.deltaTime * cameraTransitionIntensity);
        }

        // Camera Shift / Parallax Offset (Based on Input Position)
        Vector3 targetCameraPos = new Vector3(0, 0, Camera.main.transform.position.z);

        if (!gameCompleted && !isTransitioningTool && layersList.Count > 0 && isTouching)
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mouseOffset = new Vector2(
                (inputPosition.x - screenCenter.x) / screenCenter.x,
                (inputPosition.y - screenCenter.y) / screenCenter.y
            );
            targetCameraPos = new Vector3(mouseOffset.x * cameraMoveIntensity, 0, Camera.main.transform.position.z);
        }

        if (Camera.main != null)
        {
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetCameraPos, Time.deltaTime * 5f);
        }

        // Level State Constraints
        if (gameCompleted || isTransitioningTool || layersList.Count == 0) return;

        if (layerFinishedWaitingRelease)
        {
            if (!isTouching)
            {
                layerFinishedWaitingRelease = false;
                StartCoroutine(TransitionToNextLayerRoutine());
            }
            return;
        }

        if (currentLayer >= layersList.Count) return;

        //  5. REAL-TIME CLEANING LOGIC (Zero Delay Input)
        if (isTouching && currentToolData != null && toolFollower.CanClean)
        {
            Vector3 world;
            if (eraseAnchor != null)
            {
                world = eraseAnchor.position;
            }
            else
            {
                float cameraDistance = Mathf.Abs(Camera.main.transform.position.z);
                world = Camera.main.ScreenToWorldPoint(new Vector3(inputPosition.x, inputPosition.y, cameraDistance));
            }

            world.z = 0;
            bool isOverLayer = EraseAtWorldPosition(world);
            bool shouldPlay = currentToolData.soundOnlyOnHit ? isOverLayer : true;

            if (shouldPlay) effectGraceTimer = 0.15f;
        }

        // Tool Particle and Sound Grace Timer
        if (effectGraceTimer > 0)
        {
            effectGraceTimer -= Time.deltaTime;
            PlayToolEffects();
        }
        else
        {
            StopToolEffects();
        }

        // Texture Modification Apply
        if (textureNeedsApply)
        {
            texture.Apply(false);
            textureNeedsApply = false;
            needsProgressCheck = true;
        }

        // Progress Calculation Management
        if (needsProgressCheck)
        {
            progressTimer += Time.deltaTime;
            if (progressTimer > 0.15f || !isTouching)
            {
                UpdateProgress();
                progressTimer = 0f;
                needsProgressCheck = false;
            }
        }

        // Progress Bar Smooth Fill
        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 15f);
        progressFill.fillAmount = currentFill;
    }

    void PlayToolEffects()
    {
        // TOOL ANIMATION CHALANA
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
        // TOOL ANIMATION MOMENT PE POSITIONS RESET KARNA
        ResetToolPosition();

        foreach (GameObject activePart in activeParticlesList)
        {
            if (activePart != null)
            {
                ParticleSystem[] allParticles = activePart.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in allParticles) if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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
            foreach (ParticleSystem ps in allParticles) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            activeParticlesList.Add(currentParticle);
        }

        if (tool.useSecondParticles && tool.secondParticlePrefab != null)
        {
            GameObject secondParticle = Instantiate(tool.secondParticlePrefab, effectAnchor);
            secondParticle.transform.localPosition = tool.secondParticleOffset;

            ParticleSystem[] allParticles = secondParticle.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

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
        Vector3 bottomScreenTarget = new Vector3(Screen.width / 2f, Screen.height * 0.3f, camZ);
        Vector3 restTarget = Camera.main.ScreenToWorldPoint(bottomScreenTarget);
        restTarget.z = 0;
        Vector3 rightStart = restTarget + Vector3.right * 15f;
        toolFollower.transform.position = rightStart;

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
            toolFollower.transform.position = Vector3.Lerp(rightStart, restTarget, t);
            yield return null;
        }

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
            time += Time.deltaTime; float t = time / duration;
            float scale = Mathf.Lerp(0, 1, t * t * (3f - 2f * t)); img.transform.localScale = new Vector3(scale, scale, scale);
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
                upcomingIcons[i].gameObject.SetActive(true); upcomingIcons[i].sprite = layersList[layerIndex].sprite; if (animate)
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

        //  Naya tool select hote hi position/rotation save karne ka variable reset kardo
        isToolPosSaved = false;

        SetupToolVariantsPanel(tool);
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

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.TriggerCoinSwoopAnimation(20);
        }
    }


    // =========================================================================
    // SARI ANIMATION LOGIC (SCRUBBING, SPRAYING & ROTATION) YAHAN HAI
    // =========================================================================
    Transform GetToolTransformToAnimate()
    {
        if (toolFollower == null) return null;
        // Agar tool follower ke andar sprite ya model child hai to usko rotate/move karega
        return toolFollower.transform.childCount > 0 ? toolFollower.transform.GetChild(0) : toolFollower.transform;
    }

    void AnimateTool()
    {
        if (currentToolData == null || toolFollower == null) return;
        Transform toolObj = GetToolTransformToAnimate();
        if (toolObj == null) return;

        // Shuru mein tool ki ek baar default local position aur rotation save kar lo
        if (!isToolPosSaved)
        {
            originalToolLocalPos = toolObj.localPosition;
            originalToolRotation = toolObj.localRotation;
            isToolPosSaved = true;
        }

        // 1. Scrubbing Animation (Left-Right Move Hona)
        if (currentToolData.movementType == ToolMovementType.Scrubbing)
        {
            float shake = Mathf.Sin(Time.time * currentToolData.scrubSpeed) * currentToolData.scrubAmount;
            toolObj.localPosition = originalToolLocalPos + new Vector3(shake, 0, 0);
            toolObj.localRotation = originalToolRotation; // Rotation reset rakhein
        }
        // 2. Spraying Animation (Zabardast Vibration Effect)
        else if (currentToolData.movementType == ToolMovementType.Spraying)
        {
            float vibration = Random.Range(-0.05f, 0.05f);
            toolObj.localPosition = originalToolLocalPos + new Vector3(vibration, vibration, 0);
            toolObj.localRotation = originalToolRotation;
        }
        // 3.  Rotation Animation (Clockwise aur Counter-Clockwise ghoomna)
        else if (currentToolData.movementType == ToolMovementType.Rotation)
        {
            float angle = Mathf.Sin(Time.time * currentToolData.rotationSpeed) * currentToolData.rotationAmount;
            // Z axis par sprite rotate hoga (2D game ke liye)
            toolObj.localRotation = originalToolRotation * Quaternion.Euler(0, 0, angle);
            toolObj.localPosition = originalToolLocalPos; // Position reset rakhein
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
    // =========================================================================
    // REF VIDEO JAISA VARIANT SYSTEM & ANIMATION
    // =========================================================================
    void SetupToolVariantsPanel(ToolData tool)
    {
        // 1. Purane buttons saaf karo
        foreach (ToolVariantButton btn in spawnedVariantButtons) if (btn != null) Destroy(btn.gameObject);
        spawnedVariantButtons.Clear();

        // 2. Agar tool me variants OFF hain ya 3 sprites nahi daali, to panel hide kardo
        if (tool == null || !tool.hasVariants || tool.toolVariants.Count == 0)
        {
            if (variantMainPanel != null && variantMainPanel.activeSelf)
                StartCoroutine(AnimateVariantPanelVideoStyle(false));
            return;
        }

        // 3. Panel ON karo aur Video Jaisi Pop-Up Animation chalao
        if (variantMainPanel != null)
        {
            variantMainPanel.SetActive(true);
            StartCoroutine(AnimateVariantPanelVideoStyle(true));
        }

        //  4. NAYA BADLAV: Hamesha Default (Pehli) skin ko hi Equipped set kardo!
        // (Is se purani select ki hui skin reset ho jayegi aur default tool ban jayega)
        if (tool.toolVariants.Count > 0)
        {
            PlayerPrefs.SetString(tool.name + "_Equipped", tool.toolVariants[0].variantName);
        }

        // 5. ToolData me jitni sprites (3 variants) daali hain unke buttons banao
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

        // 6. Default (Pehli) skin ko direct haath me laga do (Bina animation ke)
        if (tool.toolVariants.Count > 0)
        {
            ApplyVariantSkin(tool, tool.toolVariants[0], false);
        }
    }

    //  UPGRADE: Ismein ab humne 'bool animate = false' add kar diya hai
    public void ApplyVariantSkin(ToolData tool, ToolVariant variant, bool animate = false)
    {
        if (toolFollower == null || variant == null) return;

        if (animate)
        {
            // Agar button click se call hua hai, to Slide Out -> Change -> Slide In animation chalao!
            StartCoroutine(AnimateVariantSkinRoutine(variant));
        }
        else
        {
            // Agar level shuru ho raha hai, to direct photo badal do
            SpriteRenderer toolSR = toolFollower.GetComponentInChildren<SpriteRenderer>();
            if (toolSR != null && variant.toolSprite != null)
            {
                toolSR.sprite = variant.toolSprite;
            }
        }

        // Saare buttons ka UI (Equipped / Free text) update karo
        foreach (ToolVariantButton btn in spawnedVariantButtons)
        {
            if (btn != null) btn.UpdateUI();
        }
    }

    // NAYA FUNCTION: ACTUAL LAYER TRANSITION JAISI OUT & IN ANIMATION
    IEnumerator AnimateVariantSkinRoutine(ToolVariant variant)
    {
        if (toolFollower == null) yield break;

        // 1. Player ka control thodi der ke liye band karo taake animation disturb na ho
        toolFollower.enabled = false;

        Vector3 startPos = toolFollower.transform.position;
        // Tool ko tezi se Left side slide karke screen se bahar bhejo (Out)
        Vector3 outTarget = startPos + Vector3.left * 15f;

        float time = 0;
        float durationOut = 0.3f; // 0.3 seconds me bahar jayega
        while (time < durationOut)
        {
            time += Time.deltaTime;
            float t = time / durationOut;
            toolFollower.transform.position = Vector3.Lerp(startPos, outTarget, t * t);
            yield return null;
        }

        // 2. Screen se bahar jane ke baad Sprite (Photo) badal do
        SpriteRenderer toolSR = toolFollower.GetComponentInChildren<SpriteRenderer>();
        if (toolSR != null && variant.toolSprite != null)
        {
            toolSR.sprite = variant.toolSprite;
        }

        // 3. Ab naye tool ko Right side se wapas screen ke center me lao (In)
        float camZ = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 bottomScreenTarget = new Vector3(Screen.width / 2f, Screen.height * 0.3f, camZ);
        Vector3 restTarget = Camera.main.ScreenToWorldPoint(bottomScreenTarget);
        restTarget.z = 0;
        Vector3 inStart = restTarget + Vector3.right * 15f;

        toolFollower.transform.position = inStart;

        time = 0;
        float durationIn = 0.4f; // 0.4 seconds me andar aayega
        while (time < durationIn)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / durationIn);
            toolFollower.transform.position = Vector3.Lerp(inStart, restTarget, t);
            yield return null;
        }

        // 4. Wapas Mouse ka control player ke haath me de do!
        toolFollower.enabled = true;
    }

    public void ToggleVariantPanelDuringCleaning(bool hide)
    {
        // Agar current tool ke paas variants hi nahi hain, to kuch mat karo
        if (currentToolData == null || !currentToolData.hasVariants || currentToolData.toolVariants.Count == 0) return;
        if (variantMainPanel == null) return;

        // Purani animation agar chal rahi ho to use rok do taake bug na bane
        if (panelAnimCoroutine != null) StopCoroutine(panelAnimCoroutine);

        // Nayi animation chalao
        panelAnimCoroutine = StartCoroutine(AnimateVariantPanelVideoStyle(!hide));
    }

    //  EXACT VIDEO JAISI BOUNCE / POP ANIMATION
    IEnumerator AnimateVariantPanelVideoStyle(bool show)
    {
        if (variantMainPanel == null) yield break;

        if (show)
        {
            // 1. Shuru me panel ko scale 0 (invisible) kardo taake wait karte waqt wo dikhe nahi
            variantMainPanel.transform.localScale = new Vector3(0.5f, 0f, 1f);
            variantMainPanel.SetActive(true);

            // =========================================================================
            //  YAHAN SE DELAY EDIT HOGA:
            // (0.25f ka matlab hai 0.25 seconds. Jitna late laana ho number badha lein jaise 0.4f ya 0.5f)
            yield return new WaitForSeconds(0.8f);
            // =========================================================================
        }

        Vector3 startScale = variantMainPanel.transform.localScale;
        Vector3 targetScale = show ? Vector3.one : new Vector3(0.5f, 0f, 1f);

        float time = 0f;
        float duration = 0.25f; // Yeh panel ke pop-up hone ki speed hai

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            // Smooth Bounce effect
            float smoothT = t * t * (3f - 2f * t);
            variantMainPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            yield return null;
        }

        variantMainPanel.transform.localScale = targetScale;
        if (!show) variantMainPanel.SetActive(false);
    }

    //  EXACT BAQI TOOLS JAISA SLIDE-IN ANIMATION FOR FIRST TOOL
    IEnumerator AnimateFirstToolOnStartup()
    {
        // 1. Update() loop ko roko taake wo tool ko beech me na chhede
        isTransitioningTool = true;

        // 2. Scene aur Camera load hone ka thoda sa wait karo
        yield return new WaitForSeconds(0.15f);

        if (toolFollower != null && Camera.main != null)
        {
            toolFollower.gameObject.SetActive(true);

            //  3. Asli Rest Position (Jahan tool ko aakar rukna hai - Screen ka center/niche)
            Vector3 restTarget = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.2f, 10f));
            restTarget.z = 0f;

            //  4. Start Position (Screen ke bahar - Right side se ya Left side se)
            // (Hum isko Right side se slide karwa rahe hain jaise baqi tools aate hain)
            Vector3 startPos = restTarget + Vector3.right * 15f;

            // Shuru me tool ko screen ke bahar rakho
            toolFollower.transform.position = startPos;

            // 5. Panel ko bhi setup aur animate kardo
            if (currentToolData != null)
            {
                SetupToolVariantsPanel(currentToolData);
            }

            //  6. Smooth Slide-In Animation (Bahar se andar aana)
            float time = 0f;
            float duration = 0.4f; // 0.4 seconds me mast slide hoke aayega

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, time / duration); // Smooth braking effect
                toolFollower.transform.position = Vector3.Lerp(startPos, restTarget, t);
                yield return null;
            }

            // Exactly rest target par bitha do
            toolFollower.transform.position = restTarget;
        }

        // 7. Animation khatam! Ab player touch karke safai kar sakta hai
        isTransitioningTool = false;
    }

}