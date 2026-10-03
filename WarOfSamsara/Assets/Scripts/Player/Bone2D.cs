using UnityEngine;

namespace WarOfSamsara.Player
{
    /// <summary>
    /// Component แสดงผลโครงกระดูก 2D (Bone) ใน Scene View
    /// วาดทรงข้าวหลามตัด (Bone Diamond) เชื่อมต่อไปยัง Bone ลูก ให้เห็นและจับดัดท่าได้ง่าย
    /// </summary>
    [ExecuteInEditMode]
    [SelectionBase]
    public class Bone2D : MonoBehaviour
    {
        [Header("Bone Settings")]
        [SerializeField] private Color boneColor = new Color(0.2f, 0.8f, 1f, 0.8f);
        [SerializeField] private float jointRadius = 0.06f;

        private void OnDrawGizmos()
        {
            // วาดจุดข้อต่อ (Joint)
            Gizmos.color = boneColor;
            Gizmos.DrawWireSphere(transform.position, jointRadius);

            // วาดกระดูกต่อไปยัง Bone ลูกทุกตัว
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Bone2D childBone = child.GetComponent<Bone2D>();
                if (childBone != null)
                {
                    DrawBoneToChild(transform.position, child.position);
                }
            }
        }

        private void DrawBoneToChild(Vector3 start, Vector3 end)
        {
            Vector3 diff = end - start;
            float length = diff.magnitude;
            if (length < 0.01f) return;

            Vector3 dir = diff.normalized;
            Vector3 normal = new Vector3(-dir.y, dir.x, 0f);

            float width = Mathf.Min(0.08f, length * 0.25f);
            Vector3 midPoint = start + dir * (length * 0.3f);

            Vector3 pA = midPoint + normal * width;
            Vector3 pB = midPoint - normal * width;

            // วาดเส้นกระดูกทรงข้าวหลามตัด (Bone Diamond)
            Gizmos.color = boneColor;
            Gizmos.DrawLine(start, pA);
            Gizmos.DrawLine(pA, end);
            Gizmos.DrawLine(end, pB);
            Gizmos.DrawLine(pB, start);
            Gizmos.DrawLine(pA, pB);
        }
    }
}
