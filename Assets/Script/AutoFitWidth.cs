using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class AutoFillBackground : MonoBehaviour
{
    private Camera cam;
    private SpriteRenderer sr;

    void Start()
    {
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
        ScaleToFill();
    }

#if UNITY_EDITOR
    void Update()
    {
        ScaleToFill();
    }
#endif

    void ScaleToFill()
    {
        if (cam == null) cam = Camera.main;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        // 1. Simpan rotasi & Reset Scale
        Quaternion currentRot = transform.rotation;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // 2. Ambil ukuran Layar & Sprite
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        float spriteWidth = sr.bounds.size.x;
        float spriteHeight = sr.bounds.size.y;

        // 3. Hitung rasio kebutuhan
        float widthRatio = screenWidth / spriteWidth;
        float heightRatio = screenHeight / spriteHeight;

        // --- BAGIAN KUNCI ---
        // Kita ambil nilai TERBESAR (Max) di antara kebutuhan lebar atau tinggi.
        // Ini memastikan gambar di-zoom secara proporsional sampai menutupi seluruh layar.
        float finalScale = Mathf.Max(widthRatio, heightRatio);

        // Terapkan skala yang SAMA untuk X dan Y agar gambar tidak gepeng
        transform.localScale = new Vector3(finalScale, finalScale, 1f);

        // Kembalikan rotasi
        transform.rotation = currentRot;
    }
}