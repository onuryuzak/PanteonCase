#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Panteon.Data;
using Panteon.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.EditorTools
{
    public sealed class RuntimeHudGeneratorWindow : EditorWindow
    {
        private const string ReferenceUrl = "https://github.com/zexanein/panteon-case-project";

        [MenuItem("Tools/Panteon/HUD Generator")]
        private static void Open()
        {
            var window = GetWindow<RuntimeHudGeneratorWindow>(true, "Panteon HUD Generator");
            window.minSize = new Vector2(460f, 250f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Reference-style Runtime HUD", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates the complete HUD as an editable prefab. Runtime code only binds data, events and pooled template instances; it does not construct UI graphics.",
                MessageType.Info);
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Output", RuntimeHudPrefabBuilder.PrefabPath);
            EditorGUILayout.LabelField("Reference", ReferenceUrl);
            EditorGUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(
                       EditorApplication.isPlayingOrWillChangePlaymode ||
                       EditorApplication.isCompiling ||
                       EditorApplication.isUpdating))
            {
                if (GUILayout.Button("Generate / Refresh Runtime HUD", GUILayout.Height(44f)))
                    RuntimeHudPrefabBuilder.Generate();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "After generation, edit RuntimeHUD.prefab directly. Running this tool again intentionally rebuilds the prefab from the reference layout.",
                MessageType.Warning);
        }
    }

    internal static class RuntimeHudPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/RuntimeHUD.prefab";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/SO_BuildingCatalog.asset";
        private const string RegularFontPath = "Assets/_Project/Fonts/static/PixelifySans-Regular.ttf";
        private const string BoldFontPath = "Assets/_Project/Fonts/static/PixelifySans-Bold.ttf";
        private const string GeneratedFolder = "Assets/_Project/Generated/UI";
        private const string RoundedSpritePath = GeneratedFolder + "/HUD_Rounded.png";
        private const string CircleSpritePath = GeneratedFolder + "/HUD_Circle.png";

        private static readonly Color Panel = new Color32(47, 47, 47, 255);
        private static readonly Color Card = new Color32(64, 64, 64, 255);
        private static readonly Color Header = new Color32(75, 86, 77, 255);
        private static readonly Color Plate = new Color32(107, 104, 90, 255);
        private static readonly Color Border = new Color32(117, 114, 99, 255);
        private static readonly Color TextColor = new Color32(246, 244, 238, 255);
        private static readonly Color MutedText = new Color32(205, 202, 190, 255);
        private static readonly Color Danger = new Color32(198, 72, 72, 255);
        private static readonly Color TransparentCard = new Color(0.07f, 0.08f, 0.1f, 0.48f);

        private static Font _regularFont;
        private static Font _boldFont;
        private static Sprite _roundedSprite;
        private static Sprite _circleSprite;

        public static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog("HUD Generator", "Exit Play Mode and wait for Unity to finish compiling before generating the HUD.", "OK");
                return;
            }

            try
            {
                EnsureFolders();
                GenerateStyleSprites();
                LoadAssets();

                var catalog = AssetDatabase.LoadAssetAtPath<BuildingCatalogSO>(CatalogPath);
                if (catalog == null) throw new InvalidOperationException($"Building catalog was not found at {CatalogPath}.");

                var root = BuildHierarchy(catalog);
                try
                {
                    var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    if (prefab == null) throw new InvalidOperationException("Unity could not save RuntimeHUD.prefab.");
                    BindOpenScenes(prefab.GetComponent<RuntimeHudView>());
                    AssetDatabase.SaveAssets();
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                    Debug.Log($"Generated reference-style Runtime HUD at {PrefabPath}");
                    EditorUtility.DisplayDialog("HUD Generator", "RuntimeHUD.prefab was generated successfully.", "OK");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("HUD Generator Failed", exception.Message, "OK");
            }
        }

        private static GameObject BuildHierarchy(BuildingCatalogSO catalog)
        {
            var rootObject = new GameObject(
                "RuntimeHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(RuntimeHudView));
            rootObject.layer = 5;
            var root = (RectTransform)rootObject.transform;
            root.sizeDelta = new Vector2(1920f, 1080f);

            var canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = rootObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            root.localScale = Vector3.one;

            var board = CreateRect(root, "BoardViewport", new Rect(400f, 0f, 1120f, 1080f));
            var production = BuildProductionPanel(root, catalog);
            var information = BuildInformationPanel(root, catalog);
            rootObject.GetComponent<RuntimeHudView>().Configure(board, production, information);
            return rootObject;
        }

        private static RectTransform BuildProductionPanel(RectTransform root, BuildingCatalogSO catalog)
        {
            var panel = CreateImage(root, "ProductionPanel", new Rect(0f, 0f, 400f, 1080f), Panel).rectTransform;
            var titlePlate = CreateImage(panel, "ProductionTitlePlate", new Rect(30f, 15f, 340f, 60f), Header);
            CreateText(titlePlate.transform, "ProductionTitle", "Production", 32, true, TextAnchor.MiddleCenter, TextColor,
                new Rect(0f, 0f, 340f, 60f));
            var subtitle = CreateText(panel, "ProductionSubtitle", "Infinite Scrollview", 20, false,
                TextAnchor.MiddleLeft, MutedText, new Rect(30f, 76f, 340f, 28f));
            subtitle.gameObject.SetActive(false);

            var scrollRoot = CreateRect(panel, "ProductionScroll", new Rect(25f, 95f, 350f, 960f));
            var scrollImage = scrollRoot.gameObject.AddComponent<Image>();
            scrollImage.color = Color.clear;
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = CreateRect(scrollRoot, "Viewport", new Rect(0f, 0f, 350f, 960f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = CreateRect(viewport, "Content", new Rect(0f, 0f, 350f, 1f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            scroll.viewport = viewport;
            scroll.content = content;

            var buildings = CollectBuildings(catalog);
            for (var i = 0; i < buildings.Count; i++)
            {
                var row = i / 2;
                var column = i % 2;
                CreateCardButton(
                    content,
                    buildings[i].DisplayName.Replace(" ", string.Empty),
                    buildings[i].DisplayName,
                    buildings[i].Icon,
                    new Rect(25f + column * 160f, row * 160f, 140f, 140f),
                    34f);
            }
            content.sizeDelta = new Vector2(0f, Mathf.Max(1f, Mathf.Ceil(buildings.Count / 2f) * 160f));

            var status = CreateText(panel, "Status", string.Empty, 20, false, TextAnchor.MiddleLeft, MutedText,
                new Rect(25f, 1040f, 350f, 30f));
            status.gameObject.SetActive(false);
            return panel;
        }

        private static RectTransform BuildInformationPanel(RectTransform root, BuildingCatalogSO catalog)
        {
            var panel = CreateImage(root, "InformationPanel", new Rect(1520f, 0f, 400f, 1080f), Panel).rectTransform;
            var header = CreateImage(panel, "InfoHeaderPlate", new Rect(30f, 15f, 340f, 60f), Header);
            CreateText(header.transform, "InfoHeader", "Information", 32, true, TextAnchor.MiddleCenter, TextColor,
                new Rect(0f, 0f, 340f, 60f));

            var genericTitle = CreateText(panel, "InfoTitle", string.Empty, 26, true, TextAnchor.MiddleLeft, TextColor,
                new Rect(30f, 105f, 340f, 35f));
            var genericSubtitle = CreateText(panel, "InfoSubtitle", string.Empty, 20, false, TextAnchor.MiddleLeft, MutedText,
                new Rect(30f, 145f, 340f, 28f));
            var genericPreview = CreateImage(panel, "InfoPreview", new Rect(30f, 185f, 340f, 160f), Card);
            genericPreview.preserveAspect = true;
            var genericHp = CreateText(panel, "InfoHp", string.Empty, 22, true, TextAnchor.MiddleLeft, TextColor,
                new Rect(30f, 360f, 340f, 36f));
            genericTitle.gameObject.SetActive(false);
            genericSubtitle.gameObject.SetActive(false);
            genericPreview.gameObject.SetActive(false);
            genericHp.gameObject.SetActive(false);

            BuildUnitDetails(panel);
            BuildBuildingDetails(panel);
            BuildSelectedUnits(panel);
            BuildUnitProduction(panel, catalog);
            BuildDestroyButton(panel);
            return panel;
        }

        private static void BuildUnitDetails(RectTransform panel)
        {
            var root = CreateRect(panel, "UnitDetails", new Rect(25f, 95f, 350f, 820f));
            var portraitFrame = CreateImage(root, "PortraitFrame", new Rect(55f, 0f, 240f, 240f), Plate, _circleSprite);
            var portraitMask = CreateImage(root, "PortraitMask", new Rect(75f, 20f, 200f, 200f), Color.white, _circleSprite);
            var mask = portraitMask.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var portrait = CreateImage(portraitMask.transform, "Portrait", new Rect(0f, 0f, 200f, 200f), Color.white);
            portrait.preserveAspect = true;
            var namePlate = CreateImage(root, "NamePlate", new Rect(15f, 255f, 320f, 35f), Plate);
            CreateText(namePlate.transform, "Name", string.Empty, 22, true, TextAnchor.MiddleCenter, TextColor,
                new Rect(0f, 0f, 320f, 35f));
            var descriptionPlate = CreateImage(root, "DescriptionPlate", new Rect(0f, 305f, 350f, 90f), TransparentCard);
            CreateText(descriptionPlate.transform, "Description", string.Empty, 20, false, TextAnchor.UpperLeft, TextColor,
                new Rect(12f, 10f, 326f, 70f));
            CreateText(root, "HealthStat", string.Empty, 22, true, TextAnchor.MiddleLeft,
                new Color32(242, 89, 89, 255), new Rect(15f, 415f, 145f, 45f));
            CreateText(root, "AttackStat", string.Empty, 22, true, TextAnchor.MiddleLeft,
                new Color32(192, 194, 174, 255), new Rect(190f, 415f, 145f, 45f));
            root.gameObject.SetActive(false);
        }

        private static void BuildBuildingDetails(RectTransform panel)
        {
            var root = CreateRect(panel, "BuildingDetails", new Rect(25f, 95f, 350f, 520f));
            var portraitFrame = CreateImage(root, "PortraitFrame", new Rect(15f, 0f, 320f, 300f), Plate);
            portraitFrame.gameObject.AddComponent<RectMask2D>();
            var portrait = CreateImage(portraitFrame.transform, "Portrait", new Rect(8f, 8f, 304f, 284f), Color.white);
            portrait.preserveAspect = false;
            var namePlate = CreateImage(root, "NamePlate", new Rect(15f, 310f, 320f, 35f), Plate);
            CreateText(namePlate.transform, "Name", string.Empty, 22, true, TextAnchor.MiddleCenter, TextColor,
                new Rect(0f, 0f, 320f, 35f));
            var descriptionPlate = CreateImage(root, "DescriptionPlate", new Rect(0f, 355f, 350f, 90f), TransparentCard);
            CreateText(descriptionPlate.transform, "Description", string.Empty, 20, false, TextAnchor.UpperLeft, TextColor,
                new Rect(12f, 10f, 326f, 70f));
            CreateText(root, "HealthStat", string.Empty, 22, true, TextAnchor.MiddleCenter,
                new Color32(242, 89, 89, 255), new Rect(0f, 455f, 350f, 45f));
            CreateImage(root, "Separator", new Rect(0f, 515f, 350f, 3f), TextColor);
            root.gameObject.SetActive(false);
        }

        private static void BuildUnitProduction(RectTransform panel, BuildingCatalogSO catalog)
        {
            var root = CreateRect(panel, "UnitProduction", new Rect(25f, 630f, 350f, 240f));
            var titlePlate = CreateImage(root, "TitlePlate", new Rect(0f, 0f, 350f, 35f), Plate);
            CreateText(titlePlate.transform, "UnitProductionTitle", "Unit Production", 20, true,
                TextAnchor.MiddleCenter, TextColor, new Rect(0f, 0f, 350f, 35f));
            var content = CreateRect(root, "UnitProductionContent", new Rect(0f, 45f, 350f, 180f));
            var units = CollectUnits(catalog);
            var count = Mathf.Max(1, units.Count);
            var spacing = 4f;
            var width = (350f - spacing * (count - 1)) / count;
            for (var i = 0; i < count; i++)
            {
                var unit = i < units.Count ? units[i] : null;
                var button = CreateCardButton(
                    content,
                    unit != null ? unit.DisplayName.Replace(" ", string.Empty) : $"UnitTemplate{i + 1}",
                    unit != null ? unit.DisplayName : string.Empty,
                    unit != null ? unit.Icon : null,
                    new Rect(i * (width + spacing), 0f, width, 160f),
                    32f,
                    18);
                button.gameObject.SetActive(false);
            }
            root.gameObject.SetActive(false);
        }

        private static void BuildSelectedUnits(RectTransform panel)
        {
            var root = CreateImage(panel, "SelectedUnits", new Rect(25f, 185f, 350f, 760f), Card).rectTransform;
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = CreateRect(root, "Viewport", new Rect(0f, 0f, 350f, 760f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = CreateRect(viewport, "Content", new Rect(0f, 0f, 350f, 1f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            scroll.viewport = viewport;
            scroll.content = content;

            for (var i = 0; i < 32; i++)
            {
                var row = CreateImage(content, $"SelectedUnit_{i + 1}", new Rect(8f, 8f + i * 60f, 334f, 54f),
                    new Color32(35, 42, 46, 255)).rectTransform;
                var icon = CreateImage(row, "Icon", new Rect(7f, 7f, 40f, 40f), Card);
                icon.preserveAspect = true;
                CreateText(row, "Name", string.Empty, 20, true, TextAnchor.MiddleLeft, TextColor,
                    new Rect(56f, 0f, 268f, 54f));
                row.gameObject.SetActive(false);
            }
            root.gameObject.SetActive(false);
        }

        private static void BuildDestroyButton(RectTransform panel)
        {
            var root = CreateImage(panel, "DestroyButton", new Rect(40f, 1010f, 320f, 48f), Danger);
            var button = root.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Danger;
            colors.highlightedColor = new Color32(224, 88, 88, 255);
            colors.pressedColor = new Color32(153, 49, 49, 255);
            button.colors = colors;
            CreateText(root.transform, "Label", "Destruct", 24, true, TextAnchor.MiddleCenter, TextColor,
                new Rect(0f, 0f, 320f, 48f));
            root.gameObject.SetActive(false);
        }

        private static Button CreateCardButton(
            Transform parent,
            string objectName,
            string label,
            Sprite icon,
            Rect rect,
            float labelHeight,
            int labelFontSize = 20)
        {
            var root = CreateImage(parent, objectName, rect, Card);
            root.gameObject.AddComponent<RectMask2D>();
            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(3f, -3f);
            var button = root.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Card;
            colors.highlightedColor = new Color32(81, 84, 74, 255);
            colors.pressedColor = new Color32(90, 108, 82, 255);
            button.colors = colors;

            var iconHeight = Mathf.Max(1f, rect.height - labelHeight - 12f);
            var iconImage = CreateImage(root.transform, "Icon", new Rect(8f, 7f, rect.width - 16f, iconHeight),
                icon != null ? Color.white : new Color(1f, 1f, 1f, 0.08f));
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            var labelPlate = CreateImage(root.transform, "LabelPlate",
                new Rect(1f, rect.height - labelHeight - 5f, rect.width - 2f, labelHeight), Card);
            labelPlate.raycastTarget = false;
            CreateText(root.transform, "Label", label, Mathf.Min(labelFontSize, Mathf.RoundToInt(labelHeight - 6f)), true,
                TextAnchor.MiddleCenter, TextColor,
                new Rect(1f, rect.height - labelHeight - 5f, rect.width - 2f, labelHeight));
            return button;
        }

        private static Image CreateImage(Transform parent, string name, Rect rect, Color color, Sprite sprite = null)
        {
            var target = CreateRect(parent, name, rect);
            var image = target.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : _roundedSprite;
            image.type = image.sprite == _roundedSprite ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int size,
            bool bold,
            TextAnchor anchor,
            Color color,
            Rect rect)
        {
            var target = CreateRect(parent, name, rect);
            var text = target.gameObject.AddComponent<Text>();
            text.font = bold ? _boldFont : _regularFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.text = value;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static RectTransform CreateRect(Transform parent, string name, Rect rect)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.layer = 5;
            var transform = (RectTransform)target.transform;
            transform.SetParent(parent, false);
            transform.anchorMin = transform.anchorMax = new Vector2(0f, 1f);
            transform.pivot = new Vector2(0f, 1f);
            transform.anchoredPosition = new Vector2(rect.x, -rect.y);
            transform.sizeDelta = rect.size;
            return transform;
        }

        private static List<BuildingDefinitionSO> CollectBuildings(BuildingCatalogSO catalog)
        {
            var result = new List<BuildingDefinitionSO>();
            foreach (var building in catalog.Buildings)
                if (building != null) result.Add(building);
            return result;
        }

        private static List<UnitDefinitionSO> CollectUnits(BuildingCatalogSO catalog)
        {
            var result = new List<UnitDefinitionSO>();
            var seen = new HashSet<UnitDefinitionSO>();
            foreach (var building in catalog.Buildings)
            {
                if (building == null || building.Producibles == null) continue;
                foreach (var unit in building.Producibles)
                    if (unit != null && seen.Add(unit)) result.Add(unit);
            }
            return result;
        }

        private static void BindOpenScenes(RuntimeHudView prefab)
        {
            if (prefab == null) return;
            var controllers = UnityEngine.Object.FindObjectsOfType<GameHudController>(true);
            foreach (var controller in controllers)
            {
                var existingViews = UnityEngine.Object.FindObjectsOfType<RuntimeHudView>(true);
                foreach (var existing in existingViews)
                {
                    if (existing == null || existing.gameObject.scene != controller.gameObject.scene) continue;
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                var instanceObject = PrefabUtility.InstantiatePrefab(prefab.gameObject, controller.gameObject.scene) as GameObject;
                if (instanceObject == null) throw new InvalidOperationException("RuntimeHUD prefab could not be placed in the scene.");
                instanceObject.name = prefab.name;
                instanceObject.transform.SetParent(controller.transform, false);
                instanceObject.transform.SetAsLastSibling();
                var instance = instanceObject.GetComponent<RuntimeHudView>();
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("_hud").objectReferenceValue = instance;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }
        }

        private static void LoadAssets()
        {
            _regularFont = AssetDatabase.LoadAssetAtPath<Font>(RegularFontPath);
            _boldFont = AssetDatabase.LoadAssetAtPath<Font>(BoldFontPath);
            _roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            _circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            if (_regularFont == null || _boldFont == null || _roundedSprite == null || _circleSprite == null)
                throw new InvalidOperationException("HUD fonts or generated style sprites could not be loaded.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project/Prefabs", "UI");
            EnsureFolder("Assets/_Project", "Generated");
            EnsureFolder("Assets/_Project/Generated", "UI");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static void GenerateStyleSprites()
        {
            WriteSprite(RoundedSpritePath, CreateRoundedTexture(32, 8), new Vector4(8f, 8f, 8f, 8f));
            WriteSprite(CircleSpritePath, CreateCircleTexture(64), Vector4.zero);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureSprite(RoundedSpritePath, new Vector4(8f, 8f, 8f, 8f));
            ConfigureSprite(CircleSpritePath, Vector4.zero);
            AssetDatabase.SaveAssets();
        }

        private static void WriteSprite(string assetPath, Texture2D texture, Vector4 border)
        {
            try
            {
                File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Texture importer was not found for {path}.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var texture = NewTexture(size);
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radiusSquared = center * center;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                pixels[y * size + x] = dx * dx + dy * dy <= radiusSquared
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateRoundedTexture(int size, int radius)
        {
            var texture = NewTexture(size);
            var pixels = new Color32[size * size];
            var corner = radius - 0.5f;
            var radiusSquared = corner * corner;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x < radius ? corner - x : x >= size - radius ? x - (size - radius) - corner : 0f;
                var dy = y < radius ? corner - y : y >= size - radius ? y - (size - radius) - corner : 0f;
                pixels[y * size + x] = dx * dx + dy * dy <= radiusSquared
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D NewTexture(int size) => new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }
}
#endif
