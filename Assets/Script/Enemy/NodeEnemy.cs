using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeEnemy : EnemyBase
{
    [Header("Movement")]
    public float moveSpeedToSlot = 1.5f;
    public float smoothTime = 0.3f;
    public float stopDistance = 0.15f;

    [Header("Position Settings")]
    public float edgePadding = 0.5f;
    public float nodeMinY = 0.5f;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;
    public float fireInterval = 0.5f;
    public float initialFireDelay = 0.5f;
    public float restDuration = 2f;

    public const int MAX_ACTIVE = 2;
    private static List<NodeEnemy> activeNodes = new List<NodeEnemy>();

    public static bool CanSpawn()
    {
        activeNodes.RemoveAll(n => n == null);
        return activeNodes.Count < MAX_ACTIVE;
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void EditorReset() => activeNodes.Clear();
#endif

    private float timer;
    private bool isFirstAttack = true;
    private bool isRightSide = false;
    private bool isShooting = false;
    private Vector3 shootDirection;
    private Coroutine shootCoroutine;

    private Vector3 targetPos;
    private bool targetReached = false;
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
            { EnemyState.Dead,   new State(null,          null,         null, null) }
        };

        _fsm.Initialize(map);
    }

    protected override void Start()
    {
        base.Start();
        activeNodes.RemoveAll(n => n == null);
        activeNodes.Add(this);
        _fsm.ChangeState(EnemyState.Spawn);
    }

    // ========== SPAWN ==========
    void OnSpawnEnter()
    {
        PickEdgePosition();
        animator.SetTrigger("spawnDone");
        _fsm.ChangeState(EnemyState.Move);
    }

    void PickEdgePosition()
    {
        Camera cam = Camera.main;
        float halfH = cam != null ? cam.orthographicSize : 5f;
        float halfW = cam != null ? halfH * cam.aspect : 3.5f;

        activeNodes.RemoveAll(n => n == null);
        NodeEnemy other = activeNodes.Find(n => n != this);

        if (other != null)
            isRightSide = !other.isRightSide;
        else
            isRightSide = Random.value > 0.5f;

        float xPos = isRightSide ? halfW - edgePadding : -halfW + edgePadding;
        float yPos = player != null ? player.position.y : 1.5f;
        float yMaxScreen = halfH - 0.5f;
        yPos = Mathf.Clamp(yPos, nodeMinY, yMaxScreen);

        targetPos = new Vector3(xPos, yPos, 0f);
        shootDirection = isRightSide ? Vector2.left : Vector2.right;
    }

    // ========== MOVE ==========
    void OnMoveEnter()
    {
        targetReached = false;
        velocity = Vector3.zero;
    }

    void OnMoveUpdate()
    {
        if (targetReached) return;

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref velocity, smoothTime, moveSpeedToSlot);

        if (Vector3.Distance(transform.position, targetPos) < stopDistance)
        {
            targetReached = true;
            transform.position = targetPos;
            FaceShootDirection();
            animator.SetTrigger("reachedTarget");
            _fsm.ChangeState(EnemyState.Idle);
        }
    }

    void FaceShootDirection()
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = isRightSide;
    }

    // ========== IDLE ==========
    void OnIdleEnter()
    {
        ForceStopShooting();
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

    // ========== ATTACK ==========
    void OnAttackEnter()
    {
        ForceStopShooting();
        // Animation Events handle semuanya
    }

    // Animation Event: 0:160
    public void StartFiring()
    {
        if (isDead) return;
        isShooting = true;
        if (shootCoroutine != null) StopCoroutine(shootCoroutine);
        shootCoroutine = StartCoroutine(FireRoutine());
    }

    IEnumerator FireRoutine()
    {
        while (isShooting && !isDead)
        {
            FireBullet();
            yield return new WaitForSeconds(fireInterval);
        }
    }

    // Animation Event: 4:040
    public void StopFiring()
    {
        ForceStopShooting();
    }

    // Animation Event: 4:160
    public void OnAttackFinished()
    {
        StopFiring();
        if (!isDead)
        {
            animator.SetTrigger("endAttack");
            _fsm.ChangeState(EnemyState.Idle);
        }
    }

    void ForceStopShooting()
    {
        isShooting = false;
        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
        }
    }

    void FireBullet()
    {
        if (bulletPrefab == null || isDead) return;

        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.baseDamage = damage;
            bullet.type = BulletType.EnemyBullet;
            bullet.speed = bulletSpeed * PowerUpManager.enemyBulletSpeedMultiplier;
        }

        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        bulletObj.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    // ========== DIE ==========
    protected override void Die()
    {
        ForceStopShooting();
        StopAllCoroutines();
        activeNodes.Remove(this);
        base.Die();
    }

    void OnDestroy()
    {
        activeNodes.Remove(this);
    }
}