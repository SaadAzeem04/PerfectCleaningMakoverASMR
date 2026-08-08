using UnityEngine;
using UnityEngine.SceneManagement; // Scene loading detection ke liye
using System.Collections;          // Coroutine ke liye

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource toolSource;

    [Header("Audio Clips")]
    public AudioClip homeScreenMusic;
    public AudioClip gameSceneMusic;
    public AudioClip layerClearSFX;

    [Header("Smooth Transition Settings")]
    [Range(0f, 1f)] public float targetMusicVolume = 0.8f; // BGM ki maximum volume
    public float fadeDuration = 1.5f;                       // Slow increase/fade ka duration (seconds mein)

    [Header("Scene Identification")]
    public string homeSceneName = "HomeScene"; // Inspector mein apne Home scene ka exact naam rakhein

    public bool isSoundOn = true;
    public bool isMusicOn = true;

    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAudioSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Har Scene Load hone par check karega aur smoothly music change/increase karega
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isMusicOn) return;

        // Check karein ke Home Scene hai ya Game Scene
        AudioClip clipToPlay = (scene.name == homeSceneName) ? homeScreenMusic : gameSceneMusic;

        if (clipToPlay != null)
        {
            PlayMusicWithFade(clipToPlay, targetMusicVolume, fadeDuration);
        }
    }

    // Smooth Fade-Out -> Music Switch -> Slowly Increase (Fade-In) Function
    public void PlayMusicWithFade(AudioClip newClip, float targetVolume, float duration)
    {
        if (musicSource == null || !isMusicOn) return;

        // Agar same clip pehle se chal raha hai aur playing hai tou dobara restart na karein
        if (musicSource.clip == newClip && musicSource.isPlaying) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAndSwitchMusicRoutine(newClip, targetVolume, duration));
    }

    private IEnumerator FadeAndSwitchMusicRoutine(AudioClip newClip, float targetVol, float duration)
    {
        float halfDuration = duration / 2f;

        // 1. Agar pehle se koi music chal raha hai, tou pehle usay smoothly FADE OUT karein
        if (musicSource.isPlaying)
        {
            float startVol = musicSource.volume;
            float timer = 0f;

            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, timer / halfDuration);
                yield return null;
            }
        }

        // 2. Naya Clip set karein aur Volume 0 se start karein
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        // 3. Volume ko SLOWLY INCREASE (Fade-In) karein target volume tak
        float fadeInTimer = 0f;
        while (fadeInTimer < halfDuration)
        {
            fadeInTimer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVol, fadeInTimer / halfDuration);
            yield return null;
        }

        musicSource.volume = targetVol;
    }

    private void LoadAudioSettings()
    {
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        if (musicSource != null)
        {
            if (!isMusicOn) musicSource.Stop();
            else if (!musicSource.isPlaying && musicSource.clip != null) musicSource.Play();
        }
    }

    // 1. Direct Music Play (Backward Compatibility)
    public void PlayMusic(AudioClip clip)
    {
        PlayMusicWithFade(clip, targetMusicVolume, fadeDuration);
    }

    // 2. Simple SFX Chalane Ka Function
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null || !isSoundOn) return;
        sfxSource.PlayOneShot(clip);
    }

    // 3. Looping SFX Chalane Ka Function
    public void PlayLoopingSFX(AudioClip clip, bool shouldLoop)
    {
        if (toolSource == null || clip == null || !isSoundOn) return;

        if (toolSource.clip != clip)
        {
            toolSource.clip = clip;
            toolSource.loop = shouldLoop;
            toolSource.Play();
        }
        else if (!toolSource.isPlaying)
        {
            toolSource.loop = shouldLoop;
            toolSource.Play();
        }
    }

    // 4. Tool Ki Sound Rokne Ka Function
    public void StopToolSFX()
    {
        if (toolSource != null && toolSource.isPlaying)
        {
            toolSource.Stop();
        }
    }

    // Sound ON/OFF
    public void ToggleSound(bool isOn)
    {
        isSoundOn = isOn;
        PlayerPrefs.SetInt("SoundOn", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (!isSoundOn && toolSource != null) toolSource.Stop();
        if (!isSoundOn && sfxSource != null) sfxSource.Stop();
    }

    // Music ON/OFF
    public void ToggleMusic(bool isOn)
    {
        isMusicOn = isOn;
        PlayerPrefs.SetInt("MusicOn", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (!isMusicOn && musicSource != null)
        {
            musicSource.Stop();
        }
        else if (isMusicOn && musicSource != null && !musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.Play();
        }
    }
}