using UnityEngine;

[ExecuteAlways]
public class TilemapScaler : MonoBehaviour
{
    public Camera cam;
    public float referenceWidth = 4.3f;

    void Start()
    {
        if (!cam)
            cam = Camera.main;

        ScaleToCamera();
    }

#if UNITY_EDITOR
    void Update()
    {
        ScaleToCamera();
    }
#endif

    void ScaleToCamera()
    {
        if (!cam || !cam.orthographic) return;

        float cameraWorldWidth = cam.orthographicSize * 2f * cam.aspect;
        float scaleFactor = cameraWorldWidth / referenceWidth;

        transform.localScale = new Vector3(
            scaleFactor,
            scaleFactor,
            1f
        );
    }
}
