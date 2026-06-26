using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI Elements")]
    public GameObject gameOverPanel;
    public Text waveReachedText;
    public Text highestWaveText;
    public Button playAgainButton;
    public Button homeButton;

    [Header("Animation")]
    public float fadeInDuration = 0.5f;
    public CanvasGroup canvasGroup;

    [Header("Scene References")]
    public PlayerTouchMove playerTouchMove;
    public PlayerCombat playerCombat;
    public WaveManager waveManager;

    private const string HIGHEST_WAVE_KEY = "HighestWave";
    private int currentHighestWave = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Start()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(PlayAgain);

        if (homeButton != null)
            homeButton.onClick.AddListener(GoHome);

        if (playerTouchMove == null)
            playerTouchMove = FindObjectOfType<PlayerTouchMove>();
        if (playerCombat == null)
            playerCombat = FindObjectOfType<PlayerCombat>();
        if (waveManager == null)
            waveManager = FindObjectOfType<WaveManager>();

        currentHighestWave = PlayerPrefs.GetInt(HIGHEST_WAVE_KEY, 0);
    }

    public void ShowGameOver(int currentWave)
    {
        // 🔥 DISABLE PLAYER INPUT
        if (playerTouchMove != null)
        {
            playerTouchMove.DisableInput();
        }
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (waveReachedText != null)
                waveReachedText.text = $"Wave: {currentWave}";

            bool isNewRecord = SaveHighestWave(currentWave);
            
            if (highestWaveText != null)
            {
                if (isNewRecord)
                {
                    highestWaveText.text = $"NEW RECORD! Wave {currentWave}";
                    highestWaveText.color = Color.yellow;
                }
                else
                {
                    highestWaveText.text = $"Best: {currentHighestWave}";
                    highestWaveText.color = Color.white;
                }
            }

            if (canvasGroup != null)
                StartCoroutine(FadeIn());
        }

        // 🔥 HIDE WAVE UI SAAT PLAYER MATI
        if (WaveUIManager.Instance != null)
            WaveUIManager.Instance.HideWaveUI();

        Time.timeScale = 0f;
    }

    private bool SaveHighestWave(int currentWave)
    {
        if (currentWave > currentHighestWave)
        {
            currentHighestWave = currentWave;
            PlayerPrefs.SetInt(HIGHEST_WAVE_KEY, currentWave);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void PlayAgain()
    {
        ZenithBoss.ResetAppearanceCount();
    AetherBoss.ResetAppearanceCount();
    NexusBoss.ResetAppearanceCount();
        
        // Hide game over panel
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

         if (PowerUpManager.Instance != null)
        PowerUpManager.Instance.ResetAllPowerUps();

        // Reset time scale
        Time.timeScale = 1f;

        DestroyAllEnemiesAndBullets();
        ResetPlayerFull();

        if (waveManager != null)
        {
            waveManager.StopAllCoroutines();
            waveManager.currentWave = 0;
            waveManager.enemiesAlive = 0;
        }

        WaveUIManager waveUI = FindObjectOfType<WaveUIManager>();
        if (waveUI != null)
            waveUI.HideWaveUI();

        if (FormationManager.Instance != null)
            FormationManager.Instance.ResetAllSlots();

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();

        if (waveManager != null)
            waveManager.StartWaves();

        if (playerCombat != null)
        {
            playerCombat.SetAbsorbMode(true);
            playerCombat.ResetAbsorbBarInstant();
        }

        // 🔥 RESTART MUSIK DARI AWAL
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("Battle");
            AudioManager.Instance.StartFadeIn(3f);
        }

    }

    public void GoHome()
{


    // 🔥 SHOW MAIN MENU BUTTONS SEBELUM RELOAD
    MainMenuUI mainMenu = FindObjectOfType<MainMenuUI>();
    if (mainMenu != null)
        mainMenu.ShowAllButtons();

    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.PlayMusic("Backsound");
    }

    if (PowerUpManager.Instance != null)
        PowerUpManager.Instance.ResetAllPowerUps();

    Time.timeScale = 1f;
    UnityEngine.SceneManagement.SceneManager.LoadScene(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
    );
}

    void DestroyAllEnemiesAndBullets()
    {
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        foreach (EnemyBase e in enemies)
            if (e != null) Destroy(e.gameObject);
        
        Bullet[] bullets = FindObjectsOfType<Bullet>();
        foreach (Bullet b in bullets)
            if (b != null) Destroy(b.gameObject);
        
        HomingBullet[] homingBullets = FindObjectsOfType<HomingBullet>();
        foreach (HomingBullet hb in homingBullets)
            if (hb != null) Destroy(hb.gameObject);
    }

    void ResetPlayerFull()
    {
        if (playerCombat != null)
        {
            playerCombat.FullReset(); // 🔥 SUDAH TERMASUK RESET WARNA
        }

        if (playerTouchMove != null)
        {
            playerTouchMove.transform.position = new Vector3(0f, -3.74f, 0f);
            playerTouchMove.ResetForNewGame();
            
            SpriteRenderer sr = playerTouchMove.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;
            
            Collider2D col = playerTouchMove.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
            
            playerTouchMove.enabled = true;
            playerTouchMove.EnableInput();
        }

    }
}