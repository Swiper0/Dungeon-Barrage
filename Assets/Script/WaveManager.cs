using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [Header("Settings")]
    public float timeBetweenWaves = 3f;

    [Header("Enemies – Common")]
    public GameObject[] commonEnemyPrefabs;

    [Header("Enemies – Uncommon")]
    public GameObject[] uncommonEnemyPrefabs;

    [Header("Enemies – Elite")]
    public GameObject[] eliteEnemyPrefabs;

    [Header("Bosses")]
    public GameObject[] bossPrefabs;

    [Header("UI")]
    public WaveUIManager waveUI;

    [Header("Spawn Settings")]
    public float topOffset = 2f;
    public float sidePadding = 1f;

   [Header("Wave Progression")]
public int baseEnemyCount = 5;
public int enemyIncrement = 1;
public int maxEnemiesOnScreen = 6;
public int maxEnemiesPerWave = 15;

    [Header("Status")]
    public int currentWave = 0;
    public int enemiesAlive = 0;
    private bool isWaveActive = false;
    private bool isSpawning = false;

    [Header("Spawn Fix")]
    public float enemySpawnExtraOffset = 1.5f;

    // ── Spawn Queue ──────────────────────────
    private Queue<GameObject> spawnQueue = new Queue<GameObject>();
    private int totalEnemiesRemaining = 0;

    private int cheatWaveInput = 0;
    private float cheatInputTimer = 0f;

        public GameObject shopButton;
        public GameObject aboutButton;
        public GameObject htpButton;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void StartWaves()
{
    StopAllCoroutines();

    // 🔥 HIDE SAAT WAVE DIMULAI
    MainMenuUI mainMenu = FindObjectOfType<MainMenuUI>();
    if (mainMenu != null) mainMenu.HideAllButtons();

    if (shopButton != null) shopButton.SetActive(false);
    if (aboutButton != null) aboutButton.SetActive(false);
    if (htpButton != null) htpButton.SetActive(false);
    
    currentWave = 0;
    enemiesAlive = 0;
    totalEnemiesRemaining = 0;
    isWaveActive = false;
    isSpawning = false;
    spawnQueue.Clear();

    if (waveUI != null) waveUI.ShowWaveUI(1);

    StartCoroutine(StartNextWave(timeBetweenWaves));
}

    IEnumerator StartNextWave(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!GameManager.Instance.gameStarted)
            yield break;

        currentWave++;
        spawnQueue.Clear();

        if (waveUI != null)
            waveUI.HideWaveUI();

        if (currentWave % 10 == 0)
        {
            isSpawning = true;
            SpawnBossWave();
            isSpawning = false;
        }
        else
        {
            isSpawning = true;
            yield return StartCoroutine(SpawnNormalWave());
            isSpawning = false;
        }

        isWaveActive = true;
    }

    // ──────────────────────────────────────────
    // PERHITUNGAN JUMLAH MUSUH
    // En = a + (n-1) * b
    // ──────────────────────────────────────────

    int GetEnemyCount(int wave)
{
    int count = baseEnemyCount + (wave - 1) * enemyIncrement;
    return Mathf.Min(count, maxEnemiesPerWave); // 🔥 MAX 15
}


    // ══════════════════════════════════════════
    // RARITY WEIGHT SYSTEM
    //
    // Siklus 1 (Wave 1-9):
    //   W1-3: 100/0/0  W4-6: 70/30/0  W7-9: 50/35/15
    //
    // Per siklus berikutnya (setiap 10 wave):
    //   Common  -= 10  (min 20)
    //   Uncommon += 5
    //   Elite   += 5   (max 40)
    // ══════════════════════════════════════════

    void GetRarityWeights(int wave, out float common, out float uncommon, out float elite)
    {
        // Siklus = berapa kali sudah lewat wave kelipatan 10
        int cycle = (wave - 1) / 10;  // 0 untuk wave 1-10, 1 untuk 11-20, dst.

        // Posisi dalam siklus (1-9 untuk normal wave)
        int posInCycle = ((wave - 1) % 10) + 1;

        // Base weight dari fase puncak siklus 1
        float baseCommon   = 50f;
        float baseUncommon = 35f;
        float baseElite    = 15f;

        // Geser per siklus
        float shiftedCommon   = Mathf.Max(20f, baseCommon - cycle * 10f);
        float shiftedUncommon = baseUncommon + cycle * 5f;
        float shiftedElite    = Mathf.Min(40f, baseElite + cycle * 5f);

        // Sisanya ke uncommon agar total 100%
        shiftedUncommon = 100f - shiftedCommon - shiftedElite;

        // Dalam siklus pertama, fase awal punya bobot berbeda
        if (cycle == 0)
        {
            if (posInCycle <= 3)
            {
                // Wave 1-3: hanya common
                common   = 100f;
                uncommon = 0f;
                elite    = 0f;
                return;
            }
            else if (posInCycle <= 6)
            {
                // Wave 4-6: mulai uncommon
                common   = 70f;
                uncommon = 30f;
                elite    = 0f;
                return;
            }
            // Wave 7-9: pakai base (50/35/15)
        }

        common   = shiftedCommon;
        uncommon = shiftedUncommon;
        elite    = shiftedElite;
    }

    GameObject PickEnemyByRarity(int wave)
    {
        GetRarityWeights(wave, out float wCommon, out float wUncommon, out float wElite);

        float roll = Random.Range(0f, 100f);

        if (roll < wCommon)
        {
            // Common
            if (commonEnemyPrefabs.Length > 0)
                return commonEnemyPrefabs[Random.Range(0, commonEnemyPrefabs.Length)];
        }
        else if (roll < wCommon + wUncommon)
        {
            // Uncommon
            if (uncommonEnemyPrefabs.Length > 0)
                return uncommonEnemyPrefabs[Random.Range(0, uncommonEnemyPrefabs.Length)];
        }
        else
        {
            // Elite
            if (eliteEnemyPrefabs.Length > 0)
                return eliteEnemyPrefabs[Random.Range(0, eliteEnemyPrefabs.Length)];
        }

        // Fallback: jika array target kosong, coba dari yang ada
        if (commonEnemyPrefabs.Length > 0)
            return commonEnemyPrefabs[Random.Range(0, commonEnemyPrefabs.Length)];
        if (uncommonEnemyPrefabs.Length > 0)
            return uncommonEnemyPrefabs[Random.Range(0, uncommonEnemyPrefabs.Length)];
        if (eliteEnemyPrefabs.Length > 0)
            return eliteEnemyPrefabs[Random.Range(0, eliteEnemyPrefabs.Length)];

        return null;
    }

    // ──────────────────────────────────────────
    // SPAWN NORMAL WAVE (dengan spawn queue + rarity)
    // ──────────────────────────────────────────

    IEnumerator SpawnNormalWave()
    {
        int enemyCount = GetEnemyCount(currentWave);
        totalEnemiesRemaining = enemyCount;

        GetRarityWeights(currentWave, out float c, out float u, out float e);
        Debug.Log($"[Wave {currentWave}] Total: {enemyCount} | Rarity: C={c:F0}% U={u:F0}% E={e:F0}%");

        // Siapkan antrian dengan rarity-based selection
        for (int i = 0; i < enemyCount; i++)
        {
            GameObject prefab = PickEnemyByRarity(currentWave);
            if (prefab != null)
                spawnQueue.Enqueue(prefab);
        }

        // Spawn sampai batas max on screen
        while (spawnQueue.Count > 0 && enemiesAlive < maxEnemiesOnScreen)
        {
            if (!GameManager.Instance.gameStarted) yield break;

            SpawnFromQueue();
            yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));
        }
    }

    void SpawnFromQueue()
{
    if (spawnQueue.Count == 0) return;

    GameObject prefab = null;
    int attempts = spawnQueue.Count;

    for (int i = 0; i < attempts; i++)
    {
        GameObject candidate = spawnQueue.Dequeue();

        // Cek apakah NodeEnemy dan sudah mencapai maksimal
        if (candidate.GetComponent<NodeEnemy>() != null && !NodeEnemy.CanSpawn())
        {
            // 🔥 GANTI DENGAN ENEMY RANDOM LAIN (bukan NodeEnemy)
            GameObject replacement = GetRandomNonNodeEnemy();
            if (replacement != null)
            {
                prefab = replacement;
                Debug.Log("NodeEnemy penuh, diganti dengan enemy lain");
                break;
            }
            else
            {
                // Tidak ada pengganti, kembalikan ke queue
                spawnQueue.Enqueue(candidate);
            }
        }
        else
        {
            prefab = candidate;
            break;
        }
    }

    if (prefab == null) return;

    Camera cam = Camera.main;
    float camHeight = cam.orthographicSize;
    float camWidth = camHeight * cam.aspect;

    float x = Random.Range(-camWidth + sidePadding, camWidth - sidePadding);
    float y = camHeight + topOffset + enemySpawnExtraOffset;

    Vector3 spawnPos = new Vector3(x, y, 0);
    GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

    EnemyBase enemy = obj.GetComponent<EnemyBase>();
    if (enemy != null)
        enemy.ApplyScaling(currentWave);

    enemiesAlive++;
}

