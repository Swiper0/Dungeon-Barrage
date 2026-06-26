using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WaveUIManager : MonoBehaviour
{
    [Header("Settings")]
    public float fadeDuration = 0.5f;

    [Header("UI GameObjects")]
    public Text[] waveCount;
    public GameObject settingButton;

    [Header("UI Canvas Groups (Wajib Dipasang di Inspector)")]
    public CanvasGroup backgroundGroup;
    public CanvasGroup normalWaveGroup;
    public CanvasGroup bossWaveGroup;
public static WaveUIManager Instance;

    [Header("Hint Text")]
public GameObject hintText;
    private Coroutine currentFadeRoutine;
    private bool wasBossWavePreviously = false;

    void Awake()
{
    Instance = this;
}

    void Start()
    {
        PrepareCanvasGroup(backgroundGroup, false);
        PrepareCanvasGroup(normalWaveGroup, false);
        PrepareCanvasGroup(bossWaveGroup, false);

        wasBossWavePreviously = false;
    }

    void PrepareCanvasGroup(CanvasGroup group, bool active)
    {
        if (group == null) return;
        group.alpha = active ? 1f : 0f;
        group.gameObject.SetActive(active);
        group.blocksRaycasts = active; 
    }


public void ShowWaveUI(int wave)
{
    SetwaveCount(wave);

    if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);

    bool isBossWave = (wave % 10 == 0);

    // 🔥 HINT TEXT: MUNCUL HANYA DI WAVE 1
    if (hintText != null)
        hintText.SetActive(wave == 1);

    if (AudioManager.Instance != null)
    {
        if (isBossWave)
        {
            AudioManager.Instance.PlayMusic("Boss");
            AudioManager.Instance.StartFadeIn(3f);
            wasBossWavePreviously = true;
        }
        else
        {
            if (wasBossWavePreviously)
            {
                AudioManager.Instance.PlayMusic("Battle");
                AudioManager.Instance.StartFadeIn(3f);
                wasBossWavePreviously = false;
            }
        }
    }

    CanvasGroup targetWaveGroup = isBossWave ? bossWaveGroup : normalWaveGroup;
    CanvasGroup otherWaveGroup = isBossWave ? normalWaveGroup : bossWaveGroup;

    PrepareCanvasGroup(otherWaveGroup, false);
    backgroundGroup.gameObject.SetActive(true);
    backgroundGroup.alpha = 0f;
    
    targetWaveGroup.gameObject.SetActive(true);
    targetWaveGroup.alpha = 0f;

    currentFadeRoutine = StartCoroutine(FadeInRoutine(backgroundGroup, targetWaveGroup));
    settingButton.SetActive(false);
}

public void HideWaveUI()
{
    if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
    
    currentFadeRoutine = StartCoroutine(FadeOutRoutine(backgroundGroup, normalWaveGroup, bossWaveGroup));
    settingButton.SetActive(true);

    // 🔥 SEMBUNYIKAN HINT TEXT
    if (hintText != null)
        hintText.SetActive(false);
}


    public void SetwaveCount(int wave)
    {
        if (waveCount != null && waveCount.Length > 0)
        {
            foreach (Text t in waveCount)
            {
                if (t != null) t.text = "WAVE " + wave;
            }
        }
    }

    IEnumerator FadeInRoutine(params CanvasGroup[] groupsToFade)
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float newAlpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);

            foreach (var group in groupsToFade)
            {
               if(group != null) group.alpha = newAlpha;
            }
            yield return null;
        }

        foreach (var group in groupsToFade)
        {
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
            }
        }
        currentFadeRoutine = null;
    }

    IEnumerator FadeOutRoutine(params CanvasGroup[] groupsToFade)
    {
        foreach (var group in groupsToFade)
        {
            if (group != null) group.blocksRaycasts = false;
        }

        float timer = 0f;
        float startAlpha = (groupsToFade.Length > 0 && groupsToFade[0] != null) ? groupsToFade[0].alpha : 1f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);

            foreach (var group in groupsToFade)
            {
               if(group != null) group.alpha = newAlpha;
            }
            yield return null;
        }

        foreach (var group in groupsToFade)
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.gameObject.SetActive(false);
            }
        }
        currentFadeRoutine = null;
    }
}