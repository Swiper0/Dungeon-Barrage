using UnityEngine;
using System.Collections.Generic;

public class SkinAnimationApplier : MonoBehaviour
{
    [Header("Override Controllers per Skin")]
    public AnimatorOverrideController defaultSkin;
    public AnimatorOverrideController eliteSkin;
    public AnimatorOverrideController legendarySkin;

    [Header("Collider Offset")]
    public Vector2 defaultSkinColliderOffset = new Vector2(0.02f, 0f);
    public Vector2 legendarySkinColliderOffset = new Vector2(0.01f, 0f);
    public Vector2 secretSkinColliderOffset = new Vector2(-1f, 0f);

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Dictionary<string, AnimatorOverrideController> skinControllers;
    private CircleCollider2D playerCollider;
    private Vector2 originalColliderOffset;
    private Vector2 activeColliderOffset;
    private bool hasActiveColliderOffset;

    public bool IsUsingStaticSkin { get; private set; }

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        CacheCollider();
    }

    void Start()
    {
        if (SkinManager.Instance != null && SkinManager.Instance.currentCharacterSkin != null)
            ApplySkin(SkinManager.Instance.currentCharacterSkin);
    }

    void CacheCollider()
    {
        if (playerCollider == null)
            playerCollider = GetComponent<CircleCollider2D>();

        if (playerCollider != null)
            originalColliderOffset = playerCollider.offset;
    }

    public void ApplySkin(SkinData skin)
    {
        if (skin == null) return;

        CacheCollider();

        if (UsesStaticSprite(skin))
            ApplyStaticSpriteSkin(skin);
        else
            ApplyAnimatedSkin(skin.skinName);
    }

    static bool UsesStaticSprite(SkinData skin)
    {
        return skin.useStaticSprite || skin.rarity == SkinData.Rarity.Secret;
    }

    void ApplyStaticSpriteSkin(SkinData skin)
    {
        IsUsingStaticSkin = true;

        if (animator != null)
            animator.enabled = false;

        if (spriteRenderer != null && skin.skinSprite != null)
            spriteRenderer.sprite = skin.skinSprite;

        Vector2 offset = secretSkinColliderOffset;
        if (skin.useCustomColliderOffset)
            offset = skin.customColliderOffset;

        SetColliderOffset(offset);
    }

    void ApplyAnimatedSkin(string skinName)
    {
        IsUsingStaticSkin = false;

        if (animator != null)
            animator.enabled = true;

        if (skinControllers == null)
        {
            skinControllers = new Dictionary<string, AnimatorOverrideController>
            {
                { "Normal Knight", defaultSkin },
                { "Elite Knight", eliteSkin },
                { "Legendary Knight", legendarySkin }
            };
        }

        if (skinControllers.TryGetValue(skinName, out AnimatorOverrideController controller))
        {
            if (controller != null)
                animator.runtimeAnimatorController = controller;
        }

        if (skinName == "Normal Knight")
            SetColliderOffset(defaultSkinColliderOffset);
        else if (skinName == "Legendary Knight")
            SetColliderOffset(legendarySkinColliderOffset);
        else
            SetColliderOffset(originalColliderOffset);
    }

    void SetColliderOffset(Vector2 offset)
    {
        CacheCollider();
        activeColliderOffset = offset;
        hasActiveColliderOffset = true;

        if (playerCollider != null)
            playerCollider.offset = offset;
    }

    public void RefreshColliderOffset()
    {
        if (!hasActiveColliderOffset) return;

        CacheCollider();
        if (playerCollider != null)
            playerCollider.offset = activeColliderOffset;
    }
}
