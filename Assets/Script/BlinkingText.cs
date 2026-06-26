using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BlinkingText : MonoBehaviour
{
    public float blinkSpeed = 0.5f;

    private Text text;
    private Coroutine blinkRoutine;

    void Start()
    {
        text = GetComponent<Text>();
        
        if (text != null)
            text.enabled = true;
        
        // 🔥 Subscribe event (jika GameManager sudah ada)
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStarted += HideInstant;

        // 🔥 LANGSUNG MULAI BLINKING
        StartBlinking();
    }

    void OnEnable()
    {
        if (text == null)
            text = GetComponent<Text>();
        
        if (text != null)
            text.enabled = true;
        
        // Subscribe event lagi
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStarted -= HideInstant; // Hindari double subscribe
            GameManager.Instance.OnGameStarted += HideInstant;
        
        // 🔥 MULAI BLINKING SAAT ENABLED
        StartBlinking();
    }

    void OnDisable()
    {
        StopBlinking();
        
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStarted -= HideInstant;
    }

    void OnDestroy()
    {
        StopBlinking();
        
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStarted -= HideInstant;
    }

    void HideInstant()
    {
        StopBlinking();

        if (text != null)
            text.enabled = false;
    }

    // 🔥 MULAI BLINKING
    void StartBlinking()
    {
        // Hentikan yang lama dulu
        StopBlinking();
        
        // Mulai baru
        blinkRoutine = StartCoroutine(Blink());
    }

    // 🔥 HENTIKAN BLINKING
    void StopBlinking()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
    }

    IEnumerator Blink()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(blinkSpeed);
            
            if (text != null)
                text.enabled = !text.enabled;
        }
    }
}