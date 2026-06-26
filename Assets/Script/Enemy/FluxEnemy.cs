using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class FluxEnemy : EnemyBase
{
    [Header("Movement")]
    public float zigzagWidth = 2f;
    public float zigzagStepY = -0.5f;
    public float moveSpeed = 2f;
    [Range(0f, 1f)] public float zigzagWidthRandomness = 0.35f;
    [Range(0f, 1f)] public float zigzagStepYRandomness = 0.3f;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public float idleDuration = 0.5f;
    public float attackAnimLength = 0.5f;
    public float postAttackDelay = 1f;
    public float bulletSpeed = 3f;

    private Vector3 moveTarget;
    private bool movingRight = true;
    private bool moveReached = false;
    private bool isAttacking = false;
    private bool hasEnteredCamera = false; // 🔥 FLAG MASUK KAMERA
    private bool bulletFiredThisAttack = false;

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

    void OnSpawnEnter()
    {
        animator.SetTrigger("spawnDone");
        movingRight = Random.value > 0.5f;
        SetNextTarget();
        _fsm.ChangeState(EnemyState.Move);
    }

    void SetNextTarget()
    {
        float widthMultiplier = Random.Range(1f - zigzagWidthRandomness, 1f + zigzagWidthRandomness);
        float stepYMultiplier = Random.Range(1f - zigzagStepYRandomness, 1f + zigzagStepYRandomness);

        float xOffset = (movingRight ? zigzagWidth : -zigzagWidth) * widthMultiplier;
        float yOffset = zigzagStepY * stepYMultiplier;

        moveTarget = transform.position + new Vector3(xOffset, yOffset, 0);
        moveTarget.x = Mathf.Clamp(moveTarget.x, minX, maxX);
    }

    void OnMoveEnter()
    {
        animator.SetTrigger("startMove");
        moveReached = false;
    }

    void OnMoveUpdate()
    {
        // 🔥 CEK APAKAH SUDAH MASUK KAMERA
        if (!hasEnteredCamera && IsInsideCameraSafe())
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

                // 🔥 HANYA IDLE JIKA SUDAH MASUK KAMERA
                if (hasEnteredCamera)
                {
                    animator.SetTrigger("reachedTarget");
                    _fsm.ChangeState(EnemyState.Idle);
                }
                else
                {
                    // 🔥 BELUM MASUK KAMERA → LANJUT ZIGZAG
                    movingRight = !movingRight;
                    SetNextTarget();
                    _fsm.ChangeState(EnemyState.Move);
                }
            }
        }
    }

    // 🔥 CEK APAKAH DI DALAM AREA KAMERA
    bool IsInsideCameraSafe()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);

        float margin = 0.15f; // 🔥 bisa kamu tweak (0.1 - 0.25)

        return viewportPos.x > margin && viewportPos.x < 1f - margin &&
            viewportPos.y > margin && viewportPos.y < 1f - margin;
    }
    void OnIdleEnter()
    {
        StartCoroutine(IdleThenAttack());
    }

    IEnumerator IdleThenAttack()
    {
        yield return new WaitForSeconds(idleDuration);

        animator.SetTrigger("doAttack");
        _fsm.ChangeState(EnemyState.Attack);
    }

    void OnAttackEnter()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            bulletFiredThisAttack = false;
            StartCoroutine(AttackThenMove());
        }
    }

    IEnumerator AttackThenMove()
    {
        // Fallback: kalau animation event FireBullet tidak kepanggil di attack pertama,
        // tetap paksa tembak sekali di tengah animasi attack.
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
        _fsm.ChangeState(EnemyState.Move);
    }

    public void FireBullet()
    {
        bulletFiredThisAttack = true;

        if (bulletPrefab != null && player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0, 0, angle - 90f));

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.baseDamage = damage;
                bullet.type = BulletType.EnemyBullet;
                bullet.speed = bulletSpeed * PowerUpManager.enemyBulletSpeedMultiplier;
            }
        }
    }

    protected override void Die()
    {
        base.Die();
    }
}