using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject aboutPanel;
    
    [Header("Buttons to Hide/Show")]
    public GameObject settingButton;
    public GameObject shopButton;
    public Button closeAboutButton;
    public GameObject aboutButton;
    public GameObject htpButton;

    [Header("Coin UI")]
    public Text coinText;

    [Header("Highest Wave UI")]
    public GameObject highestWavePanel;
    public Text waveNumberText;

    [Header("Coin Earned Popup")]
    public GameObject coinEarnedParent;
    public CanvasGroup coinEarnedCanvasGroup;
    public Text coinEarnedText;
    private Coroutine coinPopupCoroutine;
    
    void Start()
    {
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (closeAboutButton != null) closeAboutButton.onClick.AddListener(CloseAbout);
        
        if (settingButton != null) settingButton.SetActive(true);
        if (shopButton != null) shopButton.SetActive(true);
        if (aboutButton != null) aboutButton.SetActive(true);
        if (htpButton != null) htpButton.SetActive(true);

        // 🔥 SETUP COIN EARNED - HIDE DI AWAL
        if (coinEarnedParent != null)
        {
            if (coinEarnedCanvasGroup == null)
                coinEarnedCanvasGroup = coinEarnedParent.GetComponent<CanvasGroup>();
            if (coinEarnedCanvasGroup == null)
                coinEarnedCanvasGroup = coinEarnedParent.AddComponent<CanvasGroup>();
            coinEarnedCanvasGroup.alpha = 0f;
        }

        // 🔥 HIGHEST WAVE MUNCUL DI MAIN MENU
        UpdateHighestWaveUI();

        UpdateCoinUI();

        if (SkinManager.Instance != null)
            SkinManager.Instance.OnCoinsUpdated += UpdateCoinUI;
    }

    void OnDestroy()
    {
        if (SkinManager.Instance != null)
            SkinManager.Instance.OnCoinsUpdated -= UpdateCoinUI;
    }

    void UpdateCoinUI()
    {
        if (coinText != null && SkinManager.Instance != null)
            coinText.text = SkinManager.Instance.GetCoins().ToString();
    }

    void UpdateHighestWaveUI()
    {
        if (waveNumberText != null)
        {
            int highest = PlayerPrefs.GetInt("HighestWave", 0);
            waveNumberText.text = highest.ToString();
        }
        if (highestWavePanel != null)
            highestWavePanel.SetActive(true); // 🔥 MUNCUL DI MAIN MENU
    }

    // 🔥 HIDE SAAT GAME MULAI (HIGHEST WAVE IKUT HIDE)
    public void HideAllButtons()
    {
        if (aboutButton != null) aboutButton.SetActive(false);
        if (settingButton != null) settingButton.SetActive(false);
        if (shopButton != null) shopButton.SetActive(false);
        if (highestWavePanel != null) highestWavePanel.SetActive(false);
        if (htpButton != null) htpButton.SetActive(false); // 🔥 HIDE SAAT WAVE DIMULAI
    }

    // 🔥 SHOW SAAT KEMBALI KE MAIN MENU
    public void ShowAllButtons()
    {
        if (aboutButton != null) aboutButton.SetActive(true);
        if (settingButton != null) settingButton.SetActive(true);
        if (shopButton != null) shopButton.SetActive(true);
        if (highestWavePanel != null) highestWavePanel.SetActive(true);
        if (htpButton != null) htpButton.SetActive(true); // 🔥 MUNCUL LAGI
        UpdateHighestWaveUI(); // 🔥 UPDATE ANGKA
    }

    // 🔥 COIN EARNED POPUP
    public void ShowCoinEarned(int amount)
    {
        if (coinEarnedParent != null)
        {
            if (coinEarnedText != null)
                coinEarnedText.text = $"+{amount}";
            
            if (coinPopupCoroutine != null)
                StopCoroutine(coinPopupCoroutine);
            coinPopupCoroutine = StartCoroutine(FlashCoinEarned());
        }
    }

    IEnumerator FlashCoinEarned()
    {
        float elapsed = 0f;
        float fadeInDuration = 0.3f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            coinEarnedCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        coinEarnedCanvasGroup.alpha = 1f;
        
        yield return new WaitForSecondsRealtime(2f);
        
        elapsed = 0f;
        float fadeOutDuration = 0.5f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            coinEarnedCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        coinEarnedCanvasGroup.alpha = 0f;
    }
    
    public void OpenAbout()
    {
        if (aboutPanel != null) aboutPanel.SetActive(true);
        if (settingButton != null) settingButton.SetActive(false);
        if (shopButton != null) shopButton.SetActive(false);
        if (aboutButton != null) aboutButton.SetActive(false);
        if (htpButton != null) htpButton.SetActive(false);
        AudioManager.Instance?.PlaySFX("Click");
    }
    
    public void CloseAbout()
    {
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (settingButton != null) settingButton.SetActive(true);
        if (shopButton != null) shopButton.SetActive(true);
        if (aboutButton != null) aboutButton.SetActive(true);
        if (htpButton != null) htpButton.SetActive(true);
        AudioManager.Instance?.PlaySFX("Click");
    }
}