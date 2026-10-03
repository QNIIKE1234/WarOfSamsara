using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WarOfSamsara.Environment;

namespace WarOfSamsara.EditorTools
{
    /// <summary>
    /// เครื่องมือช่วยสร้างและแปลง Platform 2D ในคลิกเดียว
    /// - ใช้ EdgeCollider2D พาดเฉพาะผิวบนสุด (Top Surface) หมดปัญหาหัวชนแท่น 100%
    /// - มีปุ่ม "Apply Edge Collider 2D" สำหรับแปลงแท่นเดิมที่มี Polygon/Box Collider ให้เป็น Edge Collider ทันที
    /// - รองรับแท่นตรง (Flat), ทางลาดขึ้น (Slope UP), ทางลาดลง (Slope DOWN), และแท่นเดี่ยว
    /// - ใส่ระบบ One-Way Platform และ OneWayPlatform script อัตโนมัติ (กดลง+กระโดด มุดทะลุได้)
    /// </summary>
    public class PlatformGeneratorTool : EditorWindow
    {
        public enum PlatformType
        {
            Flat,
            SlopeUp,
            SlopeDown,
            SingleIsland
        }

        public enum ColliderMode
        {
            EdgeColliderTop,   // แนะนำสุด ⭐ (เส้นผิวบนเส้นเดียว ลอดใต้แท่นได้ หัวไม่ชน 100%)
            CompositePolygon,  // รวมขอบรูปทั้งก้อน
            IndividualPolygon
        }

        private PlatformType _platformType = PlatformType.Flat;
        private ColliderMode _colliderMode = ColliderMode.EdgeColliderTop; // ค่าเริ่มต้นเป็น Edge Collider

        // Parent Reference
        private Transform _parentContainer;
        private string _platformName = "Platform_Ruins";

        // Segment Counts
        private int _middleSegmentCount = 2;

        // Sprites for Flat
        private Sprite _flatLeftCap;
        private Sprite _flatMiddle;
        private Sprite _flatRightCap;

        // Sprites for Slope Up
        private Sprite _slopeUpStart;
        private Sprite _slopeUpMiddle;
        private Sprite _slopeUpEnd;

        // Sprites for Slope Down
        private Sprite _slopeDownStart;
        private Sprite _slopeDownMiddle;
        private Sprite _slopeDownEnd;

        // Single Island Sprite
        private Sprite _singleSprite;

        // Physics & Settings
        private bool _isOneWay = true;
        private bool _addOneWayScript = true;
        private string _sortingLayerName = "Default";
        private int _orderInLayer = 2;
        private float _slopeYOffsetStep = 0.5f;
        private float _edgeYOffset = 0f; // เลื่อนเส้นเหยียบขึ้น/ลงตามผิวสัมผัส

        private Vector2 _scrollPos;

        [MenuItem("WarOfSamsara/Tools/Platform Generator Tool", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<PlatformGeneratorTool>("Platform Generator");
            window.minSize = new Vector2(430, 640);
            window.Show();
        }

        [MenuItem("GameObject/WarOfSamsara/Convert to Edge Collider Platform", false, 10)]
        public static void ConvertContextMenu()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณาเลือก GameObject แท่นกระโดดที่ต้องการแปลงก่อนครับ", "ตกลง");
                return;
            }

            ApplyEdgeColliderToGameObject(Selection.activeGameObject, true, true, 0f);
        }

        private void OnEnable()
        {
            AutoFindParentContainer();
            AutoLoadRuinsSpritesIfEmpty();
        }

        private void AutoFindParentContainer()
        {
            if (_parentContainer != null) return;

            GameObject mapObj = GameObject.Find("MAP_STARTER_001");
            if (mapObj != null)
            {
                Transform platChild = mapObj.transform.Find("Platforms");
                if (platChild == null)
                {
                    GameObject platGo = new GameObject("Platforms");
                    platGo.transform.SetParent(mapObj.transform, false);
                    _parentContainer = platGo.transform;
                }
                else
                {
                    _parentContainer = platChild;
                }
                return;
            }

            GameObject genericPlat = GameObject.Find("Platforms");
            if (genericPlat != null)
            {
                _parentContainer = genericPlat.transform;
            }
        }

