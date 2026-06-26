using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class VectorEnemy : EnemyBase
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float bottomOffsetForTeleport = 1f;
    public float topOffsetAfterTeleport = 1f;
    public float hitAnimDuration = 0.2f;

    private bool hasHitPlayer = false;

    protected override void InitFSM()
    {
        if (_fsm.StateInitialized) return;

        var map = new Dictionary<EnemyState, State>()
        {
            { EnemyState.Spawn,  new State(OnSpawnEnter,  null,            null, null) },
            { EnemyState.Move,   new State(OnMoveEnter,   OnMoveUpdate,    null, null) },
            { EnemyState.Dead,   new State(null, null, null, null) }
        };

        _fsm.Initialize(map);
    }

    protected override void Start()
    {
        base.Start();
        _fsm.ChangeState(EnemyState.Spawn);
    }

    protected override void LateUpdate()
    {
        if (!boundsReady) return;

        // Biarkan Y lewat bawah layar agar teleport loop bisa terjadi.
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;
    }

    // ================= SPAWN =================
    void OnSpawnEnter()
    {
        animator.SetTrigger("spawnDone");
        _fsm.ChangeState(EnemyState.Move);
    }

    // ================= MOVE =================
    void OnMoveEnter()
    {
        animator.SetInteger("attackPhase", 0);
        animator.SetTrigger("startMove");
        hasHitPlayer = false;
    }

    void OnMoveUpdate()
    {
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;

        // keluar layar bawah -> teleport ke atas, tetap lanjut jalan
        if (transform.position.y < minY - bottomOffsetForTeleport)
        {
            TeleportToTop();
        }
    }

    // ================= HIT PLAYER =================
    void OnTriggerEnter2D(Collider2D other)
    {
        TryHitPlayer(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryHitPlayer(other);
    }

    void TryHitPlayer(Collider2D other)
    {
        if (hasHitPlayer) return;
        if (!other.CompareTag("Player")) return;

        hasHitPlayer = true;

        PlayerCombat player = other.GetComponent<PlayerCombat>();
        if (player != null)
            player.TakeDamage(damage);

        StartCoroutine(PlayHitAnimation());
    }

    IEnumerator PlayHitAnimation()
    {
        // ATTACK3 - tabrak (visual saja)
        animator.SetInteger("attackPhase", 3);

        yield return new WaitForSeconds(hitAnimDuration);

        // balik ke dash visual
        animator.SetInteger("attackPhase", 2);
    }

    // ================= TELEPORT =================
    void TeleportToTop()
    {
        // 🔥 X tetap (biar ga geser)
        transform.position = new Vector3(transform.position.x, maxY + topOffsetAfterTeleport, 0f);
        hasHitPlayer = false;
        animator.SetInteger("attackPhase", 0);
    }

    // ================= DIE =================
    protected override void Die()
    {
        StopAllCoroutines();
        base.Die();
    }
}