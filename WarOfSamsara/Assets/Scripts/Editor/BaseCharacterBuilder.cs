#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using WarOfSamsara.CameraControl;
using WarOfSamsara.Player;

namespace WarOfSamsara.Editor
{
    /// <summary>
    /// สคริปต์จัดการตัวละครหลัก WarOfSamsara
    /// 1. ระบบ Frame-by-Frame (FBF) ด้วย Base_Walk.png (8 เฟรม แอนิเมชันเดินลูป)
    /// 2. ระบบ Modular Base Character (โครงสร้าง SD / Chibi สไตล์ MapleStory)
    /// </summary>
    public static class BaseCharacterBuilder
    {
        private const string FBF_WALK_PATH = "Assets/Resources/Base/Player1/Base_Walk.png";
        private const string FBF_ATTACK_PATH = "Assets/Resources/Base/Player1/Base_Attack.png";
        private const string FBF_H_ATTACK_PATH = "Assets/Resources/Base/Player1/Base_H_Attack.png";
        private const string FBF_S_ATTACK_PATH = "Assets/Resources/Base/Player1/Base_S_Attack.png";
        private const string ANIM_DIR = "Assets/Animations/Player";
        private const string BASE_SHEET_PATH = "Assets/Resources/Character/Modular_Base_Character.png";
        private const string BASE_PARTS_DIR = "Assets/Resources/Character/BaseParts";
        private const string PREFAB_DIR = "Assets/Prefabs/Characters";
        private const string PREFAB_PATH = "Assets/Prefabs/Characters/BasePlayer.prefab";
        private const string FRAMES_DIR = "Assets/Resources/Base/Player1/Frames";

        #region --- 1. Frame-by-Frame (FBF) Setup ---

        [MenuItem("WarOfSamsara/Setup Base Character (Frame-by-Frame)", false, 1)]
        [MenuItem("WarOfSamsara/Setup Base_Walk (Frame-by-Frame)", false, 2)]
        public static void SetupBaseWalkCharacter()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[BaseCharacterBuilder] กรุณากดออกจาก Play Mode ก่อนรันครับ");
                return;
            }

            if (!File.Exists(FBF_WALK_PATH))
            {
                Debug.LogError($"[BaseCharacterBuilder] ไม่พบไฟล์ที่ {FBF_WALK_PATH}");
                return;
            }

            // 1. สกัดและโหลด 8 เฟรมของทุกภาพที่มี (Walk, Attack, Heavy Attack, Special Attack)
            Sprite[] walkSprites = ExtractAndLoadSheetFrames(FBF_WALK_PATH, "Walk");
            if (walkSprites == null || walkSprites.Length < 8)
            {
                Debug.LogError($"[BaseCharacterBuilder] โหลด Sprite Base_Walk ได้ไม่ครบ 8 เฟรม");
                return;
            }

            Sprite[] atkSprites = ExtractAndLoadSheetFrames(FBF_ATTACK_PATH, "Attack");
            Sprite[] hAtkSprites = ExtractAndLoadSheetFrames(FBF_H_ATTACK_PATH, "HAttack");
            Sprite[] sAtkSprites = ExtractAndLoadSheetFrames(FBF_S_ATTACK_PATH, "SAttack");

            // 2. สร้าง Animation Clips ครบทุกท่า และ Animator Controller
            AnimatorController animController = CreateAnimationsAndController(walkSprites, atkSprites, hAtkSprites, sAtkSprites);

            // 3. ประกอบร่าง Player_Base ใน Scene
            GameObject player = AssembleFbfPlayer(walkSprites[0], animController);

