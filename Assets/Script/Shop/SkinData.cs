using UnityEngine;

[CreateAssetMenu(fileName = "New Skin", menuName = "Skin System/Skin Data")]
public class SkinData : ScriptableObject
{
    public enum SkinType { Character, Bullet } // 🔥 TAMBAHKAN
    public SkinType skinType = SkinType.Character;

    public string skinName;
    public Sprite skinSprite; // Untuk preview di UI
    public bool useStaticSprite; // Tanpa animator — pakai skinSprite langsung
    public bool useCustomColliderOffset;
    public Vector2 customColliderOffset;
    public int price = 100;
    public Rarity rarity = Rarity.Common;

    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Secret
    }
}