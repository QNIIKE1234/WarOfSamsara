#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using WarOfSamsara.CameraControl;
using WarOfSamsara.Environment;
using WarOfSamsara.Player;

namespace WarOfSamsara.Editor
{
    /// <summary>
    /// เครื่องมือช่วยสร้างโครงสร้าง Hierarchy มาตรฐานสำหรับ 2D MMORPG (MapleStory + SoulSaver Style)
    /// เรียกใช้งานได้จากเมนู: WarOfSamsara > Setup 2D MMO Scene Hierarchy Template
    /// </summary>
    public static class SceneHierarchyTemplateBuilder
    {
        [MenuItem("WarOfSamsara/Setup 2D MMO Scene Hierarchy Template", false, 10)]
        public static void BuildHierarchyTemplate()
        {
            if (!EditorUtility.DisplayDialog(
                "ปรับสเกลและสร้าง Scene Hierarchy Template",
                "ระบบจะปรับสัดส่วนตัวละคร (1.2 x 1.8 ม.) และขยายแมพสนามทดสอบให้กว้างขวาง (พื้นยาว 58 ม., แพลตฟอร์ม 3 ชั้น, เชือกปีนตรงกลาง) ตามมาตรฐาน 2D Action MMORPG\n\nต้องการดำเนินการต่อหรือไม่?",
                "สร้างและปรับสเกลทันที (Build)", "ยกเลิก (Cancel)"))
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup 2D MMO Scene Hierarchy");

            // 1. Core & Managers
            GameObject managersRoot = GetOrCreateRoot("--- MANAGERS ---");
            GetOrCreateChild(managersRoot, "GameManager");
            GetOrCreateChild(managersRoot, "NetworkRunner (Fusion 2)");
            GetOrCreateChild(managersRoot, "AudioManager");

            // 2. Camera & Rendering (With 2D Global Light)
            GameObject renderingRoot = GetOrCreateRoot("--- CAMERAS & RENDERING ---");
            SetupMainCamera(renderingRoot);

            // 3. Environment & World
            GameObject envRoot = GetOrCreateRoot("--- ENVIRONMENT ---");
            SetupEnvironment(envRoot);

            // 4. Spawn Points
            GameObject spawnRoot = GetOrCreateRoot("--- SPAWN POINTS ---");
            GetOrCreateChild(spawnRoot, "Player_SpawnPoint");
            GetOrCreateChild(spawnRoot, "Monster_SpawnPoints");
            GetOrCreateChild(spawnRoot, "NPC_SpawnPoints");

            // 5. Dynamic Entities (Players, Monsters, Items)
            GameObject entitiesRoot = GetOrCreateRoot("--- ENTITIES (Dynamic) ---");
            GetOrCreateChild(entitiesRoot, "Players");
            GetOrCreateChild(entitiesRoot, "Monsters");
            GetOrCreateChild(entitiesRoot, "DropItems (Souls & Loot)");
            GetOrCreateChild(entitiesRoot, "Projectiles");

            // 6. Setup Test Playable Stage (Ground, Platforms, Rope, Player)
            SetupTestPlayground(envRoot, entitiesRoot);

            // 7. UI / Canvas (1920 x 1080)
            GameObject uiRoot = GetOrCreateRoot("--- UI CANVAS (1920x1080) ---");
            SetupUICanvas(uiRoot);

            // Link Camera Bounds
            LinkCameraToBounds();

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("สำเร็จ!", "ปรับสเกลตัวละครและฉากเรียบร้อยแล้ว!\nตัวละครขนาดพอดีตา ไม่ตกโลก สามารถกด Play (▶️) เล่นได้ทันที", "ตกลง");
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

        private static GameObject GetOrCreateChild(GameObject parent, string name)
        {
            Transform child = parent.transform.Find(name);
            if (child == null)
            {
                GameObject newObj = new GameObject(name);
                newObj.transform.SetParent(parent.transform, false);
                Undo.RegisterCreatedObjectUndo(newObj, "Create " + name);
                return newObj;
            }
            return child.gameObject;
        }

        private static void SetupMainCamera(GameObject parent)
        {
            // Remove 3D Directional Light if it exists
            GameObject dirLight = GameObject.Find("Directional Light");
            if (dirLight != null)
            {
                Undo.DestroyObjectImmediate(dirLight);
            }

            // 2D Global Light (จำเป็นมากสำหรับ URP 2D เพื่อให้มองเห็น Sprites ทั้งหมด)
            GameObject lightObj = GetOrCreateChild(parent, "Global Light 2D");
            var light2d = lightObj.GetComponent<Light2D>();
            if (light2d == null) light2d = lightObj.AddComponent<Light2D>();
            light2d.lightType = Light2D.LightType.Global;
            light2d.color = Color.white;
            light2d.intensity = 1.0f;
            EditorUtility.SetDirty(light2d);

            // Camera Setup
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
            camObj.transform.position = new Vector3(0f, 0f, -10f);

            // ตั้งค่ากล้อง 2D Orthographic (Size 5.4 = จอสูง 10.8 เมตร พอดีเป๊ะกับความละเอียด 1080p)
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.4f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.11f, 0.13f, 0.18f, 1f); // Dark Charcoal Blue
            mainCam.rect = new Rect(0f, 0f, 1f, 1f);
            EditorUtility.SetDirty(mainCam);

            // Ensure URP Additional Camera Data
            if (camObj.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                camObj.AddComponent<UniversalAdditionalCameraData>();
            }

            // เพิ่มคอมโพเนนต์ CameraFollow2D และ LetterboxAspectEnforcer
            if (camObj.GetComponent<CameraFollow2D>() == null)
            {
                camObj.AddComponent<CameraFollow2D>();
            }

            if (camObj.GetComponent<LetterboxAspectEnforcer>() == null)
            {
                camObj.AddComponent<LetterboxAspectEnforcer>();
            }
        }

