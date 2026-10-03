#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WarOfSamsara.CameraControl;
using WarOfSamsara.Player;

namespace WarOfSamsara.Editor
{
    /// <summary>
    /// สคริปต์สกัดชิ้นส่วน Sprite Sheet (Paperdoll) เป็นไฟล์ Sprite แต่ละชิ้นในโฟลเดอร์ Parts
    /// และประกอบร่างเป็นตัวละคร Modular 2D เชื่อมระบบฟิสิกส์ PlayerMovement2D วางในฉาก
    /// </summary>
    public static class ModularCharacterBuilder
    {
        private const string SPRITE_PATH = "Assets/Resources/Character/character_parts_transparent_2.png";
        private const string PARTS_DIR = "Assets/Resources/Character/Parts";
        private const string PREFAB_DIR = "Assets/Prefabs/Characters";
        private const string PREFAB_PATH = "Assets/Prefabs/Characters/ModularPlayer.prefab";

        [MenuItem("WarOfSamsara/Build Modular Character (Paperdoll)", false, 10)]
        public static void BuildAndSpawnModularCharacter()
        {
            // 1. ตรวจสอบและสกัดชิ้นส่วนออกมาเป็นไฟล์ภาพย่อยๆ ในโฟลเดอร์ Parts
            ExtractParts();

            // 2. ประกอบร่างตัวละคร (Assembly)
            GameObject playerObj = AssembleCharacter();
            if (playerObj == null)
            {
                Debug.LogError("[ModularCharacterBuilder] เกิดข้อผิดพลาดในการประกอบร่างตัวละคร");
                return;
            }

            // 3. บันทึกเป็น Prefab
            SaveAsPrefab(playerObj);

            // 4. เชื่อมต่อกล้องและเซฟฉาก
            ConnectToCamera(playerObj);

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"<color=#00FF88>[ModularCharacterBuilder]</color> ประกอบร่างตัวละคร Modular Paperdoll สำเร็จเรียบร้อย! พร้อมกด Play (▶️) ทดสอบได้ทันที!");
        }

        private static void ExtractParts()
        {
            if (!Directory.Exists(PARTS_DIR))
            {
                Directory.CreateDirectory(PARTS_DIR);
            }

            TextureImporter importer = AssetImporter.GetAtPath(SPRITE_PATH) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SPRITE_PATH);
            if (srcTex == null)
            {
                Debug.LogError($"[ModularCharacterBuilder] ไม่พบภาพต้นฉบับที่ {SPRITE_PATH}");
                return;
            }

            int width = srcTex.width;
            int height = srcTex.height;
            Color32[] pixels = srcTex.GetPixels32();

            List<Rect> boundingBoxes = FindIslands(pixels, width, height, minPixels: 150);
            Debug.Log($"[ModularCharacterBuilder] ตรวจพบชิ้นส่วน {boundingBoxes.Count} ชิ้น กำลังสกัดเป็น Sprite...");

            for (int i = 0; i < boundingBoxes.Count; i++)
            {
                Rect r = boundingBoxes[i];
                int rx = (int)r.x;
                int ry = (int)r.y;
                int rw = (int)r.width;
                int rh = (int)r.height;

                Texture2D partTex = new Texture2D(rw, rh, TextureFormat.RGBA32, false);
                Color32[] partPixels = new Color32[rw * rh];

                for (int py = 0; py < rh; py++)
                {
                    for (int px = 0; px < rw; px++)
                    {
                        partPixels[py * rw + px] = pixels[(ry + py) * width + (rx + px)];
                    }
                }

                partTex.SetPixels32(partPixels);
                partTex.Apply();

                byte[] pngData = partTex.EncodeToPNG();
                Object.DestroyImmediate(partTex);

                string partName = ClassifyPart(r, width, height, i);
                string outPath = $"{PARTS_DIR}/{partName}.png";
                File.WriteAllBytes(outPath, pngData);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // ตั้งค่า Texture ทั้งหมดในโฟลเดอร์ Parts ให้เป็น Sprite 2D
            string[] files = Directory.GetFiles(PARTS_DIR, "*.png");
            foreach (var f in files)
            {
                string assetPath = f.Replace('\\', '/');
                TextureImporter partImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (partImporter != null && (partImporter.textureType != TextureImporterType.Sprite || !partImporter.alphaIsTransparency))
                {
                    partImporter.textureType = TextureImporterType.Sprite;
                    partImporter.alphaIsTransparency = true;
                    partImporter.spritePixelsPerUnit = 100;
                    partImporter.SaveAndReimport();
                }
            }
        }

