using UnityEngine;

[CreateAssetMenu(fileName = "New PowerUp", menuName = "PowerUp System/PowerUp Data")]
public class PowerUpData : ScriptableObject
{
    public enum Rarity { Common, Rare, Legendary }
    public enum DurationType { Permanent, Temporary }

    public string powerUpName;
    public Sprite icon;
    public Rarity rarity = Rarity.Common;
    public DurationType duration = DurationType.Permanent;
    public string description;
    public int temporaryWaveCount = 5;

    // Efek (diaplikasikan oleh PowerUpManager)
    public enum EffectType
    {
        MaxHP,              // Common 1: +20% HP
        Damage,             // Common 2: +15% Damage
        AbsorbCapacity,     // Common 3: +2 kapasitas absorpsi
        BurstFireRate,      // Common 4: +25% kecepatan burst
        SlowEnemyBullet,    // Common 5: -20% speed bullet musuh (temp)
        EmergencyShield,    // Common 6: Tahan 1 serangan (temp)

        HPRegen,            // Rare 1: Regen HP setelah 4s
        BurstDamage,        // Rare 2: +15% damage burst
        WideShot,           // Rare 3: Tembakan menyebar 3
        EchoShot,           // Rare 4: Setiap 3 tembakan +1
        PierceEnemy,        // Rare 5: Tembus 2 musuh (temp)
        AbsorbRadius,       // Rare 6: +25% radius absorpsi (temp)

        KineticHeal,        // Legendary 1: Burst heal saat kena musuh
        BurstCooldown,      // Legendary 2: Cooldown burst lebih singkat
        BulletSplit,        // Legendary 3: Burst pecah saat kena
        MirrorShot,         // Legendary 4: +1 proyektil ke bawah
        Overcharge,         // Legendary 5: Tembus semua musuh
        BossSlayer          // Legendary 6: +30% damage ke boss
    }

    public EffectType effectType;
    public float effectValue; // Persentase atau nilai absolut
}