#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WarOfSamsara.Enemy;

namespace WarOfSamsara.Editor
{
    /// <summary>
    /// เครื่องมืออัตโนมัติสำหรับสร้าง Enemy Prefab, Animations, และ Config
    /// รองรับสไปรต์มอนสเตอร์ ENE_0001 (Stone Golem)
    /// </summary>
    public static class EnemyCharacterBuilder
    {
        private const string ENEMY_SHEET_PATH = "Assets/Resources/Character/Enemy/ENE_0001.png";
        private const string ENEMY_FRAMES_DIR = "Assets/Resources/Character/Enemy/Frames";
        private const string ENEMY_ANIM_DIR = "Assets/Animations/Enemy";
        private const string PREFAB_DIR = "Assets/Prefabs/Characters";
        private const string PREFAB_PATH = "Assets/Prefabs/Characters/BaseEnemy.prefab";
        private const string CONFIG_PATH = "Assets/Resources/Character/Enemy/Enemy_Golem_Config.asset";

        [MenuItem("WarOfSamsara/Create Base Enemy Prefab (ENE_0001 Golem)", false, 20)]
        public static void CreateEnemyPrefabAndAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[EnemyCharacterBuilder] กรุณากดออกจาก Play Mode ก่อนรันครับ");
                return;
            }

            if (!File.Exists(ENEMY_SHEET_PATH))
            {
                Debug.LogError($"[EnemyCharacterBuilder] ไม่พบภาพมอนสเตอร์ที่ {ENEMY_SHEET_PATH}");
                return;
            }

            // 1. สกัด 8 เฟรม (4 เฟรมแถวบน = Idle, 4 เฟรมแถวล่าง = Attack)
            Sprite[] frames = ExtractAndLoadFrames();
            if (frames == null || frames.Length < 8)
            {
                Debug.LogError("[EnemyCharacterBuilder] สกัดเฟรมมอนสเตอร์ล้มเหลว");
                return;
            }

            // 2. สร้าง Animation Clips และ Animator Controller
            AnimatorController controller = CreateEnemyAnimations(frames);

            // 3. สร้าง Enemy Config Asset (ScriptableObject)
            EnemyConfig config = GetOrCreateEnemyConfig();