/// <summary>
/// Ambil random enemy dari pool, tapi BUKAN NodeEnemy
/// </summary>
GameObject GetRandomNonNodeEnemy()
{
    List<GameObject> validPrefabs = new List<GameObject>();

    // Gabung semua pool kecuali NodeEnemy
    foreach (GameObject prefab in commonEnemyPrefabs)
        if (prefab != null && prefab.GetComponent<NodeEnemy>() == null)
            validPrefabs.Add(prefab);

    foreach (GameObject prefab in uncommonEnemyPrefabs)
        if (prefab != null && prefab.GetComponent<NodeEnemy>() == null)
            validPrefabs.Add(prefab);

    foreach (GameObject prefab in eliteEnemyPrefabs)
        if (prefab != null && prefab.GetComponent<NodeEnemy>() == null)
            validPrefabs.Add(prefab);

    if (validPrefabs.Count > 0)
        return validPrefabs[Random.Range(0, validPrefabs.Count)];

    return null;
}

    // ──────────────────────────────────────────
    // BOSS WAVE (dengan stat scaling)
    // ──────────────────────────────────────────

    void SpawnBossWave()
{
    totalEnemiesRemaining = 1;

    if (bossPrefabs.Length == 0) return;

    Camera cam = Camera.main;
    float camHeight = cam.orthographicSize;
    float camWidth = camHeight * cam.aspect;

    float x = Random.Range(-camWidth + sidePadding, camWidth - sidePadding);
    float y = camHeight + topOffset + enemySpawnExtraOffset;
    Vector3 spawnPos = new Vector3(x, y, 0);

    // 🔥 ZENITH: 10, 40, 70, 100... (mod 30 == 10)
    // 🔥 NEXUS:  20, 50, 80, 110... (mod 30 == 20)
    // 🔥 AETHER: 30, 60, 90, 120... (mod 30 == 0)
    int waveMod = currentWave % 30;
    int bossIndex;
    
    if (waveMod == 10)
        bossIndex = 0; // Zenith
    else if (waveMod == 20)
        bossIndex = 1; // Nexus
    else
        bossIndex = 2; // Aether (waveMod == 0)

    GameObject prefab = bossPrefabs[Mathf.Clamp(bossIndex, 0, bossPrefabs.Length - 1)];

    GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

    EnemyBase boss = obj.GetComponent<EnemyBase>();
    if (boss != null)
        boss.ApplyScaling(currentWave);

    enemiesAlive++;
}


    // ──────────────────────────────────────────
    // ENEMY KILLED — spawn dari queue jika ada sisa
    // ──────────────────────────────────────────

    public void OnEnemyKilled()
    {
        enemiesAlive--;
        enemiesAlive = Mathf.Max(0, enemiesAlive);

        totalEnemiesRemaining--;
        totalEnemiesRemaining = Mathf.Max(0, totalEnemiesRemaining);

        // Jika masih ada di queue dan ada slot, spawn pengganti
        if (spawnQueue.Count > 0 && enemiesAlive < maxEnemiesOnScreen)
        {
            SpawnFromQueue();
        }
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    // ──────────────────────────────────────────
    // UPDATE — cek wave selesai + cheat
    // ──────────────────────────────────────────

    void Update()
    {
        // 🔥 CHEAT - JALAN TERUS, BAHKAN SEBELUM WAVE AKTIF
        #if UNITY_EDITOR
        // Cek K untuk mulai input cheat
        if (Input.GetKeyDown(KeyCode.K))
        {
            cheatWaveInput = 0;
            cheatInputTimer = 5f;
            Debug.Log("<color=yellow>CHEAT: Masukkan nomor wave lalu ENTER (contoh: 15)</color>");
        }

        // Cek N untuk next wave
        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("<color=yellow>CHEAT: Next Wave</color>");
            NextWave();
        }

        // Cek F1-F10 untuk wave 1-10
        if (Input.GetKeyDown(KeyCode.F1)) { SkipToWave(1); return; }
        if (Input.GetKeyDown(KeyCode.F2)) { SkipToWave(2); return; }
        if (Input.GetKeyDown(KeyCode.F3)) { SkipToWave(3); return; }
        if (Input.GetKeyDown(KeyCode.F4)) { SkipToWave(4); return; }
        if (Input.GetKeyDown(KeyCode.F5)) { SkipToWave(5); return; }
        if (Input.GetKeyDown(KeyCode.F6)) { SkipToWave(6); return; }
        if (Input.GetKeyDown(KeyCode.F7)) { SkipToWave(7); return; }
        if (Input.GetKeyDown(KeyCode.F8)) { SkipToWave(8); return; }
        if (Input.GetKeyDown(KeyCode.F9)) { SkipToWave(9); return; }
        if (Input.GetKeyDown(KeyCode.F10)) { SkipToWave(10); return; }

        if (cheatInputTimer > 0)
        {
            cheatInputTimer -= Time.unscaledDeltaTime;

            // Input angka
            for (int i = 0; i <= 9; i++)
            {
                KeyCode alphaKey = (KeyCode)((int)KeyCode.Alpha0 + i);
                KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad0 + i);

                if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
                {
                    cheatWaveInput = cheatWaveInput * 10 + i;
                    Debug.Log($"<color=cyan>CHEAT INPUT: {cheatWaveInput}</color>");
                    cheatInputTimer = 5f;
                }
            }

            // ENTER
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (cheatWaveInput > 0)
                {
                    Debug.Log($"<color=green>CHEAT: Skip to Wave {cheatWaveInput}!</color>");
                    SkipToWave(cheatWaveInput);
                }
                else
                {
                    Debug.Log("<color=red>CHEAT: Tidak ada input, batal.</color>");
                }
                cheatInputTimer = 0;
                cheatWaveInput = 0;
            }
        }
        #endif

        if (!GameManager.Instance.gameStarted || !isWaveActive) return;
        if (isSpawning) return;

        // Update bagian wave selesai di WaveManager
