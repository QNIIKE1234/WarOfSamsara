using UnityEngine;

namespace WarOfSamsara.Enemy
{
    /// <summary>
    /// ข้อมูล Config ประจำตัวมอนสเตอร์ (ScriptableObject)
    /// สามารถคลิกขวาใน Project -> Create -> WarOfSamsara -> Enemy Config เพื่อสร้าง Config ให้มอนสเตอร์แต่ละชนิดได้อิสระ
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "WarOfSamsara/Enemy Config", order = 1)]
    public class EnemyConfig : ScriptableObject
    {
        [Header("--- ข้อมูลระบุตัวตน (Identity) ---")]
        [Tooltip("รหัสมอนสเตอร์ เช่น ENE_0001")]
        public string enemyId = "ENE_0001";
        [Tooltip("ชื่อมอนสเตอร์")]
        public string enemyName = "Stone Golem";
        [Tooltip("เลเวลของมอนสเตอร์")]
        public int level = 1;
        [Tooltip("EXP ที่ได้รับเมื่อสังหาร")]
        public int expReward = 20;
        [Tooltip("เงิน/เหรียญที่ดรอปเมื่อสังหาร")]
        public int goldReward = 10;

        [Header("--- พลังชีวิต (Vitals) ---")]
        [Tooltip("เลือดสูงสุด")]
        public int maxHp = 100;

        [Header("--- การต่อสู้ (Combat) ---")]
        [Tooltip("ดาเมจเมื่อผู้เล่นเดินชนตัวมอนสเตอร์ (สไตล์ MapleStory Touch Damage)")]
        public int touchDamage = 10;
        [Tooltip("ดาเมจเมื่อมอนสเตอร์ใช้ท่าโจมตี")]
        public int attackDamage = 25;
        [Tooltip("ระยะการโจมตี (ถ้าผู้เล่นอยู่ในระยะนี้จะเริ่มโจมตี)")]
        public float attackRange = 1.8f;
        [Tooltip("คูลดาวน์ระหว่างการโจมตีแต่ละครั้ง (วินาที)")]
        public float attackCooldown = 2.0f;
        [Tooltip("พลังป้องกัน (ลดทอนดาเมจที่ได้รับ)")]
        public int defense = 5;

        [Header("--- การเคลื่อนที่และ AI (Movement & AI) ---")]
        [Tooltip("ความเร็วในการเดิน/เคลื่อนที่")]
        public float moveSpeed = 2.0f;
        [Tooltip("รัศมีการเดินลาดตระเวนรอบจุดเกิด")]
        public float patrolRadius = 4.0f;
        [Tooltip("ระยะการตรวจจับผู้เล่น (มองเห็นและเริ่มวิ่งไล่)")]
        public float detectRadius = 6.0f;
        [Tooltip("ระยะหลุดการตรวจจับ (ถ้าผู้เล่นหนีห่างเกินระยะนี้จะหยุดไล่และกลับไปลาดตระเวน)")]
        public float loseTargetRadius = 9.0f;
        [Tooltip("เวลาหยุดพักยืนเฉยๆ ระหว่างเปลี่ยนทิศทางลาดตระเวน (วินาที)")]
        public float idleWaitTime = 1.5f;
        [Tooltip("เป็นมอนสเตอร์บินได้หรือไม่ (ถ้าติ๊กถูกจะไม่สนการตรวจจับขอบเหว)")]
        public bool hasFlyingMovement = false;

        [Header("--- การตอบสนองต่อดาเมจ (Damage Reaction) ---")]
        [Range(0f, 1f)]
        [Tooltip("ความต้านทานแรงกระเด็น (0 = ปลิวตามแรงเต็มที่, 1 = ยืนนิ่งไม่ขยับเลย)")]
        public float knockbackResistance = 0.2f;
        [Tooltip("ระยะเวลาอมตะชั่วขณะหลังโดนตี (ป้องกันโดนดาเมจซ้อนรัวเกินไป)")]
        public float invincibilityDuration = 0.25f;
    }
}
