using UnityEngine;

public class EnemyBounds : MonoBehaviour
{
    public static EnemyBounds Instance;

    [Header("Padding (jarak dari tepi layar)")]
    public float paddingX = 0.5f;
    public float paddingY = 0.8f;

    private float minX, maxX, minY, maxY;

    void Awake()
    {
        Instance = this;
        CalculateBounds();
    }

    void Update()
    {
        // Update terus kalau layar berubah
        CalculateBounds();
    }

    public float GetTopBound()
    {
        return maxY;
    }

    void CalculateBounds()
    {
        Camera cam = Camera.main;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        Vector3 camPos = cam.transform.position;

        minX = camPos.x - camWidth + paddingX;
        maxX = camPos.x + camWidth - paddingX;
        minY = camPos.y - camHeight + paddingY;
        maxY = camPos.y + camHeight - paddingY;
    }

    public Vector3 Clamp(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        return pos;
    }
}