            // 4. ประกอบ GameObject และเซฟเป็น BaseEnemy.prefab
            CreateBaseEnemyPrefab(frames[0], controller, config);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00FF88>[EnemyCharacterBuilder]</color> สร้าง BaseEnemy.prefab พร้อม Config และ Animations สำเร็จเรียบร้อย! อยู่ที่: " + PREFAB_PATH);
        }

        private static Sprite[] ExtractAndLoadFrames()
        {
            if (!Directory.Exists(ENEMY_FRAMES_DIR)) Directory.CreateDirectory(ENEMY_FRAMES_DIR);

            byte[] fileData = File.ReadAllBytes(ENEMY_SHEET_PATH);
            Texture2D srcTex = new Texture2D(2, 2);
            srcTex.LoadImage(fileData);

            int width = srcTex.width;
            int height = srcTex.height;
            int cols = 4;
            int rows = 2;
            int cellW = width / cols;
            int cellH = height / rows;

            Color32[] allPixels = srcTex.GetPixels32();

            int index = 0;
            for (int r = rows - 1; r >= 0; r--)
            {
                for (int c = 0; c < cols; c++)
                {
                    Texture2D frameTex = new Texture2D(cellW, cellH, TextureFormat.RGBA32, false);
                    Color32[] framePixels = new Color32[cellW * cellH];

                    int startX = c * cellW;
                    int startY = r * cellH;

                    for (int py = 0; py < cellH; py++)
                    {
                        for (int px = 0; px < cellW; px++)
                        {
                            Color32 p = allPixels[(startY + py) * width + (startX + px)];
                            if (p.r > 240 && p.g > 240 && p.b > 240)
                            {
                                p = new Color32(0, 0, 0, 0);
                            }
                            framePixels[py * cellW + px] = p;
                        }
                    }

                    frameTex.SetPixels32(framePixels);
                    frameTex.Apply();

                    string framePath = $"{ENEMY_FRAMES_DIR}/ENE_{index}.png";
                    File.WriteAllBytes(framePath, frameTex.EncodeToPNG());
                    Object.DestroyImmediate(frameTex);
                    index++;
                }
            }
            Object.DestroyImmediate(srcTex);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Sprite[] sprites = new Sprite[8];
            for (int i = 0; i < 8; i++)
            {
                string framePath = $"{ENEMY_FRAMES_DIR}/ENE_{i}.png";
                TextureImporter importer = AssetImporter.GetAtPath(framePath) as TextureImporter;
                if (importer != null)
                {
                    bool dirty = false;
                    if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                    if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                    if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                    if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }

                    TextureImporterSettings tis = new TextureImporterSettings();
                    importer.ReadTextureSettings(tis);
                    if (tis.spriteAlignment != (int)SpriteAlignment.Custom || tis.spritePivot != new Vector2(0.5f, 0.1f) || tis.spritePixelsPerUnit != 100)
                    {
                        tis.spriteAlignment = (int)SpriteAlignment.Custom;
                        tis.spritePivot = new Vector2(0.5f, 0.1f);
                        tis.spritePixelsPerUnit = 100;
                        importer.SetTextureSettings(tis);
                        dirty = true;
                    }
                    if (dirty) importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            for (int i = 0; i < 8; i++)
            {
                string framePath = $"{ENEMY_FRAMES_DIR}/ENE_{i}.png";
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                if (sprites[i] == null)
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(framePath);
                    if (tex != null)
                    {
                        sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.1f), 100f);
                    }
                }
            }

            return sprites;
        }

        private static AnimatorController CreateEnemyAnimations(Sprite[] frames)
        {
            if (!Directory.Exists("Assets/Animations")) Directory.CreateDirectory("Assets/Animations");
            if (!Directory.Exists(ENEMY_ANIM_DIR)) Directory.CreateDirectory(ENEMY_ANIM_DIR);

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "VisualRig/BodyRenderer",
                propertyName = "m_Sprite"
            };

            // 1. Idle Clip (4 เฟรมแถวบน: 0, 1, 2, 3 ลูปที่ 6 FPS)
            string idlePath = $"{ENEMY_ANIM_DIR}/Enemy_Idle.anim";
            AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
            if (idleClip == null)
            {
                idleClip = new AnimationClip { frameRate = 6f, name = "Enemy_Idle" };
                ObjectReferenceKeyframe[] idleKeys = new ObjectReferenceKeyframe[5];
                for (int i = 0; i < 4; i++)
                {
                    idleKeys[i] = new ObjectReferenceKeyframe { time = i * (1f / 6f), value = frames[i] };
                }
                idleKeys[4] = new ObjectReferenceKeyframe { time = 4 * (1f / 6f), value = frames[0] };
                AnimationUtility.SetObjectReferenceCurve(idleClip, binding, idleKeys);

                var s = AnimationUtility.GetAnimationClipSettings(idleClip);
                s.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(idleClip, s);
                AssetDatabase.CreateAsset(idleClip, idlePath);
            }

            // 2. Attack Clip (4 เฟรมแถวล่าง: 4, 5, 6, 7 ที่ 8 FPS)
            string atkPath = $"{ENEMY_ANIM_DIR}/Enemy_Attack.anim";
            AnimationClip atkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(atkPath);
            if (atkClip == null)
            {
                atkClip = new AnimationClip { frameRate = 8f, name = "Enemy_Attack" };
                ObjectReferenceKeyframe[] atkKeys = new ObjectReferenceKeyframe[4];
                for (int i = 0; i < 4; i++)
                {
                    atkKeys[i] = new ObjectReferenceKeyframe { time = i * (1f / 8f), value = frames[4 + i] };
                }
                AnimationUtility.SetObjectReferenceCurve(atkClip, binding, atkKeys);

                var s = AnimationUtility.GetAnimationClipSettings(atkClip);
                s.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(atkClip, s);
                AssetDatabase.CreateAsset(atkClip, atkPath);
            }

            // 3. Hit Clip (เฟรม 4 ชะงัก)
            string hitPath = $"{ENEMY_ANIM_DIR}/Enemy_Hit.anim";
            AnimationClip hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(hitPath);
            if (hitClip == null)
            {
                hitClip = new AnimationClip { frameRate = 10f, name = "Enemy_Hit" };
                ObjectReferenceKeyframe[] hitKeys = new ObjectReferenceKeyframe[1];
                hitKeys[0] = new ObjectReferenceKeyframe { time = 0f, value = frames[4] };
                AnimationUtility.SetObjectReferenceCurve(hitClip, binding, hitKeys);
                AssetDatabase.CreateAsset(hitClip, hitPath);
            }

            // 4. Die Clip
            string diePath = $"{ENEMY_ANIM_DIR}/Enemy_Die.anim";
            AnimationClip dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(diePath);
            if (dieClip == null)
            {
                dieClip = new AnimationClip { frameRate = 6f, name = "Enemy_Die" };
                ObjectReferenceKeyframe[] dieKeys = new ObjectReferenceKeyframe[2];
                dieKeys[0] = new ObjectReferenceKeyframe { time = 0f, value = frames[4] };
                dieKeys[1] = new ObjectReferenceKeyframe { time = 0.3f, value = frames[6] };
                AnimationUtility.SetObjectReferenceCurve(dieClip, binding, dieKeys);
                AssetDatabase.CreateAsset(dieClip, diePath);
            }

            // 5. Animator Controller
            string controllerPath = $"{ENEMY_ANIM_DIR}/Enemy_Base.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            AnimatorState idleState = sm.AddState("Idle", new Vector3(250, 0, 0));
            idleState.motion = idleClip;

            AnimatorState attackState = sm.AddState("Attack", new Vector3(500, 0, 0));
            attackState.motion = atkClip;

            AnimatorState hitState = sm.AddState("Hit", new Vector3(250, 150, 0));
            hitState.motion = hitClip;

            AnimatorState dieState = sm.AddState("Die", new Vector3(500, 150, 0));
            dieState.motion = dieClip;

            sm.defaultState = idleState;

            // Idle -> Attack
            var toAtk = idleState.AddTransition(attackState);
            toAtk.hasExitTime = false;
            toAtk.duration = 0f;
            toAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            // Attack -> Idle
            var toIdle = attackState.AddTransition(idleState);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 1f;
            toIdle.duration = 0f;

            // AnyState -> Hit
            var toHit = sm.AddAnyStateTransition(hitState);
            toHit.hasExitTime = false;
            toHit.duration = 0f;
            toHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");

            // Hit -> Idle
            var hitToIdle = hitState.AddTransition(idleState);
            hitToIdle.hasExitTime = true;
            hitToIdle.exitTime = 1f;
            hitToIdle.duration = 0f;

            // AnyState -> Die
            var toDie = sm.AddAnyStateTransition(dieState);
            toDie.hasExitTime = false;
            toDie.duration = 0f;
            toDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

            AssetDatabase.SaveAssets();
            return controller;
        }

        private static EnemyConfig GetOrCreateEnemyConfig()
        {
            EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(CONFIG_PATH);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                config.enemyId = "ENE_0001";
                config.enemyName = "Stone Golem";
                config.level = 1;
                config.maxHp = 120;
                config.defense = 4;
                config.touchDamage = 12;
                config.attackDamage = 25;
                config.attackRange = 1.8f;
                config.attackCooldown = 2.2f;
                config.moveSpeed = 1.8f;
                config.patrolRadius = 4.5f;
                config.detectRadius = 6.0f;
                config.loseTargetRadius = 9.0f;
                config.expReward = 35;
                config.goldReward = 15;

                AssetDatabase.CreateAsset(config, CONFIG_PATH);
                AssetDatabase.SaveAssets();
            }
            return config;
        }

        private static void CreateBaseEnemyPrefab(Sprite defaultSprite, AnimatorController controller, EnemyConfig config)
        {
            if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);

            // สร้าง GameObject ชั่วคราวเพื่อบันทึกลง Prefab
            GameObject enemyObj = new GameObject("BaseEnemy");

            Rigidbody2D rb = enemyObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3.0f;

            BoxCollider2D col = enemyObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 1.8f);
            col.offset = new Vector2(0f, 0.9f);

            GameObject visualRig = new GameObject("VisualRig");
            visualRig.transform.SetParent(enemyObj.transform, false);
            visualRig.transform.localPosition = Vector3.zero;
            visualRig.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            GameObject bodyRendererObj = new GameObject("BodyRenderer");
            bodyRendererObj.transform.SetParent(visualRig.transform, false);
            bodyRendererObj.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = bodyRendererObj.AddComponent<SpriteRenderer>();
            sr.sprite = defaultSprite;
            sr.sortingOrder = 4;
            sr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            Animator animator = enemyObj.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            BaseEnemy baseEnemy = enemyObj.AddComponent<BaseEnemy>();
            baseEnemy.SetConfig(config);

            SerializedObject so = new SerializedObject(baseEnemy);
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("visualTransform").objectReferenceValue = visualRig.transform;
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("playerLayer").intValue = LayerMask.GetMask("Default", "Player");
            so.FindProperty("groundLayer").intValue = LayerMask.GetMask("Default", "Ground", "Environment");
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(enemyObj, PREFAB_PATH);
            Object.DestroyImmediate(enemyObj);
        }
    }
}
#endif