            // 4. เซฟ Prefab เชื่อมต่อกล้อง และเซฟฉาก
            SaveAsPrefab(player);
            ConnectToCamera(player);

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=#00FF88>[BaseCharacterBuilder]</color> เซ็ตอัปตัวละคร FBF พร้อมท่าเดินและการโจมตีครบทุกคอมโบ (Normal, Heavy, Special) สำเร็จเรียบร้อย! พร้อมกด Play ได้ทันที!");
        }

        private static Sprite[] ExtractAndLoadSheetFrames(string sheetPath, string prefix)
        {
            if (!File.Exists(sheetPath)) return null;
            if (!Directory.Exists(FRAMES_DIR)) Directory.CreateDirectory(FRAMES_DIR);

            byte[] fileData = File.ReadAllBytes(sheetPath);
            Texture2D srcTex = new Texture2D(2, 2);
            srcTex.LoadImage(fileData);

            int width = srcTex.width;
            int height = srcTex.height;
            int cols = 4;
            int rows = 2;
            int cellW = width / cols;
            int cellH = height / rows;

            Color32[] allPixels = srcTex.GetPixels32();

            // สกัด 8 เฟรม (แถวบน 0..3, แถวล่าง 4..7)
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
                            // ลบพื้นหลังสีขาวถ้ามี
                            if (p.r > 240 && p.g > 240 && p.b > 240)
                            {
                                p = new Color32(0, 0, 0, 0);
                            }
                            framePixels[py * cellW + px] = p;
                        }
                    }

                    frameTex.SetPixels32(framePixels);
                    frameTex.Apply();

                    string framePath = $"{FRAMES_DIR}/{prefix}_{index}.png";
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
                string framePath = $"{FRAMES_DIR}/{prefix}_{i}.png";
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
                    if (tis.spriteAlignment != (int)SpriteAlignment.Custom || tis.spritePivot != new Vector2(0.5f, 0.08f) || tis.spritePixelsPerUnit != 100)
                    {
                        tis.spriteAlignment = (int)SpriteAlignment.Custom;
                        tis.spritePivot = new Vector2(0.5f, 0.08f);
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
                string framePath = $"{FRAMES_DIR}/{prefix}_{i}.png";
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                if (sprites[i] == null)
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(framePath);
                    if (tex != null)
                    {
                        sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.08f), 100f);
                    }
                }
            }

            Debug.Log($"[BaseCharacterBuilder] สกัดและโหลด 8 เฟรม {prefix} เรียบร้อย!");
            return sprites;
        }

        private static AnimationClip GetOrCreateClip(string clipName, float frameRate, bool loop, Sprite[] keyframeSprites, EditorCurveBinding binding, bool forceOverwrite = false)
        {
            string clipPath = $"{ANIM_DIR}/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip != null && !forceOverwrite)
            {
                return clip;
            }

            if (clip == null)
            {
                clip = new AnimationClip { frameRate = frameRate, name = clipName };
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            else
            {
                clip.frameRate = frameRate;
            }

            if (keyframeSprites != null && keyframeSprites.Length > 0)
            {
                ObjectReferenceKeyframe[] keys;
                if (loop && keyframeSprites.Length > 1)
                {
                    keys = new ObjectReferenceKeyframe[keyframeSprites.Length + 1];
                    for (int i = 0; i < keyframeSprites.Length; i++)
                    {
                        keys[i] = new ObjectReferenceKeyframe { time = i * (1f / frameRate), value = keyframeSprites[i] };
                    }
                    keys[keyframeSprites.Length] = new ObjectReferenceKeyframe { time = keyframeSprites.Length * (1f / frameRate), value = keyframeSprites[0] };
                }
                else
                {
                    keys = new ObjectReferenceKeyframe[keyframeSprites.Length];
                    for (int i = 0; i < keyframeSprites.Length; i++)
                    {
                        keys[i] = new ObjectReferenceKeyframe { time = i * (1f / frameRate), value = keyframeSprites[i] };
                    }
                }

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateAnimationsAndController(Sprite[] walkSprites, Sprite[] atkSprites, Sprite[] hAtkSprites, Sprite[] sAtkSprites)
        {
            if (!Directory.Exists("Assets/Animations")) Directory.CreateDirectory("Assets/Animations");
            if (!Directory.Exists(ANIM_DIR)) Directory.CreateDirectory(ANIM_DIR);

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "VisualRig/BodyRenderer",
                propertyName = "m_Sprite"
            };

            // 1. สร้างคลิปแอนิเมชันครบทั้ง 13 ท่าพร้อม Track สำหรับใส่รูปภาพ
            AnimationClip idleClip   = GetOrCreateClip("Base_Idle", 10f, true, new[] { walkSprites[2] }, binding);
            AnimationClip walkClip   = GetOrCreateClip("Base_Walk", 10f, true, walkSprites, binding);
            AnimationClip jumpClip   = GetOrCreateClip("Base_Jump", 10f, false, new[] { walkSprites[1] }, binding);
            AnimationClip fallClip   = GetOrCreateClip("Base_Fall", 10f, true, new[] { walkSprites[5] }, binding);
            AnimationClip crouchClip = GetOrCreateClip("Base_Crouch", 10f, true, new[] { walkSprites[0] }, binding);
            AnimationClip climbClip  = GetOrCreateClip("Base_Climb", 8f, true, new[] { walkSprites[3], walkSprites[7] }, binding);
            AnimationClip dashClip   = GetOrCreateClip("Base_Dash", 12f, false, new[] { walkSprites[4] }, binding);

            // โจมตี Normal (8 เฟรมจาก Base_Attack.png)
            Sprite[] finalAtk1 = (atkSprites != null && atkSprites.Length >= 8) ? atkSprites : new[] { walkSprites[0], walkSprites[1] };
            AnimationClip atk1Clip   = GetOrCreateClip("Base_Attack1", 12f, false, finalAtk1, binding, forceOverwrite: (atkSprites != null));

            // โจมตี Heavy (8 เฟรมจาก Base_H_Attack.png)
            Sprite[] finalAtk2 = (hAtkSprites != null && hAtkSprites.Length >= 8) ? hAtkSprites : finalAtk1;
            AnimationClip atk2Clip   = GetOrCreateClip("Base_Attack2", 12f, false, finalAtk2, binding, forceOverwrite: (hAtkSprites != null));

            // โจมตี Special (8 เฟรมจาก Base_S_Attack.png)
            Sprite[] finalAtk3 = (sAtkSprites != null && sAtkSprites.Length >= 8) ? sAtkSprites : finalAtk2;
            AnimationClip atk3Clip   = GetOrCreateClip("Base_Attack3", 12f, false, finalAtk3, binding, forceOverwrite: (sAtkSprites != null));

            // สกิล Special Attack
            AnimationClip skillClip  = GetOrCreateClip("Base_Skill", 14f, false, finalAtk3, binding, forceOverwrite: (sAtkSprites != null));

            AnimationClip hitClip    = GetOrCreateClip("Base_Hit", 10f, false, new[] { walkSprites[5] }, binding);
            AnimationClip dieClip    = GetOrCreateClip("Base_Die", 8f, false, new[] { walkSprites[0], walkSprites[1] }, binding);

            // 2. สร้าง Animator Controller และ Parameters ครบทุกสถานะ
            string controllerPath = $"{ANIM_DIR}/BasePlayer_FBF.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsClimbing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Skill", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            // วาง State ตำแหน่งชัดเจนใน Graph
            AnimatorState idleState   = sm.AddState("Idle", new Vector3(250, 0, 0));
            idleState.motion = idleClip;

            AnimatorState walkState   = sm.AddState("Walk", new Vector3(250, 100, 0));
            walkState.motion = walkClip;

            AnimatorState jumpState   = sm.AddState("Jump", new Vector3(500, -80, 0));
            jumpState.motion = jumpClip;

            AnimatorState fallState   = sm.AddState("Fall", new Vector3(500, 80, 0));
            fallState.motion = fallClip;

            AnimatorState crouchState = sm.AddState("Crouch", new Vector3(50, 100, 0));
            crouchState.motion = crouchClip;

            AnimatorState climbState  = sm.AddState("Climb", new Vector3(50, -80, 0));
            climbState.motion = climbClip;

            AnimatorState dashState   = sm.AddState("Dash", new Vector3(250, 200, 0));
            dashState.motion = dashClip;

            AnimatorState atk1State   = sm.AddState("Attack1", new Vector3(500, 200, 0));
            atk1State.motion = atk1Clip;

            AnimatorState atk2State   = sm.AddState("Attack2", new Vector3(680, 200, 0));
            atk2State.motion = atk2Clip;

            AnimatorState atk3State   = sm.AddState("Attack3", new Vector3(860, 200, 0));
            atk3State.motion = atk3Clip;

            AnimatorState skillState  = sm.AddState("Skill", new Vector3(500, 300, 0));
            skillState.motion = skillClip;

            AnimatorState hitState    = sm.AddState("Hit", new Vector3(-150, 0, 0));
            hitState.motion = hitClip;

            AnimatorState dieState    = sm.AddState("Die", new Vector3(-150, 100, 0));
            dieState.motion = dieClip;

            sm.defaultState = idleState;

            // Helper สร้าง Transition ทันที (ไร้รอยต่อ 0s blend)
            AnimatorStateTransition Trans(AnimatorState from, AnimatorState to, bool hasExitTime = false, float exitTime = 1f, float duration = 0f)
            {
                var t = from.AddTransition(to);
                t.hasExitTime = hasExitTime;
                if (hasExitTime) t.exitTime = exitTime;
                t.duration = duration;
                return t;
            }

            // --- Idle <-> Walk ---
            var toWalk = Trans(idleState, walkState);
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            var toIdle = Trans(walkState, idleState);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            // --- Air (Jump & Fall) ---
            var idleToJump = Trans(idleState, jumpState);
            idleToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            idleToJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");

            var walkToJump = Trans(walkState, jumpState);
            walkToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            walkToJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");

            var idleToFall = Trans(idleState, fallState);
            idleToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            idleToFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

            var walkToFall = Trans(walkState, fallState);
            walkToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            walkToFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

            var jumpToFall = Trans(jumpState, fallState);
            jumpToFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

            // Jump / Fall Landing
            var jumpToIdle = Trans(jumpState, idleState);
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            var jumpToWalk = Trans(jumpState, walkState);
            jumpToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            jumpToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

            var fallToIdle = Trans(fallState, idleState);
            fallToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            fallToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            var fallToWalk = Trans(fallState, walkState);
            fallToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            fallToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

            // --- Crouch ---
            var idleToCrouch = Trans(idleState, crouchState);
            idleToCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");
            idleToCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            var walkToCrouch = Trans(walkState, crouchState);
            walkToCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");

            var crouchToIdle = Trans(crouchState, idleState);
            crouchToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");

            // --- Climb (Ladder / Rope) ---
            var idleToClimb = Trans(idleState, climbState);
            idleToClimb.AddCondition(AnimatorConditionMode.If, 0, "IsClimbing");

            var walkToClimb = Trans(walkState, climbState);
            walkToClimb.AddCondition(AnimatorConditionMode.If, 0, "IsClimbing");

            var jumpToClimb = Trans(jumpState, climbState);
            jumpToClimb.AddCondition(AnimatorConditionMode.If, 0, "IsClimbing");

            var fallToClimb = Trans(fallState, climbState);
            fallToClimb.AddCondition(AnimatorConditionMode.If, 0, "IsClimbing");

            var climbToIdle = Trans(climbState, idleState);
            climbToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing");
            climbToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            var climbToFall = Trans(climbState, fallState);
            climbToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing");
            climbToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // --- Dash ---
            var idleToDash = Trans(idleState, dashState);
            idleToDash.AddCondition(AnimatorConditionMode.If, 0, "IsDashing");

            var walkToDash = Trans(walkState, dashState);
            walkToDash.AddCondition(AnimatorConditionMode.If, 0, "IsDashing");

            var dashToIdle = Trans(dashState, idleState);
            dashToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");

            // --- Attack Combo 1 -> 2 -> 3 ---
            var idleToAtk1 = Trans(idleState, atk1State);
            idleToAtk1.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            var walkToAtk1 = Trans(walkState, atk1State);
            walkToAtk1.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            Trans(atk1State, idleState, hasExitTime: true, exitTime: 1f, duration: 0f);

            var atk1ToAtk2 = Trans(atk1State, atk2State);
            atk1ToAtk2.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            Trans(atk2State, idleState, hasExitTime: true, exitTime: 1f, duration: 0f);

            var atk2ToAtk3 = Trans(atk2State, atk3State);
            atk2ToAtk3.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            Trans(atk3State, idleState, hasExitTime: true, exitTime: 1f, duration: 0f);

            // --- Skill ---
            var anyToSkill = sm.AddAnyStateTransition(skillState);
            anyToSkill.hasExitTime = false;
            anyToSkill.duration = 0f;
            anyToSkill.AddCondition(AnimatorConditionMode.If, 0, "Skill");

            Trans(skillState, idleState, hasExitTime: true, exitTime: 1f, duration: 0f);

            // --- Hit & Die (AnyState) ---
            var anyToHit = sm.AddAnyStateTransition(hitState);
            anyToHit.hasExitTime = false;
            anyToHit.duration = 0f;
            anyToHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");

            Trans(hitState, idleState, hasExitTime: true, exitTime: 1f, duration: 0f);

            var anyToDie = sm.AddAnyStateTransition(dieState);
            anyToDie.hasExitTime = false;
            anyToDie.duration = 0f;
            anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

            AssetDatabase.SaveAssets();
            return controller;
        }

        private static GameObject AssembleFbfPlayer(Sprite defaultSprite, AnimatorController controller)
        {
            GameObject old1 = GameObject.Find("Player_Fighter (FGT001)");
            if (old1 != null) Undo.DestroyObjectImmediate(old1);
            GameObject old2 = GameObject.Find("Player_Modular");
            if (old2 != null) Undo.DestroyObjectImmediate(old2);
            GameObject old3 = GameObject.Find("Player_Base");
            if (old3 != null) Undo.DestroyObjectImmediate(old3);

            GameObject entitiesRoot = GameObject.Find("--- ENTITIES (Dynamic) ---");
            Transform parentTrans = (entitiesRoot != null) ? entitiesRoot.transform : null;

            GameObject root = new GameObject("Player_Base");
            if (parentTrans != null) root.transform.SetParent(parentTrans, false);

            root.transform.position = new Vector3(-8f, 1.25f, 0f);
            root.transform.localScale = Vector3.one;

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3.5f;

            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.8f);
            col.offset = new Vector2(0f, 0.9f);

            PlayerMovement2D movement = root.AddComponent<PlayerMovement2D>();
            root.AddComponent<PlayerInputHandler>();

            SerializedObject so = new SerializedObject(movement);
            so.FindProperty("normalColliderSize").vector2Value = new Vector2(1.2f, 1.8f);
            so.FindProperty("normalColliderOffset").vector2Value = new Vector2(0f, 0.9f);
            so.FindProperty("crouchColliderSize").vector2Value = new Vector2(1.2f, 1.1f);
            so.FindProperty("crouchColliderOffset").vector2Value = new Vector2(0f, 0.55f);
            so.ApplyModifiedProperties();

            GameObject visualRig = new GameObject("VisualRig");
            visualRig.transform.SetParent(root.transform, false);
            visualRig.transform.localPosition = Vector3.zero;
            visualRig.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            GameObject bodyRendererObj = new GameObject("BodyRenderer");
            bodyRendererObj.transform.SetParent(visualRig.transform, false);
            bodyRendererObj.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = bodyRendererObj.AddComponent<SpriteRenderer>();
            sr.sprite = defaultSprite;
            sr.sortingOrder = 5;
            sr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            PlayerVisual2D visual = root.AddComponent<PlayerVisual2D>();
            visual.SetupComponents(animator, sr, visualRig.transform);

            Undo.RegisterCreatedObjectUndo(root, "Create FBF Player");
            return root;
        }

        #endregion

        #region --- 2. Modular Base Character (SD / Chibi Maple Style) Setup ---

        [MenuItem("WarOfSamsara/Build Modular Base Character (Maple Style)", false, 11)]
        [MenuItem("WarOfSamsara/Rebuild Player_Base with Sprites", false, 12)]
        public static void BuildBaseCharacter()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[BaseCharacterBuilder] กรุณากดออกจาก Play Mode ก่อนรันการสร้างตัวละครครับ");
                return;
            }

            ExtractBaseParts();

            GameObject playerObj = AssembleBaseCharacter();
            if (playerObj == null)
            {
                Debug.LogError("[BaseCharacterBuilder] เกิดข้อผิดพลาดในการประกอบร่างตัวละคร");
                return;
            }

            SaveAsPrefab(playerObj);
            ConnectToCamera(playerObj);

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=#00FF88>[BaseCharacterBuilder]</color> ประกอบร่าง Modular_Base_Character (SD Maple Style) สำเร็จเรียบร้อย! พร้อมเล่นได้ทันที!");
        }

        private static void ExtractBaseParts()
        {
            if (!Directory.Exists(BASE_PARTS_DIR))
            {
                Directory.CreateDirectory(BASE_PARTS_DIR);
            }

            string[] existingFiles = Directory.GetFiles(BASE_PARTS_DIR, "*.png");
            if (existingFiles.Length < 50)
            {
                TextureImporter importer = AssetImporter.GetAtPath(BASE_SHEET_PATH) as TextureImporter;
                if (importer != null)
                {
                    bool needReimport = false;
                    if (!importer.isReadable) { importer.isReadable = true; needReimport = true; }
                    if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; needReimport = true; }
                    if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; needReimport = true; }
                    if (needReimport) importer.SaveAndReimport();
                }

                Texture2D srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BASE_SHEET_PATH);
                if (srcTex == null)
                {
                    Debug.LogError($"[BaseCharacterBuilder] ไม่พบไฟล์ที่ {BASE_SHEET_PATH}");
                    return;
                }

                int width = srcTex.width;
                int height = srcTex.height;
                Color32[] pixels = srcTex.GetPixels32();

                List<Rect> islands = FindIslands(pixels, width, height, minPixels: 80);
                Debug.Log($"[BaseCharacterBuilder] ตรวจพบชิ้นส่วน Base Body ทั้งหมด {islands.Count} ชิ้น!");

                List<Rect> rowHeads = new List<Rect>();
                List<Rect> rowBodies = new List<Rect>();
                List<Rect> rowArms = new List<Rect>();
                List<Rect> rowLegs = new List<Rect>();
                List<Rect> rowFeet = new List<Rect>();

                foreach (var r in islands)
                {
                    float normY = r.center.y / height;
                    if (normY > 0.74f) rowHeads.Add(r);
                    else if (normY > 0.50f) rowBodies.Add(r);
                    else if (normY > 0.32f) rowArms.Add(r);
                    else if (normY > 0.12f) rowLegs.Add(r);
                    else rowFeet.Add(r);
                }

                rowHeads.Sort((a, b) => a.x.CompareTo(b.x));
                rowBodies.Sort((a, b) => a.x.CompareTo(b.x));
                rowArms.Sort((a, b) => a.x.CompareTo(b.x));
                rowLegs.Sort((a, b) => a.x.CompareTo(b.x));
                rowFeet.Sort((a, b) => a.x.CompareTo(b.x));

                SaveRowParts(rowHeads, "Head", pixels, width, height);
                SaveRowParts(rowBodies, "Body", pixels, width, height);
                SaveRowParts(rowArms, "Arm", pixels, width, height);
                SaveRowParts(rowLegs, "Leg", pixels, width, height);
                SaveRowParts(rowFeet, "Foot", pixels, width, height);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            string[] files = Directory.GetFiles(BASE_PARTS_DIR, "*.png");
            foreach (var f in files)
            {
                string assetPath = f.Replace('\\', '/');
                TextureImporter partImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (partImporter != null)
                {
                    bool dirty = false;
                    if (partImporter.textureType != TextureImporterType.Sprite) { partImporter.textureType = TextureImporterType.Sprite; dirty = true; }
                    if (partImporter.spriteImportMode != SpriteImportMode.Single) { partImporter.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                    if (!partImporter.alphaIsTransparency) { partImporter.alphaIsTransparency = true; dirty = true; }
                    if (partImporter.spritePixelsPerUnit != 100) { partImporter.spritePixelsPerUnit = 100; dirty = true; }
                    if (partImporter.filterMode != FilterMode.Point) { partImporter.filterMode = FilterMode.Point; dirty = true; }
                    if (dirty) partImporter.SaveAndReimport();
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void SaveRowParts(List<Rect> rects, string prefix, Color32[] pixels, int width, int height)
        {
            for (int i = 0; i < rects.Count; i++)
            {
                Rect r = rects[i];
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

                string outPath = $"{BASE_PARTS_DIR}/{prefix}_{i + 1:D2}.png";
                File.WriteAllBytes(outPath, pngData);
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

        private static Sprite LoadBasePart(string partName)
        {
            string path = $"{BASE_PARTS_DIR}/{partName}.png";
            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr == null)
            {
                spr = Resources.Load<Sprite>($"Character/BaseParts/{partName}");
            }
            if (spr == null)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    spr.name = partName;
                }
            }
            return spr;
        }

        private static GameObject AssembleBaseCharacter()
        {
            GameObject oldPlayer1 = GameObject.Find("Player_Fighter (FGT001)");
            if (oldPlayer1 != null) Undo.DestroyObjectImmediate(oldPlayer1);

            GameObject oldPlayer2 = GameObject.Find("Player_Modular");
            if (oldPlayer2 != null) Undo.DestroyObjectImmediate(oldPlayer2);

            GameObject oldPlayer3 = GameObject.Find("Player_Base");
            if (oldPlayer3 != null) Undo.DestroyObjectImmediate(oldPlayer3);

            GameObject entitiesRoot = GameObject.Find("--- ENTITIES (Dynamic) ---");
            Transform parentTrans = (entitiesRoot != null) ? entitiesRoot.transform : null;

            GameObject root = new GameObject("Player_Base");
            if (parentTrans != null) root.transform.SetParent(parentTrans, false);

            root.transform.position = new Vector3(-8f, 3.05f, 0f);
            root.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3.5f;

            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 2.6f);
            col.offset = new Vector2(0f, 0f);

            PlayerMovement2D movement = root.AddComponent<PlayerMovement2D>();
            root.AddComponent<PlayerInputHandler>();

            GameObject visualRig = new GameObject("VisualRig");
            visualRig.transform.SetParent(root.transform, false);

            Material unlitMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            GameObject bonePelvis = CreateBone(visualRig, "Bone_Pelvis", new Vector3(0f, 0f, 0f));
            CreatePart(bonePelvis, "Body", LoadBasePart("Body_01"), Vector3.zero, 5, unlitMat);

            GameObject boneHead = CreateBone(bonePelvis, "Bone_Head", new Vector3(0f, 0.75f, 0f));
            CreatePart(boneHead, "Head", LoadBasePart("Head_01"), Vector3.zero, 6, unlitMat);

            string hairPath = "Assets/Resources/Character/Parts/Hair_Front.png";
            string eyePath = "Assets/Resources/Character/Parts/Eyes_Battle.png";
            Sprite hairSprite = AssetDatabase.LoadAssetAtPath<Sprite>(hairPath);
            Sprite eyeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(eyePath);

            if (eyeSprite != null)
            {
                CreatePart(boneHead, "Eyes", eyeSprite, new Vector3(-0.1f, -0.05f, 0f), 7, unlitMat);
            }
            if (hairSprite != null)
            {
                CreatePart(boneHead, "Hair", hairSprite, new Vector3(0.05f, 0.25f, 0f), 8, unlitMat);
            }

            GameObject boneArmL = CreateBone(bonePelvis, "Bone_Arm_L", new Vector3(0.35f, 0.05f, 0f));
            CreatePart(boneArmL, "Arm_L", LoadBasePart("Arm_01"), Vector3.zero, 8, unlitMat);

            GameObject boneArmR = CreateBone(bonePelvis, "Bone_Arm_R", new Vector3(-0.35f, 0.05f, 0f));
            CreatePart(boneArmR, "Arm_R", LoadBasePart("Arm_02"), Vector3.zero, 3, unlitMat);

            GameObject boneLegL = CreateBone(bonePelvis, "Bone_Leg_L", new Vector3(0.20f, -0.65f, 0f));
            CreatePart(boneLegL, "Leg_L", LoadBasePart("Leg_01"), Vector3.zero, 7, unlitMat);

            GameObject boneFootL = CreateBone(boneLegL, "Bone_Foot_L", new Vector3(0f, -0.40f, 0f));
            CreatePart(boneFootL, "Foot_L", LoadBasePart("Foot_01"), Vector3.zero, 7, unlitMat);

            GameObject boneLegR = CreateBone(bonePelvis, "Bone_Leg_R", new Vector3(-0.25f, -0.65f, 0f));
            CreatePart(boneLegR, "Leg_R", LoadBasePart("Leg_02"), Vector3.zero, 3, unlitMat);

            GameObject boneFootR = CreateBone(boneLegR, "Bone_Foot_R", new Vector3(0f, -0.40f, 0f));
            CreatePart(boneFootR, "Foot_R", LoadBasePart("Foot_02"), Vector3.zero, 3, unlitMat);

            SerializedObject so = new SerializedObject(movement);
            so.FindProperty("crouchColliderSize").vector2Value = new Vector2(1.5f, 1.6f);
            so.FindProperty("crouchColliderOffset").vector2Value = new Vector2(0f, -0.5f);
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(root, "Create Base Player");
            return root;
        }

        private static GameObject CreateBone(GameObject parent, string name, Vector3 localPos)
        {
            GameObject boneObj = new GameObject(name);
            boneObj.transform.SetParent(parent.transform, false);
            boneObj.transform.localPosition = localPos;
            boneObj.transform.localRotation = Quaternion.identity;
            boneObj.transform.localScale = Vector3.one;

            boneObj.AddComponent<Bone2D>();
            return boneObj;
        }

        private static GameObject CreatePart(GameObject parent, string name, Sprite sprite, Vector3 localPos, int sortingOrder, Material mat)
        {
            GameObject partObj = new GameObject(name);
            partObj.transform.SetParent(parent.transform, false);
            partObj.transform.localPosition = localPos;
            partObj.transform.localScale = Vector3.one;

            SpriteRenderer sr = partObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            if (mat != null) sr.material = mat;
            EditorUtility.SetDirty(sr);

            return partObj;
        }

        #endregion

        #region --- 3. Shared Helpers ---

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

        #endregion
    }
}
#endif