        private static void SetupEnvironment(GameObject parent)
        {
            // Parallax BG
            GameObject parallaxObj = GetOrCreateChild(parent, "Parallax_Background");
            if (parallaxObj.GetComponent<ParallaxBackground>() == null)
            {
                parallaxObj.AddComponent<ParallaxBackground>();
            }
            GetOrCreateChild(parallaxObj, "Layer_01_Sky");
            GetOrCreateChild(parallaxObj, "Layer_02_FarMountains");
            GetOrCreateChild(parallaxObj, "Layer_03_MidHills");

            // Grid & Tilemaps
            GameObject gridObj = GetOrCreateChild(parent, "Level_Grid");
            if (gridObj.GetComponent<Grid>() == null)
            {
                gridObj.AddComponent<Grid>();
            }

            CreateTilemapLayer(gridObj, "Tilemap_Ground", 0);
            CreateTilemapLayer(gridObj, "Tilemap_Platforms (One-Way)", 1);
            CreateTilemapLayer(gridObj, "Tilemap_Hazards", 2);
            CreateTilemapLayer(gridObj, "Tilemap_Foreground", 10);

            // Interactions
            GameObject interactObj = GetOrCreateChild(parent, "Interactions");
            GetOrCreateChild(interactObj, "Ropes_and_Ladders");
            GetOrCreateChild(interactObj, "Portals");

            // Map Bounds (ขนาด 60 x 20 เมตร รองรับกล้อง 16:9 สบายๆ)
            GameObject boundsObj = GetOrCreateChild(parent, "MapBounds");
            MapBounds mapBounds = boundsObj.GetComponent<MapBounds>();
            if (mapBounds == null) mapBounds = boundsObj.AddComponent<MapBounds>();
            mapBounds.Collider.size = new Vector2(60f, 20f);
            mapBounds.Collider.offset = new Vector2(0f, 0f);
            EditorUtility.SetDirty(mapBounds.Collider);
        }

        private static void CreateTilemapLayer(GameObject gridObj, string name, int orderInLayer)
        {
            GameObject tmObj = GetOrCreateChild(gridObj, name);
            if (tmObj.GetComponent<Tilemap>() == null) tmObj.AddComponent<Tilemap>();
            TilemapRenderer tr = tmObj.GetComponent<TilemapRenderer>();
            if (tr == null) tr = tmObj.AddComponent<TilemapRenderer>();
            tr.sortingOrder = orderInLayer;
            EditorUtility.SetDirty(tr);
        }

        private static Sprite GetOrCreateWhiteSprite()
        {
            string spritePath = "Assets/Art/Sprites/Square.png";

            // 1. Try loading all sub-assets from Square.png
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            if (assets != null && assets.Length > 0)
            {
                foreach (var obj in assets)
                {
                    if (obj is Sprite s) return s;
                }
            }

            // 2. Fallback to Unity's built-in UI Background/UISprite
            Sprite builtin = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (builtin != null) return builtin;
            builtin = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (builtin != null) return builtin;

            return null;
        }

