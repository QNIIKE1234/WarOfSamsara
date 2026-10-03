using UnityEngine;

namespace WarOfSamsara.Environment
{
    /// <summary>
    /// สคริปต์ Parallax 2D สำหรับแปะลงบน GameObject ของฉากหลังแต่ละชิ้นได้โดยตรง
    /// - เลื่อนฉากหลังตามมุมมองกล้องด้วยความเร็วที่ต่างกัน สร้างมิติความลึก (Depth) สไตล์ MapleStory
    /// - รองรับ Infinite Horizontal Scrolling (วนลูปไร้รอยต่อแบบไม่มีวันสุดแมพ)
    /// - รองรับ Auto-Scroll (เมฆลอย หรือหมอกเคลื่อนที่อัตโนมัติ)
    /// - คำนวณจากสูตรสมบูรณ์แบบ ไม่สะสม Floating Point Drift
    /// </summary>
    [DisallowMultipleComponent]
    public class ParallaxLayer2D : MonoBehaviour
    {
        [Header("--- กล้องอ้างอิง (Camera) ---")]
        [Tooltip("กล้องหลักที่ใช้จับความเคลื่อนไหว (ถ้าเว้นว่างไว้จะดึง Camera.main ให้อัตโนมัติ)")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Header("--- ความลึกของเลเยอร์ (Parallax Factor) ---")]
        [Range(-1f, 1.5f)]
        [Tooltip("ความเร็วการเลื่อนแนวนอน:\n0 = ติดกับโลกจริง (เลเยอร์พื้นดิน)\n0.5 = ระยะกลาง (ต้นไม้/เสาไฟ)\n0.8 = ระยะไกล (ภูเขา)\n1.0 = ไกลสุดสายตา (ก้อนเมฆ/ท้องฟ้า/ดวงจันทร์)\n-0.3 = เลเยอร์หน้าสุด (Foreground วิ่งเร็วกว่าตัวละคร)")]
        [SerializeField] private float parallaxFactorX = 0.5f;

        [Range(-1f, 1f)]
        [Tooltip("ความเร็วการเลื่อนแนวตั้ง (ตามการกระโดด/ร่วงของตัวละคร)")]
        [SerializeField] private float parallaxFactorY = 0.1f;

        [Tooltip("ล็อกแกน Y ไว้คงที่ ไม่ขยับขึ้นลงตามกล้องตอนกระโดด (เหมาะสำหรับภูเขาหรือพื้นดิน)")]
        [SerializeField] private bool lockY = true;

        [Header("--- การวนลูปแนวนอนไม่รู้จบ (Infinite Loop) ---")]
        [Tooltip("วนลูปเลเยอร์นี้แนวนอนแบบไม่มีวันจบ (เมื่อกล้องเดินเลยความกว้าง จะสลับตำแหน่งไปต่อข้างหน้าทันที)")]
        [SerializeField] private bool infiniteHorizontal = true;

        [Tooltip("ความกว้างของภาพ (ถ้าใส่ 0 ระบบจะคำนวณจากขนาดจริงของ SpriteRenderer ให้อัตโนมัติ)")]
        [SerializeField] private float customWidth = 0f;

        [Header("--- เลื่อนเองอัตโนมัติ (Auto Drift / Clouds) ---")]
        [Tooltip("ความเร็วการเลื่อนตัวเองอัตโนมัติ (เช่น เมฆลอยช้าๆ หรือหมอกพัดผ่าน)")]
        [SerializeField] private float autoScrollSpeedX = 0f;

        private float _startPosX;
        private float _startPosY;
        private float _textureWidth;
        private float _autoScrollOffset = 0f;

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            _startPosX = transform.position.x;
            _startPosY = transform.position.y;

            // คำนวณความกว้างของ Sprite
            CalculateTextureWidth();
        }

        public void CalculateTextureWidth()
        {
            if (customWidth > 0f)
            {
                _textureWidth = customWidth;
                return;
            }

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                _textureWidth = sr.bounds.size.x;
            }
            else
            {
                // ถ้าเป็น GameObject แม่ ให้คำนวณจากผลรวม Bounds ของลูกทั้งหมด
                SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
                if (childRenderers.Length > 0)
                {
                    Bounds combinedBounds = childRenderers[0].bounds;
                    for (int i = 1; i < childRenderers.Length; i++)
                    {
                        combinedBounds.Encapsulate(childRenderers[i].bounds);
                    }
                    _textureWidth = combinedBounds.size.x;
                }
            }
        }

        public void SetCamera(UnityEngine.Camera cam)
        {
            targetCamera = cam;
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = Object.FindFirstObjectByType<UnityEngine.Camera>();
                    if (targetCamera == null) return;
                }
            }

            // เลื่อนอัตโนมัติ (เช่น เมฆลอย)
            if (Mathf.Abs(autoScrollSpeedX) > 0.001f)
            {
                _autoScrollOffset += autoScrollSpeedX * Time.deltaTime;
            }

            Vector3 camPos = targetCamera.transform.position;

            // ระยะที่กล้องเคลื่อนที่ไป
            float distX = (camPos.x * parallaxFactorX) + _autoScrollOffset;
            float distY = lockY ? 0f : (camPos.y * parallaxFactorY);

            // อัปเดตตำแหน่ง Layer
            transform.position = new Vector3(_startPosX + distX, _startPosY + distY, transform.position.z);

            // คำนวณการวนลูป Infinite Loop แนวนอน
            if (infiniteHorizontal && _textureWidth > 0.1f)
            {
                float tempX = (camPos.x * (1f - parallaxFactorX)) - _autoScrollOffset;

                if (tempX > _startPosX + (_textureWidth * 0.5f))
                {
                    _startPosX += _textureWidth;
                }
                else if (tempX < _startPosX - (_textureWidth * 0.5f))
                {
                    _startPosX -= _textureWidth;
                }
            }
        }

        // รีเซ็ตค่าเริ่มต้นให้ตรงกับตำแหน่งปัจจุบันใน Scene
        [ContextMenu("Reset Start Position to Current")]
        public void ResetStartPosition()
        {
            _startPosX = transform.position.x;
            _startPosY = transform.position.y;
            _autoScrollOffset = 0f;
            CalculateTextureWidth();
        }

        private void OnDrawGizmosSelected()
        {
            if (_textureWidth > 0.1f)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
                Vector3 center = new Vector3(_startPosX, _startPosY, transform.position.z);
                Gizmos.DrawWireCube(center, new Vector3(_textureWidth, 5f, 0.1f));
            }
        }
    }
}
