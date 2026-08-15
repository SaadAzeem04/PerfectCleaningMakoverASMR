using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuPanel;
    public GameObject getDiamondPanel; // In-Game Get Diamond Panel Slot
    public GameObject getCoinsPanel;   //  NEW: In-Game Get Coins Panel Slot
    public static bool IsGamePaused = false;

    [Header("Audio Toggles (Game Scene)")]
    public Toggle soundToggle;
    public Toggle musicToggle;

    void Awake()
    {
        IsGamePaused = false;
    }

    void Start()
    {
        IsGamePaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (getDiamondPanel != null) getDiamondPanel.SetActive(false);
        if (getCoinsPanel != null) getCoinsPanel.SetActive(false); // Start mein Coins Panel hide rahega

        Time.timeScale = 1f;

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

    public void PauseGame()
    {
        IsGamePaused = true;
        Debug.Log("GAME PAUSED! IsGamePaused is now: " + IsGamePaused);

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        // Sub panels ki state check karke hi game unpause hogi
        CheckAndResumeTime();
    }

    // Get Diamond Panel Functions
    public void OpenGetDiamondPanel()
    {
        IsGamePaused = true;
        Time.timeScale = 0f; // Game Pause
        if (getDiamondPanel != null) getDiamondPanel.SetActive(true);
    }

    public void CloseGetDiamondPanel()
    {
        if (getDiamondPanel != null) getDiamondPanel.SetActive(false);

        // Safety check before unpausing
        CheckAndResumeTime();
    }

    // Get Coins Panel Functions (NEW)
    public void OpenGetCoinsPanel()
    {
        IsGamePaused = true;
        Time.timeScale = 0f; // Game Pause
        if (getCoinsPanel != null) getCoinsPanel.SetActive(true);
    }

    public void CloseGetCoinsPanel()
    {
        if (getCoinsPanel != null) getCoinsPanel.SetActive(false);

        // Safety check before unpausing
        CheckAndResumeTime();
    }

    //  HELPER: Check karega ke agar koi bhi doosra panel khula nahi hai tabhi Game Unpause ho
    private void CheckAndResumeTime()
    {
        bool isPauseOpen = (pauseMenuPanel != null && pauseMenuPanel.activeSelf);
        bool isDiamondOpen = (getDiamondPanel != null && getDiamondPanel.activeSelf);
        bool isCoinsOpen = (getCoinsPanel != null && getCoinsPanel.activeSelf);

        if (!isPauseOpen && !isDiamondOpen && !isCoinsOpen)
        {
            IsGamePaused = false;
            Time.timeScale = 1f;
        }
    }

    public void GoToHome()
    {
        IsGamePaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("HomeScene");
    }
}