        private static void SetupTestPlayground(GameObject envRoot, GameObject entitiesRoot)
        {
            Sprite whiteSprite = GetOrCreateWhiteSprite();
            if (whiteSprite == null)
            {
                Debug.LogError("[WarOfSamsara] Failed to load any sprite for test playground!");
                return;
            }

            // เคลียร์ของเก่าทิ้งทั้งหมดก่อน เพื่อไม่ให้มี Collider หรือ Offset เก่าตกค้าง
            string[] testEnvNames = { "Test_Ground", "Test_Platform_1", "Test_Platform_2", "Test_Platform_3", "Test_Rope" };
            foreach (string name in testEnvNames)
            {
                Transform t = envRoot.transform.Find(name);
                if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
            }

            Transform playersContainer = entitiesRoot.transform.Find("Players");
            GameObject playersObj = (playersContainer != null) ? playersContainer.gameObject : entitiesRoot;
            for (int i = playersObj.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(playersObj.transform.GetChild(i).gameObject);
            }

            // 1. Ground Floor (พื้นล่างสุด - กว้าง 50 เมตร หนา 1.5 เมตร ทอดผ่านกลางจอ)
            GameObject ground = new GameObject("Test_Ground");
            ground.transform.SetParent(envRoot.transform, false);
            ground.transform.position = new Vector3(0f, -3.5f, 0f);
            ground.transform.localScale = new Vector3(50f, 1.5f, 1f);
            SpriteRenderer srGround = ground.AddComponent<SpriteRenderer>();
            srGround.sprite = whiteSprite;
            srGround.color = new Color(0.18f, 0.65f, 0.28f); // Vibrant Grass Green
            srGround.sortingOrder = 0;
            BoxCollider2D colGround = ground.AddComponent<BoxCollider2D>();
            colGround.size = new Vector2(1f, 1f);
            colGround.offset = Vector2.zero;
            Undo.RegisterCreatedObjectUndo(ground, "Create Test_Ground");

            // 2. One-Way Platforms (แท่นกระโดดไม้ 3 แท่น สไตล์ MapleStory)
            // แท่นล่างซ้าย
            GameObject plat1 = new GameObject("Test_Platform_1");
            plat1.transform.SetParent(envRoot.transform, false);
            plat1.transform.position = new Vector3(-6f, -1.0f, 0f);
            plat1.transform.localScale = new Vector3(8f, 0.5f, 1f);
            SpriteRenderer srPlat1 = plat1.AddComponent<SpriteRenderer>();
            srPlat1.sprite = whiteSprite;
            srPlat1.color = new Color(0.72f, 0.44f, 0.2f); // Wood Brown
            srPlat1.sortingOrder = 1;
            BoxCollider2D colPlat1 = plat1.AddComponent<BoxCollider2D>();
            colPlat1.size = new Vector2(1f, 1f);
            colPlat1.offset = Vector2.zero;
            plat1.AddComponent<OneWayPlatform>();
            Undo.RegisterCreatedObjectUndo(plat1, "Create Test_Platform_1");

            // แท่นล่างขวา
            GameObject plat2 = new GameObject("Test_Platform_2");
            plat2.transform.SetParent(envRoot.transform, false);
            plat2.transform.position = new Vector3(6f, -1.0f, 0f);
            plat2.transform.localScale = new Vector3(8f, 0.5f, 1f);
            SpriteRenderer srPlat2 = plat2.AddComponent<SpriteRenderer>();
            srPlat2.sprite = whiteSprite;
            srPlat2.color = new Color(0.72f, 0.44f, 0.2f); // Wood Brown
            srPlat2.sortingOrder = 1;
            BoxCollider2D colPlat2 = plat2.AddComponent<BoxCollider2D>();
            colPlat2.size = new Vector2(1f, 1f);
            colPlat2.offset = Vector2.zero;
            plat2.AddComponent<OneWayPlatform>();
            Undo.RegisterCreatedObjectUndo(plat2, "Create Test_Platform_2");

            // แท่นบนตรงกลาง
            GameObject plat3 = new GameObject("Test_Platform_3");
            plat3.transform.SetParent(envRoot.transform, false);
            plat3.transform.position = new Vector3(0f, 1.5f, 0f);
            plat3.transform.localScale = new Vector3(9f, 0.5f, 1f);
            SpriteRenderer srPlat3 = plat3.AddComponent<SpriteRenderer>();
            srPlat3.sprite = whiteSprite;
            srPlat3.color = new Color(0.72f, 0.44f, 0.2f);
            srPlat3.sortingOrder = 1;
            BoxCollider2D colPlat3 = plat3.AddComponent<BoxCollider2D>();
            colPlat3.size = new Vector2(1f, 1f);
            colPlat3.offset = Vector2.zero;
            plat3.AddComponent<OneWayPlatform>();
            Undo.RegisterCreatedObjectUndo(plat3, "Create Test_Platform_3");

            // 3. Climbable Rope (เชือกปีนทอง เชื่อมจากพื้นถึงแท่นบน สูง 5.5 เมตร)
            GameObject rope = new GameObject("Test_Rope");
            rope.transform.SetParent(envRoot.transform, false);
            rope.transform.position = new Vector3(0f, -1.0f, 0f);
            rope.transform.localScale = new Vector3(0.4f, 5.5f, 1f);
            SpriteRenderer srRope = rope.AddComponent<SpriteRenderer>();
            srRope.sprite = whiteSprite;
            srRope.color = new Color(1f, 0.85f, 0.2f); // Gold Rope
            srRope.sortingOrder = -1;
            BoxCollider2D colRope = rope.AddComponent<BoxCollider2D>();
            colRope.isTrigger = true;
            colRope.size = new Vector2(1f, 1f);
            colRope.offset = Vector2.zero;
            rope.AddComponent<ClimbableRope>();
            Undo.RegisterCreatedObjectUndo(rope, "Create Test_Rope");

            // 4. Test Player (สัดส่วนตัวละคร 2D Chibi Hero ขนาด 1.4 x 1.8 เมตร)
            GameObject player = new GameObject("Test_Player");
            player.transform.SetParent(playersObj.transform, false);
            player.transform.position = new Vector3(0f, 0f, 0f); // เกิดตรงกลางจอ ร่วงลงมายืนบนพื้นสวยๆ
            player.transform.localScale = new Vector3(1.4f, 1.8f, 1f);

            SpriteRenderer srPlayer = player.AddComponent<SpriteRenderer>();
            srPlayer.sprite = whiteSprite;
            srPlayer.color = new Color(0f, 0.82f, 1f); // Hero Cyan
            srPlayer.sortingOrder = 5;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3.5f;

            BoxCollider2D col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            col.offset = Vector2.zero;

            player.AddComponent<PlayerMovement2D>();
            player.AddComponent<PlayerInputHandler>();
            Undo.RegisterCreatedObjectUndo(player, "Create Test_Player");

            // ตั้งค่ากล้องให้ติดตาม Player
            CameraFollow2D camFollow = Object.FindFirstObjectByType<CameraFollow2D>();
            if (camFollow != null)
            {
                camFollow.SetTarget(player.transform);
                EditorUtility.SetDirty(camFollow);
            }
        }

