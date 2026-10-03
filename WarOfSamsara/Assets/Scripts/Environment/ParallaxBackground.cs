using System.Collections.Generic;
using UnityEngine;

namespace WarOfSamsara.Environment
{
    /// <summary>
    /// สคริปต์ควบคุม Parallax รวมศูนย์ แปะไว้ที่ GameObject ตัวแม่ (เช่น MAP_STARTER_001) จุดเดียวจบ
    /// - ลากลูกๆ แต่ละชิ้น (Sky, BG, MG, FG) มาใส่ใน List ได้เลย
    /// - ปรับค่าความเร็ว (Parallax Factor), ล็อกแกน Y, Infinite Loop ของแต่ละชิ้นได้ในหน้าจอเดียว
    /// - มีปุ่มคลิกขวา "Auto Detect Child Layers" ดึงลูกๆ มาจัดลง List ให้อัตโนมัติพร้อมตั้งค่าแนะนำ
    /// - คำนวณแบบ Drift-Free แม่นยำ 100% ไม่สะสม Floating Point Error
    /// </summary>
    [AddComponentMenu("WarOfSamsara/Environment/Parallax Background (Root Controller)")]
    [DisallowMultipleComponent]
    public class ParallaxBackground : MonoBehaviour
    {
        [System.Serializable]
        public class LayerConfig
        {
            [Tooltip("ชื่อระบุเลเยอร์ (ใส่เพื่อดูง่ายๆ ใน Inspector)")]
            public string name;

            [Tooltip("Transform ของเลเยอร์ที่ต้องการให้ขยับ")]
            public Transform layerTransform;

            [Range(-1f, 1.5f)]
            [Tooltip("ความเร็วการเลื่อนแนวนอน:\n0 = นิ่งกับที่\n0.3-0.5 = ระยะกลาง\n0.7-0.8 = ระยะไกล (ภูเขา)\n0.9-1.0 = ไกลสุด (ฟ้า/เมฆ)\n-0.3 = เลเยอร์หน้าสุด (Foreground)")]
            public float parallaxFactorX = 0.5f;

            [Range(-1f, 1f)]
            [Tooltip("ความเร็วการเลื่อนแนวตั้งตามกล้อง")]
            public float parallaxFactorY = 0.1f;

            [Tooltip("ล็อกแกน Y ไว้คงที่ ไม่ขยับขึ้นลงตามกล้อง")]
            public bool lockY = true;

            [Tooltip("วนลูปภาพแนวนอนไม่รู้จบ")]
            public bool infiniteHorizontal = true;

            [Tooltip("ความกว้างภาพ (ใส่ 0 เพื่อให้ระบบคำนวณจาก SpriteRenderer ให้อัตโนมัติ)")]
            public float customWidth = 0f;

            [Tooltip("ความเร็วการเลื่อนเองอัตโนมัติ (เช่น เมฆลอยเอื่อยๆ)")]
            public float autoScrollSpeedX = 0f;

            // Runtime values (ไม่ต้องแสดงใน Inspector)
            [System.NonSerialized] public float startPosX;
            [System.NonSerialized] public float startPosY;
            [System.NonSerialized] public float textureWidth;
            [System.NonSerialized] public float autoScrollOffset;
            [System.NonSerialized] public bool isInitialized;
        }

        [Header("--- กล้องอ้างอิง (Camera) ---")]
        [Tooltip("กล้องหลัก (ถ้าเว้นว่างไว้จะดึง Camera.main หรือกล้องในฉากให้อัตโนมัติ)")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Header("--- รายการเลเยอร์ที่ต้องการให้ขยับ (Parallax Layers) ---")]
        [SerializeField] private List<LayerConfig> layers = new List<LayerConfig>();

        public List<LayerConfig> Layers => layers;

        private void Start()
        {
            FindCameraIfMissing();
            InitializeAllLayers();
        }

        public void SetCamera(UnityEngine.Camera cam)
        {
            targetCamera = cam;
        }

