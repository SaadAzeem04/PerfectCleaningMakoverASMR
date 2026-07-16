using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // UI Toggles use karne ke liye yeh line lazmi hai

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuPanel;
    public static bool IsGamePaused = false;

    [Header("Audio Toggles (Game Scene)")]
    public Toggle soundToggle; //  Inspector mein Sound Checkbox yahan drag karein
    public Toggle musicToggle; //  Inspector mein Music Checkbox yahan drag karein

    void Awake()
    {
        IsGamePaused = false;
    }

    void Start()
    {
        IsGamePaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;

        //  CODE LISTENERS SETUP: Game start hote hi toggles ko purani settings par set karein aur functions connect karein
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

    // Jab player Sound Toggle ka checkmark badlega
    private void OnSoundToggleChanged(bool isOn)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleSound(isOn);
        }
    }

    // Jab player Music Toggle ka checkmark badlega
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
        IsGamePaused = false;
        Debug.Log("GAME RESUMED! IsGamePaused is now: " + IsGamePaused);

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void GoToHome()
    {
        IsGamePaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("HomeScene");
    }
}