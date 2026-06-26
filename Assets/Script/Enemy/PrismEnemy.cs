using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrismEnemy : EnemyBase
{
    [Header("Movement")]
    public float moveSpeedToSlot = 1.5f;
    public float smoothTime = 0.3f;
    public float stopDistance = 0.05f;

    [Header("Attack")]
    public GameObject bigBulletPrefab;
    public GameObject smallBulletPrefab;
    public float bulletSpeed = 3f;
    public float smallBulletSpeed = 4f;
    public int bigBulletDamage = 15;       // 🔥 HANYA UNTUK PELURU BESAR
    public float initialFireDelay = 0.8f;
    public float fireCooldown = 3f;
    public float attackAnimLength = 0.5f;

    [Tooltip("Peluru besar pecah setelah ~sejauh ini (world units). Waktu = splitAfterWorldUnits / bulletSpeed. Bukan detik tetap 5.")]
    public float splitAfterWorldUnits = 1.8f;

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

    public void FireBullet()
    {
        if (bigBulletPrefab == null || player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject bulletObj = Instantiate(bigBulletPrefab, transform.position, Quaternion.Euler(0, 0, angle - 90f));

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.baseDamage = bigBulletDamage;              // 🔥 DAMAGE BESAR (custom)
            bullet.type = BulletType.UnabsorbableBullet;
            bullet.speed = bulletSpeed * PowerUpManager.enemyBulletSpeedMultiplier;
            bullet.ignoreScreenBounds = true;
        }

        float delay = splitAfterWorldUnits / Mathf.Max(bulletSpeed, 0.01f);
        StartCoroutine(SplitAfterTime(bulletObj, angle, delay));
    }

    IEnumerator SplitAfterTime(GameObject bigBullet, float baseAngle, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (bigBullet == null) yield break;

        Vector3 splitPos = bigBullet.transform.position;

        for (int i = 0; i < 3; i++)
        {
            float spreadAngle = -30f + (i * 30f);
            float finalAngle = baseAngle + spreadAngle - 90f;

            GameObject smallBullet = Instantiate(smallBulletPrefab, splitPos, Quaternion.Euler(0, 0, finalAngle));

            Bullet small = smallBullet.GetComponent<Bullet>();
            if (small != null)
            {
                small.baseDamage = damage;                    // 🔥 DAMAGE KECIL = EnemyBase.damage (10)
                small.type = BulletType.EnemyBullet;
                small.speed = smallBulletSpeed;
            }
        }

        Destroy(bigBullet);
    }


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