// Update bagian wave selesai di WaveManager
if (totalEnemiesRemaining <= 0 && enemiesAlive <= 0 && spawnQueue.Count == 0)
{
    isWaveActive = false;
    
    // 🔥 CEK UNLOCK SECRET SKIN DI WAVE 30
    if (currentWave == 30)
    {
        HandleWave30Completion();
    }
    
    // 🔥 PANGGIL OnWaveCompleted UNTUK TEMPORARY POWER-UP
    if (PowerUpManager.Instance != null)
        PowerUpManager.Instance.OnWaveCompleted();

    if (currentWave % 5 == 0 && currentWave > 0)
    {
        StartCoroutine(ShowPowerUpAfterWaveUI(currentWave));
    }
    else
    {
        int nextWave = currentWave + 1;
        if (waveUI != null) waveUI.ShowWaveUI(nextWave);
        StartCoroutine(StartNextWave(timeBetweenWaves));
    }
}

// ========== WAVE 30 COMPLETION HANDLER ==========
// ========== WAVE 30 COMPLETION HANDLER ==========
void HandleWave30Completion()
{
    if (SkinManager.Instance == null)
    {
        Debug.LogError("SkinManager.Instance is null! Cannot handle Wave 30 completion.");
        return;
    }
    
    // 🔥 Cek apakah ini pertama kali menyelesaikan wave 30
    if (!SkinManager.Instance.HasCompletedWave30())
    {
        // PERTAMA KALI! Unlock & auto-equip secret skins
        Debug.Log("🎉 FIRST TIME Wave 30 completed! Unlocking secret skins...");
        
        bool unlocked = SkinManager.Instance.TryUnlockSecretSkins();
        
        if (unlocked)
        {
            // Tandai wave 30 sudah selesai
            SkinManager.Instance.MarkWave30Completed();
            Debug.Log("✅ Wave 30 marked as completed! Secret skins auto-equipped!");
        }
    }
    else
    {
        // SUDAH PERNAH menyelesaikan wave 30 sebelumnya
        Debug.Log("Wave 30 completed again - secret skins already unlocked & equipped.");
        
        // 🔥 Safety check: pastikan secret skin tetap ter-unlock
        if (!SkinManager.Instance.HasUnlockedSecretSkin())
        {
            Debug.LogWarning("Wave 30 flag exists but secret skin flag missing! Re-unlocking...");
            SkinManager.Instance.TryUnlockSecretSkins();
        }
    }
}
}

