using UnityEngine;

namespace WarOfSamsara.CameraControl
{
    /// <summary>
    /// กล้อง 2D ติดตามผู้เล่นสไตล์ MapleStory / SoulSaver
    /// - เคลื่อนที่แบบ SmoothDamp พร้อม Look-Ahead ตามทิศทางการหัน
    /// - ล็อกขอบเขตตาม MapBounds เพื่อไม่ให้กล้องหลุดออกนอกแมพ
    /// - รองรับ Camera Shake เมื่อใช้สกิลหรือโดนโจมตี
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private bool lookAhead = true;
        [SerializeField] private float lookAheadDistance = 1.5f;

        [Header("Map Boundary Clamping")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Collider2D boundaryCollider;

        [Header("Camera Reference")]
        private Camera cam;
        private Vector3 currentVelocity;
        private float shakeTimeRemaining;
        private float shakeMagnitude;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(Collider2D bounds)
        {
            boundaryCollider = bounds;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 targetPos = target.position + offset;

            // Look-ahead ตามทิศทางการหันของตัวละคร
            if (lookAhead)
            {
                float facingDirection = Mathf.Sign(target.localScale.x);
                targetPos.x += facingDirection * lookAheadDistance;
            }

            // คำนวณตำแหน่ง SmoothDamp
            Vector3 smoothPos = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);

            // Clamp ขอบเขตแมพ
            if (useBounds && boundaryCollider != null)
            {
                float camHalfHeight = cam.orthographicSize;
                float camHalfWidth = camHalfHeight * cam.aspect;

                Bounds b = boundaryCollider.bounds;

                // ตรวจสอบว่าแมพกว้างกว่าจอกล้องหรือไม่
                if (b.size.x >= camHalfWidth * 2f)
                {
                    smoothPos.x = Mathf.Clamp(smoothPos.x, b.min.x + camHalfWidth, b.max.x - camHalfWidth);
                }
                else
                {
                    smoothPos.x = b.center.x; // กึ่งกลางถ้าแมพแคบกว่าจอ
                }

                if (b.size.y >= camHalfHeight * 2f)
                {
                    smoothPos.y = Mathf.Clamp(smoothPos.y, b.min.y + camHalfHeight, b.max.y - camHalfHeight);
                }
                else
                {
                    smoothPos.y = b.center.y;
                }
            }

            // Camera Shake Effect
            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= Time.deltaTime;
                Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                shakeOffset.z = 0f;
                smoothPos += shakeOffset;
            }

            smoothPos.z = offset.z;
            transform.position = smoothPos;
        }

        /// <summary>
        /// สั่งให้กล้องสั่นเมื่อเกิดการระเบิดหรือคอมโบหนัก
        /// </summary>
        public void TriggerShake(float duration = 0.2f, float magnitude = 0.25f)
        {
            shakeTimeRemaining = duration;
            shakeMagnitude = magnitude;
        }
    }
}
