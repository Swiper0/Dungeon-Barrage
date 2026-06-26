using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreEnemy : EnemyBase
{
    [Header("Movement")]
    public float moveSpeedToSlot = 1.5f;
    public float smoothTime = 0.3f;
    public float stopDistance = 0.05f;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;
    public int burstCount = 5;
    public float burstInterval = 0.12f;
    public float initialFireDelay = 0.8f;
    public float fireCooldown = 3.5f;
    public float attackAnimLength = 0.6f;

    private float fireTimer;
    private bool isFirstAttack = true;

    private Vector3 slotTarget;
    private int assignedSlotIndex = -1;
    private bool slotReached = false;
    private Vector3 velocity = Vector3.zero;

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
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(attackAnimLength);
        _fsm.ChangeState(EnemyState.Idle);
    }

    // Dipanggil dari Animation Event
    public void FireBullet()
    {
        if (bulletPrefab == null || player == null) return;

        StartCoroutine(FireBurst());
    }

    IEnumerator FireBurst()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < burstCount; i++)
        {
            if (isDead) yield break;

                GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0, 0, 180f));

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.baseDamage = damage;
                bullet.type = BulletType.EnemyBullet;
                bullet.speed = bulletSpeed * PowerUpManager.enemyBulletSpeedMultiplier;
            }

            yield return new WaitForSeconds(burstInterval);
        }
    }

    // ========== DIE ==========
    protected override void Die()
    {
        if (assignedSlotIndex >= 0 && FormationManager.Instance != null)
        {
            FormationManager.Instance.ReleaseSlot(assignedSlotIndex);
            assignedSlotIndex = -1;
        }

        base.Die();
    }
}