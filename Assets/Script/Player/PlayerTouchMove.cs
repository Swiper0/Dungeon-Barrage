using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(Animator))]
public class PlayerTouchMove : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject normalBulletPrefab;
    public GameObject absorptionBulletPrefab;

    [Header("Player Stats")]
    public int playerDamage = 10;
    public int empoweredDamage = 15;
    
    private int originalPlayerDamage;
    private int originalEmpoweredDamage;

    [Header("Movement Settings")]
    public float padding = 0.7f;
    [Range(0.1f, 1f)]
    public float overloadSpeedPenalty = 0.4f;

    [Header("Shooting Settings")]
    public Transform shootPoint;
    public float shootInterval = 0.3f;

    // 🔥 POWER-UP VARIABLES
    [HideInInspector] public bool hasWideShot = false;
    [HideInInspector] public bool hasEchoShot = false;
    [HideInInspector] public bool hasPierce = false;
    [HideInInspector] public bool hasOvercharge = false;  // 🔥 TAMBAHKAN
    [HideInInspector] public bool hasMirrorShot = false;  // 🔥 TAMBAHKAN
    private int echoShotCounter = 0;


    private Camera cam;
    private bool isDragging = false;
    private Vector3 offset;
    private Coroutine shootingCoroutine;
    private Coroutine burstCoroutine;
    private float nextFireTime = 0f;
    private PlayerCombat playerCombat;
    private bool isBursting = false;
    private bool isInputDisabled = false;
    private Animator animator;

    [HideInInspector] public float burstInterval = 0.3f;

    


    void Start()
    {
        originalPlayerDamage = playerDamage;       // 🔥 SIMPAN DAMAGE ASLI
        originalEmpoweredDamage = empoweredDamage; // 🔥 SIMPAN BURST ASLI

        cam = Camera.main;
        playerCombat = GetComponent<PlayerCombat>();
        animator = GetComponent<Animator>();
        SetAnimationState("Idle");

        burstInterval = shootInterval;

        if (GameManager.Instance != null)
            GameManager.Instance.StopGame();
    }

    void Update()
    {
        if (isInputDisabled) return;
        if (OptionManager.Instance != null && OptionManager.Instance.optionPanel.activeSelf) return;
        if (GameOverManager.Instance != null && GameOverManager.Instance.gameOverPanel != null && 
            GameOverManager.Instance.gameOverPanel.activeSelf) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosWorld = cam.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, cam.nearClipPlane));
            touchPosWorld.z = 0;

            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            switch (touch.phase)
            {
                case TouchPhase.Began: HandleTouchBegan(touchPosWorld); break;
                case TouchPhase.Moved: HandleTouchMoved(touchPosWorld); break;
                case TouchPhase.Ended: case TouchPhase.Canceled: HandleTouchEnded(); break;
            }
        }

        #if UNITY_EDITOR
        HandleEditorInput();
        #endif
    }

    #if UNITY_EDITOR
    void HandleEditorInput()
    {
        if (GameOverManager.Instance != null && GameOverManager.Instance.gameOverPanel != null && 
            GameOverManager.Instance.gameOverPanel.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePosWorld.z = 0;
            HandleTouchBegan(mousePosWorld);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 mousePosWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePosWorld.z = 0;
            HandleTouchMoved(mousePosWorld);
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            HandleTouchEnded();
        }
    }
    #endif

    void HandleTouchBegan(Vector3 touchPosWorld)
    {
        RaycastHit2D hit = Physics2D.Raycast(touchPosWorld, Vector2.zero);
        if (hit.collider != null && hit.transform == transform)
        {
            isDragging = true;
            offset = transform.position - touchPosWorld;

             if (playerCombat != null)
    {
        playerCombat.SetAbsorbMode(false); // 🔥 COLLIDER KECIL
        playerCombat.ResetAbsorbBarInstant();
    }

            animator.Play("Attack", 0, 0f);
            StartGameLogic();

            if (playerCombat != null && playerCombat.currentEnergy > 0 && !isBursting)
            {
                isBursting = true;
                burstCoroutine = StartCoroutine(BurstHomingShoot());
            }
            else if (!isBursting)
            {
                StartShooting();
            }
        }
    }

    void HandleTouchMoved(Vector3 touchPosWorld)
    {
        if (!isDragging) return;
        float currentSpeedModifier = playerCombat != null && playerCombat.IsOverloaded() ? overloadSpeedPenalty : 1f;

        Vector3 targetPos = touchPosWorld + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, currentSpeedModifier);

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;
        float minX = cam.transform.position.x - camHalfWidth + padding;
        float maxX = cam.transform.position.x + camHalfWidth - padding;
        float minY = cam.transform.position.y - camHalfHeight + padding;
        float maxY = cam.transform.position.y + camHalfHeight - padding;

        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);
        transform.position = clampedPos;
    }

    void HandleTouchEnded()
{
    if (!isDragging) return;
    isDragging = false;
    animator.SetTrigger("Idle");
    StopShooting();
    OnInputReleased(); // SetAbsorbMode(true) dipanggil di sini → coroutine mulai
}

    void StartGameLogic()
{
    if (!GameManager.Instance.gameStarted)
    {
        Time.timeScale = 1;
        GameManager.Instance.StartGame();

        if (WaveManager.Instance != null) WaveManager.Instance.StartWaves();
        // 🔥 JANGAN PANGGIL HideAllButtons DI SINI

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("Battle");
            AudioManager.Instance.StartFadeIn(3f);
        }
    }
}

    void StartShooting()
    {
        if (shootingCoroutine == null)
            shootingCoroutine = StartCoroutine(ShootRoutine());
    }

    void StopShooting()
    {
        if (shootingCoroutine != null)
        {
            StopCoroutine(shootingCoroutine);
            shootingCoroutine = null;
        }
    }

    IEnumerator ShootRoutine()
{
    while (isDragging && GameManager.Instance.gameStarted && !isBursting)
    {
        if (Time.time < nextFireTime)
            yield return new WaitForSeconds(nextFireTime - Time.time);
        if (!isDragging) yield break;

        if (hasWideShot) ShootWide();
        else Shoot();

        if (hasEchoShot)
        {
            echoShotCounter++;
            if (echoShotCounter >= 3)
            {
                echoShotCounter = 0;
                // 🔥 JEDA KECIL SEBELUM TEMBAKAN TAMBAHAN
                yield return new WaitForSeconds(0.08f);
                Shoot();
            }
        }

        nextFireTime = Time.time + shootInterval;
        yield return new WaitForSeconds(shootInterval);
    }
    shootingCoroutine = null;
}

    void Shoot()
{
    if (normalBulletPrefab == null) return;

    GameObject bulletObj = Instantiate(normalBulletPrefab, shootPoint.position, Quaternion.identity);
    Bullet bulletScript = bulletObj.GetComponent<Bullet>();
    if (bulletScript != null)
    {
        bulletScript.type = BulletType.Normal;
        bulletScript.baseDamage = playerDamage;

        if (hasOvercharge) bulletScript.pierceCount = 99;
        else if (hasPierce) bulletScript.pierceCount = 2;
        else bulletScript.pierceCount = 0;
    }

    // 🔥 TERAPKAN SKIN BULLET (JIKA ADA)
    if (SkinManager.Instance != null && SkinManager.Instance.currentBulletSprite != null)
    {
        SpriteRenderer bulletSR = bulletObj.GetComponent<SpriteRenderer>();
        if (bulletSR != null)
        {
            bulletSR.sprite = SkinManager.Instance.currentBulletSprite;
        }
    }
    // 🔥 JIKA NULL, PAKAI SPRITE DEFAULT DARI PREFAB

    if (hasMirrorShot)
    {
        GameObject mirrorBullet = Instantiate(normalBulletPrefab, shootPoint.position, Quaternion.Euler(0, 0, 180f));
        Bullet mirrorScript = mirrorBullet.GetComponent<Bullet>();
        if (mirrorScript != null)
        {
            mirrorScript.type = BulletType.Normal;
            mirrorScript.baseDamage = playerDamage;
        }

        // 🔥 SKIN BULLET UNTUK MIRROR
        if (SkinManager.Instance != null && SkinManager.Instance.currentBulletSprite != null)
        {
            SpriteRenderer mirrorSR = mirrorBullet.GetComponent<SpriteRenderer>();
            if (mirrorSR != null)
                mirrorSR.sprite = SkinManager.Instance.currentBulletSprite;
        }
    }

    if (AudioManager.Instance != null)
        AudioManager.Instance.PlaySFXOverlap("Sword");
}

    void ShootWide()
    {
        if (normalBulletPrefab == null) return;
        Vector3[] directions = { Vector3.up, new Vector3(-0.3f, 1f).normalized, new Vector3(0.3f, 1f).normalized };
        foreach (Vector3 dir in directions)
        {
            GameObject bulletObj = Instantiate(normalBulletPrefab, shootPoint.position, Quaternion.identity);
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.type = BulletType.Normal;
                bulletScript.baseDamage = playerDamage;

                // 🔥 PIERCE SUPPORT UNTUK WIDE SHOT
                if (hasOvercharge) bulletScript.pierceCount = 99;
                else if (hasPierce) bulletScript.pierceCount = 2;
                else bulletScript.pierceCount = 0;
            }
            bulletObj.transform.up = dir;

            // 🔥 TERAPKAN SKIN BULLET (SAMA SEPERTI Shoot())
            if (SkinManager.Instance != null && SkinManager.Instance.currentBulletSprite != null)
            {
                SpriteRenderer bulletSR = bulletObj.GetComponent<SpriteRenderer>();
                if (bulletSR != null)
                    bulletSR.sprite = SkinManager.Instance.currentBulletSprite;
            }
        }
    }

