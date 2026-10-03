using UnityEngine;

namespace WarOfSamsara.Environment
{
    /// <summary>
    /// ควบคุมเลเยอร์พื้นหลังหลายชั้นให้เลื่อนตามกล้องด้วยความเร็วที่ต่างกัน (Parallax Effect)
    /// สไตล์ภาพซ้อนมิติของ MapleStory และ Ghost Online
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayer
        {
            public Transform layerTransform;
            [Range(0f, 1f)] public float parallaxFactorX = 0.5f;
            [Range(0f, 1f)] public float parallaxFactorY = 0.2f;
            public bool infiniteHorizontal = true;
            [HideInInspector] public float textureUnitSizeX;
        }

        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private ParallaxLayer[] layers;

        private Vector3 lastCameraPosition;

        private void Start()
        {
            if (targetCamera == null) targetCamera = UnityEngine.Camera.main;
            if (targetCamera != null) lastCameraPosition = targetCamera.transform.position;

            if (layers != null)
            {
                foreach (var layer in layers)
                {
                    if (layer.layerTransform != null)
                    {
                        SpriteRenderer sr = layer.layerTransform.GetComponent<SpriteRenderer>();
                        if (sr != null && sr.sprite != null)
                        {
                            layer.textureUnitSizeX = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                        }
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null || layers == null) return;

            Vector3 deltaMovement = targetCamera.transform.position - lastCameraPosition;

            foreach (var layer in layers)
            {
                if (layer.layerTransform == null) continue;

                Vector3 newPos = layer.layerTransform.position;
                newPos.x += deltaMovement.x * layer.parallaxFactorX;
                newPos.y += deltaMovement.y * layer.parallaxFactorY;
                layer.layerTransform.position = newPos;

                // ตรวจสอบ Infinite Scroll แนวนอน
                if (layer.infiniteHorizontal && layer.textureUnitSizeX > 0f)
                {
                    float offsetFromCameraX = targetCamera.transform.position.x - layer.layerTransform.position.x;
                    if (Mathf.Abs(offsetFromCameraX) >= layer.textureUnitSizeX)
                    {
                        float offsetPositionX = (offsetFromCameraX > 0) ? layer.textureUnitSizeX : -layer.textureUnitSizeX;
                        layer.layerTransform.position = new Vector3(layer.layerTransform.position.x + offsetPositionX, layer.layerTransform.position.y, layer.layerTransform.position.z);
                    }
                }
            }

            lastCameraPosition = targetCamera.transform.position;
        }
    }
}
