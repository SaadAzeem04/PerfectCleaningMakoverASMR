using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource toolSource;

    [Header("Audio Clips")]
    public AudioClip homeScreenMusic; // Fixed: Iska naam 'homeScreenMusic' hona chahiye tha
    public AudioClip gameSceneMusic;
    public AudioClip layerClearSFX;

    public bool isSoundOn = true;
    public bool isMusicOn = true;

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

    // 1. Music Chalane Ka Function
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null || !isMusicOn) return;

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    //  2. Simple SFX Chalane Ka Function (Jo error CS1061 line 712 ko fix karega)
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null || !isSoundOn) return;
        sfxSource.PlayOneShot(clip);
    }

    //  3. Looping SFX Chalane Ka Function (Jo Spray/Brush ki sound ko repeat karta hai - Line 394 & 413 fix)
    // Bool logic ke sath updated function jo error CS1503 ko khatam karega:
    public void PlayLoopingSFX(AudioClip clip, bool shouldLoop)
    {
        if (toolSource == null || clip == null || !isSoundOn) return;

        if (toolSource.clip != clip)
        {
            toolSource.clip = clip;
        }

        // Code se jo true/false aa raha hai, usay direct loops par apply karein
        toolSource.loop = shouldLoop;

        if (!toolSource.isPlaying)
        {
            toolSource.Play();
        }
    }

    //  4. Tool Ki Sound Rokne Ka Function (Jo Pause karne par kaam aayega)
    public void StopToolSFX()
    {
        if (toolSource != null && toolSource.isPlaying)
        {
            toolSource.Stop();
        }
    }

    // 1. Sound (SFX aur Tool) ko ON/OFF karne ka function
    public void ToggleSound(bool isOn)
    {
        isSoundOn = isOn;
        PlayerPrefs.SetInt("SoundOn", isOn ? 1 : 0);
        PlayerPrefs.Save();

        // Agar sound OFF ki hai, toh chalte hue tool ki sound ko FORAN band karo
        if (!isSoundOn && toolSource != null)
        {
            toolSource.Stop();
        }
        if (!isSoundOn && sfxSource != null)
        {
            sfxSource.Stop();
        }
    }

    //  2. Music (Background BGM) ko ON/OFF karne ka function
    public void ToggleMusic(bool isOn)
    {
        isMusicOn = isOn;
        PlayerPrefs.SetInt("MusicOn", isOn ? 1 : 0);
        PlayerPrefs.Save();

        // Agar music OFF kiya hai, toh background music ko FORAN roko
        if (!isMusicOn && musicSource != null)
        {
            musicSource.Stop();
        }
        // Agar music ON kiya hai aur pehle se nahi chal raha, toh dubara play karo
        else if (isMusicOn && musicSource != null && !musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.Play();
        }
    }
}