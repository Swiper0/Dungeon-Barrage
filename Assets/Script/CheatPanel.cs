using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CheatPanel : MonoBehaviour
{
    [Header("Cheat Input")]
    public InputField waveInputField;
    public Button skipButton;
    public Button nextWaveButton;
    public Button closeButton;
    
    [Header("Power-Up Cheat")]
    public Dropdown powerUpDropdown;
    public Button confirmPowerUpButton;
    public Button giveCoinsButton;
    
    private List<PowerUpData> allPowerUps = new List<PowerUpData>();
    
    void Start()
{
    if (skipButton != null)
        skipButton.onClick.AddListener(SkipToWave);
    if (nextWaveButton != null)
        nextWaveButton.onClick.AddListener(NextWave);
    if (closeButton != null)
        closeButton.onClick.AddListener(() => 
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
        });
    if (giveCoinsButton != null)
        giveCoinsButton.onClick.AddListener(GiveCoins);
    if (confirmPowerUpButton != null)
        confirmPowerUpButton.onClick.AddListener(ApplySelectedPowerUp);
}

void OnEnable()
{
    SetupPowerUpDropdown(); // 🔥 SETUP ULANG SETIAP PANEL AKTIF
    Time.timeScale = 0f;
}
    
    void SetupPowerUpDropdown()
{
    if (powerUpDropdown == null || PowerUpManager.Instance == null) return;
    
    powerUpDropdown.ClearOptions();
    allPowerUps.Clear();
    
    allPowerUps.AddRange(PowerUpManager.Instance.allCommonPowerUps);
    allPowerUps.AddRange(PowerUpManager.Instance.allRarePowerUps);
    allPowerUps.AddRange(PowerUpManager.Instance.allLegendaryPowerUps);
    
    List<string> options = new List<string>();
    foreach (var pu in allPowerUps)
    {
        string text = $"[{pu.rarity}] {pu.powerUpName}";
        if (text.Length > 30) text = text.Substring(0, 30) + "..."; // 🔥 BATASI
        options.Add(text);
    }
    
    powerUpDropdown.AddOptions(options);
}
    
    public void ApplySelectedPowerUp()
    {
        if (powerUpDropdown == null || PowerUpManager.Instance == null) return;
        
        int index = powerUpDropdown.value;
        if (index >= 0 && index < allPowerUps.Count)
        {
            PowerUpData selected = allPowerUps[index];
            // 🔥 PAKAI ApplyCheatPowerUp AGAR TIDAK TRIGGER WAVE
            PowerUpManager.Instance.ApplyCheatPowerUp(selected);
            gameObject.SetActive(false);
            Time.timeScale = 1f;
        }
    }
    
    public void SkipToWave()
{
    if (WaveManager.Instance != null && !string.IsNullOrEmpty(waveInputField.text))
    {
        if (int.TryParse(waveInputField.text, out int wave))
        {
            gameObject.SetActive(false); // 🔥 HANYA SEMBUNYIKAN CHEAT PANEL
            
            Time.timeScale = 1f;
            
            Time.timeScale = 1f;
            WaveManager.Instance.SkipToWave(wave);
        }
    }
}
    
    public void NextWave()
{
    if (WaveManager.Instance != null)
    {
        gameObject.SetActive(false);
        
        // 🔥 TUTUP OPTION PANEL JIKA TERBUKA
        if (OptionManager.Instance != null)
            OptionManager.Instance.CloseOption();
        
        Time.timeScale = 1f;
        WaveManager.Instance.NextWave();
    }
}
    
    public void GiveCoins()
    {
        if (SkinManager.Instance != null)
        {
            SkinManager.Instance.AddCoins(500);
        }
    }

    public void CloseCheatPanel()
{
    gameObject.SetActive(false);
    Time.timeScale = 1f;
}
}