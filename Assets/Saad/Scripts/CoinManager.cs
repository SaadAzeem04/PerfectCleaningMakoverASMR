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
    public GameObject coinPrefab; // Ek chota UI Coin ka prefab jisme Image component ho
    public Transform coinSpawnPoint; // Jahan se coin nikalna shuru honge (e.g., Claim Button)
    public Transform coinTargetPoint; // Jahan coins ne swoop hoke jana hai (Top-Left Coin Icon)

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
        // NAYI LOGIC: Pehli Baar Download Karne Par 100 Coins Dena!
        // =========================================================================
        if (!PlayerPrefs.HasKey("TotalCoins"))
        {
            // Agar player naya hai (kabhi coins save nahi hue), to 100 coins do
            PlayerPrefs.SetInt("TotalCoins", 100);
            PlayerPrefs.Save();
            Debug.Log("New Player! Initially 100 Coins de diye gaye hain.");
        }

        // Coins data load karna PlayerPrefs se (Ab yeh 0 ke bajaye 100 uthayega!)
        currentCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        // =========================================================================
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

    // Visual Swoop/Fly Animation Function
    public void TriggerCoinSwoopAnimation(int totalCoinsToAdd)
    {
        // Agar pehle se animation chal rahi hai toh ruk jao taake double coins add na hon
        if (isAnimating) return;

        if (coinPrefab == null || coinSpawnPoint == null || coinTargetPoint == null)
        {
            AddCoins(totalCoinsToAdd);
            return;
        }

        StartCoroutine(AnimateCoinsRoutine(totalCoinsToAdd));
    }

    private IEnumerator AnimateCoinsRoutine(int totalCoinsToAdd)
    {
        isAnimating = true; // Animation shuru ho gayi, lock laga diya

        int coinVisualCount = 8;
        RectTransform targetRect = coinTargetPoint.GetComponent<RectTransform>();

        for (int i = 0; i < coinVisualCount; i++)
        {
            GameObject spawnedCoin = Instantiate(coinPrefab, coinSpawnPoint.position, Quaternion.identity, coinSpawnPoint.parent);

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

        // Exact fix value add hogi jab loop khatam ho jaye
        AddCoins(totalCoinsToAdd);

        // Thora sa stay karwaen ge taake animation smooth end ho jaye
        yield return new WaitForSeconds(0.5f);

        isAnimating = false; // Lock khol diya agle level ke liye
    }

    private IEnumerator MoveCoinToTargetUI(GameObject coin, RectTransform targetRect, Vector3 offset)
    {
        float time = 0f;
        float duration = 0.5f; // Coin udne ka waqt

        RectTransform coinRect = coin.GetComponent<RectTransform>();
        Vector3 startScreenPos = coin.transform.position;

        // Shuruat mein thoda phelna
        if (coinRect != null)
        {
            coinRect.anchoredPosition += (Vector2)offset;
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Smooth curve movement
            t = t * t * (3f - 2f * t);

            if (coin != null)
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