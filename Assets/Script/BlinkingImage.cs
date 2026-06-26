using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class BlinkingImage : MonoBehaviour
{
    public float blinkSpeed = 0.5f;

    private Image targetImage;

    void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (targetImage != null)
        {
            targetImage.enabled = true;
            StartCoroutine(BlinkRoutine());
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(blinkSpeed);
            
            if (targetImage != null)
            {
                targetImage.enabled = !targetImage.enabled;
            }
        }
    }
}