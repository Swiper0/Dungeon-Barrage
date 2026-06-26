using UnityEngine;
using System.Collections.Generic;
using System;

public class SkinManager : MonoBehaviour
{
    public static SkinManager Instance;
    
    [Header("Skins")]
    public List<SkinData> allCharacterSkins = new List<SkinData>();
    public List<SkinData> allBulletSkins = new List<SkinData>();
    public List<SkinData> ownedSkins = new List<SkinData>();
    public SkinData currentCharacterSkin;
    public SkinData currentBulletSkin;
    [HideInInspector] public Sprite currentBulletSprite;
    
    [Header("UI References")]
    public SkinShopUI shopUI;

    [Header("Override Controllers")]
    public AnimatorOverrideController defaultSkin;
    public AnimatorOverrideController eliteSkin;
    public AnimatorOverrideController legendarySkin;
    
    private const string OWNED_SKINS_KEY = "OwnedSkins";
    private const string CURRENT_CHAR_SKIN_KEY = "CurrentCharSkin";
    private const string CURRENT_BULLET_SKIN_KEY = "CurrentBulletSkin";
    private const string WAVE_30_COMPLETED_KEY = "Wave30Completed";
    private const string SECRET_SKIN_UNLOCKED_KEY = "SecretSkinUnlocked";
    
