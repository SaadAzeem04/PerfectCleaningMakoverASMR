using UnityEngine;
using UnityEngine.UI;

public class HomeSettingsManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;

    [Header("Audio Toggles")]
    public Toggle soundToggle;
    public Toggle musicToggle;

    void Start()
    {
        // Shuru mein settings panel band rahay
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Toggles ko purani saved settings par set karein (taake sahi ON/OFF dikhayein)
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

    // Sound toggle ka function
    private void OnSoundToggleChanged(bool isOn)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleSound(isOn);
        }
    }

    // Music toggle ka function
    private void OnMusicToggleChanged(bool isOn)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMusic(isOn);
        }
    }

    // Settings Panel Kholne Ke Liye (Settings Button par lagayein)
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // Settings Panel Band Karne Ke Liye (Close/X Button par lagayein)
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
}