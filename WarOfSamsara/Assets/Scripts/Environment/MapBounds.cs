using UnityEngine;

namespace WarOfSamsara.Environment
{
    /// <summary>
    /// กำหนดขอบเขตแผนที่ (Map Boundaries)
    /// - ใช้สำหรับจำกัดระยะของกล้อง (Camera Clamping)
    /// - แสดงเส้น Gizmos สีเขียวสดใสใน Unity Editor เพื่อให้จัดวางฉากได้ง่าย
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [ExecuteAlways]
    public class MapBounds : MonoBehaviour
    {
        [Header("Boundary Settings")]
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.4f, 0.35f);
        [SerializeField] private Color wireColor = new Color(0f, 1f, 0.4f, 1f);

        private BoxCollider2D boxCollider;

        public BoxCollider2D Collider
        {
            get
            {
                if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
                return boxCollider;
            }
        }

        public Bounds Bounds => Collider.bounds;

        private void Start()
        {
            if (Application.isPlaying)
            {
                var camFollow = Object.FindFirstObjectByType<CameraControl.CameraFollow2D>();
                if (camFollow != null)
                {
                    camFollow.SetBounds(Collider);
                }
            }
        }

        private void Reset()
        {
            boxCollider = GetComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(30f, 18f); // ขนาดเริ่มต้นประมาณ 1-2 จอ
        }

        private void OnDrawGizmos()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);

            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
        }
    }
}
