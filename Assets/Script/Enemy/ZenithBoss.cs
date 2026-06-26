using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss Zenith — 3 behaviour layer berdasarkan kemunculan ke-berapa.
///
/// Kemunculan ke-1 → Layer 1: hujan peluru random dari atas kamera
/// Kemunculan ke-2 → Layer 2: dinding peluru rapat dengan celah
/// Kemunculan ke-3+ → Layer 1 + Layer 2 bersamaan
///
/// Saat spawn, boss bergerak ke tengah layar (agak atas).
/// </summary>
public class ZenithBoss : EnemyBase
{
    // ── Spawn Movement ───────────────────────
    [Header("Spawn Movement")]
    public float spawnTargetYOffset = 1.5f;
    public float spawnMoveSpeed = 4f;

    [Header("Attack Timing")]
    public float idleBetweenAttacks = 2f;

    // ── Layer 1: Hujan Peluru Random ─────────
    [Header("Layer 1 – Bullet Rain")]
    public GameObject layer1NormalBulletPrefab;
    public GameObject layer1UnabsorbBulletPrefab;
    [Tooltip("Jumlah peluru per 1x attack")]
    public int   layer1TotalBullets   = 35;
    [Tooltip("Jeda minimum antar peluru (detik)")]
    public float layer1MinDelay       = 0.18f;
    [Tooltip("Jeda maksimum antar peluru (detik)")]
    public float layer1MaxDelay       = 0.45f;
    [Range(0f, 1f)]
    public float layer1UnabsorbChance = 0.35f;
    public float layer1BulletSpeed    = 7f;

    // ── Layer 2: Dinding Peluru ──────────────
    [Header("Layer 2 – Bullet Wall")]
    public GameObject layer2UnabsorbBulletPrefab;
    public GameObject layer2AbsorbBulletPrefab;
    public int   layer2Columns    = 7;
    public int   layer2Rows       = 4;
    public float layer2RowDelay   = 0.18f;
    public float layer2BulletSpeed = 4f;

    // ── Private ──────────────────────────────
    private Vector3 spawnTargetPos;
    private bool    reachedSpawnPos;
    private float   idleTimer;

    private static int appearanceCount = 0;

    private int layer2GapCol    = -1;
    private int layer2AbsorbCol = -1;

    private enum Phase { Layer1Only, Layer2Only, BothLayers }
    private Phase currentPhase;

    // ── Static Reset ─────────────────────────
    public static void ResetAppearanceCount() => appearanceCount = 0;
    public static void SetAppearanceCount(int count) => appearanceCount = count;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void EditorReset() => appearanceCount = 0;
#endif

    // ── Lifecycle ────────────────────────────

    protected override void InitFSM()
{
    if (_fsm.StateInitialized) return;
    _fsm.Initialize(new Dictionary<EnemyState, State>
    {
        { EnemyState.Spawn,  new State(OnSpawnEnter,  null,           null, null) },
        { EnemyState.Move,   new State(OnMoveEnter,   OnMoveUpdate,   null, null) }, // 🔥 TAMBAH
        { EnemyState.Idle,   new State(OnIdleEnter,   OnIdleUpdate,   null, null) },
        { EnemyState.Attack, new State(OnAttackEnter, null,           null, null) },
        { EnemyState.Dead,   new State(null, null, null, null) }
    });
}

    protected override void Start()
    {
        base.Start();

        Camera cam = Camera.main;
        if (cam != null)
            spawnTargetPos = new Vector3(0f, cam.orthographicSize - spawnTargetYOffset, 0f);
        else
            spawnTargetPos = new Vector3(0f, 3f, 0f);

        reachedSpawnPos = false;
        _fsm.ChangeState(EnemyState.Spawn);
    }

    // ── SPAWN ────────────────────────────────

