using UnityEngine;
using TMPro;

public class HomeCoinDisplay : MonoBehaviour
{
    public TMP_Text homeCoinText; // Home screen ka Coin_Text yahan lagayein

    void Start()
    {

        if (AudioManager.Instance != null) { AudioManager.Instance.PlayMusic(AudioManager.Instance.homeScreenMusic); }
        // Jaise hi Home Scene load ho, fauran check karein aur UI refresh karein
        RefreshHomeCoins();
    }

    void OnEnable()
    {
        // Agar scene background se wapas samne aaye, tab bhi refresh ho
        RefreshHomeCoins();
    }

    public void RefreshHomeCoins()
    {
        // PlayerPrefs se latest aur taaza coins data uthao
        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        if (homeCoinText != null)
        {
            homeCoinText.text = totalCoins.ToString();
            Debug.Log("Home Screen Coins Refreshed: " + totalCoins);
        }
        else
        {
            Debug.LogError("Home Screen ka Coin Text assign nahi hua wa!");
        }
    }
}