using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Health Stats")]
    public int maxHealth = 200;
    private int currentHealth;
    private int originalMaxHealth;

    [Header("Absorb & Energy System")]
    public bool isAbsorbing = false;
    public int currentEnergy = 0;
    public int maxEnergy = 15;

    [Tooltip("Waktu maksimal (detik) untuk mendapatkan Perfect Absorb")]
    public float perfectAbsorbWindow = 0.2f;
    private float absorbStartTime = 0f;

    [Header("Flash Effect")]
    public SpriteRenderer spriteRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    private Color originalColor;
    private Coroutine flashCoroutine;

    [Header("HP UI")]
    public Slider hpSlider;
    public Text hpText;

    [Header("Energy UI")]
    public Slider energySlider;
    public Text energyText;
    public Image energyFillImage;
    private Color energyDefaultColor;
    
    [Header("Absorb Duration")]
    public float absorbDuration = 10f;
    private float absorbTimer = 0f;

    [Header("Absorb Visual")]
    public GameObject absorbGlow;

    [Header("Glow Settings")]
    public float blinkThreshold = 2f;
    public float blinkSpeed = 10f;

    [Header("Glow Animation")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.3f;
    public float flickerSpeed = 15f;
    public float flickerAmount = 0.15f;

    private SpriteRenderer glowRenderer;
    private float glowBaseAlpha;

    [Header("Absorb UI Settings")]
    public float smoothSpeed = 12f;
    public float fastRefillSpeed = 25f;
    public float fadeSpeed = 10f;

    private Vector3 originalGlowScale;

    [Header("Absorb UI")]
    public Slider absorbSlider;

    [Header("Absorb Slider Color")]
    public Image absorbFillImage;

    private Color normalFillColor;
    public Color warningFillColor = new Color32(135, 29, 32, 255);

    private bool absorbReady = true;
    private Animator animator;
    private bool useAbsorb1 = true;
    [HideInInspector] public float defaultRadius;
    private CircleCollider2D circleCol;
    private float touchReleasedTime = -1f;
    public float absorbSFXDelay = 0.2f;

    // 🔥 POWER-UP VARIABLES
    [HideInInspector] public bool hasEmergencyShield = false;
    [HideInInspector] public bool hasHPRegen = false;
    [HideInInspector] public bool hasKineticHeal = false;
    [HideInInspector] public bool hasBulletSplit = false;
    [HideInInspector] public bool hasMirrorShot = false;
    [HideInInspector] public bool hasOvercharge = false;
    [HideInInspector] public float absorbRadiusMultiplier = 1f;
    [HideInInspector] public float bossDamageMultiplier = 1f;
    private float hpRegenTimer = 0f;
    private float hpRegenInterval = 4f;

    private float lastAbsorbSFXTime = 0f;
    private float absorbSFXCooldown = 1.5f;
    private Coroutine absorbSFXCoroutine;
    private Coroutine glowFadeInCoroutine;

    [Header("Damage Popup")]
    public GameObject damagePopupPrefab;

    void Start()
    {
        originalMaxHealth = maxHealth;
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (absorbFillImage != null)
            normalFillColor = absorbFillImage.color;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
            defaultRadius = col.radius;

        if (absorbGlow != null)
        {
            glowRenderer = absorbGlow.GetComponent<SpriteRenderer>();
            originalGlowScale = absorbGlow.transform.localScale;

            if (glowRenderer != null)
            {
                glowBaseAlpha = glowRenderer.color.a;
                Color c = glowRenderer.color;
                c.a = 0f;
                glowRenderer.color = c;
            }

            absorbGlow.SetActive(false);
        }

        if (absorbSlider != null)
        {
            absorbSlider.maxValue = absorbDuration;
            absorbSlider.value = absorbDuration;
            absorbSlider.gameObject.SetActive(true);
        }

        if (hpSlider != null)
            hpSlider.maxValue = maxHealth;

        if (energyFillImage != null)
            energyDefaultColor = energyFillImage.color;
        else
            Debug.LogWarning("energyFillImage not assigned in Inspector!");

        if (energySlider != null)
            energySlider.maxValue = maxEnergy;

        UpdateHPUI();
        UpdateEnergyUI();

        circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null)
            defaultRadius = circleCol.radius;

        SyncGlowToCollider();
    }

    public void NotifyTouchReleased()
    {
        touchReleasedTime = Time.time;
    }

    public void SyncGlowToCollider()
    {
        if (absorbGlow == null)
        {
            Debug.LogWarning("absorbGlow is null!");
            return;
        }
        if (circleCol == null)
        {
            Debug.LogWarning("circleCol is null!");
            return;
        }

        float radiusRatio = circleCol.radius / defaultRadius;
        absorbGlow.transform.localScale = originalGlowScale * radiusRatio;
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameStarted)
            return;

        HandleAbsorbTimer();
        HandleGlowEffect();
        UpdateAbsorbUI();

        if (isAbsorbing && absorbGlow != null && circleCol != null)
        {
            absorbGlow.transform.localPosition = circleCol.offset;
            SyncGlowToCollider();
        }

        if (hasHPRegen && currentHealth < maxHealth && currentHealth > 0)
        {
            hpRegenTimer += Time.deltaTime;
            if (hpRegenTimer >= hpRegenInterval)
            {
                hpRegenTimer = 0f;
                currentHealth = Mathf.Min(maxHealth, currentHealth + 5);
                UpdateHPUI();
            }
        }
    }

    void HandleAbsorbTimer()
{
    if (!isAbsorbing) return;

    // 🔥 JIKA SHIELD AKTIF, ABSORB TIDAK PUNYA BATAS WAKTU
    if (hasEmergencyShield)
    {
        absorbTimer = absorbDuration; // Reset terus timer
        return;
    }

    absorbTimer -= Time.deltaTime;
    if (absorbTimer <= 0f) EndAbsorb();
}

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHPUI();
    }

    void UpdateAbsorbUI()
    {
        if (absorbSlider == null) return;
        float targetValue = absorbSlider.value;

        if (isAbsorbing)
        {
            targetValue = absorbTimer;
            absorbSlider.value = Mathf.Lerp(absorbSlider.value, targetValue, Time.deltaTime * smoothSpeed);
        }
        else
        {
            if (absorbReady)
            {
                targetValue = absorbDuration;
                absorbSlider.value = Mathf.MoveTowards(absorbSlider.value, targetValue, Time.deltaTime * fastRefillSpeed);
            }
            else
            {
                targetValue = 0f;
                absorbSlider.value = Mathf.Lerp(absorbSlider.value, targetValue, Time.deltaTime * smoothSpeed);
            }
        }

        if (absorbFillImage != null)
        {
            float alphaTarget = (targetValue <= 0.01f) ? 0f : 1f;

            if (isAbsorbing && absorbTimer <= blinkThreshold)
            {
                float blink = Mathf.PingPong(Time.time * 8f, 1f);
                Color blinkColor = Color.Lerp(warningFillColor, Color.white, blink);
                blinkColor.a = Mathf.Lerp(absorbFillImage.color.a, alphaTarget, Time.deltaTime * fadeSpeed);
                absorbFillImage.color = blinkColor;
            }
            else if (!isAbsorbing && !absorbReady)
            {
                Color c = warningFillColor;
                c.a = Mathf.Lerp(absorbFillImage.color.a, alphaTarget, Time.deltaTime * fadeSpeed);
                absorbFillImage.color = c;
            }
            else
            {
                Color c = normalFillColor;
                c.a = Mathf.Lerp(absorbFillImage.color.a, alphaTarget, Time.deltaTime * fadeSpeed);
                absorbFillImage.color = c;
            }
        }

        if (energyText != null)
        {
            if (currentEnergy >= maxEnergy)
            {
                float blink = Mathf.PingPong(Time.time * 6f, 1f);
                energyText.text = "MAX";
                energyText.color = Color.Lerp(Color.red, Color.white, blink);
            }
            else
            {
                energyText.text = currentEnergy + " / " + maxEnergy;
                energyText.color = Color.white;
            }
        }
    }

    void HandleGlowEffect()
{
    // 🔥 SHIELD AKTIF → GLOW SELALU AKTIF
    bool glowActive = isAbsorbing || hasEmergencyShield;
    if (!glowActive || glowRenderer == null) return;

    if (glowFadeInCoroutine != null)
    {
        float radiusRatio = (circleCol != null) ? circleCol.radius / defaultRadius : 1f;
        float scaleMultiplier = 1f + Mathf.Sin(Time.time * 3f) * 0.08f;
        absorbGlow.transform.localScale = originalGlowScale * radiusRatio * scaleMultiplier;
        return;
    }

    float baseAlpha = 0.35f;
    float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
    float flicker = (Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) - 0.5f) * flickerAmount;
    float finalAlpha = Mathf.Clamp(baseAlpha + pulse + flicker, 0f, 1f);

    // 🔥 SHIELD AKTIF + TIDAK ABSORB → GLOW HIJAU
    if (hasEmergencyShield && !isAbsorbing)
    {
        Color targetColor = new Color(0f, 1f, 0f, finalAlpha); // HIJAU
        glowRenderer.color = Color.Lerp(glowRenderer.color, targetColor, Time.deltaTime * 8f);
    }
    // 🔥 SHIELD AKTIF + SEDANG ABSORB → GLOW BIRU/PUTIH (NORMAL)
    else if (hasEmergencyShield && isAbsorbing)
    {
        if (absorbTimer <= blinkThreshold)
        {
            float t = 1f - (absorbTimer / blinkThreshold);
            float dynamicSpeed = Mathf.Lerp(2f, 6f, t);
            float blink = (Mathf.Sin(Time.time * dynamicSpeed * Mathf.PI) + 1f) * 0.5f;
            blink = Mathf.Lerp(0.2f, 1f, blink);
            float blinkAlpha = Mathf.Clamp(finalAlpha * blink, 0f, 1f);

            Color normalColor = Color.white;
            Color silverColor = new Color(0.75f, 0.75f, 0.75f);
            Color targetColor = Color.Lerp(normalColor, silverColor, t);
            targetColor.a = blinkAlpha;
            glowRenderer.color = Color.Lerp(glowRenderer.color, targetColor, Time.deltaTime * 12f);
        }
        else
        {
            Color c = glowRenderer.color;
            c.a = Mathf.Lerp(c.a, finalAlpha, Time.deltaTime * 8f);
            glowRenderer.color = c;
        }
    }
    // 🔥 ABSORB NORMAL (TANPA SHIELD)
    else if (absorbTimer <= blinkThreshold)
    {
        float t = 1f - (absorbTimer / blinkThreshold);
        float dynamicSpeed = Mathf.Lerp(2f, 6f, t);
        float blink = (Mathf.Sin(Time.time * dynamicSpeed * Mathf.PI) + 1f) * 0.5f;
        blink = Mathf.Lerp(0.2f, 1f, blink);
        float blinkAlpha = Mathf.Clamp(finalAlpha * blink, 0f, 1f);

        Color normalColor = Color.white;
        Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        Color targetColor = Color.Lerp(normalColor, silverColor, t);
        targetColor.a = blinkAlpha;
        glowRenderer.color = Color.Lerp(glowRenderer.color, targetColor, Time.deltaTime * 12f);
    }
    else
    {
        Color c = glowRenderer.color;
        c.a = Mathf.Lerp(c.a, finalAlpha, Time.deltaTime * 8f);
        glowRenderer.color = c;
    }

    float radiusRatioFinal = (circleCol != null) ? circleCol.radius / defaultRadius : 1f;
    float scaleMult = 1f + Mathf.Sin(Time.time * 3f) * 0.08f;
    absorbGlow.transform.localScale = originalGlowScale * radiusRatioFinal * scaleMult;
}

    void EndAbsorb()
{
    isAbsorbing = false;
    absorbTimer = 0f;
    absorbReady = false;

    // 🔥 JANGAN MATIKAN GLOW JIKA SHIELD MASIH AKTIF
    if (hasEmergencyShield)
    {
        // Tetap nyalakan glow dengan warna hijau
        if (absorbGlow != null && !absorbGlow.activeSelf)
        {
            absorbGlow.SetActive(true);
            SyncGlowToCollider();
        }
        return;
    }

    // Normal: matikan glow
    if (absorbGlow != null)
    {
        absorbGlow.SetActive(false);
        if (glowRenderer != null)
        {
            Color c = glowRenderer.color;
            c.a = 0f;
            glowRenderer.color = c;
        }
    }
}

    public void ResetAbsorbBarInstant()
    {
        absorbReady = true;
        if (absorbFillImage != null)
        {
            absorbFillImage.enabled = true;
            Color c = normalFillColor;
            c.a = 1f;
            absorbFillImage.color = c;
        }
    }

    public void ResetAbsorbReady()
    {
        absorbReady = true;
        absorbTimer = absorbDuration;
        if (absorbSlider != null)
            absorbSlider.value = absorbDuration;
        if (absorbFillImage != null)
        {
            Color c = normalFillColor;
            c.a = 1f;
            absorbFillImage.color = c;
        }
    }


    public void SetAbsorbMode(bool status)
{
    // 🔥 SHIELD AKTIF → SAAT ATTACK (status=false), TETAP IMMUNE TAPI TIDAK ABSORB
    if (!status && hasEmergencyShield)
    {
        isAbsorbing = false;
        
        // 🔥 GLOW TETAP HIJAU (SUDAH DI-HANDLE HandleGlowEffect)
        if (absorbGlow != null && !absorbGlow.activeSelf)
        {
            absorbGlow.SetActive(true);
            SyncGlowToCollider();
        }
        
        // 🔥 COLLIDER TETAP BESAR (TIDAK DIUBAH)
        // Tidak perlu ubah radius karena shield = immune, bukan absorb
        
        RefreshSkinColliderOffset();
        
        // Hentikan coroutine absorb
        if (absorbSFXCoroutine != null) 
        { 
            StopCoroutine(absorbSFXCoroutine); 
            absorbSFXCoroutine = null; 
        }
        if (glowFadeInCoroutine != null) 
        { 
            StopCoroutine(glowFadeInCoroutine); 
            glowFadeInCoroutine = null; 
        }
        
        return;
    }

    if (isAbsorbing == status) return;
    isAbsorbing = status;

    if (status)
    {
        absorbStartTime = Time.time;
        absorbTimer = absorbDuration;
        absorbReady = true;

        if (circleCol != null)
            circleCol.radius = defaultRadius * absorbRadiusMultiplier;

        RefreshSkinColliderOffset();

        if (absorbSFXCoroutine != null) { StopCoroutine(absorbSFXCoroutine); absorbSFXCoroutine = null; }
        if (glowFadeInCoroutine != null) { StopCoroutine(glowFadeInCoroutine); glowFadeInCoroutine = null; }

        if (absorbGlow != null)
        {
            if (glowRenderer != null)
            {
                Color c = glowRenderer.color;
                c.a = 0f;
                glowRenderer.color = c;
            }
            absorbGlow.SetActive(true);
            if (circleCol != null)
                absorbGlow.transform.localPosition = circleCol.offset;
            SyncGlowToCollider();
            glowFadeInCoroutine = StartCoroutine(GlowFadeIn(absorbSFXDelay));
            absorbSFXCoroutine = StartCoroutine(PlayAbsorbGlowSFXDelayed(absorbSFXDelay));
        }
    }
    else
    {
        if (circleCol != null)
            circleCol.radius = defaultRadius * 0.5f;

        RefreshSkinColliderOffset();

        if (absorbSFXCoroutine != null) { StopCoroutine(absorbSFXCoroutine); absorbSFXCoroutine = null; }
        if (glowFadeInCoroutine != null) { StopCoroutine(glowFadeInCoroutine); glowFadeInCoroutine = null; }

        EndAbsorb();
    }
}

    void RefreshSkinColliderOffset()
    {
        SkinAnimationApplier applier = GetComponent<SkinAnimationApplier>();
        if (applier != null)
            applier.RefreshColliderOffset();
    }

    IEnumerator PlayAbsorbGlowSFXDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance?.PlaySFX("AbsorbGlow", 0.5f);
        absorbSFXCoroutine = null;
    }

    // 🔥 FADE IN GLOW SESUAI absorbSFXDelay
    IEnumerator GlowFadeIn(float duration)
{
    if (glowRenderer == null) yield break;

    float elapsed = 0f;
    float startAlpha = glowRenderer.color.a; // 🔥 MULAI DARI ALPHA SEKARANG, BUKAN 0
    float targetAlpha = 0.35f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, elapsed / duration); // 🔥 SMOOTHSTEP AGAR TIDAK KAKU

        Color c = glowRenderer.color;
        c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
        glowRenderer.color = c;

        yield return null;
    }

    Color final = glowRenderer.color;
    final.a = targetAlpha;
    glowRenderer.color = final;

    glowFadeInCoroutine = null;
}


    private void OnTriggerEnter2D(Collider2D other)
    {
        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet != null && (bullet.type == BulletType.EnemyBullet || bullet.type == BulletType.UnabsorbableBullet))
            HandleEnemyBulletHit(bullet);
    }

    public void HandleEnemyBulletHit(Bullet bullet)
{
    if (bullet == null) return;
    if (bullet.hasBeenHit) return;
    bullet.hasBeenHit = true;

    // 🔥 SHIELD AKTIF → IMMUNE SEMUA DAMAGE (BAHKAN UnabsorbableBullet)
    if (hasEmergencyShield)
    {
        Debug.Log("Emergency Shield blocked damage! Player is IMMUNE!");
        Destroy(bullet.gameObject);
        return;
    }

    if (bullet.type == BulletType.EnemyBullet || bullet.type == BulletType.UnabsorbableBullet)
    {
        // Absorb normal
        if (isAbsorbing && bullet.type == BulletType.EnemyBullet)
        {
            if (currentEnergy < maxEnergy)
                AbsorbSuccess();
            else
                TakeDamage(bullet.baseDamage);

            Destroy(bullet.gameObject);
            return;
        }

        if (isAbsorbing && bullet.type == BulletType.UnabsorbableBullet)
        {
            TakeDamage(bullet.absorptionDamage > 0 ? bullet.absorptionDamage : bullet.baseDamage);
            Destroy(bullet.gameObject);
            return;
        }

        TakeDamage(bullet.baseDamage);
    }

    Destroy(bullet.gameObject);
}

    private void AbsorbSuccess()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Absorb",1.5f);

        if (animator != null)
        {
            if (useAbsorb1) animator.SetTrigger("Absorb1");
            else animator.SetTrigger("Absorb2");
            useAbsorb1 = !useAbsorb1;
        }

        float timeSinceAbsorbStarted = Time.time - absorbStartTime;
        if (timeSinceAbsorbStarted <= perfectAbsorbWindow)
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + 3);
        else
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + 1);

        UpdateEnergyUI();
    }

    public bool UseEnergy()
    {
        if (currentEnergy > 0)
        {
            currentEnergy--;
            UpdateEnergyUI();
            return true;
        }
        return false;
    }

    IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    public bool IsOverloaded()
    {
        return currentEnergy > (maxEnergy / 2f);
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

    public void TakeDamage(int damage)
{
    // 🔥 SHIELD AKTIF → IMMUNE SEMUA DAMAGE
    if (hasEmergencyShield)
    {
        Debug.Log("Emergency Shield blocked non-bullet damage! Player is IMMUNE!");
        return;
    }

    int finalDamage = damage;
    if (currentEnergy > maxEnergy / 2)
    {
        finalDamage = damage * 2;
        Debug.Log($"Double damage! Energy: {currentEnergy}/{maxEnergy}, Damage: {damage} → {finalDamage}");
    }

    currentHealth -= finalDamage;
    if (currentHealth < 0) currentHealth = 0;
    UpdateHPUI();

    AudioManager.Instance?.PlaySFX("PlayerHit", 0.6f);
    SpawnDamagePopup(finalDamage, Color.red);

    if (DamageVignette.Instance != null)
        DamageVignette.Instance.Flash();

    if (spriteRenderer != null)
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    if (currentHealth <= 0) Die();
}

    void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHealth;
            if (hpSlider.fillRect != null)
                hpSlider.fillRect.gameObject.SetActive(currentHealth > 0);
        }
        if (hpText != null)
            hpText.text = currentHealth + " / " + maxHealth;
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy;
            if (energySlider.fillRect != null)
                energySlider.fillRect.gameObject.SetActive(currentEnergy > 0);
        }

        if (energyFillImage != null)
        {
            if (currentEnergy >= maxEnergy)
                energyFillImage.color = Color.gray;
            else
                energyFillImage.color = energyDefaultColor;
        }
    }

    private void Die()
    {
        Debug.Log("Player Hancur!");

        PlayerTouchMove touchMove = GetComponent<PlayerTouchMove>();
        if (touchMove != null) touchMove.DisableInput();

        if (animator != null)
        {
            animator.SetTrigger("Death");
            StartCoroutine(ShowGameOverAfterDeath());
        }
        else
        {
            ShowGameOver();
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    IEnumerator ShowGameOverAfterDeath()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        if (GameManager.Instance != null) GameManager.Instance.StopGame();
        ShowGameOver();
    }

    void ShowGameOver()
    {
        if (GameOverManager.Instance != null)
        {
            int finalWave = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWave() : 0;
            GameOverManager.Instance.ShowGameOver(finalWave);
        }
    }

    public void FullReset()
{
    maxHealth = originalMaxHealth;
    currentHealth = maxHealth;
    currentEnergy = 0;
    isAbsorbing = false;
    absorbTimer = 0f;
    absorbReady = true;
    absorbStartTime = 0f;

    hasEmergencyShield = false;
    hasHPRegen = false;
    hasKineticHeal = false;
    hasBulletSplit = false;

    absorbRadiusMultiplier = 1f;
    bossDamageMultiplier = 1f;
    hpRegenTimer = 0f;

    // 🔥 STOP COROUTINE SAAT RESET
    if (absorbSFXCoroutine != null)
    {
        StopCoroutine(absorbSFXCoroutine);
        absorbSFXCoroutine = null;
    }

    if (glowFadeInCoroutine != null)
    {
        StopCoroutine(glowFadeInCoroutine);
        glowFadeInCoroutine = null;
    }

    if (flashCoroutine != null)
    {
        StopCoroutine(flashCoroutine);
        flashCoroutine = null;
    }

    if (spriteRenderer != null)
    {
        spriteRenderer.color = originalColor;
        spriteRenderer.enabled = true;
    }

    // 🔥 MATIKAN GLOW SAAT RESET
    if (absorbGlow != null)
    {
        absorbGlow.SetActive(false);
        if (glowRenderer != null)
        {
            Color c = glowRenderer.color;
            c.a = 0f;
            glowRenderer.color = c;
        }
    }

    if (absorbSlider != null)
        absorbSlider.value = absorbDuration;

    if (absorbFillImage != null)
        absorbFillImage.color = normalFillColor;

    UpdateHPUI();
    UpdateEnergyUI();

    Collider2D col = GetComponent<Collider2D>();
    if (col != null) col.enabled = true;
}
}