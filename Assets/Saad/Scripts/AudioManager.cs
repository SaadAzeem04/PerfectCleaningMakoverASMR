using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
    public AudioClip buySFX; // <--- Professional Buy / Coins Falling SFX Slot

    [Header("Smooth Transition Settings")]
    [Range(0f, 1f)] public float targetMusicVolume = 0.8f;
    public float fadeDuration = 1.5f;

    [Header("Scene Identification")]
    public string homeSceneName = "HomeScene";

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isMusicOn) return;

        AudioClip clipToPlay = (scene.name == homeSceneName) ? homeScreenMusic : gameSceneMusic;

        if (clipToPlay != null)
        {
            PlayMusicWithFade(clipToPlay, targetMusicVolume, fadeDuration);
        }
    }

    public void PlayMusicWithFade(AudioClip newClip, float targetVolume, float duration)
    {
        if (musicSource == null || !isMusicOn) return;

        if (musicSource.clip == newClip && musicSource.isPlaying) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAndSwitchMusicRoutine(newClip, targetVolume, duration));
    }

    private IEnumerator FadeAndSwitchMusicRoutine(AudioClip newClip, float targetVol, float duration)
    {
        float halfDuration = duration / 2f;

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

        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

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

    public void PlayMusic(AudioClip clip)
    {
        PlayMusicWithFade(clip, targetMusicVolume, fadeDuration);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null || !isSoundOn) return;
        sfxSource.PlayOneShot(clip);
    }

    // SPECIAL BUY SFX FUNCTION
    public void PlayBuySFX()
    {
        PlaySFX(buySFX);
    }

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

    public void StopToolSFX()
    {
        if (toolSource != null && toolSource.isPlaying)
        {
            toolSource.Stop();
        }
    }

    public void ToggleSound(bool isOn)
    {
        isSoundOn = isOn;
        PlayerPrefs.SetInt("SoundOn", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (!isSoundOn && toolSource != null) toolSource.Stop();
        if (!isSoundOn && sfxSource != null) sfxSource.Stop();
    }

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