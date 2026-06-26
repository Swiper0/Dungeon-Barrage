using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss Aether — Bos tingkat akhir, 3 behaviour layer.
///
/// Kemunculan ke-1 → Layer 1: formasi geometris acak (segitiga, kotak, pentagon)
/// Kemunculan ke-2 → Layer 2: donat peluru → berhenti → meluncur ke player
/// Kemunculan ke-3+ → Layer 1 + Layer 2 (donat dengan jeda tidak teratur)
///
/// HP: 6000, Damage: 40
/// Saat spawn, boss bergerak ke tengah layar (agak atas).
/// </summary>
public class AetherBoss : EnemyBase
{
    // ── Spawn Movement ───────────────────────
    [Header("Spawn Movement")]
    public float spawnTargetYOffset = 1.5f;
    public float spawnMoveSpeed = 4f;

    [Header("Attack Timing")]
    public float idleBetweenAttacks = 2.5f;

    // ── Layer 1: Formasi Geometris ───────────
    [Header("Layer 1 – Geometric Formations")]
    public GameObject layer1BulletPrefab;
    public GameObject layer1UnabsorbBulletPrefab;
    [Tooltip("Jumlah formasi per attack")]
    public int   layer1FormationCount = 3;
    [Tooltip("Jeda antar formasi")]
    public float layer1FormationDelay = 1.5f;
    [Tooltip("Ukuran formasi (radius)")]
    public float layer1ShapeRadius    = 1.2f;
    [Tooltip("Jumlah peluru per sisi bentuk geometris")]
    public int   layer1BulletsPerSide = 3;
    public float layer1BulletSpeed    = 5f;
    [Range(0f, 1f)]
    public float layer1UnabsorbChance = 0.3f;

    // ── Layer 2: Donat Peluru ────────────────
    [Header("Layer 2 – Donut Burst")]
    public GameObject layer2BulletPrefab;
    [Tooltip("Jumlah peluru per donat")]
    public int   layer2BulletCount    = 12;
    [Tooltip("Radius donat")]
    public float layer2DonutRadius    = 1.5f;
    [Tooltip("Waktu berhenti sebelum meluncur (detik)")]
    public float layer2PauseTime      = 1.0f;
    [Tooltip("Variasi jeda pada Layer 3 (min)")]
    public float layer2PauseMin       = 0.3f;
    [Tooltip("Variasi jeda pada Layer 3 (max)")]
    public float layer2PauseMax       = 1.5f;
    [Tooltip("Jumlah donat per attack")]
    public int   layer2DonutCount     = 2;
    [Tooltip("Jeda antar donat")]
    public float layer2DonutDelay     = 1.2f;
    public float layer2BulletSpeed    = 7f;

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

