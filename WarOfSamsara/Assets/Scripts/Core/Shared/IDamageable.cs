using UnityEngine;

namespace WarOfSamsara.Shared
{
    /// <summary>
    /// Interface สำหรับ Object หรือ Entity ทุกตัวที่สามารถรับดาเมจและแรงกระเด็นได้
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int damage, Vector2 knockbackDirection);
        bool IsDead { get; }
    }
}
