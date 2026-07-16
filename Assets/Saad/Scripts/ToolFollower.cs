using UnityEngine;
using UnityEngine.EventSystems;

public class ToolFollower : MonoBehaviour
{
    public SpriteRenderer toolSprite;
    public Transform spawnPoint;

    public bool CanClean { get; private set; }

    private bool canFollow;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;

        if (toolSprite != null)
        {
            toolSprite.enabled = false;
            toolSprite.color = new Color(1, 1, 1, 0);
        }
    }

    void Update()
    {
        if (PauseManager.IsGamePaused || !toolSprite.enabled)
            return;

        bool touchStarted = false;
        bool touchPressing = false;
        Vector2 inputPosition = Vector2.zero;
        int pointerId = -1; // Default for PC Mouse

        //  1. MOBILE NATIVE TOUCH DETECTION (Super Fast & Sensitive)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = touch.position;
            pointerId = touch.fingerId; // Mobile ki exact finger ID pakro

            if (touch.phase == TouchPhase.Began)
            {
                touchStarted = true;
            }
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                touchPressing = true;
            }
        }
        //  2. PC EDITOR TESTING BACKUP (Mouse Click)
        else
        {
            inputPosition = Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) touchStarted = true;
            if (Input.GetMouseButton(0)) touchPressing = true;
        }

        // 3. UI BLOCK CHECK (Mobile Safe Fixed! Pointer ID dena zaroori hai)
        if (touchPressing && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
        {
            CanClean = false;
            return; // Agar ungli UI button par hai to tool move nahi hoga
        }

        //  4. STARTUP SPAWN POINT LOGIC (Aapka purana system)
        if (!canFollow)
        {
            if (touchStarted)
            {
                canFollow = true;
            }
            else
            {
                return; // Jab tak pehla touch na ho, tool spawn point par hi ruka rahe
            }
        }

        // 5. RELEASE CHECK (Jab ungli utha li jaye)
        if (!touchPressing)
        {
            CanClean = false;
            return;
        }

        // 6. REAL-TIME POSITION UPDATER (Zero Latency Tracking)
        Vector3 screenPos = new Vector3(
            inputPosition.x,
            inputPosition.y,
            Mathf.Abs(cam.transform.position.z));

        Vector3 world = cam.ScreenToWorldPoint(screenPos);
        world.z = 0;

        // Direct position assignment bina kisi delay ke! Ungli ke sath chipak kar chalega.
        transform.position = world;

        CanClean = true;
    }

    public void SetTool(ToolData tool)
    {
        if (tool == null)
            return;

        toolSprite.enabled = true;
        toolSprite.sprite = tool.toolSprite;
        toolSprite.color = Color.white;

        toolSprite.transform.localPosition = tool.toolOffset;

        transform.localScale = Vector3.one * 1.5f;

        if (spawnPoint != null)
            transform.position = spawnPoint.position;

        canFollow = false;
        CanClean = false;
    }

    public void HideTool()
    {
        toolSprite.enabled = false;
        canFollow = false;
        CanClean = false;
    }
}