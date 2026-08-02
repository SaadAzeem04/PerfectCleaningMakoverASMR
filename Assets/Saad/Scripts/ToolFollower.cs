using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolFollower : MonoBehaviour
{
    [Header("Sprite & Transform References")]
    public SpriteRenderer toolSprite;
    public SpriteRenderer capSpriteRenderer;
    public Transform spawnPoint;

    [Header("Hand Gesture Settings")]
    public SpriteRenderer handGestureRenderer;
    [Tooltip("Purse / Target ka Transform yahan dein. Agar kisi step par hand nahi chahiye to isay null chhod dein.")]
    public Transform handGestureTarget;

    [Tooltip("Glue position se hand kitna left se start ho (Default X: -1.2, Y: -0.2)")]
    public Vector3 handStartOffset = new Vector3(-1.2f, -0.2f, 0f);

    // States
    public bool CanClean { get; private set; }

    // NEW: Glue waving aur slide-out animation ke waqt player touch freeze karne ke liye
    public bool IsInputLocked { get; set; } = false;

    private bool canFollow;
    private bool hasPlayerInteracted = false;
    private bool isDragging = false;
    private Camera cam;
    private Collider2D toolCollider;
    private ToolData currentToolData;

    public ToolData CurrentToolData => currentToolData;

    private Coroutine capCoroutine;
    private Coroutine handGestureCoroutine;

    void Awake()
    {
        cam = Camera.main;
        toolCollider = GetComponentInChildren<Collider2D>();

        if (toolSprite != null) toolSprite.enabled = false;
        if (capSpriteRenderer != null) capSpriteRenderer.gameObject.SetActive(false);
        if (handGestureRenderer != null) handGestureRenderer.gameObject.SetActive(false);
    }

    void Update()
    {
        // ADDED: IsInputLocked check - Jab touch lock ho to movement aur cleaning freeze ho jaye
        if (IsInputLocked || PauseManager.IsGamePaused || toolSprite == null || !toolSprite.enabled)
        {
            CanClean = false;
            UpdateColliderState(false);
            return;
        }

        // 1. INPUT DETECTION
        bool touchStarted = false;
        bool touchPressing = false;
        bool touchMoved = false;
        Vector2 inputPosition = Vector2.zero;
        int pointerId = -1;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = touch.position;
            pointerId = touch.fingerId;

            if (touch.phase == TouchPhase.Began) touchStarted = true;
            if (touch.phase == TouchPhase.Moved) touchMoved = true;
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                touchPressing = true;
            }
        }
        else
        {
            inputPosition = Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) touchStarted = true;
            if (Input.GetMouseButton(0)) touchPressing = true;
            if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0) touchMoved = true;
        }

        // 2. UI CLICK CHECK
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
        {
            CanClean = false;
            canFollow = false;
            isDragging = false;
            UpdateColliderState(false);
            return;
        }

        // 3. TOUCH START - Touch karte hi Hand Gesture instant stop ho jayega
        if (touchStarted || touchPressing)
        {
            if (!hasPlayerInteracted)
            {
                hasPlayerInteracted = true;
                StopHandGesture();
            }
            canFollow = true;
        }
        else
        {
            canFollow = false;
            isDragging = false;
            CanClean = false;
            transform.rotation = Quaternion.identity;
            UpdateColliderState(false);
            return;
        }

        // Position Calculation
        Vector3 screenPos = new Vector3(
            inputPosition.x,
            inputPosition.y,
            Mathf.Abs(cam.transform.position.z));

        Vector3 world = cam.ScreenToWorldPoint(screenPos);
        world.z = 0;

        Vector3 targetPos = world + (currentToolData != null ? currentToolData.toolOffset : Vector3.zero);
        Quaternion targetRot = Quaternion.identity;

        if (touchMoved || Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            isDragging = true;
        }

        if (currentToolData != null)
        {
            switch (currentToolData.movementType)
            {
                case ToolMovementType.Scrubbing:
                    float scrubOffset = Mathf.Sin(Time.time * currentToolData.scrubSpeed) * currentToolData.scrubAmount;
                    targetPos += new Vector3(scrubOffset, 0f, 0f);
                    break;

                case ToolMovementType.Spraying:
                    Vector3 sprayJitter = (Vector3)(UnityEngine.Random.insideUnitCircle * 0.02f);
                    targetPos += sprayJitter;
                    break;

                case ToolMovementType.Rotation:
                    float rotAngle = Mathf.Sin(Time.time * currentToolData.rotationSpeed) * currentToolData.rotationAmount;
                    targetRot = Quaternion.Euler(0f, 0f, rotAngle);
                    break;

                case ToolMovementType.StandardFollow:
                default:
                    break;
            }
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        CanClean = true;
        UpdateColliderState(true);
    }

    private void UpdateColliderState(bool state)
    {
        if (toolCollider != null && toolCollider.enabled != state)
        {
            toolCollider.enabled = state;
        }
    }

    public void SetTool(ToolData data)
    {
        Debug.Log("<color=green>SetTool Called! Tool Name: </color>" + (data != null ? data.name : "NULL"));

        // ADDED: Direct Reset so every new tool starts with touch enabled
        IsInputLocked = false;

        currentToolData = data;
        canFollow = false;
        isDragging = false;
        hasPlayerInteracted = false;
        transform.rotation = Quaternion.identity;

        StopHandGesture();

        if (data != null)
        {
            // 1. Ensure Main Tool GameObject is active
            gameObject.SetActive(true);

            if (toolSprite != null)
            {
                // Sprite GameObject ko active karein aur render enable karein
                toolSprite.gameObject.SetActive(true);
                toolSprite.sprite = data.toolSprite;
                toolSprite.enabled = true;

                Color c = toolSprite.color;
                c.a = 1f;
                toolSprite.color = c;
            }

            // 2. Tool ko immediately spawn point par move karein (taake drag se pehle visible ho)
            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;

                if (data.particleOffset != Vector3.zero)
                {
                    spawnPoint.localPosition = data.particleOffset;
                }
                else if (data.eraseOffset != Vector3.zero)
                {
                    spawnPoint.localPosition = data.eraseOffset;
                }
            }

            if (capSpriteRenderer != null)
            {
                capSpriteRenderer.sprite = data.capSprite;

                if (data.capSprite != null)
                {
                    capSpriteRenderer.transform.localPosition = data.capLocalPosition;
                    capSpriteRenderer.transform.localRotation = Quaternion.identity;

                    Color c = capSpriteRenderer.color;
                    c.a = 1f;
                    capSpriteRenderer.color = c;
                    capSpriteRenderer.gameObject.SetActive(true);
                }
                else
                {
                    capSpriteRenderer.gameObject.SetActive(false);
                }
            }
        }
    }

    public void SetHandGestureTarget(Transform target)
    {
        handGestureTarget = target;
    }

    // Cap Animation Call
    public void AnimateCapOff()
    {
        hasPlayerInteracted = false;
        StopHandGesture();

        if (capSpriteRenderer == null || capSpriteRenderer.sprite == null)
        {
            if (capSpriteRenderer != null) capSpriteRenderer.gameObject.SetActive(false);
            return;
        }

        if (currentToolData != null)
        {
            capSpriteRenderer.transform.localPosition = currentToolData.capLocalPosition;
        }

        capSpriteRenderer.transform.localRotation = Quaternion.identity;

        Color c = capSpriteRenderer.color;
        c.a = 1f;
        capSpriteRenderer.color = c;
        capSpriteRenderer.gameObject.SetActive(true);

        if (capCoroutine != null) StopCoroutine(capCoroutine);
        capCoroutine = StartCoroutine(CapFlyAndFadeRoutine());
    }

    private IEnumerator CapFlyAndFadeRoutine()
    {
        float duration = 0.8f;
        float elapsed = 0f;
        float fadeSpeedMultiplier = 1f;

        Vector3 startPos = capSpriteRenderer.transform.localPosition;
        Vector3 targetPos = startPos + new Vector3(0f, 2f, 0f);

        while (elapsed < duration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            float t = elapsed / duration;

            capSpriteRenderer.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            capSpriteRenderer.transform.Rotate(0f, 720f * dt, 0f, Space.Self);

            Color c = capSpriteRenderer.color;
            c.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t * fadeSpeedMultiplier));
            capSpriteRenderer.color = c;

            yield return null;
        }

        capSpriteRenderer.gameObject.SetActive(false);

        // Cap animation complete hone ke BAAD 2-Step Hand Gesture Trigger!
        if (!hasPlayerInteracted && handGestureTarget != null)
        {
            StartHandGesture();
        }
    }

    public void StartHandGesture()
    {
        if (handGestureRenderer == null || handGestureTarget == null || hasPlayerInteracted)
            return;

        StopHandGesture();
        handGestureCoroutine = StartCoroutine(HandGestureRoutine());
    }

    private IEnumerator HandGestureRoutine()
    {
        if (handGestureRenderer == null || handGestureTarget == null) yield break;

        handGestureRenderer.gameObject.SetActive(true);

        while (!hasPlayerInteracted && handGestureTarget != null)
        {
            Vector3 gluePos = transform.position; // Glue tool ki position
            Vector3 leftStartPos = gluePos + handStartOffset; // Left side position
            Vector3 targetPos = handGestureTarget.position; // Target (Purse) position

            float duration = 1.3f; // Total movement time
            float elapsed = 0f;

            handGestureRenderer.transform.position = leftStartPos;
            Color c = handGestureRenderer.color;
            c.a = 0f;
            handGestureRenderer.color = c;

            while (elapsed < duration)
            {
                if (hasPlayerInteracted || handGestureTarget == null)
                {
                    StopHandGesture();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                Vector3 currentPos;

                // 1. STEP 1 (Pehle 35% time): Left se Glue ki taraf aayega
                if (t < 0.35f)
                {
                    float tSub = t / 0.35f;
                    currentPos = Vector3.Lerp(leftStartPos, gluePos, Mathf.SmoothStep(0f, 1f, tSub));
                }
                // 2. STEP 2 (Baaki 65% time): Glue se Target point ki taraf upar jayega
                else
                {
                    float tSub = (t - 0.35f) / 0.65f;
                    currentPos = Vector3.Lerp(gluePos, targetPos, Mathf.SmoothStep(0f, 1f, tSub));
                }

                handGestureRenderer.transform.position = currentPos;

                // Smooth Fade-In and Fade-Out
                if (t < 0.15f)
                {
                    c.a = Mathf.Lerp(0f, 1f, t / 0.15f);
                }
                else if (t > 0.85f)
                {
                    c.a = Mathf.Lerp(1f, 0f, (t - 0.85f) / 0.15f);
                }
                else
                {
                    c.a = 1f;
                }

                handGestureRenderer.color = c;
                yield return null;
            }

            c.a = 0f;
            handGestureRenderer.color = c;

            yield return new WaitForSeconds(0.25f);
        }

        StopHandGesture();
    }

    public void StopHandGesture()
    {
        if (handGestureCoroutine != null)
        {
            StopCoroutine(handGestureCoroutine);
            handGestureCoroutine = null;
        }

        if (handGestureRenderer != null)
        {
            handGestureRenderer.gameObject.SetActive(false);
        }
    }

    public void HideTool()
    {
        StopHandGesture();

        if (toolSprite != null) toolSprite.enabled = false;
        if (capSpriteRenderer != null) capSpriteRenderer.gameObject.SetActive(false);

        canFollow = false;
        isDragging = false;
        hasPlayerInteracted = false;
        CanClean = false;
        IsInputLocked = false; // Safety reset on hide

        UpdateColliderState(false);
    }
}