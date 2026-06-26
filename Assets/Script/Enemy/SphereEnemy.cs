using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereEnemy : EnemyBase
{
    private enum MovePhase
    {
        ToSlot,
        ChasePlayer
    }

    [Header("Movement")]
    public float moveSpeedToSlot = 1.5f;
    public float smoothTime = 0.3f;
    public float stopDistance = 0.05f;
    public float moveToPlayerSpeed = 2f;

    [Header("Chase")]
    [Tooltip("Jarak ke player untuk mulai attack (anim doAttack).")]
    public float chaseAttackDistance = 1.35f;

    [Header("Shield Settings")]
    public int orbitCount = 6;
    public float orbitRadius = 1.5f;
    public float orbitSpeed = 90f;

    [Header("Attack Settings")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 4f;
    public float shieldRespawnDelay = 5f;
    [Tooltip("Durasi maks menunggu Animation Event tembak; jika shield masih ada setelah ini, FireOrbitBullets dipanggil otomatis.")]
    public float attackAnimLength = 0.5f;

    [Header("Animator")]
    [Tooltip("Bool di Animator untuk walk saat mengejar player (mis. isWalking). Kosongkan jika tidak dipakai.")]
    public string walkBoolParameter = "isWalking";
    [Tooltip("Trigger untuk clip attack (biasanya doAttack).")]
    public string attackTriggerParameter = "doAttack";

    private float respawnTimer;
    private float orbitAngle;
    private GameObject[] orbitBullets;
    private bool shieldActive = true;

    /// <summary>
    /// True setelah attack shield sampai kembali ke slot dan menunggu respawn shield.
    /// </summary>
    private bool waitingShieldAtSlot;

    private MovePhase movePhase = MovePhase.ToSlot;

    private Vector3 slotTarget;
    private int assignedSlotIndex = -1;
    private bool slotReached;
    private Vector3 velocity = Vector3.zero;

    private Vector3 bulletPrefabRootScale = Vector3.one;
    private Coroutine attackRoutine;

    protected override void InitFSM()
    {
        if (_fsm.StateInitialized) return;

        var map = new Dictionary<EnemyState, State>()
        {
            { EnemyState.Spawn,  new State(OnSpawnEnter,  null,         null, null) },
            { EnemyState.Move,   new State(OnMoveEnter,   OnMoveUpdate, null, null) },
            { EnemyState.Idle,   new State(OnIdleEnter,   OnIdleUpdate, null, null) },
            { EnemyState.Attack, new State(OnAttackEnter, null,         null, null) },
            { EnemyState.Dead,   new State(null, null, null, null) }
        };

        _fsm.Initialize(map);
    }

    void SetWalkAnim(bool walking)
    {
        if (animator == null || string.IsNullOrEmpty(walkBoolParameter))
            return;
        animator.SetBool(walkBoolParameter, walking);
    }

    void TriggerAttackAnim()
    {
        if (animator == null || string.IsNullOrEmpty(attackTriggerParameter))
            return;
        animator.ResetTrigger(attackTriggerParameter);
        animator.SetTrigger(attackTriggerParameter);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (animator == null || isDead) return;
        if (_fsm.currentstate != EnemyState.Move || movePhase != MovePhase.ChasePlayer || !shieldActive)
            return;
        SetWalkAnim(true);
    }

    protected override void Start()
    {
        base.Start();
        if (bulletPrefab != null)
            bulletPrefabRootScale = bulletPrefab.transform.localScale;

        SpawnOrbitShield();
        _fsm.ChangeState(EnemyState.Spawn);
    }

    void SpawnOrbitShield()
    {
        if (bulletPrefab == null) return;

        if (orbitBullets != null)
        {
            foreach (GameObject obj in orbitBullets)
            {
                if (obj != null) Destroy(obj);
            }
        }

        orbitBullets = new GameObject[orbitCount];
        Vector3 p = transform.lossyScale;
        float ix = 1f / Mathf.Max(p.x, 1e-4f);
        float iy = 1f / Mathf.Max(p.y, 1e-4f);
        float iz = 1f / Mathf.Max(p.z, 1e-4f);

        for (int i = 0; i < orbitCount; i++)
        {
            float angle = (i * (360f / orbitCount)) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * orbitRadius, Mathf.Sin(angle) * orbitRadius, 0);

            orbitBullets[i] = Instantiate(bulletPrefab, transform);
            orbitBullets[i].transform.localPosition = pos;
            orbitBullets[i].transform.localRotation = Quaternion.identity;
            orbitBullets[i].transform.localScale = new Vector3(
                bulletPrefabRootScale.x * ix,
                bulletPrefabRootScale.y * iy,
                bulletPrefabRootScale.z * iz);

            Bullet bullet = orbitBullets[i].GetComponent<Bullet>();
            if (bullet != null) bullet.enabled = false;

            Collider2D col = orbitBullets[i].GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        shieldActive = true;
    }

    void OnSpawnEnter()
    {
        animator.SetTrigger("spawnDone");
        ClaimSlot();
        movePhase = MovePhase.ToSlot;
        waitingShieldAtSlot = false;
        _fsm.ChangeState(EnemyState.Move);
    }

    void ClaimSlot()
    {
        if (FormationManager.Instance != null)
        {
            var (index, position) = FormationManager.Instance.GetFreeSlot();
            assignedSlotIndex = index;
            slotTarget = position;

            if (assignedSlotIndex < 0)
                slotTarget = new Vector3(Random.Range(-3f, 3f), 2f, 0);
        }
        else
        {
            slotTarget = new Vector3(Random.Range(-3f, 3f), 2f, 0);
        }
    }

    void OnMoveEnter()
    {
        slotReached = false;
        velocity = Vector3.zero;

        if (movePhase == MovePhase.ChasePlayer)
        {
            SetWalkAnim(true);
        }
        else
        {
            SetWalkAnim(false);
            if (animator != null)
                animator.SetTrigger("startMove");
        }
    }

    void OnMoveUpdate()
    {
        UpdateOrbit();

        if (movePhase == MovePhase.ToSlot)
        {
            SetWalkAnim(false);

            if (!slotReached)
            {
                float currentSpeed = moveSpeedToSlot;
                float distance = Vector3.Distance(transform.position, slotTarget);

                if (distance < smoothTime * currentSpeed)
                    currentSpeed = Mathf.Lerp(currentSpeed * 0.3f, currentSpeed, distance / (smoothTime * currentSpeed));

                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    slotTarget,
                    ref velocity,
                    smoothTime,
                    currentSpeed
                );

                if (distance < stopDistance)
                {
                    slotReached = true;
                    transform.position = slotTarget;
                    animator.SetTrigger("reachedTarget");

                    if (shieldActive && player != null)
                    {
                        movePhase = MovePhase.ChasePlayer;
                        SetWalkAnim(true);
                    }
                    else
                        _fsm.ChangeState(EnemyState.Idle);
                }
            }
            return;
        }

        if (movePhase == MovePhase.ChasePlayer)
        {
            if (!shieldActive || player == null)
                return;

            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                moveToPlayerSpeed * Time.deltaTime
            );

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= chaseAttackDistance)
                _fsm.ChangeState(EnemyState.Attack);
        }
    }

    void OnIdleEnter()
    {
        if (waitingShieldAtSlot)
            respawnTimer = shieldRespawnDelay;
    }

    void OnIdleUpdate()
    {
        if (!waitingShieldAtSlot)
            return;

        respawnTimer -= Time.deltaTime;
        if (respawnTimer > 0f)
            return;

        SpawnOrbitShield();
        movePhase = MovePhase.ChasePlayer;
        StartCoroutine(ResumeChaseAfterShieldRoutine());
    }

    IEnumerator ResumeChaseAfterShieldRoutine()
    {
        yield return null;
        SetWalkAnim(true);
        if (animator != null)
            animator.SetTrigger("startMove");
        _fsm.ChangeState(EnemyState.Move);
    }

    void OnAttackEnter()
    {
        SetWalkAnim(false);
        TriggerAttackAnim();

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        yield return null;

        // Tembakan shield dari Animation Event (FireOrbitBullets / FireBullet); tunggu sampai pecah atau timeout.
        float t = 0f;
        while (shieldActive && t < attackAnimLength)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (shieldActive)
            FireOrbitBullets();

        yield return null;

        if (assignedSlotIndex >= 0 && FormationManager.Instance != null)
        {
            FormationManager.Instance.ReleaseSlot(assignedSlotIndex);
            assignedSlotIndex = -1;
        }

        ClaimSlot();
        movePhase = MovePhase.ToSlot;
        slotReached = false;
        velocity = Vector3.zero;

        attackRoutine = null;
        SetWalkAnim(false);
        _fsm.ChangeState(EnemyState.Move);
    }

    public void FireOrbitBullets()
    {
        FireOrbitBulletsInternal();
    }

    public void FireBullet() => FireOrbitBullets();

    void UpdateOrbit()
    {
        if (!shieldActive || orbitBullets == null) return;

        orbitAngle += orbitSpeed * Time.deltaTime;
        for (int i = 0; i < orbitBullets.Length; i++)
        {
            if (orbitBullets[i] == null) continue;

            float angle = (orbitAngle + i * (360f / orbitCount)) * Mathf.Deg2Rad;
            orbitBullets[i].transform.localPosition = new Vector3(
                Mathf.Cos(angle) * orbitRadius,
                Mathf.Sin(angle) * orbitRadius,
                0
            );
        }
    }

    void FireOrbitBulletsInternal()
    {
        if (orbitBullets == null) return;

        foreach (GameObject obj in orbitBullets)
        {
            if (obj == null) continue;

            obj.transform.SetParent(null);
            obj.transform.localScale = bulletPrefabRootScale;

            Bullet bullet = obj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.enabled = true;
                bullet.baseDamage = damage;
                bullet.type = BulletType.EnemyBullet;
                bullet.speed = bulletSpeed * PowerUpManager.enemyBulletSpeedMultiplier;
            }

            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                rb.WakeUp();
            }

            Vector3 dir = obj.transform.position - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        orbitBullets = null;
        shieldActive = false;

        waitingShieldAtSlot = true;
    }

    protected override void Die()
    {
        SetWalkAnim(false);

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (orbitBullets != null)
        {
            foreach (GameObject obj in orbitBullets)
            {
                if (obj != null) Destroy(obj);
            }
        }

        if (assignedSlotIndex >= 0 && FormationManager.Instance != null)
        {
            FormationManager.Instance.ReleaseSlot(assignedSlotIndex);
            assignedSlotIndex = -1;
        }

        base.Die();
    }
}
