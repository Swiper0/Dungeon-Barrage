using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public Sound[] musicSounds;
    public AudioSource musicSource;
    public AudioSource SFXSource;
    private Coroutine activeFade;

    public static AudioManager Instance;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const float MAX_VOLUME_LIMIT = 0.3f;

    [Header("Audio Settings")]
    public float fadeInDuration = 3f; 

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadMusicVolume();
            LoadSFXVolume();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
{
    PlayMusic("Backsound");
}

    public void PlayMusic(string name)
    {
        Sound s = System.Array.Find(musicSounds, x => x.name == name);

        if (s == null)
        {
            Debug.LogWarning("Sound Not Found: " + name);
            return;
        }

        musicSource.Stop(); // 🔥 STOP DULU
        musicSource.clip = s.clip;
        musicSource.Play(); // 🔥 PLAY DARI AWAL
    }

    public void MusicVolume(float volume)
    {
        if (activeFade != null) StopCoroutine(activeFade);
        
        SaveMusicVolume(volume);
        musicSource.volume = volume * MAX_VOLUME_LIMIT;
    }

    public void SFXVolume(float volume)
    {
        SaveSFXVolume(volume);
        SFXSource.volume = volume * MAX_VOLUME_LIMIT;
    }

    public void ToggleMusic()
    {
        musicSource.mute = !musicSource.mute;
    }

    public void ToggleSFX()
    {
        SFXSource.mute = !SFXSource.mute;
    }

    private void LoadMusicVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1.0f);
        musicSource.volume = savedVolume * MAX_VOLUME_LIMIT;
    }

    private void LoadSFXVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1.0f);
        SFXSource.volume = savedVolume * MAX_VOLUME_LIMIT;
    }

    public void RefreshMusicVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1.0f);
        musicSource.volume = savedVolume * MAX_VOLUME_LIMIT;
    }

    public void StartFadeIn(float duration)
    {
        if (activeFade != null) StopCoroutine(activeFade);
        activeFade = StartCoroutine(FadeInMusicRoutine(duration));
    }

    private IEnumerator FadeInMusicRoutine(float duration)
    {
        float timer = 0;
        float savedVolumePercent = PlayerPrefs.GetFloat(MusicVolumeKey, 1.0f);
        float targetFinalVolume = savedVolumePercent * MAX_VOLUME_LIMIT;

        musicSource.volume = 0;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Gunakan unscaled agar tidak berhenti saat Time.timeScale = 0
            musicSource.volume = Mathf.Lerp(0, targetFinalVolume, timer / duration);
            yield return null;
        }
        musicSource.volume = targetFinalVolume;
        activeFade = null;
    }

    public void PlaySFX(string name, float volumeMultiplier = 1f)
{
    Sound s = System.Array.Find(musicSounds, x => x.name == name);
    if (s == null)
    {
        Debug.LogWarning("SFX Not Found: " + name);
        return;
    }

    // 🔥 UNTUK SWORD & CHARGED → OVERLAP (AUDIOSOURCE BARU, IGNORE TIMESCALE)
    if (name == "Sword" || name == "Charged")
    {
        GameObject temp = new GameObject("TempSFX");
        temp.transform.SetParent(transform);
        AudioSource tempSource = temp.AddComponent<AudioSource>();
        tempSource.clip = s.clip;
        tempSource.volume = SFXSource.volume * volumeMultiplier;
        tempSource.ignoreListenerPause = true; // 🔥 IGNORE TIMESCALE
        tempSource.Play();
        Destroy(temp, s.clip.length);
    }
    else
    {
        SFXSource.PlayOneShot(s.clip, volumeMultiplier);
    }
}

public void PlayClickSFX()
{
    PlaySFX("Click");
}

public void PlaySFXOverlap(string name, float volumeMultiplier = 1f)
{
    Sound s = System.Array.Find(musicSounds, x => x.name == name);
    if (s == null)
    {
        Debug.LogWarning("SFX Not Found: " + name);
        return;
    }

    // 🔥 BUAT AUDIOSOURCE SEMENTARA UNTUK OVERLAP
    GameObject temp = new GameObject("TempSFX");
    temp.transform.SetParent(transform);
    AudioSource tempSource = temp.AddComponent<AudioSource>();
    tempSource.clip = s.clip;
    tempSource.volume = SFXSource.volume * volumeMultiplier;
    tempSource.Play();
    Destroy(temp, s.clip.length); // 🔥 HANCURKAN SETELAH SELESAI
}

    private void SaveMusicVolume(float volume) { PlayerPrefs.SetFloat(MusicVolumeKey, volume); PlayerPrefs.Save(); }
    private void SaveSFXVolume(float volume) { PlayerPrefs.SetFloat(SFXVolumeKey, volume); PlayerPrefs.Save(); }
}