using UnityEngine;
using System;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool gameStarted = false;
    public event Action OnGameStarted;

    [Header("UI")]
    public GameObject gameplayUI;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (gameplayUI != null)
        {
            gameplayUI.SetActive(false);
            canvasGroup = gameplayUI.GetComponent<CanvasGroup>();
        }
    }

    public void StartGame()
    {
        if (gameStarted) return;

        gameStarted = true;
        Time.timeScale = 1f;

        if (gameplayUI != null)
        {
            gameplayUI.SetActive(true);

            if (canvasGroup != null)
                StartCoroutine(FadeIn());
        }

        OnGameStarted?.Invoke();
    }

    public void StopGame()
    {
        gameStarted = false;
        Time.timeScale = 0f;

        if (gameplayUI != null)
            gameplayUI.SetActive(false);
    }

    IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}