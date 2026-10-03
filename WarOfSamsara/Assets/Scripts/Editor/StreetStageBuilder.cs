#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using WarOfSamsara.CameraControl;
using WarOfSamsara.Environment;
using WarOfSamsara.Player;

namespace WarOfSamsara.Editor
{
    /// <summary>
    /// ตัวสร้างฉากจริง STG001_The Streets พร้อมแยกเลเยอร์ Parallax
    /// พื้นถนนจริงมี Collider และตัวละคร Fighter (FGT001_AllRounder)
    /// </summary>
    public static class StreetStageBuilder
    {
        private const string STAGE_FOLDER = "Assets/Resources/Environments/STG001_The Streets/";
        private const string FIGHTER_PATH = "Assets/Resources/Environments/FighterTest/FGT001_AllRounder_idle001.png";

        [MenuItem("WarOfSamsara/Setup Stage - STG001 The Streets (1-Click)", false, 1)]
        public static void BuildStreetStageImmediate()
        {
            BuildStreetStageInternal(false);
        }

        [MenuItem("WarOfSamsara/Setup Stage - STG001 The Streets (With Confirm)", false, 2)]
        public static void BuildStreetStage()
        {
            BuildStreetStageInternal(true);
        }

        public static void BuildStreetStageInternal(bool showDialog)
        {
            if (showDialog && !EditorUtility.DisplayDialog(
                "สร้างฉากจริง STG001_The Streets",
                "ระบบจะนำ Asset ฉากถนนจริง (แยกเลเยอร์ Sky, BG, MG, Ground, FG ครบชุด)\nพร้อมวางพื้น Collider บนถนน และเปลี่ยนตัวละครเป็น Fighter จริง\n\nต้องการดำเนินการต่อหรือไม่?",
                "สร้างทันที (Build)", "ยกเลิก (Cancel)"))
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup STG001 Street Stage");

            // 1. เคลียร์ Environment & Entities เก่าออกเพื่อความคลีน
            GameObject oldEnv = GameObject.Find("--- ENVIRONMENT ---");
            if (oldEnv != null) Undo.DestroyObjectImmediate(oldEnv);

            GameObject oldEntities = GameObject.Find("--- ENTITIES (Dynamic) ---");
            if (oldEntities != null) Undo.DestroyObjectImmediate(oldEntities);

            // 2. Camera & Rendering
            GameObject renderingRoot = GetOrCreateRoot("--- CAMERAS & RENDERING ---");
            SetupCameraAndLighting(renderingRoot);

            // 3. Environment (Stage Layers)
            GameObject envRoot = GetOrCreateRoot("--- ENVIRONMENT ---");
            SetupStreetEnvironment(envRoot);

            // 4. Player Fighter & Enemies
            GameObject entitiesRoot = GetOrCreateRoot("--- ENTITIES (Dynamic) ---");
            SetupPlayerFighter(entitiesRoot);
            SetupEnemies(entitiesRoot);

            // 5. Connect Camera to Player and Bounds
            LinkCamera();

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"<color=#00FF88>[WarOfSamsara]</color> สร้างฉาก STG001_The Streets และเซฟลง {activeScene.path} สำเร็จเรียบร้อย!");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("สำเร็จ!", "สร้างฉาก STG001_The Streets และตัวละคร Fighter เรียบร้อยแล้ว!\nสามารถกด Play (▶️) เพื่อทดสอบวิ่ง/กระโดดบนถนนจริงได้ทันทีครับ", "ตกลง");
            }
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            }
            return obj;
        }

        private static Sprite LoadSprite(string relativePath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(relativePath);
            if (assets != null && assets.Length > 0)
            {
                foreach (var a in assets)
                {
                    if (a is Sprite s) return s;
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        }

        private static void SetupCameraAndLighting(GameObject parent)
        {
            // ลบ 3D Light
            GameObject dirLight = GameObject.Find("Directional Light");
            if (dirLight != null) Undo.DestroyObjectImmediate(dirLight);

            // 2D Global Light
            Transform lightTrans = parent.transform.Find("Global Light 2D");
            GameObject lightObj = (lightTrans != null) ? lightTrans.gameObject : new GameObject("Global Light 2D");
            lightObj.transform.SetParent(parent.transform, false);
            var light2d = lightObj.GetComponent<Light2D>();
            if (light2d == null) light2d = lightObj.AddComponent<Light2D>();
            light2d.lightType = Light2D.LightType.Global;
            light2d.color = Color.white;
            light2d.intensity = 1.0f;
            EditorUtility.SetDirty(light2d);

            // Main Camera
            Camera mainCam = Camera.main;
            GameObject camObj;
            if (mainCam == null)
            {
                camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
            }
            else
            {
                camObj = mainCam.gameObject;
                Undo.RecordObject(mainCam, "Setup Main Camera");
            }

            camObj.transform.SetParent(parent.transform, true);
            camObj.transform.position = new Vector3(0f, 2.5f, -10f);

            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.5f; // ระยะซูมกำลังสวย เห็นถนนและตึกด้านหลังอลังการ
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            mainCam.rect = new Rect(0f, 0f, 1f, 1f);
            EditorUtility.SetDirty(mainCam);

            if (camObj.GetComponent<UniversalAdditionalCameraData>() == null)
                camObj.AddComponent<UniversalAdditionalCameraData>();

            if (camObj.GetComponent<CameraFollow2D>() == null)
                camObj.AddComponent<CameraFollow2D>();

            if (camObj.GetComponent<LetterboxAspectEnforcer>() == null)
                camObj.AddComponent<LetterboxAspectEnforcer>();
        }

        private static void SetupStreetEnvironment(GameObject envRoot)
        {
            // Root สำหรับแมพ STG001
            GameObject stageRoot = new GameObject("STG001_The_Streets");
            stageRoot.transform.SetParent(envRoot.transform, false);
            stageRoot.transform.position = Vector3.zero;

            // 1. Layer 7 Sky (ท้องฟ้ายามค่ำคืน)
            CreateStageLayer(stageRoot, "Layer_07_Sky", STAGE_FOLDER + "Layer 7 Sky.png", -20);

            // 2. Layer 6 BG (ตึกระฟ้าและวิวเมืองไกล)
            CreateStageLayer(stageRoot, "Layer_06_BG", STAGE_FOLDER + "Layer 6 BG.png", -10);

            // 3. Layer 4 MG (ตึกระยะกลางและป้ายไฟ)
            CreateStageLayer(stageRoot, "Layer_04_MG", STAGE_FOLDER + "Layer 4 MG.png", -5);

            // 4. Layer 5 Ground (พื้นถนน ทางเท้า รั้ว)
            GameObject groundObj = CreateStageLayer(stageRoot, "Layer_05_Ground", STAGE_FOLDER + "Layer 5 Ground.png", 0);

            // ติดตั้ง Collider พื้นถนนจริง:
            // Texture 1280x360, PPU=24 -> กว้าง 53.333m, ส่วนทึบ Y=[0..210] -> ผิวถนนอยู่ที่ Y = +1.25f พอดีเป๊ะ
            BoxCollider2D groundCol = groundObj.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(53.333f, 8.75f);
            groundCol.offset = new Vector2(0f, -3.125f); // ขอบบนของ Collider อยู่ที่ Y = (-3.125 + 4.375) = +1.25f
            EditorUtility.SetDirty(groundCol);

            // กำแพงกั้นซ้าย-ขวา ไม่ให้เดินตกแมพ
            GameObject leftWall = new GameObject("Left_Wall");
            leftWall.transform.SetParent(stageRoot.transform, false);
            leftWall.transform.position = new Vector3(-26.666f, 3f, 0f);
            BoxCollider2D colLeft = leftWall.AddComponent<BoxCollider2D>();
            colLeft.size = new Vector2(1f, 20f);

            GameObject rightWall = new GameObject("Right_Wall");
            rightWall.transform.SetParent(stageRoot.transform, false);
            rightWall.transform.position = new Vector3(26.666f, 3f, 0f);
            BoxCollider2D colRight = rightWall.AddComponent<BoxCollider2D>();
            colRight.size = new Vector2(1f, 20f);

            // 5. Layer 2 FG (ป้าย เสาไฟ วัตถุหน้าฉาก)
            CreateStageLayer(stageRoot, "Layer_02_FG", STAGE_FOLDER + "Layer 2 FG.png", 15);

            // 6. Layer 1 Super FG (เงาแสงหน้าสุด)
            CreateStageLayer(stageRoot, "Layer_01_Super_FG", STAGE_FOLDER + "Layer 1 Super FG.png", 20);

            // 7. Map Bounds สำหรับล็อกกล้อง (เก็บไว้ใน stageRoot เพื่อให้รวมอยู่ใน Prefab แมพ)
            GameObject boundsObj = new GameObject("MapBounds");
            boundsObj.transform.SetParent(stageRoot.transform, false);
            MapBounds mapBounds = boundsObj.AddComponent<MapBounds>();
            mapBounds.Collider.size = new Vector2(53.333f, 15f);
            mapBounds.Collider.offset = new Vector2(0f, 0f);
            EditorUtility.SetDirty(mapBounds.Collider);

            // บันทึก stageRoot เป็น Prefab อัตโนมัติใน Assets/Prefabs/Maps และ Assets/Resources/Maps
            string prefabDir = "Assets/Prefabs/Maps";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Maps");
            }

            string resDir = "Assets/Resources/Maps";
            if (!AssetDatabase.IsValidFolder(resDir))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Maps");
            }

            string prefabPath = "Assets/Prefabs/Maps/STG001_The_Streets.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(stageRoot, prefabPath, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(stageRoot, "Assets/Resources/Maps/STG001_The_Streets.prefab");
            AssetDatabase.Refresh();

            Undo.RegisterCreatedObjectUndo(stageRoot, "Create STG001 Stage");
        }

        private static GameObject CreateStageLayer(GameObject parent, string layerName, string spritePath, int sortingOrder)
        {
            GameObject layerObj = new GameObject(layerName);
            layerObj.transform.SetParent(parent.transform, false);
            layerObj.transform.position = Vector3.zero;
            layerObj.transform.localScale = Vector3.one;

            SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spritePath);
            sr.sortingOrder = sortingOrder;

            Material unlitMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (unlitMat != null) sr.material = unlitMat;

            EditorUtility.SetDirty(sr);
            return layerObj;
        }

        private static void SetupPlayerFighter(GameObject entitiesRoot)
        {
            Transform playersContainer = entitiesRoot.transform.Find("Players");
            GameObject playersObj = (playersContainer != null) ? playersContainer.gameObject : entitiesRoot;

            // สร้างตัวละคร Fighter (FGT001)
            GameObject player = new GameObject("Player_Fighter (FGT001)");
            player.transform.SetParent(playersObj.transform, false);

            // สัดส่วนตัวละคร: Texture 512x512, ตัวละครจริงสูง 344px (Y=0 ถึง 343)
            // เมื่อ Scale (0.7, 0.7, 1) -> สูง 2.40 เมตร เหมาะสมกับฉากถนนอย่างสมบูรณ์แบบ
            // ขอบล่างของเท้าอยู่ที่ Local Y = -2.56, คิดเป็น World Offset = -2.56 * 0.7 = -1.792f
            // เพื่อให้เท้าวางอยู่บนผิวถนน (Y = +1.25f) -> Spawn Y = 1.25f + 1.792f = +3.042f
            player.transform.position = new Vector3(-8f, 3.05f, 0f);
            player.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(FIGHTER_PATH);
            sr.sortingOrder = 5;

            Material unlitMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (unlitMat != null) sr.material = unlitMat;
            EditorUtility.SetDirty(sr);

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3.5f;

            // BoxCollider ที่ครอบตัวละครเป๊ะ:
            // ตัวละครสูง 3.43 ใน local units, Center Y = -0.845f, ขอบล่างอยู่ที่ -2.56f ตรงกับส้นเท้า 100%!
            BoxCollider2D col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 3.43f);
            col.offset = new Vector2(0f, -0.845f);

            PlayerMovement2D movement = player.AddComponent<PlayerMovement2D>();
            player.AddComponent<PlayerInputHandler>();

            // เซ็ตค่า Crouch Collider ให้เท้าติดพื้น ไม่ลอยตอนก้ม
            SerializedObject so = new SerializedObject(movement);
            SerializedProperty crouchSizeProp = so.FindProperty("crouchColliderSize");
            SerializedProperty crouchOffsetProp = so.FindProperty("crouchColliderOffset");
            if (crouchSizeProp != null) crouchSizeProp.vector2Value = new Vector2(1.5f, 2.0f);
            if (crouchOffsetProp != null) crouchOffsetProp.vector2Value = new Vector2(0f, -1.56f);
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(player, "Create Player Fighter");
        }

        private static void SetupEnemies(GameObject entitiesRoot)
        {
            Transform enemiesContainer = entitiesRoot.transform.Find("Monsters");
            GameObject enemiesObj = (enemiesContainer != null) ? enemiesContainer.gameObject : entitiesRoot;

            Material unlitMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            // 1. Enemy Thug (ENE002) - ศัตรูร่างคน ยืนเทียบส่วนสูงกับผู้เล่น
            string thugPath = "Assets/Resources/Environments/FighterTest/ENE002_Thug_idle001.png";
            GameObject thug = new GameObject("Enemy_Thug (ENE002)");
            thug.transform.SetParent(enemiesObj.transform, false);
            thug.transform.position = new Vector3(4f, 3.05f, 0f);
            thug.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            SpriteRenderer srThug = thug.AddComponent<SpriteRenderer>();
            srThug.sprite = LoadSprite(thugPath);
            srThug.sortingOrder = 5;
            if (unlitMat != null) srThug.material = unlitMat;

            Rigidbody2D rbThug = thug.AddComponent<Rigidbody2D>();
            rbThug.bodyType = RigidbodyType2D.Dynamic;
            rbThug.constraints = RigidbodyConstraints2D.FreezeRotation;
            rbThug.gravityScale = 3.5f;

            BoxCollider2D colThug = thug.AddComponent<BoxCollider2D>();
            colThug.size = new Vector2(1.5f, 3.43f);
            colThug.offset = new Vector2(0f, -0.845f);

            // 2. Enemy Bad Dog (ENE001) - สุนัขข้างถนน
            string dogPath = "Assets/Resources/Environments/FighterTest/ENE001_BadDog_idle001.png";
            GameObject dog = new GameObject("Enemy_BadDog (ENE001)");
            dog.transform.SetParent(enemiesObj.transform, false);
            dog.transform.position = new Vector3(8f, 2.5f, 0f);
            dog.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            SpriteRenderer srDog = dog.AddComponent<SpriteRenderer>();
            srDog.sprite = LoadSprite(dogPath);
            srDog.sortingOrder = 5;
            if (unlitMat != null) srDog.material = unlitMat;

            Rigidbody2D rbDog = dog.AddComponent<Rigidbody2D>();
            rbDog.bodyType = RigidbodyType2D.Dynamic;
            rbDog.constraints = RigidbodyConstraints2D.FreezeRotation;
            rbDog.gravityScale = 3.5f;

            BoxCollider2D colDog = dog.AddComponent<BoxCollider2D>();
            colDog.size = new Vector2(2.0f, 1.8f);
            colDog.offset = new Vector2(0f, -1.6f);

            Undo.RegisterCreatedObjectUndo(thug, "Create Enemy Thug");
            Undo.RegisterCreatedObjectUndo(dog, "Create Enemy Dog");
        }

        private static void LinkCamera()
        {
            CameraFollow2D camFollow = Object.FindFirstObjectByType<CameraFollow2D>();
            MapBounds mapBounds = Object.FindFirstObjectByType<MapBounds>();
            GameObject player = GameObject.Find("Player_Fighter (FGT001)");

            if (camFollow != null)
            {
                if (player != null)
                {
                    camFollow.SetTarget(player.transform);
                }

                if (mapBounds != null)
                {
                    SerializedObject so = new SerializedObject(camFollow);
                    SerializedProperty prop = so.FindProperty("boundaryCollider");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = mapBounds.Collider;
                        so.ApplyModifiedProperties();
                    }
                }
                EditorUtility.SetDirty(camFollow);
            }
        }
    }
}
#endif
