using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class EnemyBase : MonoBehaviour
{
    public enum EnemyState { Spawn, Idle, Move, Attack, Dead }
    public enum EnemyRarity { Common, Uncommon, Elite, Boss }

    [Header("Base Stats")]
    public EnemyRarity rarity = EnemyRarity.Common;
    public int maxHealth = 100;
    public int damage = 10;
    protected int currentHealth;

    [Header("Damage Popup")]
public GameObject damagePopupPrefab;


    /// <summary>
    /// Dipanggil oleh WaveManager setelah Instantiate, sebelum Start().
    /// Mengatur HP dan damage berdasarkan siklus wave.
    /// </summary>
    public void ApplyScaling(int wave)
    {
        if (rarity == EnemyRarity.Boss)
        {
            // Boss loop = siklus 30-wave
            // Wave 10 = loop 0, wave 40 = loop 1, wave 70 = loop 2
            int loop = Mathf.Max(0, (wave / 10 - 1) / 3);
            maxHealth = Mathf.RoundToInt(maxHealth * (1f + loop * 0.5f));
            damage    = Mathf.RoundToInt(damage * (1f + loop * 0.25f));
        }
        else
        {
            // Musuh biasa: siklus = kelipatan 10 yang sudah dilewati
            int cycle = (wave - 1) / 10;
            maxHealth = Mathf.RoundToInt(maxHealth * (1f + cycle * 0.2f));
            damage    = Mathf.RoundToInt(damage * (1f + cycle * 0.1f));
        }
    }

    void SpawnDamagePopup(int damage, Color color)
    {
        if (damagePopupPrefab == null) return;

        Vector3 pos = DamagePopup.GetPositionAroundTarget(transform);

        GameObject popup = Instantiate(damagePopupPrefab, pos, Quaternion.identity);
        DamagePopup dp = popup.GetComponent<DamagePopup>();
        if (dp != null)
            dp.Setup(damage, color);
    }

    [Header("Boundary")]
    public float padding = 0.5f;
    protected float minX, maxX, minY, maxY;
    protected bool boundsReady = false;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    [Header("HP UI")]
    public Transform hpbarPos;
    public GameObject hpBarPrefab;
    protected Slider hpSlider;
    protected Image hpFillImage;
    protected Text hpText;
    
    [Header("Drop Settings")]
    public GameObject coinPrefab;        // Untuk koin biasa (1 koin)
    public GameObject bossCoinPrefab;    // 🔥 TAMBAHKAN INI untuk koin bos (15 koin)
    [Range(0f, 100f)] public float coinDropChance = 30f;

    protected Transform player;
    protected Color originalColor;
    protected Coroutine flashRoutine;

    protected simpleFSM<EnemyState> _fsm = new simpleFSM<EnemyState>();
    public string currentStateDebug;
    public bool isDead = false;
    protected Animator animator;

    protected virtual void Start()
{
    currentHealth = maxHealth;
    InitBounds();

    animator = GetComponent<Animator>();

    GameObject p = GameObject.FindGameObjectWithTag("Player");
    if (p != null) player = p.transform;

    if (spriteRenderer == null)
        spriteRenderer = GetComponent<SpriteRenderer>();

    if (spriteRenderer != null)
        originalColor = spriteRenderer.color;

    // 🔥 CARI HP BAR: Slider atau Image Fill
    if (hpSlider == null)
        hpSlider = GetComponentInChildren<Slider>(true);

    if (hpFillImage == null)
    {
        // Cari Image dengan nama "Fill" atau type Filled
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img.name.Contains("Fill") || img.type == Image.Type.Filled)
            {
                hpFillImage = img;
                break;
            }
        }
    }

    if (hpSlider != null)
    {
        hpSlider.maxValue = maxHealth;
        hpSlider.value = currentHealth;
    }

    if (hpFillImage != null)
    {
        hpFillImage.type = Image.Type.Filled;
        hpFillImage.fillMethod = Image.FillMethod.Horizontal;
        hpFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        hpFillImage.fillAmount = 1f;
    }

    // Cari Text untuk HP
    if (hpText == null)
        hpText = GetComponentInChildren<Text>(true);

    UpdateHPBar();
    UpdateHPText();

    InitFSM();
}

    protected virtual void InitFSM() { }

    void InitBounds()
    {
        if (Camera.main == null) return;

        float h = Camera.main.orthographicSize;
        float w = h * Camera.main.aspect;

        minX = -w + padding;
        maxX = w - padding;
        minY = -h + padding;
        maxY = h + 5f;

        boundsReady = true;
    }

    protected virtual void Update()
    {
        currentStateDebug = _fsm.currentstate.ToString();

        if (currentHealth <= 0 && _fsm.currentstate != EnemyState.Dead)
        {
            Die();
            return;
        }

        if (_fsm.StateInitialized)
            _fsm.Update();

        if (!boundsReady)
            InitBounds();
    }

    protected virtual void LateUpdate()
    {
        ClampPosition();
    }

    void ClampPosition()
    {
        if (!boundsReady) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    public virtual void TakeDamage(int damage)
{
    if (isDead) return;

    currentHealth -= damage;
    if (currentHealth < 0) currentHealth = 0;

    // 🔥 SFX ENEMY HIT
    if (AudioManager.Instance != null)
        AudioManager.Instance.PlaySFXOverlap("EnemyHit", 0.4f);

    SpawnDamagePopup(damage, Color.white);

    if (hpSlider != null) hpSlider.value = currentHealth;
    UpdateHPBar();
    UpdateHPText();

    if (spriteRenderer != null)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash());
    }

    if (currentHealth <= 0) Die();
}

    IEnumerator Flash()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    protected virtual void Die()
{
    if (isDead) return;
    isDead = true;

    if (_fsm.currentstate == EnemyState.Dead) return;

    _fsm.ChangeState(EnemyState.Dead);

    // 🔥 RESET SEMUA TRIGGER AGAR TIDAK GANGGU DEATH
    if (animator != null)
    {
        animator.ResetTrigger("spawnDone");
        animator.ResetTrigger("startMove");
        animator.ResetTrigger("reachedTarget");
        animator.ResetTrigger("doAttack");
        animator.ResetTrigger("toIdle");
        animator.ResetTrigger("endAttack");
        animator.SetBool("isDead", true);
    }

    Collider2D col = GetComponent<Collider2D>();
    if (col != null) col.enabled = false;

    if (hpSlider != null) hpSlider.gameObject.SetActive(false);
    if (hpFillImage != null) hpFillImage.gameObject.SetActive(false);
    if (hpText != null) hpText.gameObject.SetActive(false);

    // 🔥 LOGIKA DROP KOIN BARU
        if (rarity == EnemyRarity.Boss)
        {
            // Jika Bos, 100% drop koin khusus bos
            if (bossCoinPrefab != null)
            {
                Instantiate(bossCoinPrefab, transform.position, Quaternion.identity);
            }
        }
        else
        {
            // Jika Musuh Biasa, gunakan peluang drop koin biasa
            if (coinPrefab != null)
            {
                float roll = Random.Range(0f, 100f);
                if (roll <= coinDropChance)
                {
                    Instantiate(coinPrefab, transform.position, Quaternion.identity);
                }
            }
        }

    if (WaveManager.Instance != null) WaveManager.Instance.OnEnemyKilled();

    StartCoroutine(DestroyAfterDeath());
}

IEnumerator DestroyAfterDeath()
{
    yield return null;

    float deathAnimLength = 0.5f;
    if (animator != null)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.length > 0.01f)
            deathAnimLength = stateInfo.length;
    }

    // 🔥 MINIMAL 1 DETIK
    if (deathAnimLength < 1f) deathAnimLength = 1f;

    yield return new WaitForSeconds(deathAnimLength);
    Destroy(gameObject);
}

    void UpdateHPBar()
    {
        if (hpFillImage != null && maxHealth > 0)
            hpFillImage.fillAmount = (float)currentHealth / maxHealth;
    }

    void UpdateHPText()
    {
        if (hpText != null)
            hpText.text = currentHealth + " / " + maxHealth;
    }
}