    void OnSpawnEnter()
{
    appearanceCount++;
    if (animator != null) animator.SetTrigger("spawnDone");
    _fsm.ChangeState(EnemyState.Move); // 🔥 PINDAH KE MOVE
}

void OnMoveEnter()
{
    if (animator != null) animator.SetTrigger("startMove");
}

void OnMoveUpdate()
{
    transform.position = Vector3.MoveTowards(
        transform.position, spawnTargetPos, spawnMoveSpeed * Time.deltaTime);

    if (Vector3.Distance(transform.position, spawnTargetPos) < 0.05f)
    {
        transform.position = spawnTargetPos;
        if (animator != null) animator.SetTrigger("reachedTarget");
        _fsm.ChangeState(EnemyState.Idle);
    }
}


    // ── IDLE ─────────────────────────────────

    void OnIdleEnter()
    {
        idleTimer = idleBetweenAttacks;

        if (appearanceCount >= 3)      currentPhase = Phase.BothLayers;
        else if (appearanceCount == 2) currentPhase = Phase.Layer2Only;
        else                           currentPhase = Phase.Layer1Only;

        Debug.Log($"[ZenithBoss] Phase: {currentPhase}");
        if (animator != null) animator.SetTrigger("startIdle");
    }

    void OnIdleUpdate()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            if (animator != null) animator.SetTrigger("doAttack");
            _fsm.ChangeState(EnemyState.Attack);
        }
    }

    // ── ATTACK ───────────────────────────────

    void OnAttackEnter() => StartCoroutine(CO_Attack());

    IEnumerator CO_Attack()
    {
        RandomiseLayer2Pattern();

        switch (currentPhase)
        {
            case Phase.Layer1Only:
                yield return CO_Layer1();
                break;
            case Phase.Layer2Only:
                yield return CO_Layer2();
                break;
            case Phase.BothLayers:
                // 🔥 BERGANTIAN: Layer 1 → jeda → Layer 2
                yield return CO_Layer1();
                if (!isDead)
                {
                    yield return new WaitForSeconds(idleBetweenAttacks);
                    yield return CO_Layer2();
                }
                break;
        }

        if (!isDead) _fsm.ChangeState(EnemyState.Idle);
    }

    IEnumerator Wrap(IEnumerator r, System.Action done)
    {
        yield return StartCoroutine(r);
        done?.Invoke();
    }

    // ═══════════════════════════════════════════
    //  LAYER 1 — HUJAN PELURU RANDOM (scattered)
    //  Peluru jatuh satu-satu di posisi X acak dari atas kamera.
    //  Tipe random: EnemyBullet / UnabsorbableBullet.
    // ═══════════════════════════════════════════

    IEnumerator CO_Layer1()
    {
        if (layer1NormalBulletPrefab == null && layer1UnabsorbBulletPrefab == null)
        {
            Debug.LogWarning("[ZenithBoss] Layer1 prefab belum di-assign!");
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null) yield break;

        float halfH  = cam.orthographicSize;
        float halfW  = halfH * cam.aspect;
        float spawnY = halfH - 0.5f;
        float xMin   = -halfW * 0.9f;
        float xMax   =  halfW * 0.9f;
        float minXDist = (xMax - xMin) * 0.35f;
        float lastX    = float.NaN;

        for (int i = 0; i < layer1TotalBullets; )
        {
            // Random 1-2 peluru muncul bersamaan
            int burst = Mathf.Min(Random.Range(1, 3), layer1TotalBullets - i);

            // Animasi attack setiap burst
            if (animator != null) animator.Play("Attack", 0, 0f);

            for (int b = 0; b < burst; b++)
            {
                // Posisi X random, pastikan jauh dari peluru sebelumnya
                float x;
                int safety = 0;
                do
                {
                    x = Random.Range(xMin, xMax);
                    safety++;
                }
                while (!float.IsNaN(lastX) && Mathf.Abs(x - lastX) < minXDist && safety < 20);

                lastX = x;
                Vector3 pos = new Vector3(x, spawnY, 0f);

                bool unabs     = Random.value < layer1UnabsorbChance;
                GameObject pfb = unabs ? layer1UnabsorbBulletPrefab : layer1NormalBulletPrefab;
                if (pfb == null) { pfb = unabs ? layer1NormalBulletPrefab : layer1UnabsorbBulletPrefab; }
                if (pfb == null) { i++; continue; }

                GameObject obj = Instantiate(pfb, pos, Quaternion.Euler(0, 0, 180f));
                Bullet bl = obj.GetComponent<Bullet>();
                if (bl != null)
                {
                    bl.type       = unabs ? BulletType.UnabsorbableBullet : BulletType.EnemyBullet;
                    bl.baseDamage = damage;
                    bl.speed      = layer1BulletSpeed* PowerUpManager.enemyBulletSpeedMultiplier;;
                }

                i++;
            }

            // Jeda random setelah burst
            yield return new WaitForSeconds(Random.Range(layer1MinDelay, layer1MaxDelay));
        }
    }

    // ═══════════════════════════════════════════
    //  LAYER 2 — DINDING PELURU (BULLET WALL)
    //  Dinding rapat: 1 celah (gap), 1 kolom absorbable,
    //  sisanya unabsorbable. Bergerak perlahan ke bawah.
    //
    //  Contoh (7 kolom, gap=3, absorb=1):
    //    | / |   | | |
    //    | / |   | | |
    //    | / |   | | |
    //    | / |   | | |
    // ═══════════════════════════════════════════

    void RandomiseLayer2Pattern()
{
    // 🔥 CELAH TIDAK BOLEH DI POJOK (min 1 dari tepi)
    layer2GapCol = Random.Range(1, layer2Columns - 1);
    
    // 🔥 ABSORBABLE TIDAK BOLEH SAMA DENGAN CELAH
    do 
    { 
        layer2AbsorbCol = Random.Range(0, layer2Columns); 
    }
    while (layer2AbsorbCol == layer2GapCol);
}   

    IEnumerator CO_Layer2()
    {
        if (layer2UnabsorbBulletPrefab == null && layer2AbsorbBulletPrefab == null)
        {
            Debug.LogWarning("[ZenithBoss] Layer2 prefab belum di-assign!");
            yield break;
        }

        for (int r = 0; r < layer2Rows; r++)
        {
            if (animator != null) animator.Play("Attack", 0, 0f);
            SpawnWallRow();
            yield return new WaitForSeconds(layer2RowDelay);
        }
    }

    void SpawnWallRow()
{
    Camera cam = Camera.main;
    if (cam == null) return;

    float halfH   = cam.orthographicSize;
    float halfW   = halfH * cam.aspect;
    float spawnY  = halfH - 0.5f;
    
    // 🔥 LEBAR TOTAL 95% LAYAR
    float totalW  = halfW * 2f * 0.95f;
    float originX = -totalW * 0.5f;
    float step    = layer2Columns > 1 ? totalW / (layer2Columns - 1) : 0f;

    for (int c = 0; c < layer2Columns; c++)
    {
        if (c == layer2GapCol) continue; // celah

        bool isAbsorb = (c == layer2AbsorbCol);

        GameObject pfb;
        BulletType bType;

        if (isAbsorb && layer2AbsorbBulletPrefab != null)
        {
            pfb   = layer2AbsorbBulletPrefab;
            bType = BulletType.EnemyBullet;
        }
        else
        {
            pfb   = layer2UnabsorbBulletPrefab;
            bType = BulletType.UnabsorbableBullet;
        }

        if (pfb == null) continue;

        Vector3 pos    = new Vector3(originX + step * c, spawnY, 0f);
        GameObject obj = Instantiate(pfb, pos, Quaternion.Euler(0, 0, 180f));

        Bullet b = obj.GetComponent<Bullet>();
        if (b != null)
        {
            b.type       = bType;
            b.baseDamage = damage;
            b.speed      = layer2BulletSpeed * PowerUpManager.enemyBulletSpeedMultiplier;
        }
    }
}

    // ── DIE ──────────────────────────────────

    protected override void Die()
    {
        StopAllCoroutines();
        base.Die();
    }
}