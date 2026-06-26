using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss Nexus — 3 behaviour layer berdasarkan kemunculan ke-berapa.
///
/// Kemunculan ke-1 → Layer 1: tembakan melingkar mengarah ke pemain
/// Kemunculan ke-2 → Layer 2: pola silang berputar perlahan
/// Kemunculan ke-3+ → Layer 2 berputar cepat + Layer 1 bersamaan
///
/// HP: 1250, Damage: 30
/// Saat spawn, boss bergerak ke tengah layar (agak atas).
/// </summary>
public class NexusBoss : EnemyBase
{
    // ── Spawn Movement ───────────────────────
    [Header("Spawn Movement")]
    public float spawnTargetYOffset = 1.5f;
    public float spawnMoveSpeed = 4f;

    [Header("Attack Timing")]
    public float idleBetweenAttacks = 2.5f;

    // ── Layer 1: Cincin Peluru ke Player ─────
    [Header("Layer 1 – Ring Shot to Player")]
    public GameObject layer1BulletPrefab;
    public GameObject layer1UnabsorbBulletPrefab;
    [Tooltip("Jumlah peluru per cincin")]
    public int   layer1BulletCount   = 8;
    [Tooltip("Jumlah gelombang cincin per attack")]
    public int   layer1Waves         = 3;
    [Tooltip("Jeda antar gelombang")]
    public float layer1WaveDelay     = 0.6f;
    public float layer1BulletSpeed   = 2.5f;
    [Tooltip("Radius cincin peluru dari pusat boss")]
    public float layer1RingRadius    = 1.2f;
    [Range(0f, 1f)]
    [Tooltip("Peluang peluru unabsorbable dalam cincin")]
    public float layer1UnabsorbChance = 0.3f;

    // ── Layer 2: Pola Silang Berputar ────────
    [Header("Layer 2 – Rotating Cross")]
    public GameObject layer2BulletPrefab;
    [Tooltip("Jumlah lengan silang (4 = pola +)")]
    public int   layer2Arms          = 4;
    [Tooltip("Peluru per lengan per gelombang")]
    public int   layer2BulletsPerArm = 3;
    [Tooltip("Jarak antar peluru di lengan")]
    public float layer2BulletSpacing = 0.8f;
    [Tooltip("Kecepatan rotasi silang (derajat/detik) — Layer 2")]
    public float layer2RotSpeed      = 45f;
    [Tooltip("Kecepatan rotasi silang (derajat/detik) — Layer 3 (cepat)")]
    public float layer3RotSpeed      = 120f;
    [Tooltip("Durasi serangan silang (detik)")]
    public float layer2Duration      = 4f;
    [Tooltip("Jeda antar gelombang silang")]
    public float layer2WaveDelay     = 0.3f;
    public float layer2BulletSpeed   = 5f;

    // ── Private ──────────────────────────────
    private Vector3 spawnTargetPos;
    private bool    reachedSpawnPos;
    private float   idleTimer;

    private static int appearanceCount = 0;

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

        Debug.Log($"[NexusBoss] Phase: {currentPhase}");
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
        switch (currentPhase)
        {
            case Phase.Layer1Only:
                yield return CO_Layer1();
                break;
            case Phase.Layer2Only:
                yield return CO_Layer2(layer2RotSpeed);
                break;
            case Phase.BothLayers:
                // 🔥 BERGANTIAN: Layer 1 → jeda → Layer 2 (cepat)
                yield return CO_Layer1();
                if (!isDead)
                {
                    yield return new WaitForSeconds(idleBetweenAttacks);
                    yield return CO_Layer2(layer3RotSpeed);
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
    //  LAYER 1 — CINCIN PELURU KE PLAYER
    //  Peluru di-spawn membentuk lingkaran di sekitar boss,
    //  lalu SEMUA bergerak ke arah posisi pemain.
    // ═══════════════════════════════════════════

    IEnumerator CO_Layer1()
    {
        if (layer1BulletPrefab == null)
        {
            Debug.LogWarning("[NexusBoss] Layer1 bullet prefab belum di-assign!");
            yield break;
        }

        for (int w = 0; w < layer1Waves; w++)
        {
            if (animator != null) animator.Play("Attack", 0, 0f);
            ShootRingAtPlayer();
            yield return new WaitForSeconds(layer1WaveDelay);
        }
    }

    void ShootRingAtPlayer()
    {
        if (player == null) return;

        // Arah ke player — SEMUA peluru bergerak ke arah ini
        Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float moveAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        float zRot = moveAngle - 90f;

        float angleStep = 360f / layer1BulletCount;

        for (int i = 0; i < layer1BulletCount; i++)
        {
            float ringAngle = angleStep * i * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(ringAngle), Mathf.Sin(ringAngle)) * layer1RingRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;

            // Random: absorbable atau unabsorbable
            bool unabs = Random.value < layer1UnabsorbChance;
            GameObject pfb = unabs && layer1UnabsorbBulletPrefab != null
                ? layer1UnabsorbBulletPrefab
                : layer1BulletPrefab;
            if (pfb == null) continue;

            GameObject obj = Instantiate(pfb, spawnPos, Quaternion.Euler(0, 0, zRot));

            Bullet b = obj.GetComponent<Bullet>();
            if (b != null)
            {
                b.type       = unabs ? BulletType.UnabsorbableBullet : BulletType.EnemyBullet;
                b.baseDamage = damage;
                b.speed      = layer1BulletSpeed* PowerUpManager.enemyBulletSpeedMultiplier;;
            }
        }
    }

    // ═══════════════════════════════════════════
    //  LAYER 2 — POLA SILANG BERPUTAR
    //  Menembak peluru dalam pola + (silang)
    //  yang berputar mengelilingi pusat bos.
    //  Layer 3: rotasi lebih cepat.
    // ═══════════════════════════════════════════

    IEnumerator CO_Layer2(float rotSpeed)
    {
        if (layer2BulletPrefab == null)
        {
            Debug.LogWarning("[NexusBoss] Layer2 bullet prefab belum di-assign!");
            yield break;
        }

        float elapsed    = 0f;
        float baseAngle  = 0f;

        while (elapsed < layer2Duration)
        {
            // Animasi attack setiap gelombang silang
            if (animator != null) animator.Play("Attack", 0, 0f);

            // Spawn pola silang pada sudut saat ini
            SpawnCrossPattern(baseAngle);

            // Putar
            baseAngle += rotSpeed * layer2WaveDelay;
            elapsed   += layer2WaveDelay;

            yield return new WaitForSeconds(layer2WaveDelay);
        }
    }

    void SpawnCrossPattern(float baseAngleDeg)
    {
        float armStep = 360f / layer2Arms;

        for (int arm = 0; arm < layer2Arms; arm++)
        {
            float armAngle = baseAngleDeg + armStep * arm;
            float rad = armAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            for (int b = 1; b <= layer2BulletsPerArm; b++)
            {
                Vector3 spawnPos = transform.position + (Vector3)(dir * layer2BulletSpacing * b);

                // Rotasi agar peluru bergerak ke arah lengan
                float zRot = armAngle - 90f;
                GameObject obj = Instantiate(layer2BulletPrefab, spawnPos, Quaternion.Euler(0, 0, zRot));

                Bullet bl = obj.GetComponent<Bullet>();
                if (bl != null)
                {
                    bl.type       = BulletType.UnabsorbableBullet;
                    bl.baseDamage = damage;
                    bl.speed      = layer2BulletSpeed* PowerUpManager.enemyBulletSpeedMultiplier;;
                }
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