        private static void SetupUICanvas(GameObject parent)
        {
            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.transform.SetParent(parent.transform, false);
                esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<StandaloneInputModule>();
#endif
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // Main Canvas
            GameObject canvasObj = GetOrCreateChild(parent, "Main_Canvas");
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // UI Elements Containers
            GameObject hud = GetOrCreateChild(canvasObj, "HUD");
            GetOrCreateChild(hud, "TopLeft_CharacterStatus (HP/MP/Soul)");
            GetOrCreateChild(hud, "TopRight_Minimap");
            GetOrCreateChild(hud, "Bottom_QuickSlots");
            GetOrCreateChild(hud, "Bottom_ChatBox");
            GetOrCreateChild(hud, "Bottom_EXPBar");

            GetOrCreateChild(canvasObj, "DamageNumbers_Container");

            GameObject windows = GetOrCreateChild(canvasObj, "Windows");
            GetOrCreateChild(windows, "InventoryWindow");
            GetOrCreateChild(windows, "CharacterStatsWindow");
            GetOrCreateChild(windows, "SkillWindow");
        }

        private static void LinkCameraToBounds()
        {
            CameraFollow2D camFollow = Object.FindFirstObjectByType<CameraFollow2D>();
            MapBounds mapBounds = Object.FindFirstObjectByType<MapBounds>();

            if (camFollow != null && mapBounds != null)
            {
                SerializedObject so = new SerializedObject(camFollow);
                SerializedProperty prop = so.FindProperty("boundaryCollider");
                if (prop != null)
                {
                    prop.objectReferenceValue = mapBounds.Collider;
                    so.ApplyModifiedProperties();
                }
            }
        }
    }
}
#endif
