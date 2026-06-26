using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApexEnemy : EnemyBase
{
    [Header("Movement")]
    public float moveSpeedToSlot = 1.5f;
    public float smoothTime = 0.3f;
    public float stopDistance = 0.05f;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 4.5f;
    public float fireInterval = 0.3f;
    public float initialFireDelay = 0.8f;
    public float restDuration = 2f;

    private Vector2[] wPattern = new Vector2[]
    {
        new Vector2(-1f, -1f).normalized,  // Diagonal kiri bawah \
        Vector2.down,                       // Bawah |
        new Vector2(1f, -1f).normalized    // Diagonal kanan bawah /
    };

    private float timer;
    private bool isFirstAttack = true;
    private bool isShooting = false;
    private Coroutine shootCoroutine;

    private Vector3 slotTarget;
    private int assignedSlotIndex = -1;
    private bool slotReached = false;
    private Vector3 velocity = Vector3.zero;
    private Coroutine attackDurationCoroutine;

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
                transform.position, slotTarget, ref velocity, smoothTime, currentSpeed);

            if (distance < stopDistance)
            {
                slotReached = true;
                transform.position = slotTarget;
                animator.SetTrigger("reachedTarget");
                _fsm.ChangeState(EnemyState.Idle);
            }
        }
    }

    void OnIdleEnter()
{
    // 🔥 Paksa stop shooting
    isShooting = false;
    if (shootCoroutine != null)
    {
        StopCoroutine(shootCoroutine);
        shootCoroutine = null;
    }

    animator.SetTrigger("toIdle"); // 🔥 Balik ke idle anim
    timer = isFirstAttack ? initialFireDelay : restDuration;
    isFirstAttack = false;
}

void OnIdleUpdate()
{
    timer -= Time.deltaTime;
    if (timer <= 0)
    {
        animator.SetTrigger("doAttack");
        _fsm.ChangeState(EnemyState.Attack);
    }
}

    void OnAttackEnter()
{
    isShooting = true;
    
    if (shootCoroutine != null) StopCoroutine(shootCoroutine);
    shootCoroutine = StartCoroutine(FirePatternRoutine());

    if (attackDurationCoroutine != null) StopCoroutine(attackDurationCoroutine);
    attackDurationCoroutine = StartCoroutine(AttackDuration());
}

IEnumerator AttackDuration()
{
    float attackTime = fireInterval * 10f; // 🔥 Sesuaikan: 10x tembak baru stop
    yield return new WaitForSeconds(attackTime);
    
    attackDurationCoroutine = null;
    
    if (!isDead)
        StopFiring();
}

IEnumerator FirePatternRoutine()
{
    while (isShooting && !isDead)
    {
        foreach (Vector2 dir in wPattern)
            FireBullet(dir);

        yield return new WaitForSeconds(fireInterval);
    }
}

public void StopFiring()
{
    isShooting = false;
    
    if (shootCoroutine != null)
    {
        StopCoroutine(shootCoroutine);
        shootCoroutine = null;
    }
    
    if (attackDurationCoroutine != null)
    {
        StopCoroutine(attackDurationCoroutine);
        attackDurationCoroutine = null;
    }

    if (!isDead)
    {
        animator.ResetTrigger("doAttack");
        animator.SetTrigger("toIdle");
        _fsm.ChangeState(EnemyState.Idle);
    }
}

    void FireBullet(Vector2 direction)
    {
        if (bulletPrefab == null) return;

        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
    if (bullet != null)
    {
        bullet.baseDamage = damage;
        bullet.type = BulletType.EnemyBullet;
        bullet.speed = bulletSpeed * PowerUpManager.enemyBulletSpeedMultiplier; // 🔥 KALIKAN
    }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bulletObj.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    protected override void Die()
{
    isShooting = false;
    
    if (shootCoroutine != null) StopCoroutine(shootCoroutine);
    if (attackDurationCoroutine != null) StopCoroutine(attackDurationCoroutine);
    
    shootCoroutine = null;
    attackDurationCoroutine = null;

    if (assignedSlotIndex >= 0 && FormationManager.Instance != null)
    {
        FormationManager.Instance.ReleaseSlot(assignedSlotIndex);
        assignedSlotIndex = -1;
    }

    base.Die();
}
}