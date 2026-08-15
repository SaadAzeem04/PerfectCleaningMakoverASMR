using UnityEngine;
using UnityEngine.UI;

public class HomeSettingsManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;
    public GameObject getDiamondPanel; // Get Diamond Panel slot
    public GameObject getCoinsPanel;   // NEW: Get Coins Panel slot

    [Header("Audio Toggles")]
    public Toggle soundToggle;
    public Toggle musicToggle;

    void Start()
    {
        // Shuru mein panels band rahay
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (getDiamondPanel != null) getDiamondPanel.SetActive(false);
        if (getCoinsPanel != null) getCoinsPanel.SetActive(false); // Start mein Coins Panel hide rahega

        // Toggles ko purani saved settings par set karein
        if (soundToggle != null)
        {
            soundToggle.isOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        }

        if (musicToggle != null)
        {
            musicToggle.isOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleSound(isOn);
        }
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMusic(isOn);
        }
    }

    // Settings Panel Functions
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // Get Diamond Panel Functions (Home Scene)
    public void OpenGetDiamondPanel()
    {
        if (getDiamondPanel != null) getDiamondPanel.SetActive(true);
    }

    public void CloseGetDiamondPanel()
    {
        if (getDiamondPanel != null) getDiamondPanel.SetActive(false);
    }

    //  Get Coins Panel Functions (Home Scene - NEW)
    public void OpenGetCoinsPanel()
    {
        if (getCoinsPanel != null) getCoinsPanel.SetActive(true);
    }

    public void CloseGetCoinsPanel()
    {
        if (getCoinsPanel != null) getCoinsPanel.SetActive(false);
    }
}