IEnumerator ShowPowerUpAfterWaveUI(int completedWave)
{
    int nextWave = completedWave + 1;
    
    // 🔥 TAMPILKAN WAVE UI DULU (Wave 6)
    if (waveUI != null)
        waveUI.ShowWaveUI(nextWave);
    
    yield return new WaitForSeconds(3f);
    
    // 🔥 PASS completedWave (5) BUKAN currentWave
    PowerUpManager.Instance?.TriggerPowerUpSelection(completedWave);
}

public IEnumerator StartNextWaveAfterPowerUp()
{
    yield return new WaitForSeconds(0.5f);
    
    currentWave++;
    isWaveActive = true;
    spawnQueue.Clear();

    if (currentWave % 10 == 0)
    {
        SpawnBossWave();
    }
    else
    {
        yield return StartCoroutine(SpawnNormalWave());
    }
}

    // ──────────────────────────────────────────
    // CHEAT FUNCTIONS
    // ──────────────────────────────────────────

    public void NextWave()
    {
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        foreach (EnemyBase enemy in enemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }

        enemiesAlive = 0;
        totalEnemiesRemaining = 0;
        spawnQueue.Clear();
        isWaveActive = false;

        int nextWave = currentWave + 1;

        // 🔥 HITUNG APPEARANCE YANG BENAR BERDASARKAN WAVE
        CalculateAndSetBossAppearances(nextWave);

        // 🔥 RESET POWER-UPS SAAT CHEAT
        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.ResetAllPowerUps();

        if (waveUI != null)
            waveUI.ShowWaveUI(nextWave);

        StopAllCoroutines();
        StartCoroutine(StartNextWave(0.5f));
        
        Debug.Log($"Cheat: Skip to Wave {nextWave}");
    }

    public void SkipToWave(int targetWave)
    {
        if (targetWave < 1) targetWave = 1;

        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        foreach (EnemyBase enemy in enemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }

        enemiesAlive = 0;
        totalEnemiesRemaining = 0;
        spawnQueue.Clear();
        currentWave = targetWave - 1;
        isWaveActive = false;

        // 🔥 HITUNG APPEARANCE YANG BENAR BERDASARKAN WAVE TARGET
        CalculateAndSetBossAppearances(targetWave);

        // 🔥 RESET POWER-UPS SAAT CHEAT SKIP
        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.ResetAllPowerUps();

        if (waveUI != null)
            waveUI.ShowWaveUI(targetWave);

        StopAllCoroutines();
        StartCoroutine(StartNextWave(0.5f));
        
        Debug.Log($"Cheat: Skip to Wave {targetWave}");
    }

    // ──────────────────────────────────────────
    // HITUNG BOSS APPEARANCE BERDASARKAN WAVE
    // Zenith: wave 10, 40, 70... (mod 30 == 10)
    // Nexus:  wave 20, 50, 80... (mod 30 == 20)
    // Aether: wave 30, 60, 90... (mod 30 == 0)
    // ──────────────────────────────────────────
    void CalculateAndSetBossAppearances(int upToWave)
    {
        int zenith = 0, nexus = 0, aether = 0;
        for (int bw = 10; bw < upToWave; bw += 10)
        {
            int mod = bw % 30;
            if (mod == 10) zenith++;
            else if (mod == 20) nexus++;
            else aether++; // mod == 0
        }

        ZenithBoss.SetAppearanceCount(zenith);
        NexusBoss.SetAppearanceCount(nexus);
        AetherBoss.SetAppearanceCount(aether);

        Debug.Log($"Boss appearances set for wave {upToWave}: Zenith={zenith}, Nexus={nexus}, Aether={aether}");
    }
}