IEnumerator BurstHomingShoot()
{
    while (playerCombat != null && playerCombat.currentEnergy > 0)
    {
        playerCombat.UseEnergy();

        if (animator != null)
            animator.Play("Attack", 0, 0f);

        if (absorptionBulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(absorptionBulletPrefab, shootPoint.position, shootPoint.rotation);
            HomingBullet homingScript = bulletObj.GetComponent<HomingBullet>();
            if (homingScript != null)
                homingScript.damage = empoweredDamage;
        }

        // 🔥 SFX CHARGED SETIAP PELURU BURST
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Charged", 0.5f);

        yield return new WaitForSeconds(burstInterval * 0.7f);
    }

    isBursting = false;

    if (isDragging)
    {
        animator.Play("Attack", 0, 0f);
        nextFireTime = Time.time;
        StartShooting();
    }
    else
    {
        OnInputReleased();
    }
}

    private void OnInputReleased()
{
    SetAnimationState("Idle");
    if (!isBursting && playerCombat != null)
        playerCombat.SetAbsorbMode(true); // 🔥 COLLIDER BESAR (ABSORB)
}

    void SetAnimationState(string stateName)
    {
        if (animator == null) return;
        animator.ResetTrigger("Idle");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Absorb1");
        animator.ResetTrigger("Absorb2");
        animator.ResetTrigger("Death");
        animator.SetTrigger(stateName);
    }

    public void ResetForNewGame()
    {
        playerDamage = originalPlayerDamage;       // 🔥 KEMBALIKAN KE ASLI
        empoweredDamage = originalEmpoweredDamage; // 🔥 KEMBALIKAN KE ASLI
        burstInterval = shootInterval;             // 🔥 KEMBALIKAN SPEED BURST KE ASLI

        isDragging = false;
        isBursting = false;
        isInputDisabled = false;
        nextFireTime = 0f;

        hasWideShot = false;
        hasEchoShot = false;
        hasPierce = false;
        hasOvercharge = false;  
        hasMirrorShot = false;  
        echoShotCounter = 0;    

        StopShooting();
    if (burstCoroutine != null)
    {
        StopCoroutine(burstCoroutine);
        burstCoroutine = null;
    }
    SetAnimationState("Idle");
}

    public void EnableInput() { isInputDisabled = false; }

    public void DisableInput()
    {
        isInputDisabled = true;
        isDragging = false;
        StopShooting();
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            burstCoroutine = null;
        }
        isBursting = false;
    }
}