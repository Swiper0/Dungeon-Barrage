using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageVignette : MonoBehaviour
{
    public static DamageVignette Instance;

    [Header("Settings")]
    public float fadeInDuration = 0.05f;   // 🔥 LEBIH CEPAT
    public float holdDuration = 0.15f;     // 🔥 SEBENTAR
    public float fadeOutDuration = 0.25f;  // 🔥 LEBIH LAMBAT
    public float maxAlpha = 0.15f;         // 🔥 SEDIKIT SAJA (25%)

    [Header("Vignette Image")]
    public Image vignetteImage;

    private Coroutine vignetteCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (vignetteImage != null)
        {
            vignetteImage.color = new Color(1, 0, 0, 0); // 🔥 RED, TRANSPARAN
            vignetteImage.raycastTarget = false;
        }
    }

    public void Flash()
    {
        if (vignetteCoroutine != null)
            StopCoroutine(vignetteCoroutine);
        vignetteCoroutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        float elapsed = 0f;

        // Fade In (Cepat)
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, maxAlpha, elapsed / fadeInDuration);
            vignetteImage.color = new Color(1, 0, 0, alpha);
            yield return null;
        }

        vignetteImage.color = new Color(1, 0, 0, maxAlpha);
        yield return new WaitForSecondsRealtime(holdDuration);

        // Fade Out (Lambat)
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(maxAlpha, 0f, elapsed / fadeOutDuration);
            vignetteImage.color = new Color(1, 0, 0, alpha);
            yield return null;
        }

        vignetteImage.color = new Color(1, 0, 0, 0);
    }
}