    public event Action OnCoinsUpdated;
    [Header("Main Menu Reference")]
    public MainMenuUI mainMenuUI;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        LoadData();
    }
    
    void Start()
    {
        ApplyCurrentSkin();
        ApplyBulletSkin();
    }
    
    // ========== COIN SYSTEM (PAKAI COINMANAGER) ==========
    public int GetCoins() => CoinManager.Instance != null ? CoinManager.Instance.currentCoins : 0;
    
    public bool SpendCoins(int amount)
    {
        if (CoinManager.Instance != null)
        {
            bool success = CoinManager.Instance.SpendCoins(amount);
            if (success) OnCoinsUpdated?.Invoke();
            return success;
        }
        return false;
    }
    
    public void AddCoins(int amount)
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(amount);
            OnCoinsUpdated?.Invoke();
            
            if (mainMenuUI != null)
                mainMenuUI.ShowCoinEarned(amount);
            else
                Debug.LogWarning("MainMenuUI not assigned in SkinManager!");

            AudioManager.Instance?.PlaySFX("Coin");
        }
    }
    
    public int GetCoinsDirect() => CoinManager.Instance != null ? CoinManager.Instance.currentCoins : 0;
    
    // ========== SKIN OWNERSHIP ==========
    public bool IsSkinOwned(SkinData skin)
    {
        return ownedSkins.Contains(skin);
    }
    
    public bool BuySkin(SkinData skin)
    {
        if (IsSkinOwned(skin)) return false;
        if (skin.rarity == SkinData.Rarity.Secret) return false;

        if (SpendCoins(skin.price))
        {
            ownedSkins.Add(skin);
            SaveOwnedSkins();
            // 🔥 JANGAN auto-equip, biarkan player yang pilih
            UpdateAllUI();
            return true;
        }
        return false;
    }
    
    public void UnlockSkin(SkinData skin)
    {
        if (!IsSkinOwned(skin))
        {
            ownedSkins.Add(skin);
            SaveOwnedSkins();
            // 🔥 JANGAN auto-equip, hanya unlock saja
            UpdateAllUI();
            
            Debug.Log($"Skin unlocked: {skin.skinName} - Player can equip manually");
        }
    }
    
    public void EquipSkin(SkinData skin)
    {
        if (!IsSkinOwned(skin)) return;

        if (allCharacterSkins.Contains(skin))
        {
            currentCharacterSkin = skin;
            PlayerPrefs.SetString(CURRENT_CHAR_SKIN_KEY, skin.skinName);
            PlayerPrefs.Save();
            ApplyCurrentSkin();
        }
        else if (allBulletSkins.Contains(skin))
        {
            currentBulletSkin = skin;
            PlayerPrefs.SetString(CURRENT_BULLET_SKIN_KEY, skin.skinName);
            PlayerPrefs.Save();
            ApplyBulletSkin();
        }
        
        UpdateAllUI();
    }
    
    void ApplyCurrentSkin()
    {
        if (currentCharacterSkin == null) return;
        
        PlayerTouchMove player = FindObjectOfType<PlayerTouchMove>();
        if (player != null)
        {
            SkinAnimationApplier applier = player.GetComponent<SkinAnimationApplier>();
            if (applier != null)
                applier.ApplySkin(currentCharacterSkin);
        }
    }
    
    void ApplyBulletSkin()
    {
        if (currentBulletSkin != null && currentBulletSkin.skinSprite != null)
        {
            currentBulletSprite = currentBulletSkin.skinSprite;
            Debug.Log($"Bullet skin applied: {currentBulletSkin.skinName}");
        }
        else
        {
            if (allBulletSkins.Count > 0)
            {
                currentBulletSkin = allBulletSkins.Find(s => s.skinName == "Default Bullet");
                if (currentBulletSkin == null) currentBulletSkin = allBulletSkins[0];
                currentBulletSprite = currentBulletSkin.skinSprite;
                Debug.Log($"Fallback to default bullet: {currentBulletSkin.skinName}");
            }
            else
            {
                currentBulletSprite = null;
                Debug.LogWarning("No bullet skins available!");
            }
        }
    }
    
    // ========== WAVE 30 & SECRET SKIN SYSTEM ==========
    
    public bool HasCompletedWave30()
    {
        return PlayerPrefs.GetInt(WAVE_30_COMPLETED_KEY, 0) == 1;
    }
    
    public void MarkWave30Completed()
    {
        PlayerPrefs.SetInt(WAVE_30_COMPLETED_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("Wave 30 completion saved permanently!");
    }
    
    public bool HasUnlockedSecretSkin()
    {
        return PlayerPrefs.GetInt(SECRET_SKIN_UNLOCKED_KEY, 0) == 1;
    }
    
    public bool TryUnlockSecretSkins()
    {
        // 🔥 Cek apakah secret skin sudah pernah di-unlock sebelumnya
        if (HasUnlockedSecretSkin())
        {
            Debug.Log("Secret skins already unlocked in previous session, skipping...");
            return false;
        }
        
        bool anyUnlocked = false;
        
        // Unlock character secret skins
        foreach (SkinData skin in allCharacterSkins)
        {
            if (skin.rarity == SkinData.Rarity.Secret && !IsSkinOwned(skin))
            {
                UnlockSkin(skin); // 🔥 Hanya unlock, TIDAK auto-equip
                anyUnlocked = true;
                Debug.Log($"🎉 Secret character skin unlocked: {skin.skinName}");
            }
        }
        
        // Unlock bullet secret skins
        foreach (SkinData skin in allBulletSkins)
        {
            if (skin.rarity == SkinData.Rarity.Secret && !IsSkinOwned(skin))
            {
                UnlockSkin(skin); // 🔥 Hanya unlock, TIDAK auto-equip
                anyUnlocked = true;
                Debug.Log($"🎉 Secret bullet skin unlocked: {skin.skinName}");
            }
        }
        
        if (anyUnlocked)
        {
            // 🔥 Simpan flag bahwa secret skin sudah di-unlock
            PlayerPrefs.SetInt(SECRET_SKIN_UNLOCKED_KEY, 1);
            PlayerPrefs.Save();
            
            AudioManager.Instance?.PlaySFX("SecretUnlock");
            
            Debug.Log("🎊 Congratulations! All secret skins unlocked! Equip them from shop!");
        }
        else
        {
            Debug.LogWarning("No secret skins found to unlock!");
        }
        
        return anyUnlocked;
    }
    
    // ========== SAVE/LOAD ==========
    void SaveOwnedSkins()
    {
        List<string> skinNames = new List<string>();
        foreach (var skin in ownedSkins)
            skinNames.Add(skin.skinName);
        
        PlayerPrefs.SetString(OWNED_SKINS_KEY, string.Join(",", skinNames));
        PlayerPrefs.Save();
    }
    
    void LoadData()
    {
        // Load owned skins
        string savedSkins = PlayerPrefs.GetString(OWNED_SKINS_KEY, "");
        if (!string.IsNullOrEmpty(savedSkins))
        {
            string[] skinNames = savedSkins.Split(',');
            List<SkinData> allSkins = new List<SkinData>();
            allSkins.AddRange(allCharacterSkins);
            allSkins.AddRange(allBulletSkins);
            
            foreach (string name in skinNames)
            {
                SkinData skin = allSkins.Find(s => s.skinName == name);
                if (skin != null && !ownedSkins.Contains(skin))
                    ownedSkins.Add(skin);
            }
        }
        
        // Load current character skin
        string charSkinName = PlayerPrefs.GetString(CURRENT_CHAR_SKIN_KEY, "");
        if (!string.IsNullOrEmpty(charSkinName))
        {
            currentCharacterSkin = allCharacterSkins.Find(s => s.skinName == charSkinName);
        }
        
        if (currentCharacterSkin == null && allCharacterSkins.Count > 0)
        {
            currentCharacterSkin = allCharacterSkins[0];
            if (!ownedSkins.Contains(currentCharacterSkin))
                ownedSkins.Add(currentCharacterSkin);
            
            PlayerPrefs.SetString(CURRENT_CHAR_SKIN_KEY, currentCharacterSkin.skinName);
            PlayerPrefs.Save();
        }
        
        // Load current bullet skin
        string bulletSkinName = PlayerPrefs.GetString(CURRENT_BULLET_SKIN_KEY, "");
        if (!string.IsNullOrEmpty(bulletSkinName))
        {
            currentBulletSkin = allBulletSkins.Find(s => s.skinName == bulletSkinName);
        }
        
        if (currentBulletSkin == null && allBulletSkins.Count > 0)
        {
            currentBulletSkin = allBulletSkins[0];
            if (!ownedSkins.Contains(currentBulletSkin))
                ownedSkins.Add(currentBulletSkin);
            
            PlayerPrefs.SetString(CURRENT_BULLET_SKIN_KEY, currentBulletSkin.skinName);
            PlayerPrefs.Save();
        }
        
        // 🔥 Log status wave 30 & secret skin
        if (HasCompletedWave30())
        {
            Debug.Log("Wave 30 has been completed in previous session");
        }
        
        if (HasUnlockedSecretSkin())
        {
            Debug.Log("Secret skins were unlocked in previous session");
        }
    }
    
    void UpdateAllUI()
    {
        if (shopUI != null) shopUI.UpdateUI();
    }
    
    public void ResetAllData()
    {
        PlayerPrefs.DeleteKey(OWNED_SKINS_KEY);
        PlayerPrefs.DeleteKey(CURRENT_CHAR_SKIN_KEY);
        PlayerPrefs.DeleteKey(CURRENT_BULLET_SKIN_KEY);
        PlayerPrefs.DeleteKey(WAVE_30_COMPLETED_KEY);
        PlayerPrefs.DeleteKey(SECRET_SKIN_UNLOCKED_KEY);
        PlayerPrefs.Save();
        
        ownedSkins.Clear();
        
        if (allCharacterSkins.Count > 0)
        {
            currentCharacterSkin = allCharacterSkins[0];
            ownedSkins.Add(currentCharacterSkin);
        }
        
        if (allBulletSkins.Count > 0)
        {
            currentBulletSkin = allBulletSkins[0];
            ownedSkins.Add(currentBulletSkin);
        }
        
        UpdateAllUI();
        ApplyCurrentSkin();
        ApplyBulletSkin();
        
        Debug.Log("All data reset including Wave 30 and Secret Skin progress");
    }
}