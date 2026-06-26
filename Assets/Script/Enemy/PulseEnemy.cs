using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseEnemy : EnemyBase
{
    [Header("Movement")]
    public float moveSpeedToSlot = 2.5f;
    public float smoothTime = 0.3f;
    public float stopDistance = 0.05f;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public int bulletCount = 8;
    public float bulletSpeed = 3f;
    public float initialFireDelay = 0.5f;
    public float fireCooldown = 2.5f;
    [Tooltip("Durasi Attack clip untuk fallback tembak kalau Animation Event gagal.")]
    public float attackAnimLength = 0.5f;
    private float fireTimer;
    private bool isFirstAttack = true;
    private bool bulletFiredThisAttack;
    private Coroutine attackCoroutine;

    private Vector3 slotTarget;
    private int assignedSlotIndex = -1;
    private bool slotReached = false;
    private Vector3 velocity = Vector3.zero;

    // 🔥 8 ARAH MATA ANGIN
    private Vector2[] directions = new Vector2[]
    {
        Vector2.up,                    // Atas
        Vector2.down,                  // Bawah
        Vector2.left,                  // Kiri
        Vector2.right,                 // Kanan
        new Vector2(1, 1).normalized,  // Kanan Atas
        new Vector2(-1, 1).normalized, // Kiri Atas
        new Vector2(1, -1).normalized, // Kanan Bawah
        new Vector2(-1, -1).normalized // Kiri Bawah
    };

    protected override void InitFSM()
    {
        if (_fsm.StateInitialized) return;

        var map = new Dictionary<EnemyState, State>()
        {
            { EnemyState.Spawn,  new State(OnSpawnEnter,  null,         null, null) },
            { EnemyState.Move,   new State(OnMoveEnter,   OnMoveUpdate, null, null) },
            { EnemyState.Idle,   new State(OnIdleEnter,   OnIdleUpdate, null, null) },
            { EnemyState.Attack, new State(OnAttackEnter, null, null, null) },
            { EnemyState.Dead,   new State(null, null, null, null) }
        };

        _fsm.Initialize(map);
    }

    protected override void Start()
    {
        base.Start();
        _fsm.ChangeState(EnemyState.Spawn);
    }

    // ========== SPAWN STATE ==========
    void OnSpawnEnter()
    {
        animator.SetTrigger("spawnDone");
        ClaimSlot();
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

    // ========== MOVE STATE ==========
    void OnMoveEnter()
    {
        animator.SetTrigger("startMove");
        slotReached = false;
        velocity = Vector3.zero;
    }

    void OnMoveUpdate()
    {
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
                _fsm.ChangeState(EnemyState.Idle);
            }
        }
    }

    // ========== IDLE STATE ==========
    void OnIdleEnter()
    {
        if (isFirstAttack)
        {
            fireTimer = initialFireDelay;
            isFirstAttack = false;
        }
        else
        {
            fireTimer = fireCooldown;
        }
    }

    void OnIdleUpdate()
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0)
        {
            animator.SetTrigger("doAttack");
            _fsm.ChangeState(EnemyState.Attack);
        }
    }

    // ========== ATTACK STATE ==========
    void OnAttackEnter()
    {
        fireTimer = fireCooldown;

        bulletFiredThisAttack = false;
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackFallbackCoroutine());
    }

    IEnumerator AttackFallbackCoroutine()
    {
        float t = Mathf.Max(0.05f, attackAnimLength * 0.5f);
        yield return new WaitForSeconds(t);
        if (!bulletFiredThisAttack)
            FireBullet();

        float remaining = Mathf.Max(0f, attackAnimLength - t);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        float extra = Mathf.Max(0f, attackAnimLength * 0.15f);
        if (extra > 0f)
            yield return new WaitForSeconds(extra);

        if (_fsm.currentstate == EnemyState.Attack && !isDead)
            TransitionAttackToIdleFromRoutine();
    }

    void TransitionAttackToIdleFromRoutine()
    {
        attackCoroutine = null;
        if (_fsm.currentstate != EnemyState.Attack || isDead) return;
        _fsm.ChangeState(EnemyState.Idle);
    }

    // Animation Event — akhir animasi Attack.
    public void OnAttackFinished()
    {
        if (_fsm.currentstate != EnemyState.Attack || isDead) return;
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        _fsm.ChangeState(EnemyState.Idle);
    }

    // Animation Event nama alternatif ("fire").
    public void fire() => FireBullet();
    public void Fire() => FireBullet();

    public void FireBullet()
    {
        bulletFiredThisAttack = true;
        if (bulletPrefab == null) return;

        foreach (Vector2 dir in directions)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.baseDamage = damage;
                bullet.type = BulletType.EnemyBullet;
                bullet.speed = bulletSpeed * PowerUpManager.enemyBulletSpeedMultiplier;
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bulletObj.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    // ========== DIE ==========
    protected override void Die()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (assignedSlotIndex >= 0 && FormationManager.Instance != null)
        {
            FormationManager.Instance.ReleaseSlot(assignedSlotIndex);
            assignedSlotIndex = -1;
        }

        base.Die();
    }
}