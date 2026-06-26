using UnityEngine;
using UnityEngine.UI;

public class OptionManager : MonoBehaviour
{
    public static OptionManager Instance;
    
    [Header("Option Panel")]
    public GameObject optionPanel;
    
    [Header("In-Game Only Buttons")]
    public GameObject homeButton;
    public GameObject playAgainButton;
    public GameObject blinkingText;
    public GameObject aboutButton;
    public GameObject shopButton;
    public GameObject htpButton;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        HideInGameButtons();
    }

    void Update()
    {
        UpdateInGameButtonsVisibility();
    }

    void UpdateInGameButtonsVisibility()
    {
        if (GameManager.Instance != null)
        {
            bool gameActive = GameManager.Instance.gameStarted;
            
            if (homeButton != null)
                homeButton.SetActive(gameActive);
                
            if (playAgainButton != null)
                playAgainButton.SetActive(gameActive);
        }
    }

    void HideInGameButtons()
    {
        if (homeButton != null)
            homeButton.SetActive(false);
            
        if (playAgainButton != null)
            playAgainButton.SetActive(false);
    

    }


    public void OpenOption()
{
    optionPanel.SetActive(true);
    Time.timeScale = 0;

    if (blinkingText != null) blinkingText.SetActive(false);

    // 🔥 HIDE ABOUT & SHOP SAAT OPTION TERBUKA (SELALU)
    if (aboutButton != null) aboutButton.SetActive(false);
    if (shopButton != null) shopButton.SetActive(false);
    if (htpButton != null) htpButton.SetActive(false);
}

public void CloseOption()
{
    optionPanel.SetActive(false);
    Time.timeScale = 1;

    // 🔥 HANYA SHOW ABOUT & SHOP JIKA GAME BELUM MULAI
    if (GameManager.Instance != null && !GameManager.Instance.gameStarted)
    {
        if (aboutButton != null) aboutButton.SetActive(true);
        if (shopButton != null) shopButton.SetActive(true);
        if (htpButton != null) htpButton.SetActive(true);
    }
    // 🔥 JIKA GAME SEDANG BERMAIN, TETAP HIDE

    if (blinkingText != null && GameManager.Instance != null && !GameManager.Instance.gameStarted)
        blinkingText.SetActive(true);
}

    public void GoHome()
    {
        optionPanel.SetActive(false);
        
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.GoHome();
        }
    }

    public void PlayAgain()
    {
        optionPanel.SetActive(false);
        
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.PlayAgain();
        }
    }
}