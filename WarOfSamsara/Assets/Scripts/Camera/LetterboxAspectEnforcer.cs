using UnityEngine;

namespace WarOfSamsara.CameraControl
{
    /// <summary>
    /// ควบคุมสัดส่วนหน้าจอให้อยู่ในสัดส่วน 16:9 (1920x1080) เสมอ
    /// - หากเล่นบนจอ Ultrawide (21:9) จะแสดงแถบดำข้างจอ (Pillarbox)
    /// - หากเล่นบนจอทรงสูง (16:10, 4:3) จะแสดงแถบดำบนล่าง (Letterbox)
    /// - เพื่อให้ระยะการมองเห็นในเกม (FOV) ยุติธรรมเท่าเทียมกันทุกคนในการฟาร์มและ PvP
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class LetterboxAspectEnforcer : MonoBehaviour
    {
        [Header("Target Aspect Ratio")]
        [SerializeField] private float targetAspectWidth = 16f;
        [SerializeField] private float targetAspectHeight = 9f;

        private Camera cam;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (Application.isPlaying)
            {
                ApplyAspect();
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
                {
                    ApplyAspect();
                }
            }
        }

        public void ApplyAspect()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            float targetAspect = targetAspectWidth / targetAspectHeight;
            float windowAspect = (float)Screen.width / (float)Screen.height;
            float scaleHeight = windowAspect / targetAspect;

            if (scaleHeight < 1.0f)
            {
                // Letterbox: แถบดำบน-ล่าง
                Rect rect = cam.rect;
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f;
                cam.rect = rect;
            }
            else
            {
                // Pillarbox: แถบดำซ้าย-ขวา
                float scaleWidth = 1.0f / scaleHeight;
                Rect rect = cam.rect;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f;
                rect.y = 0;
                cam.rect = rect;
            }
        }
    }
}
