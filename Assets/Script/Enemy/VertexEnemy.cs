using System.Collections.Generic;
using UnityEngine;

public class VertexEnemy : EnemyBase
{
    [Header("Movement")]
    public float moveSpeedToSlot = 2f;
    public float smoothTime = 0.3f;
    public float stopDistance = 0.05f;
    
    private Vector3 slotTarget;
    private int assignedSlotIndex = -1;
    private bool slotReached = false;
    private Vector3 velocity = Vector3.zero;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public float initialFireDelay = 0.5f;
    public float fireCooldown = 2f;
    private float fireTimer;
    private bool isFirstAttack = true;
    public float bulletSpeed = 3f;

    // 🔥 ANIMATOR REFERENCE (sudah ada di EnemyBase)
    // private Animator animator;

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
        // 🔥 ANIMASI SPAWNING
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
            
            if (assignedSlotIndex >= 0)
                Debug.Log($"{gameObject.name} claimed slot {assignedSlotIndex} at {slotTarget}");
            else
                slotTarget = new Vector3(Random.Range(-3f, 3f), 2f, 0);
        }
        else
        {
            slotTarget = new Vector3(Random.Range(-3f, 3f), 2f, 0);
        }
    }

    void OnMoveEnter()
    {
        // 🔥 ANIMASI MOVING
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
            {
                currentSpeed = Mathf.Lerp(currentSpeed * 0.3f, currentSpeed, distance / (smoothTime * currentSpeed));
            }
            
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
                
                // 🔥 TRIGGER IDLE (bukan attack)
                animator.SetTrigger("reachedTarget");
                _fsm.ChangeState(EnemyState.Idle);
            }
        }
    }

    void OnIdleEnter()
    {
        // 🔥 PERTAMA KALI SAMPAI → IDLE SEBENTAR (0.3s)
        if (isFirstAttack)
        {
            fireTimer = initialFireDelay;
            isFirstAttack = false;
        }
        else
        {
            // 🔥 SELANJUTNYA → IDLE LEBIH LAMA (2s)
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
        // if (bulletPrefab != null && player != null)
        // {
        //     Vector3 dir = (player.position - transform.position).normalized;
        //     float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        //     Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0, 0, angle - 90f));
        // }

        fireTimer = fireCooldown;
        _fsm.ChangeState(EnemyState.Idle);
    }

    public void FireBullet()
    {
        if (bulletPrefab != null && player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0, 0, angle - 90f));

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.baseDamage = damage;
                bullet.type = BulletType.EnemyBullet; // 🔥 PASTIKAN INI ADA!
                bullet.speed = bulletSpeed * PowerUpManager.enemyBulletSpeedMultiplier;
            }
        }
    }

    protected override void Die()
    {
        // 🔥 RELEASE SLOT DULU SEBELUM ANIMASI
        if (assignedSlotIndex >= 0 && FormationManager.Instance != null)
        {
            FormationManager.Instance.ReleaseSlot(assignedSlotIndex);
            assignedSlotIndex = -1;
        }

        base.Die(); // Panggil base (trigger animasi + destroy after delay)
    }
}