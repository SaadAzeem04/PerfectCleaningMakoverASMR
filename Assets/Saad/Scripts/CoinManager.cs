using UnityEngine;
using TMPro;
using System.Collections;

public class CoinManager : MonoBehaviour
{
    private bool isAnimating = false;
    public static CoinManager Instance;

    [Header("UI References")]
    public TMP_Text globalCoinText; // Home screen ya level complete screen ka coin text

    [Header("Animation Settings")]
    public GameObject coinPrefab;     // Ek chota UI Coin ka prefab jisme Image component ho
    public Transform coinSpawnPoint;  // Jahan se coin nikalna shuru honge (e.g., Claim Button ya Center)
    public Transform coinTargetPoint; // Jahan coins ne swoop hoke jana hai (Top Coin Icon Bar)

    private int currentCoins;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // =========================================================================
        // PEHLI BAAR DOWNLOAD PAR 100 COINS DENA
        // =========================================================================
        if (!PlayerPrefs.HasKey("TotalCoins"))
        {
            PlayerPrefs.SetInt("TotalCoins", 100);
            PlayerPrefs.Save();
            Debug.Log("New Player! Initially 100 Coins de diye gaye hain.");
        }

        currentCoins = PlayerPrefs.GetInt("TotalCoins", 100);
    }

    void Start()
    {
        UpdateCoinUI();
    }

    public void UpdateCoinUI()
    {
        if (globalCoinText != null)
        {
            globalCoinText.text = currentCoins.ToString();
        }
    }

    // Coins add karne aur save karne ka function
    public void AddCoins(int amount)
    {
        currentCoins += amount;
        PlayerPrefs.SetInt("TotalCoins", currentCoins);
        PlayerPrefs.Save();
        UpdateCoinUI();
    }

    // =========================================================================
    // NAYA SEQUENCE ROUTINE: WIN PANEL -> COIN BAR -> COIN SWOOP -> NEXT STEP
    // =========================================================================
    public IEnumerator PlayCoinSequenceRoutine(GameObject coinPanelUI, GameObject winPanelUI, float delayBeforeStart)
    {
        yield return new WaitForSeconds(delayBeforeStart);

        // 1. PEHLE WIN PANEL / LEVEL COMPLETE WINDOW SHOW KAREIN
        if (winPanelUI != null)
        {
            winPanelUI.SetActive(true);
        }

        // Win Panel slide / open hone ka waqt
        yield return new WaitForSeconds(0.4f);

        // 2. COIN BAR SHOW & BRING TO FRONT
        if (coinPanelUI != null)
        {
            coinPanelUI.SetActive(true);
            coinPanelUI.transform.SetAsLastSibling();
        }

        yield return new WaitForSeconds(0.2f);

        // 3. TRIGGER SWOOP ANIMATION (Window khulne ke BAAD)
        TriggerCoinSwoopAnimation(20);

        // Coins ud kar bar mein jaane ka time
        yield return new WaitForSeconds(1.2f);

        // 4. NOTIFY GAME STEP CONTROLLER
        if (GameStepController.Instance != null)
        {
            GameStepController.Instance.OnStepFinishedFromMinigame();
        }
    }

    // Visual Swoop/Fly Animation Function
    public void TriggerCoinSwoopAnimation(int totalCoinsToAdd)
    {
        if (isAnimating) return;

        // Safety Null Checks
        if (coinPrefab == null)
        {
            Debug.LogWarning("CoinManager: coinPrefab Inspector mein missing hai! Direct Coins add kar rahe hain.");
            AddCoins(totalCoinsToAdd);
            return;
        }

        if (coinSpawnPoint == null || coinTargetPoint == null)
        {
            Debug.LogWarning("CoinManager: coinSpawnPoint ya coinTargetPoint Missing hai! Direct Coins add kar rahe hain.");
            AddCoins(totalCoinsToAdd);
            return;
        }

        StartCoroutine(AnimateCoinsRoutine(totalCoinsToAdd));
    }

    private IEnumerator AnimateCoinsRoutine(int totalCoinsToAdd)
    {
        isAnimating = true;

        int coinVisualCount = 8;
        RectTransform targetRect = coinTargetPoint.GetComponent<RectTransform>();
        Transform parentCanvas = (targetRect != null) ? targetRect.parent : coinSpawnPoint.parent;

        Vector3 spawnPos = (coinSpawnPoint != null && coinSpawnPoint.gameObject.activeInHierarchy)
                            ? coinSpawnPoint.position
                            : new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        for (int i = 0; i < coinVisualCount; i++)
        {
            GameObject spawnedCoin = Instantiate(coinPrefab, spawnPos, Quaternion.identity, parentCanvas);
            spawnedCoin.transform.SetAsLastSibling();

            RectTransform coinRect = spawnedCoin.GetComponent<RectTransform>();
            if (coinRect != null)
            {
                coinRect.anchorMin = new Vector2(0.5f, 0.5f);
                coinRect.anchorMax = new Vector2(0.5f, 0.5f);
                coinRect.pivot = new Vector2(0.5f, 0.5f);
            }

            Vector3 randomOffset = new Vector3(Random.Range(-50f, 50f), Random.Range(-50f, 50f), 0f);
            StartCoroutine(MoveCoinToTargetUI(spawnedCoin, targetRect, randomOffset));

            yield return new WaitForSeconds(0.05f);
        }

        AddCoins(totalCoinsToAdd);
        yield return new WaitForSeconds(0.5f);

        isAnimating = false;
    }

    private IEnumerator MoveCoinToTargetUI(GameObject coin, RectTransform targetRect, Vector3 offset)
    {
        float time = 0f;
        float duration = 0.5f;

        RectTransform coinRect = coin.GetComponent<RectTransform>();
        Vector3 startScreenPos = coin.transform.position;

        if (coinRect != null)
        {
            coinRect.anchoredPosition += (Vector2)offset;
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Smooth curve movement (Ease In Out)
            t = t * t * (3f - 2f * t);

            if (coin != null && targetRect != null)
            {
                coin.transform.position = Vector3.Lerp(startScreenPos, targetRect.position, t);
            }
            yield return null;
        }

        if (coin != null)
        {
            Destroy(coin);
        }
    }
}