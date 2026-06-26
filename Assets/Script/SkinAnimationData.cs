using UnityEngine;

[CreateAssetMenu(fileName = "New Skin Animation", menuName = "Skin System/Skin Animation Data")]
public class SkinAnimationData : ScriptableObject
{
    [System.Serializable]
    public class AnimationSprites
    {
        public Sprite[] sprites;
        public float frameRate = 12f; // Frame per detik
    }

    [Header("Animation Sprites")]
    public AnimationSprites idle;
    public AnimationSprites attack;
    public AnimationSprites absorb1;
    public AnimationSprites absorb2;
    public AnimationSprites death;
}