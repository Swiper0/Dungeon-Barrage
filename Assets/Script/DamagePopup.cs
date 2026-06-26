using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 2f;
    public float fadeDuration = 0.8f;
    public float lifetime = 1f;
    public int sortingOrder = 50;

    private TextMeshPro damageText;
    private Color textColor;
    private float fadeTimer;

    void Awake()
    {
        damageText = GetComponent<TextMeshPro>();
    }

    public void Setup(int damage, Color color)
    {
        if (damageText == null)
            damageText = GetComponent<TextMeshPro>();

        damageText.text = damage.ToString();
        damageText.color = color;
        textColor = color;
        fadeTimer = fadeDuration;

        MeshRenderer renderer = damageText.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = sortingOrder;

        Destroy(gameObject, lifetime);
    }

    public static Vector3 GetPositionAroundTarget(Transform target, float padding = 0.02f)
    {
        Bounds bounds = GetTargetBounds(target);
        float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
        float radiusScale = Random.Range(0.6f, 0.75f);
        float radiusX = bounds.extents.x * radiusScale + padding;
        float radiusY = bounds.extents.y * radiusScale + padding;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radiusX,
            Mathf.Sin(angle) * radiusY,
            0f
        );

        return bounds.center + offset;
    }

    static Bounds GetTargetBounds(Transform target)
    {
        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            return spriteRenderer.bounds;

        SpriteRenderer childSprite = target.GetComponentInChildren<SpriteRenderer>();
        if (childSprite != null)
            return childSprite.bounds;

        Collider2D collider = target.GetComponent<Collider2D>();
        if (collider != null)
            return collider.bounds;

        return new Bounds(target.position, Vector3.one * 0.6f);
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            textColor.a = Mathf.Lerp(0f, 1f, fadeTimer / fadeDuration);
            damageText.color = textColor;
        }
    }
}