        private static List<Rect> FindIslands(Color32[] pixels, int width, int height, int minPixels)
        {
            bool[] visited = new bool[width * height];
            List<Rect> islands = new List<Rect>();

            int[] queueX = new int[width * height];
            int[] queueY = new int[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (visited[idx] || pixels[idx].a < 30) continue;

                    int head = 0, tail = 0;
                    queueX[tail] = x;
                    queueY[tail] = y;
                    tail++;
                    visited[idx] = true;

                    int minX = x, maxX = x, minY = y, maxY = y;
                    int count = 0;

                    while (head < tail)
                    {
                        int cx = queueX[head];
                        int cy = queueY[head];
                        head++;
                        count++;

                        if (cx < minX) minX = cx;
                        if (cx > maxX) maxX = cx;
                        if (cy < minY) minY = cy;
                        if (cy > maxY) maxY = cy;

                        int[] dx = { 1, -1, 0, 0 };
                        int[] dy = { 0, 0, 1, -1 };
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx + dx[d];
                            int ny = cy + dy[d];
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                int nIdx = ny * width + nx;
                                if (!visited[nIdx] && pixels[nIdx].a >= 30)
                                {
                                    visited[nIdx] = true;
                                    queueX[tail] = nx;
                                    queueY[tail] = ny;
                                    tail++;
                                }
                            }
                        }
                    }

