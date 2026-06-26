using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class HomingBullet : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 12f;
    public float rotateSpeed = 600f;

    [Header("Attack Settings")]
    public int damage;
    public GameObject hitEffectPrefab;

    // 🔥 FLAG AGAR SPLIT BULLET TIDAK SPLIT LAGI
    [HideInInspector] public bool isSplitBullet = false;

    private Transform target;
    private Rigidbody2D rb;
    private Camera cam;
    private PlayerCombat playerCombat; // 🔥 CACHED

    [Header("Spawn Kill Prevention")]
    public float screenOffset = 0.2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        FindClosestEnemy();
        cam = Camera.main;
        playerCombat = FindObjectOfType<PlayerCombat>(); // 🔥 CACHE SEKALI SAJA
    }

    void Update()
    {
        if (target == null || IsTargetDead())
            FindClosestEnemy();

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        float top    = cam.transform.position.y + camHalfHeight - screenOffset;
        float bottom = cam.transform.position.y - camHalfHeight + screenOffset;
        float left   = cam.transform.position.x - camHalfWidth  + screenOffset;
        float right  = cam.transform.position.x + camHalfWidth  - screenOffset;
        Vector3 pos  = transform.position;

        if (pos.y > top || pos.y < bottom || pos.x < left || pos.x > right)
            Destroy(gameObject);
    }

    void FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemyObj in enemies)
        {
            EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
            if (enemy == null || enemy.isDead || enemy.currentStateDebug == "Dead") continue;

            float dist = Vector2.Distance(transform.position, enemyObj.transform.position);
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                nearestEnemy = enemyObj;
            }
        }

        target = nearestEnemy != null ? nearestEnemy.transform : null;
    }

    bool IsTargetDead()
    {
        if (target == null) return true;
        EnemyBase enemy = target.GetComponent<EnemyBase>();
        if (enemy == null) return true;
        return enemy.isDead || enemy.currentStateDebug == "Dead";
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            Vector2 direction = ((Vector2)target.position - rb.position).normalized;
            float rotateAmount = Vector3.Cross(direction, transform.up).z;
            rb.angularVelocity = -rotateAmount * rotateSpeed;
            rb.velocity = transform.up * speed;
        }
        else
        {
            rb.velocity = transform.up * speed;
            rb.angularVelocity = 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.isDead)
            {
                PlayerCombat pc = FindObjectOfType<PlayerCombat>();
                
                int finalDamage = damage;
                if (pc != null && enemy.rarity == EnemyBase.EnemyRarity.Boss)
                {
                    finalDamage = Mathf.RoundToInt(finalDamage * pc.bossDamageMultiplier);
                }

                enemy.TakeDamage(finalDamage);

                if (pc != null && pc.hasKineticHeal) pc.Heal(5);

                // 🔥 CEK !isSplitBullet AGAR PELURU PECAHAN TIDAK PECAH LAGI
                if (pc != null && pc.hasBulletSplit && !this.isSplitBullet) 
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        float angle = 30f * i;
                        GameObject splitBullet = Instantiate(gameObject, transform.position, 
                            Quaternion.Euler(0, 0, transform.eulerAngles.z + angle));
                        
                        HomingBullet splitScript = splitBullet.GetComponent<HomingBullet>();
                        if (splitScript != null)
                        {
                            splitScript.damage = damage / 2;
                            splitScript.isSplitBullet = true; // 🔥 TANDAI PELURU INI
                        }
                        Destroy(splitBullet, 3f);
                    }
                }

                SpawnHitEffect();
                Destroy(gameObject);
            }
        }
    }

    void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}