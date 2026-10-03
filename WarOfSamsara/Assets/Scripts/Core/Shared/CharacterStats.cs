using System;

namespace WarOfSamsara.Shared
{
    [Serializable]
    public class CharacterStats
    {
        // Identification
        public long CharacterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int JobId { get; set; } // 0 = Beginner, 100 = Warrior, 200 = Magician, etc.
        public int Level { get; set; } = 1;
        public long CurrentExp { get; set; }
        public long MaxExp { get; set; } = 15;

        // Vitals
        public int CurrentHp { get; set; } = 50;
        public int MaxHp { get; set; } = 50;
        public int CurrentMp { get; set; } = 10;
        public int MaxMp { get; set; } = 10;

        // Base 4 Attributes (Classic Maple)
        public int Str { get; set; } = 12;
        public int Dex { get; set; } = 5;
        public int Int { get; set; } = 4;
        public int Luk { get; set; } = 4;
        public int AbilityPoints { get; set; }

        // Secondary Combat Stats
        public int WeaponAttack { get; set; }
        public int MagicAttack { get; set; }
        public int WeaponDefense { get; set; }
        public int MagicDefense { get; set; }
        public int Accuracy { get; set; }
        public int Avoidability { get; set; }
        public float SpeedPercent { get; set; } = 100f; // 100% to 140% cap
        public float JumpPercent { get; set; } = 100f;  // 100% to 123% cap

        // Economy
        public long Mesos { get; set; } = 0;

        /// <summary>
        /// Calculates damage range based on classic formula.
        /// </summary>
        public (int minDamage, int maxDamage) CalculateDamageRange(float weaponMastery = 0.6f)
        {
            // Simplified Warrior damage formula
            float max = (Str * 4.0f + Dex) * (WeaponAttack / 100.0f);
            float min = (Str * 4.0f * weaponMastery * 0.9f + Dex) * (WeaponAttack / 100.0f);
            return (MathfMax((int)min, 1), MathfMax((int)max, 1));
        }

        private static int MathfMax(int a, int b) => a > b ? a : b;
    }
}
