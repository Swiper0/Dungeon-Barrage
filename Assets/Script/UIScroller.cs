using UnityEngine;
using UnityEngine.UI; // Wajib untuk UI

[RequireComponent(typeof(RawImage))]
public class UIScroller : MonoBehaviour
{
    [Header("Settings")]
    public float scrollSpeedX = 0.5f; // Kecepatan horizontal
    public float scrollSpeedY = 0f;   // Kecepatan vertikal (opsional)

    private RawImage _rawImage;
    private Rect _uvRect;

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        // Opsional: Berhenti jika game belum mulai (sesuai logic game kamu sebelumnya)
        if (GameManager.Instance != null && !GameManager.Instance.gameStarted) return;

        // Ambil UV Rect saat ini
        _uvRect = _rawImage.uvRect;

        // Geser posisi X dan Y berdasarkan waktu
        // Time.deltaTime memastikan gerakan mulus di semua frame rate
        _uvRect.x += scrollSpeedX * Time.deltaTime;
        _uvRect.y += scrollSpeedY * Time.deltaTime;

        // Kembalikan nilai yang sudah digeser ke komponen
        _rawImage.uvRect = _uvRect;
    }
}