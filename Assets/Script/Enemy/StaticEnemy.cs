using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticEnemy : EnemyBase
{
    [Header("Movement")]
    public float zigzagWidth = 2f;
    public float zigzagStepY = -0.5f;
    public float moveSpeed = 2f;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 4f;
    public float idleDuration = 0.5f;
    public float attackAnimLength = 0.5f;
    public float postAttackDelay = 1f;

    private Vector3 moveTarget;
    private bool movingRight = true;
    private bool moveReached = false;
    private bool isAttacking = false;
    private bool hasEnteredCamera = false;
    private bool bulletFiredThisAttack = false;
    private Coroutine idleRoutine;
    private Coroutine attackRoutine;

    // 🔥 4 ARAH DIAGONAL
    private Vector2[] diagonalDirections = new Vector2[]
    {
        new Vector2(1, 1).normalized,   // Kanan Atas
        new Vector2(-1, 1).normalized,  // Kiri Atas
        new Vector2(1, -1).normalized,  // Kanan Bawah
        new Vector2(-1, -1).normalized  // Kiri Bawah
    };

    protected override void InitFSM()
    {
        if (_fsm.StateInitialized) return;

        var map = new Dictionary<EnemyState, State>()
        {
            { EnemyState.Spawn,  new State(OnSpawnEnter,  null,         null, null) },
            { EnemyState.Move,   new State(OnMoveEnter,   OnMoveUpdate, null, null) },
            { EnemyState.Idle,   new State(OnIdleEnter,   null,         null, null) },
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
        SetNextTarget();
        _fsm.ChangeState(EnemyState.Move);
    }

    void SetNextTarget()
    {
        float xOffset = movingRight ? zigzagWidth : -zigzagWidth;
        moveTarget = transform.position + new Vector3(xOffset, zigzagStepY, 0);
        moveTarget.x = Mathf.Clamp(moveTarget.x, minX, maxX);
    }

    // ========== MOVE STATE ==========
    void OnMoveEnter()
    {
        animator.SetTrigger("startMove");
        moveReached = false;
    }

    void OnMoveUpdate()
    {
        if (!hasEnteredCamera && IsInsideCamera())
        {
            hasEnteredCamera = true;
        }

        if (!moveReached)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                moveTarget,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, moveTarget) < 0.1f)
            {
                moveReached = true;
                transform.position = moveTarget;

                if (hasEnteredCamera)
                {
                    animator.SetTrigger("reachedTarget");
                    _fsm.ChangeState(EnemyState.Idle);
                }
                else
                {
                    movingRight = !movingRight;
                    SetNextTarget();
                    _fsm.ChangeState(EnemyState.Move);
                }
            }
        }
    }

    bool IsInsideCamera()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        return viewportPos.x > 0f && viewportPos.x < 1f
            && viewportPos.y > 0f && viewportPos.y < 1f;
    }

    // ========== IDLE STATE ==========
    void OnIdleEnter()
    {
        if (idleRoutine != null)
            StopCoroutine(idleRoutine);

        idleRoutine = StartCoroutine(IdleThenAttack());
    }

    IEnumerator IdleThenAttack()
    {
        yield return new WaitForSeconds(idleDuration);

        if (_fsm.currentstate != EnemyState.Idle || isDead)
            yield break;

        animator.SetTrigger("doAttack");
        _fsm.ChangeState(EnemyState.Attack);
    }

    // ========== ATTACK STATE ==========
    void OnAttackEnter()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            bulletFiredThisAttack = false;

            if (attackRoutine != null)
                StopCoroutine(attackRoutine);

            attackRoutine = StartCoroutine(AttackThenMove());
        }
    }

    IEnumerator AttackThenMove()
    {
        // Fallback: jika Animation Event FireBullet tidak terpanggil, tetap tembak sekali.
        float fallbackDelay = Mathf.Max(0.05f, attackAnimLength * 0.5f);
        yield return new WaitForSeconds(fallbackDelay);
        if (!bulletFiredThisAttack)
            FireBullet();

        float remainingAttack = Mathf.Max(0f, attackAnimLength - fallbackDelay);
        if (remainingAttack > 0f)
            yield return new WaitForSeconds(remainingAttack);

        yield return new WaitForSeconds(postAttackDelay);

        movingRight = !movingRight;
        SetNextTarget();
        isAttacking = false;
        attackRoutine = null;
        _fsm.ChangeState(EnemyState.Move);
    }

    // Dipanggil dari Animation Event
    public void FireBullet()
    {
        bulletFiredThisAttack = true;
        if (bulletPrefab == null) return;

        foreach (Vector2 dir in diagonalDirections)
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
        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        StopAllCoroutines();
        base.Die();
    }
}