        private void FindCameraIfMissing()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = Object.FindFirstObjectByType<UnityEngine.Camera>();
                }
            }
        }

        public void InitializeAllLayers()
        {
            if (layers == null) return;

            foreach (var layer in layers)
            {
                if (layer == null || layer.layerTransform == null) continue;

                layer.startPosX = layer.layerTransform.position.x;
                layer.startPosY = layer.layerTransform.position.y;
                layer.autoScrollOffset = 0f;
                layer.textureWidth = CalculateWidth(layer);
                layer.isInitialized = true;
            }
        }

        private float CalculateWidth(LayerConfig layer)
        {
            if (layer.customWidth > 0.01f) return layer.customWidth;

            SpriteRenderer sr = layer.layerTransform.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                return sr.bounds.size.x;
            }

            SpriteRenderer[] childRenderers = layer.layerTransform.GetComponentsInChildren<SpriteRenderer>();
            if (childRenderers.Length > 0)
            {
                Bounds combinedBounds = childRenderers[0].bounds;
                for (int i = 1; i < childRenderers.Length; i++)
                {
                    combinedBounds.Encapsulate(childRenderers[i].bounds);
                }
                return combinedBounds.size.x;
            }

            return 0f;
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                FindCameraIfMissing();
                if (targetCamera == null) return;
            }

            if (layers == null || layers.Count == 0) return;

            Vector3 camPos = targetCamera.transform.position;

            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer == null || layer.layerTransform == null) continue;

                if (!layer.isInitialized)
                {
                    layer.startPosX = layer.layerTransform.position.x;
                    layer.startPosY = layer.layerTransform.position.y;
                    layer.textureWidth = CalculateWidth(layer);
                    layer.isInitialized = true;
                }

                // ขยับเลื่อนอัตโนมัติ (เช่น เมฆลอย)
                if (Mathf.Abs(layer.autoScrollSpeedX) > 0.0001f)
                {
                    layer.autoScrollOffset += layer.autoScrollSpeedX * Time.deltaTime;
                }

                // คำนวณตำแหน่งจากพิกัดสัมบูรณ์ของกล้อง (ไม่สะสม Floating point drift)
                float distX = (camPos.x * layer.parallaxFactorX) + layer.autoScrollOffset;
                float distY = layer.lockY ? 0f : (camPos.y * layer.parallaxFactorY);

                layer.layerTransform.position = new Vector3(
                    layer.startPosX + distX,
                    layer.startPosY + distY,
                    layer.layerTransform.position.z
                );

                // Infinite loop แนวนอน
                if (layer.infiniteHorizontal && layer.textureWidth > 0.1f)
                {
                    float tempX = (camPos.x * (1f - layer.parallaxFactorX)) - layer.autoScrollOffset;

                    if (tempX > layer.startPosX + (layer.textureWidth * 0.5f))
                    {
                        layer.startPosX += layer.textureWidth;
                    }
                    else if (tempX < layer.startPosX - (layer.textureWidth * 0.5f))
                    {
                        layer.startPosX -= layer.textureWidth;
                    }
                }
            }
        }

        /// <summary>
        /// คลิกขวาที่หัวคอมโพเนนต์ -> Auto Detect Child Layers
        /// สแกนหา GameObject ลูกๆ ที่เป็นฉากหลัง แล้วแอดใส่ List พร้อมแนะนำค่าความเร็วให้อัตโนมัติทันที
        /// </summary>
        [ContextMenu("Auto Detect Child Layers")]
        public void AutoDetectChildLayers()
        {
            if (layers == null) layers = new List<LayerConfig>();
            layers.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                string childName = child.name.ToLower();

                // ข้ามเลเยอร์พื้นดิน กำแพง กล้อง ขอบเขต หรือตัวละคร (ไม่นำมาทำ Parallax)
                if (childName.Contains("ground") || 
                    childName.Contains("wall") || 
                    childName.Contains("bound") || 
                    childName.Contains("collider") || 
                    childName.Contains("spawn") || 
                    childName.Contains("camera") ||
                    childName.Contains("entity") ||
                    childName.Contains("rope") ||
                    childName.Contains("ladder") ||
                    childName.Contains("portal"))
                {
                    continue;
                }

                LayerConfig cfg = new LayerConfig
                {
                    name = child.name,
                    layerTransform = child,
                    lockY = true,
                    infiniteHorizontal = true,
                    customWidth = 0f,
                    autoScrollSpeedX = 0f
                };

                // ประเมินความเร็วแนะนำตามชื่อเลเยอร์
                if (childName.Contains("sky") || childName.Contains("cloud"))
                {
                    cfg.parallaxFactorX = 0.9f;
                    cfg.parallaxFactorY = 0.05f;
                    cfg.autoScrollSpeedX = 0.15f; // ให้เมฆลอยช้าๆ
                }
                else if (childName.Contains("06") || childName.Contains("bg") || childName.Contains("back"))
                {
                    cfg.parallaxFactorX = 0.7f;
                    cfg.parallaxFactorY = 0.1f;
                }
                else if (childName.Contains("04") || childName.Contains("mg") || childName.Contains("mid"))
                {
                    cfg.parallaxFactorX = 0.4f;
                    cfg.parallaxFactorY = 0.15f;
                }
                else if (childName.Contains("fg") || childName.Contains("fore") || childName.Contains("01") || childName.Contains("02"))
                {
                    cfg.parallaxFactorX = -0.3f; // วิ่งสวนทาง สไตล์ Foreground
                    cfg.parallaxFactorY = 0.2f;
                }
                else
                {
                    cfg.parallaxFactorX = 0.5f;
                    cfg.parallaxFactorY = 0.1f;
                }

                layers.Add(cfg);
            }

            Debug.Log($"[ParallaxBackground] ตรวจพบและตั้งค่าเลเยอร์ให้แล้ว {layers.Count} ชิ้น บน GameObject: {gameObject.name}");
        }

        [ContextMenu("Reset Start Positions to Current")]
        public void ResetStartPositions()
        {
            InitializeAllLayers();
            Debug.Log("[ParallaxBackground] รีเซ็ตพิกัดเริ่มต้นของทุกเลเยอร์เรียบร้อยแล้ว");
        }

        private void OnDrawGizmosSelected()
        {
            if (layers == null) return;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            foreach (var layer in layers)
            {
                if (layer != null && layer.layerTransform != null)
                {
                    float width = layer.customWidth > 0 ? layer.customWidth : (layer.textureWidth > 0 ? layer.textureWidth : 10f);
                    Gizmos.DrawWireCube(layer.layerTransform.position, new Vector3(width, 6f, 0.1f));
                }
            }
        }
    }
}