        Debug.Log($"[AetherBoss] Phase: {currentPhase}");
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
                yield return CO_Layer2(false);
                break;
            case Phase.BothLayers:
                // 🔥 BERGANTIAN: Layer 1 → jeda → Layer 2 (irregular)
                yield return CO_Layer1();
                if (!isDead)
                {
                    yield return new WaitForSeconds(idleBetweenAttacks);
                    yield return CO_Layer2(true);
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
    //  LAYER 1 — FORMASI GEOMETRIS ACAK
    //  Spawn formasi segitiga/kotak/pentagon dengan
    //  peluru di sepanjang sisi (edge), bukan hanya
    //  di titik sudut. Formasi jatuh ke bawah.
    // ═══════════════════════════════════════════

    IEnumerator CO_Layer1()
    {
        if (layer1BulletPrefab == null)
        {
            Debug.LogWarning("[AetherBoss] Layer1 prefab belum di-assign!");
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null) yield break;

        float halfW  = cam.orthographicSize * cam.aspect;
        // Offset spawnY agar seluruh formasi muat di dalam layar
        float spawnY = cam.orthographicSize - layer1ShapeRadius - 0.3f;
        // Clamp X agar formasi tidak terpotong di sisi kiri/kanan
        float xMax   = halfW - layer1ShapeRadius - 0.3f;

        for (int f = 0; f < layer1FormationCount; f++)
        {
            // Posisi acak di area atas layar (dalam batas aman)
            float cx = Random.Range(-xMax, xMax);
            Vector3 center = new Vector3(cx, spawnY, 0f);

            // Pilih bentuk acak: 3=segitiga, 4=kotak, 5=pentagon
            int sides = Random.Range(3, 6);

            // Rotasi acak agar formasi tidak selalu orientasi sama
            float randomRotation = Random.Range(0f, 360f);

            SpawnGeometricFormation(center, sides, randomRotation);

            // Animasi attack setiap formasi
            if (animator != null) animator.Play("Attack", 0, 0f);

            yield return new WaitForSeconds(layer1FormationDelay);
        }
    }

    /// <summary>
    /// Spawn peluru membentuk outline geometris (segitiga/kotak/pentagon).
    /// Peluru ditempatkan di sepanjang setiap sisi polygon, bukan hanya di vertex.
    /// </summary>
    void SpawnGeometricFormation(Vector3 center, int sides, float rotationDeg)
    {
        // Hitung posisi vertex
        float angleStep = 360f / sides;
        Vector2[] vertices = new Vector2[sides];

        for (int i = 0; i < sides; i++)
        {
            float angle = (angleStep * i + rotationDeg) * Mathf.Deg2Rad;
            vertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * layer1ShapeRadius;
        }

        int bulletsPerSide = Mathf.Max(layer1BulletsPerSide, 2);

        // Interpolasi peluru di sepanjang setiap sisi
        for (int i = 0; i < sides; i++)
        {
            Vector2 vA = vertices[i];
            Vector2 vB = vertices[(i + 1) % sides];

            // Spawn peluru dari vA ke vB (tidak termasuk titik akhir
            // untuk menghindari duplikat di sudut)
            for (int j = 0; j < bulletsPerSide; j++)
            {
                float t = (float)j / bulletsPerSide;
                Vector2 pos2D = Vector2.Lerp(vA, vB, t);
                Vector3 spawnPos = center + (Vector3)pos2D;

                bool unabs = Random.value < layer1UnabsorbChance;
                GameObject pfb = unabs && layer1UnabsorbBulletPrefab != null
                    ? layer1UnabsorbBulletPrefab
                    : layer1BulletPrefab;
                if (pfb == null) continue;

                // Peluru jatuh ke bawah (rotasi 180)
                GameObject obj = Instantiate(pfb, spawnPos, Quaternion.Euler(0, 0, 180f));

                Bullet b = obj.GetComponent<Bullet>();
                if (b != null)
                {
                    b.type       = unabs ? BulletType.UnabsorbableBullet : BulletType.EnemyBullet;
                    b.baseDamage = damage;
                    b.speed      = layer1BulletSpeed* PowerUpManager.enemyBulletSpeedMultiplier;;
                }
            }
        }
    }

    // ═══════════════════════════════════════════
    //  LAYER 2 — DONAT PELURU
    //  Peluru spawn membentuk donat di sekitar boss,
    //  berhenti sejenak, lalu meluncur ke arah player
    //  secara serentak.
    // ═══════════════════════════════════════════

    IEnumerator CO_Layer2(bool irregularPause)
    {
        if (layer2BulletPrefab == null)
        {
            Debug.LogWarning("[AetherBoss] Layer2 prefab belum di-assign!");
            yield break;
        }

        for (int d = 0; d < layer2DonutCount; d++)
        {
            // Animasi attack setiap donat
            if (animator != null) animator.Play("Attack", 0, 0f);

            // Spawn donat
            List<GameObject> donutBullets = SpawnDonut();

            // Berhenti sejenak (regular atau irregular)
            float pause = irregularPause
                ? Random.Range(layer2PauseMin, layer2PauseMax)
                : layer2PauseTime;
            yield return new WaitForSeconds(pause);

            // Meluncur ke arah player secara serentak
            LaunchDonutAtPlayer(donutBullets);

            yield return new WaitForSeconds(layer2DonutDelay);
        }
    }

    List<GameObject> SpawnDonut()
    {
        List<GameObject> bullets = new List<GameObject>();
        float angleStep = 360f / layer2BulletCount;

        for (int i = 0; i < layer2BulletCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * layer2DonutRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;

            // Spawn peluru dengan speed 0 (diam dulu)
            GameObject obj = Instantiate(layer2BulletPrefab, spawnPos, Quaternion.identity);

            Bullet b = obj.GetComponent<Bullet>();
            if (b != null)
            {
                b.type       = BulletType.UnabsorbableBullet;
                b.baseDamage = damage;
                b.speed      = 0f; // Diam dulu
            }

            bullets.Add(obj);
        }

        return bullets;
    }

    void LaunchDonutAtPlayer(List<GameObject> bullets)
    {
        if (player == null) return;

        Vector2 playerPos = player.position;

        foreach (GameObject obj in bullets)
        {
            if (obj == null) continue;

            // Hitung arah dari peluru ke player
            Vector2 dir = (playerPos - (Vector2)obj.transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float zRot = angle - 90f;

            // Set rotasi dan aktifkan speed
            obj.transform.rotation = Quaternion.Euler(0, 0, zRot);

            Bullet b = obj.GetComponent<Bullet>();
            if (b != null)
            {
                b.speed = layer2BulletSpeed* PowerUpManager.enemyBulletSpeedMultiplier;;
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