                    if (count >= minPixels)
                    {
                        islands.Add(new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1));
                    }
                }
            }

            return islands;
        }

        private static string ClassifyPart(Rect r, int texW, int texH, int index)
        {
            float normX = r.center.x / texW;
            float normY = r.center.y / texH;

            // ศีรษะและหน้าตา (แถวบน Y > 0.75)
            if (normY > 0.78f)
            {
                if (normX < 0.38f) return "Hair_Front";
                if (normX < 0.68f) return "Hair_Back";
                return $"Head_Base_{(normX > 0.82f ? "A" : "B")}";
            }
            if (normY > 0.67f)
            {
                if (normX < 0.15f) return "Eyes_Wink";
                if (normX < 0.28f) return "Eyes_Shocked";
                if (normX < 0.45f) return "Eyes_Battle";
                return $"Head_Face_{(normX > 0.7f ? "Right" : "Center")}";
            }

            // ลำตัวและแขน (Y ระหว่าง 0.48 - 0.70)
            if (normY > 0.48f)
            {
                if (normX > 0.28f && normX < 0.65f) return "Torso_Jacket";
                if (normX <= 0.28f) return normY > 0.60f ? "Arm_Upper_R" : "Arm_Forearm_R";
                return normY > 0.60f ? "Arm_Upper_L" : "Hand_Fist_L";
            }

            // เข็มขัดและพร็อพ (Y ระหว่าง 0.35 - 0.48)
            if (normY > 0.38f)
            {
                if (normX < 0.30f) return normY > 0.44f ? "Hand_Open_R" : "Prop_Dagger";
                if (normX < 0.70f) return "Belt_Leather";
                return "Pouch_Blue";
            }

            // กางเกงและขา (Y ระหว่าง 0.15 - 0.38)
            if (normY > 0.18f)
            {
                if (normX > 0.20f && normX < 0.85f && r.height > 100) return "Pants_Jeans";
                if (normX < 0.45f) return "Shin_Cuff_R";
                return "Shin_Cuff_L";
            }

            // รองเท้า (แถวล่างสุด Y < 0.18)
            if (normX < 0.45f) return "Shoe_R";
            return "Shoe_L";
        }

        private static Sprite LoadSpritePart(string partName)
        {
            string path = $"{PARTS_DIR}/{partName}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject AssembleCharacter()
        {
            // ลบตัวละครเก่า
            GameObject oldPlayer = GameObject.Find("Player_Fighter (FGT001)");
            if (oldPlayer != null) Undo.DestroyObjectImmediate(oldPlayer);

            GameObject oldModPlayer = GameObject.Find("Player_Modular");
            if (oldModPlayer != null) Undo.DestroyObjectImmediate(oldModPlayer);

            GameObject entitiesRoot = GameObject.Find("--- ENTITIES (Dynamic) ---");
            Transform parentTrans = (entitiesRoot != null) ? entitiesRoot.transform : null;

            // 1. Root GameObject
            GameObject root = new GameObject("Player_Modular");
            if (parentTrans != null) root.transform.SetParent(parentTrans, false);

            // วางตัวละครลงบนพื้นถนน (Y = 1.25 ผิวถนน)
            root.transform.position = new Vector3(-8f, 3.05f, 0f);
            root.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            // 2. Physics & Movement
            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3.5f;

            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.6f, 3.4f);
            col.offset = new Vector2(0f, -0.1f);

            PlayerMovement2D movement = root.AddComponent<PlayerMovement2D>();
            root.AddComponent<PlayerInputHandler>();

            // 3. Visual Rig (ประกอบชิ้นส่วนตาม Sorting Order)
            GameObject visualRig = new GameObject("VisualRig");
            visualRig.transform.SetParent(root.transform, false);

            Material unlitMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            // 3.1 ผมด้านหลัง (SortingOrder 2)
            CreatePart(visualRig, "Hair_Back", LoadSpritePart("Hair_Back"), new Vector3(0.05f, 1.25f, 0f), 2, unlitMat);

            // 3.2 ขาหลัง / รองเท้าขวา (SortingOrder 3)
            CreatePart(visualRig, "Shin_R", LoadSpritePart("Shin_Cuff_R"), new Vector3(-0.45f, -0.90f, 0f), 3, unlitMat);
            CreatePart(visualRig, "Shoe_R", LoadSpritePart("Shoe_R"), new Vector3(-0.45f, -1.35f, 0f), 4, unlitMat);

            // 3.3 แขนขวา (SortingOrder 3)
            CreatePart(visualRig, "Arm_R", LoadSpritePart("Arm_Upper_R"), new Vector3(-0.48f, 0.40f, 0f), 3, unlitMat);
            CreatePart(visualRig, "Hand_R", LoadSpritePart("Hand_Open_R"), new Vector3(-0.65f, 0.05f, 0f), 4, unlitMat);

            // 3.4 กางเกง (SortingOrder 5)
            CreatePart(visualRig, "Pants", LoadSpritePart("Pants_Jeans"), new Vector3(0f, -0.35f, 0f), 5, unlitMat);

            // 3.5 ลำตัว / แจ็กเก็ต (SortingOrder 6)
            CreatePart(visualRig, "Torso", LoadSpritePart("Torso_Jacket"), new Vector3(0f, 0.45f, 0f), 6, unlitMat);
            CreatePart(visualRig, "Belt", LoadSpritePart("Belt_Leather"), new Vector3(0f, 0.05f, 0f), 7, unlitMat);

            // 3.6 ศีรษะและใบหน้า (SortingOrder 8)
            Sprite faceSprite = LoadSpritePart("Head_Face_Center") ?? LoadSpritePart("Head_Face_Right") ?? LoadSpritePart("Head_Base_A");
            GameObject head = CreatePart(visualRig, "Head_Face", faceSprite, new Vector3(0f, 1.15f, 0f), 8, unlitMat);
            
            Sprite eyeSprite = LoadSpritePart("Eyes_Battle") ?? LoadSpritePart("Eyes_Wink") ?? LoadSpritePart("Eyes_Shocked");
            CreatePart(head, "Eyes", eyeSprite, new Vector3(0f, 0.02f, 0f), 9, unlitMat);
            CreatePart(head, "Hair_Front", LoadSpritePart("Hair_Front"), new Vector3(-0.02f, 0.28f, 0f), 10, unlitMat);

            // 3.7 ขาหน้า / รองเท้าซ้าย (SortingOrder 10)
            CreatePart(visualRig, "Shin_L", LoadSpritePart("Shin_Cuff_L"), new Vector3(0.50f, -0.85f, 0f), 10, unlitMat);
            CreatePart(visualRig, "Shoe_L", LoadSpritePart("Shoe_L"), new Vector3(0.55f, -1.30f, 0f), 11, unlitMat);

            // 3.8 แขนหน้า / กำหมัดซ้าย (SortingOrder 12)
            CreatePart(visualRig, "Arm_L", LoadSpritePart("Arm_Upper_L"), new Vector3(0.48f, 0.40f, 0f), 12, unlitMat);
            CreatePart(visualRig, "Hand_L", LoadSpritePart("Hand_Fist_L"), new Vector3(0.68f, 0.15f, 0f), 13, unlitMat);

            // ปรับขนาด Crouch
            SerializedObject so = new SerializedObject(movement);
            so.FindProperty("crouchColliderSize").vector2Value = new Vector2(1.6f, 2.0f);
            so.FindProperty("crouchColliderOffset").vector2Value = new Vector2(0f, -0.75f);
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(root, "Create Modular Player");
            return root;
        }

        private static GameObject CreatePart(GameObject parent, string name, Sprite sprite, Vector3 localPos, int sortingOrder, Material mat)
        {
            GameObject partObj = new GameObject(name);
            partObj.transform.SetParent(parent.transform, false);
            partObj.transform.localPosition = localPos;
            partObj.transform.localScale = Vector3.one;

            if (sprite != null)
            {
                SpriteRenderer sr = partObj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = sortingOrder;
                if (mat != null) sr.material = mat;
                EditorUtility.SetDirty(sr);
            }

            return partObj;
        }

        private static void SaveAsPrefab(GameObject playerObj)
        {
            if (!Directory.Exists("Assets/Prefabs"))
                Directory.CreateDirectory("Assets/Prefabs");
            if (!Directory.Exists(PREFAB_DIR))
                Directory.CreateDirectory(PREFAB_DIR);

            PrefabUtility.SaveAsPrefabAssetAndConnect(playerObj, PREFAB_PATH, InteractionMode.AutomatedAction);
            AssetDatabase.Refresh();
        }

        private static void ConnectToCamera(GameObject playerObj)
        {
            CameraFollow2D cam = Object.FindFirstObjectByType<CameraFollow2D>();
            if (cam != null)
            {
                cam.SetTarget(playerObj.transform);
                EditorUtility.SetDirty(cam);
            }
        }
    }
}
#endif
