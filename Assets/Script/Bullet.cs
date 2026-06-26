using UnityEngine;
using System.Collections.Generic;

public enum BulletType
{
    Normal,
    Absorption,
    EnemyBullet,
    UnabsorbableBullet
}

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public BulletType type = BulletType.Normal;

    [HideInInspector] public int baseDamage       = 0;
    [HideInInspector] public int absorptionDamage  = 0;
    [HideInInspector] public int pierceCount       = 0;

    public float speed = 8f;
    public GameObject hitEffectPrefab;

    [Header("Spawn Kill Prevention")]
    public float screenOffset = 0.2f;
    [Tooltip("Jika true, peluru tidak di-Destroy saat keluar kamera.")]
    public bool ignoreScreenBounds = false;
    [HideInInspector] public bool hasBeenHit = false;

    private Camera cam;
    private PlayerCombat playerCombat; // 🔥 CACHED
    private bool hitConsumed;
    private HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();

    void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.useFullKinematicContacts = true;
    }

    void Start()
    {
        cam = Camera.main;
        playerCombat = FindObjectOfType<PlayerCombat>(); // 🔥 CACHE SEKALI SAJA
    }

    public bool TryConsumeHit()
    {
        if (hitConsumed) return false;
        hitConsumed = true;
        return true;
    }

    protected virtual void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime, Space.Self);

        if (ignoreScreenBounds) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth  = camHalfHeight * cam.aspect;

        float extraOffset = 0.5f;

        float top    = cam.transform.position.y + camHalfHeight + extraOffset - screenOffset;
    float bottom = cam.transform.position.y - camHalfHeight - extraOffset + screenOffset;
    float left   = cam.transform.position.x - camHalfWidth  - extraOffset + screenOffset;
    float right  = cam.transform.position.x + camHalfWidth  + extraOffset - screenOffset;

        Vector3 pos = transform.position;
        if (pos.y > top || pos.y < bottom || pos.x < left || pos.x > right)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Peluru musuh → player
        if ((type == BulletType.EnemyBullet || type == BulletType.UnabsorbableBullet)
            && other.CompareTag("Player"))
        {
            PlayerCombat pc = other.GetComponent<PlayerCombat>();
            if (pc != null)
                pc.HandleEnemyBulletHit(this);
            return;
        }

        // Peluru pemain → musuh
        if ((type == BulletType.Normal || type == BulletType.Absorption) && other.CompareTag("Enemy"))
{
    EnemyBase enemy = other.GetComponent<EnemyBase>();
    if (enemy == null) return;
    if (hitEnemies.Contains(enemy)) return; // 🔥 SUDAH KENA? SKIP
    hitEnemies.Add(enemy);

    SpawnHitEffect();

    int finalDamage = baseDamage;
    if (enemy.rarity == EnemyBase.EnemyRarity.Boss && playerCombat != null && playerCombat.bossDamageMultiplier > 1f)
        finalDamage = Mathf.RoundToInt(baseDamage * playerCombat.bossDamageMultiplier);

    enemy.TakeDamage(finalDamage);

    // 🔥 PIERCE? KALAU 0 → DESTROY
    if (pierceCount > 0)
    {
        pierceCount--;
        return;
    }

    Destroy(gameObject);
}
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }
}