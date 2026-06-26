using UnityEngine;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;

    [Header("Power-Up Data")]
    public PowerUpData[] allCommonPowerUps;
    public PowerUpData[] allRarePowerUps;
    public PowerUpData[] allLegendaryPowerUps;
    public static float enemyBulletSpeedMultiplier = 1f;

    [Header("UI Reference")]
    public PowerUpSelectionUI selectionUI;

    [Header("Probabilities")]
    [Range(0, 100)] public float commonChance = 70f;
    [Range(0, 100)] public float rareChance = 25f;
    [Range(0, 100)] public float legendaryChance = 5f;

    // Player reference untuk apply efek
    private PlayerCombat playerCombat;
    private PlayerTouchMove playerTouchMove;
    [Header("Power-Up Effect")]
public GameObject powerUpEffectPrefab;

    // Tracking efek temporer
    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    [System.Serializable]
    public class ActiveEffect
    {
        public PowerUpData data;
        public int remainingWaves;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        playerCombat = FindObjectOfType<PlayerCombat>();
        playerTouchMove = FindObjectOfType<PlayerTouchMove>();
    }

    /// <summary>
    /// Dipanggil WaveManager setelah wave 5, 10, 15, ...
    /// </summary>
    public void TriggerPowerUpSelection(int currentWave)
    {
        if (selectionUI == null) return;

        // Generate 3 opsi random
        PowerUpData[] options = GenerateOptions();

        // Tampilkan UI
        selectionUI.ShowSelection(options, currentWave);

        // Pause game
        Time.timeScale = 0f;
    }

    PowerUpData[] GenerateOptions()
{
    PowerUpData[] options = new PowerUpData[3];
    List<PowerUpData> usedPools = new List<PowerUpData>();

    for (int i = 0; i < 3; i++)
    {
        PowerUpData selected = null;
        int safety = 0;

        do
        {
            float roll = Random.Range(0f, 100f);

            if (roll < commonChance)
            {
                selected = allCommonPowerUps[Random.Range(0, allCommonPowerUps.Length)];
            }
            else if (roll < commonChance + rareChance)
            {
                selected = allRarePowerUps[Random.Range(0, allRarePowerUps.Length)];
            }
            else
            {
                selected = allLegendaryPowerUps[Random.Range(0, allLegendaryPowerUps.Length)];
            }

            safety++;
        }
        while (usedPools.Contains(selected) && safety < 50);

        usedPools.Add(selected);
        options[i] = selected;
    }

    return options;
}

    /// <summary>
    /// Dipanggil saat player memilih power-up
    /// </summary>
   public void SelectPowerUp(PowerUpData selected)
{
    ApplyEffect(selected);
    AudioManager.Instance?.PlaySFX("PowerUp");
    
    // 🔥 SPAWN EFEK SEBAGAI CHILD PLAYER
    if (powerUpEffectPrefab != null)
    {
        PlayerTouchMove player = FindObjectOfType<PlayerTouchMove>();
        if (player != null)
        {
            GameObject effect = Instantiate(powerUpEffectPrefab, player.transform);
            effect.transform.localPosition = Vector3.zero; //
            Destroy(effect, 2f);
        }
    }
    
    Time.timeScale = 1f;

    if (WaveUIManager.Instance != null)
        WaveUIManager.Instance.HideWaveUI();

    if (WaveManager.Instance != null)
    {
        WaveManager.Instance.StartCoroutine(WaveManager.Instance.StartNextWaveAfterPowerUp());
    }
}

    /// <summary>
    /// Dipanggil CheatPanel — hanya apply efek, TANPA trigger wave berikutnya.
    /// </summary>
    public void ApplyCheatPowerUp(PowerUpData selected)
    {
        ApplyEffect(selected);
        AudioManager.Instance?.PlaySFX("PowerUp");

        // 🔥 SPAWN EFEK SEBAGAI CHILD PLAYER
        if (powerUpEffectPrefab != null)
        {
            PlayerTouchMove player = FindObjectOfType<PlayerTouchMove>();
            if (player != null)
            {
                GameObject effect = Instantiate(powerUpEffectPrefab, player.transform);
                effect.transform.localPosition = Vector3.zero;
                Destroy(effect, 2f);
            }
        }

        Debug.Log($"<color=yellow>CHEAT: Power-Up Applied (no wave trigger): {selected.powerUpName}</color>");
    }

    void ApplyEffect(PowerUpData powerUp)
    {
        switch (powerUp.effectType)
        {
            // ── COMMON ──
            case PowerUpData.EffectType.MaxHP:
                playerCombat.maxHealth = Mathf.RoundToInt(playerCombat.maxHealth * 1.2f);
                playerCombat.FullReset();
                break;

            case PowerUpData.EffectType.Damage:
                // Menambah damage peluru biasa
                playerTouchMove.playerDamage = Mathf.RoundToInt(playerTouchMove.playerDamage * 1.15f);
                
                // 🔥 TAMBAHAN: Menambah damage peluru Absorb/Burst juga
                playerTouchMove.empoweredDamage = Mathf.RoundToInt(playerTouchMove.empoweredDamage * 1.15f);
                break;

            case PowerUpData.EffectType.AbsorbCapacity:
                playerCombat.maxEnergy += 2;
                
                // 🔥 TAMBAHKAN UPDATE UI INI
                if (playerCombat.energySlider != null)
                {
                    playerCombat.energySlider.maxValue = playerCombat.maxEnergy;
                }
                // Paksa update UI Energy menggunakan Reflection karena fungsinya private di PlayerCombat
                playerCombat.GetType().GetMethod("UpdateEnergyUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(playerCombat, null);
                break;

            case PowerUpData.EffectType.BurstFireRate:
                playerTouchMove.burstInterval *= 0.75f; 
                break;

            case PowerUpData.EffectType.SlowEnemyBullet:
                AddTemporaryEffect(powerUp, powerUp.temporaryWaveCount);
                enemyBulletSpeedMultiplier = 0.8f; 
                ApplySlowEnemyBullet(0.8f); // 🔥 TAMBAHKAN BARIS INI
                break;

           case PowerUpData.EffectType.EmergencyShield:
    AddTemporaryEffect(powerUp, powerUp.temporaryWaveCount);
    playerCombat.hasEmergencyShield = true;

    // 🔥 AKTIFKAN GLOW TANPA SET ABSORB MODE
    if (playerCombat.absorbGlow != null)
    {
        playerCombat.absorbGlow.SetActive(true);
        playerCombat.SyncGlowToCollider();
    }
    break;

            // ── RARE ──
            case PowerUpData.EffectType.HPRegen:
                // Di-handle di PlayerCombat Update
                playerCombat.hasHPRegen = true;
                break;

            case PowerUpData.EffectType.BurstDamage:
                playerTouchMove.empoweredDamage = Mathf.RoundToInt(playerTouchMove.empoweredDamage * 1.15f);
                break;

            case PowerUpData.EffectType.WideShot:
                playerTouchMove.hasWideShot = true;
                break;

            case PowerUpData.EffectType.EchoShot:
                playerTouchMove.hasEchoShot = true;
                break;

            case PowerUpData.EffectType.PierceEnemy:
                AddTemporaryEffect(powerUp, powerUp.temporaryWaveCount);
                playerTouchMove.hasPierce = true;
                break;

            case PowerUpData.EffectType.AbsorbRadius:
    AddTemporaryEffect(powerUp, powerUp.temporaryWaveCount);
    playerCombat.absorbRadiusMultiplier = 1.25f;

    // 🔥 BESARKAN COLLIDER FISIK
    CircleCollider2D col = playerCombat.GetComponent<CircleCollider2D>();
    if (col != null)
        col.radius = playerCombat.defaultRadius * 1.25f;

    // 🔥 SYNC GLOW KE UKURAN BARU
    playerCombat.SyncGlowToCollider();
    break;

            // ── LEGENDARY ──
            case PowerUpData.EffectType.KineticHeal:
                playerCombat.hasKineticHeal = true;
                break;

            case PowerUpData.EffectType.BurstCooldown:
    playerTouchMove.burstInterval *= 0.5f; // 🔥 HANYA BURST
    break;

            case PowerUpData.EffectType.BulletSplit:
                playerCombat.hasBulletSplit = true;
                break;

            case PowerUpData.EffectType.MirrorShot:
    playerTouchMove.hasMirrorShot = true; // 🔥 GANTI DARI playerCombat KE playerTouchMove
    break;

case PowerUpData.EffectType.Overcharge:
    playerTouchMove.hasOvercharge = true; // 🔥 GANTI DARI playerCombat KE playerTouchMove
    break;

            case PowerUpData.EffectType.BossSlayer:
                playerCombat.bossDamageMultiplier = 1.3f;
                break;
        }

        Debug.Log($"Power-Up Applied: {powerUp.powerUpName} ({powerUp.rarity})");
    }

    void AddTemporaryEffect(PowerUpData data, int waves)
    {
        activeEffects.Add(new ActiveEffect { data = data, remainingWaves = waves });
    }

    /// <summary>
    /// Dipanggil WaveManager setiap wave selesai
    /// </summary>
    public void OnWaveCompleted()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].remainingWaves--;
            if (activeEffects[i].remainingWaves <= 0)
            {
                RemoveEffect(activeEffects[i].data);
                activeEffects.RemoveAt(i);
            }
        }
    }

    void RemoveEffect(PowerUpData data)
{
    switch (data.effectType)
    {
        case PowerUpData.EffectType.SlowEnemyBullet:
            enemyBulletSpeedMultiplier = 1f;
            break;

        case PowerUpData.EffectType.EmergencyShield:
            playerCombat.hasEmergencyShield = false;

            // 🔥 MATIKAN GLOW HANYA JIKA TIDAK SEDANG ABSORB
            if (!playerCombat.isAbsorbing && playerCombat.absorbGlow != null)
            {
                playerCombat.absorbGlow.SetActive(false);
                SpriteRenderer glowRenderer = playerCombat.absorbGlow.GetComponent<SpriteRenderer>();
                if (glowRenderer != null)
                {
                    Color c = glowRenderer.color;
                    c.a = 0f;
                    glowRenderer.color = c;
                }
            }
            
            // 🔥 JANGAN RESET ABSORB READY
            // playerCombat.ResetAbsorbReady(); // HAPUS INI
            break;

        case PowerUpData.EffectType.PierceEnemy:
            playerTouchMove.hasPierce = false;
            break;

        case PowerUpData.EffectType.AbsorbRadius:
            playerCombat.absorbRadiusMultiplier = 1f;

            CircleCollider2D col = playerCombat.GetComponent<CircleCollider2D>();
            if (col != null)
                col.radius = playerCombat.defaultRadius;

            playerCombat.SyncGlowToCollider();
            break;
    }

    Debug.Log($"Power-Up Expired: {data.powerUpName}");
}

    void ApplySlowEnemyBullet(float multiplier)
    {
        Bullet[] allBullets = FindObjectsOfType<Bullet>();
        foreach (Bullet b in allBullets)
        {
            if (b.type == BulletType.EnemyBullet || b.type == BulletType.UnabsorbableBullet)
            {
                b.speed *= multiplier;
            }
        }
    }

    public void ResetAllPowerUps()
{
    // 🔥 FIX: Jalankan RemoveEffect dulu sebelum Clear
    for (int i = activeEffects.Count - 1; i >= 0; i--)
    {
        RemoveEffect(activeEffects[i].data);
    }
    activeEffects.Clear();
    
    enemyBulletSpeedMultiplier = 1f;
    
    if (playerCombat != null)
        playerCombat.FullReset();
    
    if (playerTouchMove != null)
        playerTouchMove.ResetForNewGame();
    
    Debug.Log("All power-ups reset");
}
}