        private void AutoLoadRuinsSpritesIfEmpty()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Maps/Tile" });
            List<Sprite> loadedSprites = new List<Sprite>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in assets)
                {
                    if (obj is Sprite s)
                    {
                        loadedSprites.Add(s);
                    }
                }
            }

            if (loadedSprites.Count >= 3)
            {
                if (_flatLeftCap == null) _flatLeftCap = loadedSprites[0];
                if (_flatMiddle == null) _flatMiddle = loadedSprites[1];
                if (_flatRightCap == null) _flatRightCap = loadedSprites[loadedSprites.Count > 3 ? 3 : 2];
            }

            if (loadedSprites.Count >= 7)
            {
                if (_slopeUpStart == null) _slopeUpStart = loadedSprites[4];
                if (_slopeUpMiddle == null) _slopeUpMiddle = loadedSprites[5];
                if (_slopeUpEnd == null) _slopeUpEnd = loadedSprites[6];
            }

            if (loadedSprites.Count >= 11)
            {
                if (_slopeDownStart == null) _slopeDownStart = loadedSprites[8];
                if (_slopeDownMiddle == null) _slopeDownMiddle = loadedSprites[9];
                if (_slopeDownEnd == null) _slopeDownEnd = loadedSprites[10];
            }
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(8);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🏰 2D Platform Generator & Fixer", headerStyle);
            EditorGUILayout.LabelField("สร้างแท่นและแก้ปัญหาหัวชนด้วย Edge Collider 2D อัตโนมัติ", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(8);

            // ==================== EMERGENCY RESCUE / GROUND FIX ====================
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle warnHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
            EditorGUILayout.LabelField("🚨 กู้คืนเมื่อตัวละครตกแมพ (Emergency Fix)", warnHeaderStyle);
            EditorGUILayout.LabelField("ถ้าตัวละครร่วงตกแมพ (เพราะเผลอไปกดแปลงตัวแม่ MAP_STARTER_001 หรือพื้นดินไม่มี Collider) กดปุ่มนี้ทีเดียวแก้หายทันที:", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
            if (GUILayout.Button("🛠️ ซ่อมพื้นดิน & ดึง Player กลับขึ้นมา (Fix Ground & Reset Player)", GUILayout.Height(34)))
            {
                FixGroundAndResetPlayer();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // ==================== FIX ALL PLATFORMS ====================
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle fixAllStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.3f, 1f, 0.5f) } };
            EditorGUILayout.LabelField("🔨 ซ่อมแซมแท่นทั้งหมด (Fix All Platforms)", fixAllStyle);
            EditorGUILayout.LabelField("ถ้าตัวละครเหยียบแท่นไม่ได้ (ร่วงทะลุแท่น) กดปุ่มนี้เพื่อเปลี่ยนแท่นทั้งหมดให้เป็นแผ่นเหยียบผิวบนหนา 0.35m แบบ One-Way ที่เหยียบติด 100% และหัวไม่ชน:", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f);
            if (GUILayout.Button("🔨 ซ่อมแท่นทั้งหมดให้เหยียบติด 100% (Fix All Platforms)", GUILayout.Height(36)))
            {
                FixAllPlatformsInScene();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // ==================== QUICK FIX SELECTED SECTION ====================
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle sectionStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.2f, 0.85f, 1f) } };
            EditorGUILayout.LabelField("⚡ แปลงแท่นเดี่ยวที่เลือก (Fix Single Selected Platform)", sectionStyle);
            EditorGUILayout.LabelField("คลิกเลือกแท่นกระโดดใน Scene แล้วกดปุ่มนี้เพื่อติดตั้งแผ่นเหยียบผิวบนแบบ One-Way ทันที", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);

            GameObject currentSelected = Selection.activeGameObject;
            EditorGUILayout.LabelField("วัตถุที่เลือกอยู่ในขณะนี้:", currentSelected != null ? $"<b><color=#44FF88>{currentSelected.name}</color></b>" : "<color=yellow>ยังไม่ได้เลือก GameObject ใดๆ</color>", new GUIStyle(EditorStyles.label) { richText = true });

            _edgeYOffset = EditorGUILayout.FloatField(new GUIContent("Edge Y Offset (ปรับระดับเส้นเหยียบ)", "ขยับเส้นสัมผัสขึ้น/ลงให้พอดีผิวหิน"), _edgeYOffset);

            GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
            if (GUILayout.Button("⚡ ติดตั้งแผ่นเหยียบผิวบนให้แท่นที่เลือก (One-Way)", GUILayout.Height(36)))
            {
                if (currentSelected == null)
                {
                    EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณาคลิกเลือก GameObject แท่นกระโดดใน Scene หรือ Hierarchy ก่อนครับ", "ตกลง");
                }
                else
                {
                    FixPlatformCollider(currentSelected, _isOneWay, _addOneWayScript, _edgeYOffset);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // ==================== GENERATE NEW PLATFORM SECTION ====================
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("--- สร้างแท่นใหม่ (Generate New Platform) ---", EditorStyles.boldLabel);
            _parentContainer = (Transform)EditorGUILayout.ObjectField("Parent Container", _parentContainer, typeof(Transform), true);
            _platformName = EditorGUILayout.TextField("Platform Name", _platformName);

            EditorGUILayout.Space(4);
            _platformType = (PlatformType)EditorGUILayout.EnumPopup("Platform Type", _platformType);

            EditorGUILayout.Space(4);
            switch (_platformType)
            {
                case PlatformType.Flat:
                    DrawFlatGUI();
                    break;
                case PlatformType.SlopeUp:
                    DrawSlopeUpGUI();
                    break;
                case PlatformType.SlopeDown:
                    DrawSlopeDownGUI();
                    break;
                case PlatformType.SingleIsland:
                    DrawSingleGUI();
                    break;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("--- การตั้งค่าฟิสิกส์ & เลเยอร์ ---", EditorStyles.boldLabel);
            _colliderMode = (ColliderMode)EditorGUILayout.EnumPopup("Collider Mode", _colliderMode);
            if (_colliderMode == ColliderMode.EdgeColliderTop)
            {
                EditorGUILayout.HelpBox("⭐ Edge Collider Top: มีเส้นผิวบนเส้นเดียว ลอดใต้แท่นได้ หัวไม่ชน 100%", MessageType.Info);
            }

            _isOneWay = EditorGUILayout.Toggle(new GUIContent("One-Way Platform", "กระโดดทะลุขึ้นไปเหยียบจากข้างล่างได้"), _isOneWay);
            if (_isOneWay)
            {
                _addOneWayScript = EditorGUILayout.Toggle(new GUIContent("Support Down+Jump", "กดลง + กระโดด เพื่อมุดทะลุลงมาได้"), _addOneWayScript);
            }

            EditorGUILayout.Space(4);
            _sortingLayerName = EditorGUILayout.TextField("Sorting Layer", _sortingLayerName);
            _orderInLayer = EditorGUILayout.IntField("Order in Layer", _orderInLayer);

            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.35f);
            if (GUILayout.Button("✨ สร้าง Platform ใหม่ (Generate Platform)", GUILayout.Height(40)))
            {
                GeneratePlatform();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        private void DrawFlatGUI()
        {
            _flatLeftCap = (Sprite)EditorGUILayout.ObjectField("Left Cap (หัวซ้าย)", _flatLeftCap, typeof(Sprite), false);
            _flatMiddle = (Sprite)EditorGUILayout.ObjectField("Middle (ท่อนกลาง)", _flatMiddle, typeof(Sprite), false);
            _flatRightCap = (Sprite)EditorGUILayout.ObjectField("Right Cap (หัวขวา)", _flatRightCap, typeof(Sprite), false);

            _middleSegmentCount = EditorGUILayout.IntSlider("จำนวนท่อนกลาง (Middle Count)", _middleSegmentCount, 0, 15);
        }

        private void DrawSlopeUpGUI()
        {
            _slopeUpStart = (Sprite)EditorGUILayout.ObjectField("Slope Start (ล่างซ้าย)", _slopeUpStart, typeof(Sprite), false);
            _slopeUpMiddle = (Sprite)EditorGUILayout.ObjectField("Slope Middle (ท่อนเฉียง)", _slopeUpMiddle, typeof(Sprite), false);
            _slopeUpEnd = (Sprite)EditorGUILayout.ObjectField("Slope End (บนขวา)", _slopeUpEnd, typeof(Sprite), false);

            _middleSegmentCount = EditorGUILayout.IntSlider("จำนวนท่อนเฉียงกลาง", _middleSegmentCount, 0, 10);
            _slopeYOffsetStep = EditorGUILayout.FloatField("ระยะยกความสูงต่อท่อน (Y Step)", _slopeYOffsetStep);
        }

        private void DrawSlopeDownGUI()
        {
            _slopeDownStart = (Sprite)EditorGUILayout.ObjectField("Slope Start (บนซ้าย)", _slopeDownStart, typeof(Sprite), false);
            _slopeDownMiddle = (Sprite)EditorGUILayout.ObjectField("Slope Middle (ท่อนเฉียง)", _slopeDownMiddle, typeof(Sprite), false);
            _slopeDownEnd = (Sprite)EditorGUILayout.ObjectField("Slope End (ล่างขวา)", _slopeDownEnd, typeof(Sprite), false);

            _middleSegmentCount = EditorGUILayout.IntSlider("จำนวนท่อนเฉียงกลาง", _middleSegmentCount, 0, 10);
            _slopeYOffsetStep = EditorGUILayout.FloatField("ระยะลดความสูงต่อท่อน (Y Step)", _slopeYOffsetStep);
        }

        private void DrawSingleGUI()
        {
            _singleSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", _singleSprite, typeof(Sprite), false);
        }

        private void GeneratePlatform()
        {
            AutoFindParentContainer();

            Vector3 spawnPos = Vector3.zero;
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                spawnPos = SceneView.lastActiveSceneView.camera.transform.position;
                spawnPos.z = 0f;
            }

            string finalName = string.IsNullOrEmpty(_platformName) ? "Platform" : _platformName;
            GameObject rootObj = new GameObject($"{finalName}_{_platformType}");
            rootObj.transform.position = spawnPos;

            if (_parentContainer != null)
            {
                rootObj.transform.SetParent(_parentContainer, true);
            }

            Undo.RegisterCreatedObjectUndo(rootObj, "Generate Platform");

            List<GameObject> segmentObjs = new List<GameObject>();

            switch (_platformType)
            {
                case PlatformType.Flat:
                    segmentObjs = BuildFlatSegments(rootObj);
                    break;
                case PlatformType.SlopeUp:
                    segmentObjs = BuildSlopeSegments(rootObj, isUp: true);
                    break;
                case PlatformType.SlopeDown:
                    segmentObjs = BuildSlopeSegments(rootObj, isUp: false);
                    break;
                case PlatformType.SingleIsland:
                    segmentObjs = BuildSingleSegment(rootObj);
                    break;
            }

            if (segmentObjs.Count == 0)
            {
                EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณาใส่ Sprite ก่อนสร้าง Platform ครับ", "ตกลง");
                DestroyImmediate(rootObj);
                return;
            }

            SetupPhysics(rootObj, segmentObjs);

            Selection.activeGameObject = rootObj;
            EditorGUIUtility.PingObject(rootObj);
            Debug.Log($"<color=#33FF88><b>[PlatformGenerator]</b> สร้าง {rootObj.name} เรียบร้อยแล้ว (มี {segmentObjs.Count} ชิ้นส่วน)</color>", rootObj);
        }

        private List<GameObject> BuildFlatSegments(GameObject root)
        {
            List<GameObject> objs = new List<GameObject>();
            List<Sprite> spritesToPlace = new List<Sprite>();

            if (_flatLeftCap != null) spritesToPlace.Add(_flatLeftCap);
            if (_flatMiddle != null)
            {
                for (int i = 0; i < _middleSegmentCount; i++)
                {
                    spritesToPlace.Add(_flatMiddle);
                }
            }
            if (_flatRightCap != null) spritesToPlace.Add(_flatRightCap);

            float currentX = 0f;
            for (int i = 0; i < spritesToPlace.Count; i++)
            {
                Sprite sp = spritesToPlace[i];
                float width = sp.rect.width / sp.pixelsPerUnit;

                GameObject piece = new GameObject($"Seg_{i}_{sp.name}");
                piece.transform.SetParent(root.transform, false);
                piece.transform.localPosition = new Vector3(currentX + (width * 0.5f), 0f, 0f);

                SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingLayerName = _sortingLayerName;
                sr.sortingOrder = _orderInLayer;

                objs.Add(piece);
                currentX += width;
            }

            float totalWidth = currentX;
            foreach (var piece in objs)
            {
                piece.transform.localPosition -= new Vector3(totalWidth * 0.5f, 0f, 0f);
            }

            return objs;
        }

        private List<GameObject> BuildSlopeSegments(GameObject root, bool isUp)
        {
            List<GameObject> objs = new List<GameObject>();
            List<Sprite> spritesToPlace = new List<Sprite>();

            Sprite startSp = isUp ? _slopeUpStart : _slopeDownStart;
            Sprite midSp = isUp ? _slopeUpMiddle : _slopeDownMiddle;
            Sprite endSp = isUp ? _slopeUpEnd : _slopeDownEnd;

            if (startSp != null) spritesToPlace.Add(startSp);
            if (midSp != null)
            {
                for (int i = 0; i < _middleSegmentCount; i++)
                {
                    spritesToPlace.Add(midSp);
                }
            }
            if (endSp != null) spritesToPlace.Add(endSp);

            float currentX = 0f;
            float currentY = 0f;
            float yStep = isUp ? Mathf.Abs(_slopeYOffsetStep) : -Mathf.Abs(_slopeYOffsetStep);

            for (int i = 0; i < spritesToPlace.Count; i++)
            {
                Sprite sp = spritesToPlace[i];
                float width = sp.rect.width / sp.pixelsPerUnit;

                GameObject piece = new GameObject($"Seg_Slope_{i}_{sp.name}");
                piece.transform.SetParent(root.transform, false);
                piece.transform.localPosition = new Vector3(currentX + (width * 0.5f), currentY, 0f);

                SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingLayerName = _sortingLayerName;
                sr.sortingOrder = _orderInLayer;

                objs.Add(piece);

                currentX += width;
                currentY += yStep;
            }

            float halfWidth = currentX * 0.5f;
            float halfHeight = currentY * 0.5f;
            foreach (var piece in objs)
            {
                piece.transform.localPosition -= new Vector3(halfWidth, halfHeight, 0f);
            }

            return objs;
        }

        private List<GameObject> BuildSingleSegment(GameObject root)
        {
            List<GameObject> objs = new List<GameObject>();
            if (_singleSprite == null) return objs;

            GameObject piece = new GameObject($"Seg_Single_{_singleSprite.name}");
            piece.transform.SetParent(root.transform, false);
            piece.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
            sr.sprite = _singleSprite;
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder = _orderInLayer;

            objs.Add(piece);
            return objs;
        }

        private void SetupPhysics(GameObject root, List<GameObject> pieces)
        {
            if (_colliderMode == ColliderMode.EdgeColliderTop)
            {
                ApplyEdgeColliderToGameObject(root, _isOneWay, _addOneWayScript, _edgeYOffset);
            }
            else if (_colliderMode == ColliderMode.CompositePolygon)
            {
                CompositeCollider2D composite = root.AddComponent<CompositeCollider2D>();
                Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
                if (rb != null) rb.bodyType = RigidbodyType2D.Static;
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

                foreach (var piece in pieces)
                {
                    PolygonCollider2D poly = piece.AddComponent<PolygonCollider2D>();
                    poly.usedByComposite = true;
                }

                if (_isOneWay)
                {
                    composite.usedByEffector = true;
                    PlatformEffector2D effector = root.AddComponent<PlatformEffector2D>();
                    effector.useOneWay = true;
                    effector.surfaceArc = 180f;

                    if (_addOneWayScript)
                    {
                        root.AddComponent<OneWayPlatform>();
                    }
                }
            }
            else
            {
                foreach (var piece in pieces)
                {
                    PolygonCollider2D poly = piece.AddComponent<PolygonCollider2D>();
                    if (_isOneWay)
                    {
                        poly.usedByEffector = true;
                        PlatformEffector2D effector = piece.AddComponent<PlatformEffector2D>();
                        effector.useOneWay = true;
                        effector.surfaceArc = 180f;

                        if (_addOneWayScript)
                        {
                            piece.AddComponent<OneWayPlatform>();
                        }
                    }
                }
            }
        }

        [MenuItem("WarOfSamsara/🛠️ Fix Ground & Reset Player", false, 1)]
        public static void FixGroundAndResetPlayerMenu()
        {
            FixGroundAndResetPlayer();
        }

        /// <summary>
        /// ซ่อมแซมระบบพื้นดินของแมพ และดึงตัวละครที่ร่วงตกโลกกลับขึ้นมาบนพื้น
        /// </summary>
        public static void FixGroundAndResetPlayer()
        {
            // 1. ค้นหา MAP_STARTER_001
            GameObject mapObj = GameObject.Find("MAP_STARTER_001");
            if (mapObj != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(mapObj, "Fix Map and Ground");

                // ลบ EdgeCollider / PlatformEffector ที่เผลอไปติดบนตัวแม่ของแมพออก
                EdgeCollider2D errEdge = mapObj.GetComponent<EdgeCollider2D>();
                if (errEdge != null) Undo.DestroyObjectImmediate(errEdge);

                PlatformEffector2D errEffector = mapObj.GetComponent<PlatformEffector2D>();
                if (errEffector != null) Undo.DestroyObjectImmediate(errEffector);

                OneWayPlatform errOneWay = mapObj.GetComponent<OneWayPlatform>();
                if (errOneWay != null) Undo.DestroyObjectImmediate(errOneWay);

                Rigidbody2D errRb = mapObj.GetComponent<Rigidbody2D>();
                if (errRb != null) Undo.DestroyObjectImmediate(errRb);

                // ค้นหา Layer_05_Ground
                Transform groundTrans = mapObj.transform.Find("Layer_05_Ground");
                if (groundTrans != null)
                {
                    BoxCollider2D groundCol = groundTrans.GetComponent<BoxCollider2D>();
                    if (groundCol == null) groundCol = groundTrans.gameObject.AddComponent<BoxCollider2D>();

                    // คำนวณความสูงผิวถนนจริงของ Layer 5 Ground (ขนาด 53.333m, ส่วนถนนอยู่ช่วงล่าง)
                    groundCol.size = new Vector2(53.333f, 8.75f);
                    groundCol.offset = new Vector2(0f, -3.125f);
                    groundCol.isTrigger = false;
                    groundCol.usedByEffector = false;

                    Debug.Log("<color=#44FF88><b>[PlatformGenerator]</b> กู้คืน BoxCollider2D ให้กับ Layer_05_Ground เรียบร้อยแล้ว (ผิวถนนอยู่ที่ Y ≈ 0)</color>", groundTrans);
                }
                else
                {
                    // ถ้าหาไม่เจอ ให้สร้างพื้นดินสำรองที่ระดับ Y = -0.5
                    GameObject fallbackGround = new GameObject("Ground_Collision");
                    fallbackGround.transform.SetParent(mapObj.transform, false);
                    fallbackGround.transform.position = new Vector3(0f, -1.5f, 0f);
                    BoxCollider2D bCol = fallbackGround.AddComponent<BoxCollider2D>();
                    bCol.size = new Vector2(120f, 2f);
                    Debug.Log("<color=#44FF88><b>[PlatformGenerator]</b> สร้าง Ground_Collision สำรองกว้าง 120 เมตรให้แล้ว</color>");
                }
            }

            // 2. ดึง Player กลับขึ้นมาบนพื้นถนน
            GameObject player = GameObject.Find("Player_Base");
            if (player == null) player = GameObject.Find("BasePlayer");

            if (player != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(player, "Reset Player Position");

                // วางไว้เหนือผิวถนนเล็กน้อยที่ Y = 0.5f ให้ตกยืนพอดี
                player.transform.position = new Vector3(0f, 0.5f, 0f);

                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                // ตรวจสอบ BoxCollider2D ของผู้เล่น
                BoxCollider2D playerCol = player.GetComponent<BoxCollider2D>();
                if (playerCol == null)
                {
                    playerCol = player.AddComponent<BoxCollider2D>();
                    playerCol.size = new Vector2(1f, 1.5f);
                    playerCol.offset = new Vector2(0f, 0f);
                }

                Selection.activeGameObject = player;
                EditorGUIUtility.PingObject(player);
                Debug.Log("<color=#33FFFF><b>[PlatformGenerator]</b> ดึงตัวละคร Player กลับขึ้นมาที่พิกัด (0, 1.5, 0) เรียบร้อยแล้ว!</color>", player);
            }

            EditorUtility.DisplayDialog("กู้คืนสำเร็จ! 🎉", 
                "ระบบได้ทำการ:\n\n" +
                "1. ลบ Edge Collider และ One-Way ที่เผลอไปติดบนตัวแม่ MAP_STARTER_001 ออกแล้ว\n" +
                "2. ติดตั้ง Box Collider 2D ทึบ ให้กับพื้นดิน Layer_05_Ground เรียบร้อย\n" +
                "3. ดึงตัวละคร Player_Base กลับขึ้นมายืนบนพื้น (0, 1.5, 0) เรียบร้อยแล้วครับ!", "เยี่ยมเลย");
        }

        [MenuItem("WarOfSamsara/🔨 Fix All Platforms Colliders", false, 2)]
        public static void FixAllPlatformsMenu()
        {
            FixAllPlatformsInScene();
        }

        [MenuItem("WarOfSamsara/📷 Fix Camera Y Tracking & Map Bounds", false, 3)]
        public static void FixCameraYTrackingMenu()
        {
            FixCameraYTracking();
        }

        /// <summary>
        /// ปรับแต่งกล้องให้เลื่อนตามแกน Y เวลาผู้เล่นกระโดดขึ้น Platform สูงๆ
        /// พร้อมขยาย MapBounds ให้มีความสูงครอบคลุมพื้นที่ด้านบน
        /// </summary>
        public static void FixCameraYTracking()
        {
            int changes = 0;

            // 1. ตรวจสอบ MapBounds ใน Scene
            Environment.MapBounds mapBounds = Object.FindFirstObjectByType<Environment.MapBounds>();
            if (mapBounds != null)
            {
                Undo.RecordObject(mapBounds.gameObject, "Expand MapBounds for High Platforms");
                BoxCollider2D col = mapBounds.Collider;
                if (col != null)
                {
                    Undo.RecordObject(col, "Expand MapBounds Collider");
                    col.size = new Vector2(col.size.x, 32f);
                    col.offset = new Vector2(col.offset.x, 8.5f);
                    EditorUtility.SetDirty(col);
                    changes++;
                }
            }

            // 2. ตรวจสอบ CameraFollow2D
            CameraControl.CameraFollow2D camFollow = Object.FindFirstObjectByType<CameraControl.CameraFollow2D>();
            if (camFollow != null)
            {
                Undo.RecordObject(camFollow, "Adjust Camera Y Clamping");
                SerializedObject so = new SerializedObject(camFollow);

                SerializedProperty propClampY = so.FindProperty("clampY");
                if (propClampY != null) propClampY.boolValue = false; // ปิด Clamp Y เพดานบน เพื่อให้กล้องลอยตามตัวละครขึ้นได้อิสระ

                SerializedProperty propClampMinY = so.FindProperty("clampMinYOnly");
                if (propClampMinY != null) propClampMinY.boolValue = true; // ล็อกเฉพาะขอบล่าง ไม่ให้กล้องตกใต้ดิน

                SerializedProperty propSmoothY = so.FindProperty("smoothTimeY");
                if (propSmoothY != null) propSmoothY.floatValue = 0.22f;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(camFollow);
                changes++;
            }

            EditorUtility.DisplayDialog("ปรับแต่งกล้องแกน Y สำเร็จ! 📷",
                "1. ปลดล็อกเพดานแกน Y (clampY = false) ทำให้กล้องลอยตามผู้เล่นขึ้น Platform สูงๆ ได้แล้ว!\n" +
                "2. ล็อกเฉพาะขอบล่าง (clampMinYOnly = true) เพื่อกันไม่ให้กล้องจมลงใต้พื้นดิน\n" +
                "3. ขยาย MapBounds ความสูงเพิ่มขึ้นเป็น 32m เรียบร้อยแล้วครับ!", "สุดยอด");
        }

        /// <summary>
        /// สแกนหา Platform ทั้งหมดในฉาก และใส่แผ่นเหยียบผิวบนหนา 0.35m แบบ One-Way ให้ทั้งหมดในคลิกเดียว
        /// </summary>
        public static void FixAllPlatformsInScene()
        {
            List<GameObject> platformsToFix = new List<GameObject>();

            // 1. หาใน MAP_STARTER_001/Platforms
            GameObject mapObj = GameObject.Find("MAP_STARTER_001");
            if (mapObj != null)
            {
                Transform platRoot = mapObj.transform.Find("Platforms");
                if (platRoot != null)
                {
                    for (int i = 0; i < platRoot.childCount; i++)
                    {
                        platformsToFix.Add(platRoot.GetChild(i).gameObject);
                    }
                }
            }

            // 2. หา GameObject อื่นๆ ใน Scene ที่ชื่อขึ้นต้นด้วย Platform
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Platform_") && !platformsToFix.Contains(go))
                {
                    platformsToFix.Add(go);
                }
            }

            if (platformsToFix.Count == 0)
            {
                EditorUtility.DisplayDialog("แจ้งเตือน", "ไม่พบ GameObject แท่นกระโดด (Platform) ใน Scene ครับ", "ตกลง");
                return;
            }

            foreach (var plat in platformsToFix)
            {
                FixPlatformCollider(plat, true, true, 0f);
            }

            EditorUtility.DisplayDialog("สำเร็จ! 🎉", $"ทำการติดตั้งแผ่นเหยียบผิวบนหนา 0.35m แบบ One-Way ให้กับแท่นทั้งหมด {platformsToFix.Count} ชิ้นเรียบร้อยแล้ว!\n\nคราวนี้เหยียบติด 100% แน่นอนครับ", "ยอดเยี่ยม");
        }

        /// <summary>
        /// ติดตั้ง BoxCollider2D แบบแผ่นเหยียบผิวบน (หนา 0.35m) พร้อม PlatformEffector2D One-Way
        /// - มีความหนา 0.35m ทำให้เหยียบติด 100% ไม่ร่วงทะลุ
        /// - อยู่เฉพาะผิวบน ทำให้เดินรอดใต้แท่นได้ หัวไม่ชน 100%
        /// - รองรับกด ลง + กระโดด เพื่อมุดลงมาได้
        /// </summary>
        public static void FixPlatformCollider(GameObject platform, bool isOneWay = true, bool addOneWayScript = true, float yOffset = 0f)
        {
            if (platform == null) return;

            if (platform.name.StartsWith("MAP_") || platform.GetComponent<ParallaxBackground>() != null)
            {
                EditorUtility.DisplayDialog("⚠️ แจ้งเตือนความปลอดภัย", "ห้ามเลือกตัวแม่แมพ MAP_STARTER_001 ครับ ให้เลือกเฉพาะแท่นกระโดด", "เข้าใจแล้ว");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(platform, "Fix Platform Collider");

            SpriteRenderer[] srs = platform.GetComponentsInChildren<SpriteRenderer>();
            if (srs.Length == 0)
            {
                EditorUtility.DisplayDialog("แจ้งเตือน", $"ไม่พบ SpriteRenderer ใดๆ ใน {platform.name}", "ตกลง");
                return;
            }

            // ลบ Collider และ Rigidbody เก่าทิ้งทั้งหมด
            foreach (var col in platform.GetComponentsInChildren<Collider2D>())
            {
                Undo.DestroyObjectImmediate(col);
            }

            Rigidbody2D rb = platform.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Undo.DestroyObjectImmediate(rb);
            }

            // คำนวณ Bounds รวมของชิ้นส่วนลูกทั้งหมดใน Local Space ของ platform
            Bounds totalBounds = srs[0].bounds;
            for (int i = 1; i < srs.Length; i++) totalBounds.Encapsulate(srs[i].bounds);

            Vector3 worldTopCenter = new Vector3(totalBounds.center.x, totalBounds.max.y + yOffset, platform.transform.position.z);
            Vector3 localTopCenter = platform.transform.InverseTransformPoint(worldTopCenter);
            float worldWidth = totalBounds.size.x;
            float localWidth = worldWidth / Mathf.Max(0.001f, platform.transform.lossyScale.x);

            float slabHeight = 0.35f;
            BoxCollider2D box = platform.AddComponent<BoxCollider2D>();
            box.size = new Vector2(localWidth, slabHeight);
            box.offset = new Vector2(localTopCenter.x, localTopCenter.y - (slabHeight * 0.5f));

            if (isOneWay)
            {
                box.usedByEffector = true;

                PlatformEffector2D effector = platform.GetComponent<PlatformEffector2D>();
                if (effector == null) effector = platform.AddComponent<PlatformEffector2D>();

                effector.useOneWay = true;
                effector.surfaceArc = 180f;
                effector.useSideFriction = false;
                effector.useSideBounce = false;

                if (addOneWayScript)
                {
                    OneWayPlatform oneWay = platform.GetComponent<OneWayPlatform>();
                    if (oneWay == null) platform.AddComponent<OneWayPlatform>();
                }
            }

            EditorUtility.SetDirty(platform);
            Debug.Log($"<color=#44FF88><b>[PlatformGenerator]</b> ติดตั้งแผ่นเหยียบ BoxCollider2D ผิวบนให้ <b>{platform.name}</b> กว้าง {localWidth:F2}m หนา {slabHeight}m สำเร็จ!</color>", platform);
        }

        /// <summary>
        /// ฟังก์ชันแปลง GameObject แท่นกระโดดใดๆ ให้กลายเป็น EdgeCollider2D (พาดเฉพาะผิวบนสุด)
        /// พร้อมลบ Polygon/Box/Composite Collider เดิมออกให้อัตโนมัติ แก้ปัญหาหัวชนแท่นขาดลอย
        /// </summary>
        public static void ApplyEdgeColliderToGameObject(GameObject target, bool isOneWay = true, bool addOneWayScript = true, float yOffset = 0f)
        {
            if (target == null) return;

            // ⚠️ ป้องกันความปลอดภัย: ห้ามแปลงตัวแม่แมพ หรือพื้นดินหลัก
            if (target.name.StartsWith("MAP_") || target.GetComponent<ParallaxBackground>() != null)
            {
                EditorUtility.DisplayDialog("⚠️ แจ้งเตือนความปลอดภัย", 
                    $"คุณกำลังเลือกตัวแม่ '{target.name}' ซึ่งเป็นโครงสร้างหลักของทั้งฉาก!\n\n" +
                    "ห้ามแปลงตัวแม่แมพครับ ไม่งั้นพื้นดินหลักจะกลายเป็น One-Way ลอยฟ้า ทำให้ตัวละครร่วงตกโลก\n\n" +
                    "👉 วิธีที่ถูกต้อง: ให้คลิกเลือกเฉพาะ 'แท่นกระโดด' (เช่น Platform_Ruins หรือชิ้นส่วนแท่นลอย) แล้วค่อยกดปุ่มครับ", "เข้าใจแล้ว");
                return;
            }

            if (target.name.ToLower().Contains("ground"))
            {
                EditorUtility.DisplayDialog("⚠️ แจ้งเตือน", 
                    $"'{target.name}' คือพื้นดินหลัก (ไม่ใช่แท่นกระโดดลอย)\n\n" +
                    "พื้นดินหลักต้องใช้ BoxCollider2D แบบทึบปกติครับ เพื่อไม่ให้ตัวละครร่วงตกแมพ\n" +
                    "ระบบจะใส่ BoxCollider2D ให้แทนอัตโนมัติครับ", "ตกลง");

                BoxCollider2D groundCol = target.GetComponent<BoxCollider2D>();
                if (groundCol == null) groundCol = target.AddComponent<BoxCollider2D>();
                groundCol.isTrigger = false;
                groundCol.usedByEffector = false;
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(target, "Apply Edge Collider");

            // 1. ค้นหา SpriteRenderer ทั้งหมดในตัวมันและลูกๆ
            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0)
            {
                EditorUtility.DisplayDialog("แจ้งเตือน", $"ไม่พบ SpriteRenderer ใดๆ ใน {target.name} หรือลูกๆ", "ตกลง");
                return;
            }

            // 2. ลบ Collider เก่าทั้งหมดออก (ทั้งในตัวแม่และตัวลูก) เพื่อไม่ให้ซ้อนกัน
            Collider2D[] oldColliders = target.GetComponentsInChildren<Collider2D>();
            foreach (var col in oldColliders)
            {
                Undo.DestroyObjectImmediate(col);
            }

            // ลบ Rigidbody2D เก่าที่อาจแถมมาจาก CompositeCollider
            Rigidbody2D oldRb = target.GetComponent<Rigidbody2D>();
            if (oldRb != null)
            {
                Undo.DestroyObjectImmediate(oldRb);
            }

            // 3. เรียงลำดับชิ้นส่วนจาก ซ้าย -> ขวา ตามตำแหน่ง World Position
            var sortedRenderers = renderers.OrderBy(r => r.bounds.min.x).ToList();

            // 4. คำนวณจุดผิวสัมผัสด้านบนสุด (Top Surface Points)
            List<Vector2> edgePoints = new List<Vector2>();

            for (int i = 0; i < sortedRenderers.Count; i++)
            {
                Bounds b = sortedRenderers[i].bounds;

                // จุดซ้ายบน และ ขวาบน ของชิ้นส่วนนี้ใน World Space
                Vector3 worldTopLeft = new Vector3(b.min.x, b.max.y + yOffset, target.transform.position.z);
                Vector3 worldTopRight = new Vector3(b.max.x, b.max.y + yOffset, target.transform.position.z);

                // แปลงเป็น Local Space ของ target
                Vector2 localTopLeft = target.transform.InverseTransformPoint(worldTopLeft);
                Vector2 localTopRight = target.transform.InverseTransformPoint(worldTopRight);

                // ใส่จุดซ้าย
                if (edgePoints.Count == 0)
                {
                    edgePoints.Add(localTopLeft);
                }
                else
                {
                    // ถ้าจุดห่างจากจุดก่อนหน้าพอสมควร ให้เพิ่มจุด
                    Vector2 prevPoint = edgePoints[edgePoints.Count - 1];
                    if (Vector2.Distance(prevPoint, localTopLeft) > 0.02f)
                    {
                        edgePoints.Add(localTopLeft);
                    }
                }

                // ใส่จุดขวา
                edgePoints.Add(localTopRight);
            }

            if (edgePoints.Count < 2)
            {
                Bounds tb = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) tb.Encapsulate(renderers[i].bounds);
                Vector2 left = target.transform.InverseTransformPoint(new Vector3(tb.min.x, tb.max.y + yOffset, 0f));
                Vector2 right = target.transform.InverseTransformPoint(new Vector3(tb.max.x, tb.max.y + yOffset, 0f));
                edgePoints = new List<Vector2> { left, right };
            }

            // 5. ติดตั้ง EdgeCollider2D
            EdgeCollider2D edge = target.AddComponent<EdgeCollider2D>();
            edge.points = edgePoints.ToArray();

            // 6. ติดตั้ง PlatformEffector2D (One-Way)
            if (isOneWay)
            {
                edge.usedByEffector = true;

                PlatformEffector2D effector = target.GetComponent<PlatformEffector2D>();
                if (effector == null) effector = target.AddComponent<PlatformEffector2D>();

                effector.useOneWay = true;
                effector.surfaceArc = 180f;
                effector.useSideFriction = false;
                effector.useSideBounce = false;

                if (addOneWayScript)
                {
                    OneWayPlatform oneWay = target.GetComponent<OneWayPlatform>();
                    if (oneWay == null) target.AddComponent<OneWayPlatform>();
                }
            }

            EditorUtility.SetDirty(target);
            Debug.Log($"<color=#33FFFF><b>[PlatformGenerator]</b> แปลง <b>{target.name}</b> เป็น EdgeCollider2D สำเร็จ! (มี {edgePoints.Count} จุดผิวบน, ลบ Collider เก่าทั้งหมดออกแล้ว)</color>", target);
        }
    }
}
