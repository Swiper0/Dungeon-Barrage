using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    public int coinValue = 1;
    public float fallSpeed = 2f;
    public float lifeTime = 10f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 🔥 PANGGIL SKINMANAGER (bukan CoinManager langsung)
            if (SkinManager.Instance != null)
            {
                SkinManager.Instance.AddCoins(coinValue);
            }

            AudioManager.Instance?.PlaySFX("Coin");
            Destroy(gameObject);
        }
    }
}