using System;
using FlyMeToTheMoon;
using FlyMeToTheMoon.Instruments;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PianoDemoBuilder
{
    public const string Root = "Assets/Instruments/Piano";
    public const string ScenePath = Root + "/PianoCollectionDemo.unity";

    [MenuItem("Tools/Fly Me to the Moon/Play Piano Demo")]
    public static void Play()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Fly Me to the Moon/Create Piano Demo")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before creating the demo.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("The demo already exists. Open it instead of recreating it.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!DetectPipeline()) throw new InvalidOperationException("The demo requires the project's URP pipeline.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/FirstPlayablePrototype.unity", OpenSceneMode.Single);
        // Save under our feature directory before making any scene changes.
        EditorSceneManager.SaveScene(scene, ScenePath);
        FinishBuild(scene);
    }

    public static void RebuildForValidation()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding the demo.");
        if (!DetectPipeline()) throw new InvalidOperationException("The demo requires the project's URP pipeline.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/FirstPlayablePrototype.unity", OpenSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        FinishBuild(scene);
    }

    private static void FinishBuild(Scene scene)
    {
        EnsureFolder("Definitions");
        EnsureFolder("Prefabs");
        EnsureFolder("UI");
        var definitions = new[] { MakeDefinition("C4"), MakeDefinition("E4"), MakeDefinition("G4") };
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/FlatColor.mat");
        var template = new GameObject("Piano Score Fragment");
        var pickup = template.AddComponent<PianoPickup>();
        pickup.piece = definitions[0];
        pickup.GetComponent<CircleCollider2D>().radius = 0.5f;
        // A small score sheet, using the existing procedural pixel-art visual system.
        AddRect(template.transform, "Border", new Vector3(0, 0, -0.3f), new Vector2(0.875f, 1f), new Color(0.95f, 0.75f, 0.35f), material);
        AddRect(template.transform, "Paper", new Vector3(0, 0, -0.32f), new Vector2(0.75f, 0.875f), new Color(1f, 0.96f, 0.8f), material);
        for (int i = 0; i < 3; i++)
            AddRect(template.transform, "Staff", new Vector3(0, -0.125f + i * 0.125f, -0.34f), new Vector2(0.625f, 0.0625f), new Color(0.38f, 0.48f, 0.52f), material);
        AddRect(template.transform, "NoteHead", new Vector3(-0.06f, -0.06f, -0.36f), new Vector2(0.25f, 0.125f), new Color(0.1f, 0.2f, 0.3f), material);
        AddRect(template.transform, "NoteStem", new Vector3(0.03f, 0.1f, -0.36f), new Vector2(0.0625f, 0.375f), new Color(0.1f, 0.2f, 0.3f), material);
        var prefab = PrefabUtility.SaveAsPrefabAsset(template, Root + "/Prefabs/PianoScorePickup.prefab");
        UnityEngine.Object.DestroyImmediate(template);

        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        var collection = player.gameObject.AddComponent<PianoCollection>();
        player.GetComponent<AudioSource>().playOnAwake = false;
        player.GetComponent<AudioSource>().volume = 0.65f;
        var bodies = player.gravityManager.bodies;
        if (bodies.Length < definitions.Length + 1)
            throw new InvalidOperationException("The piano demo requires at least three destination moons.");
        Place(prefab, definitions[0], bodies[1], 180f);
        Place(prefab, definitions[1], bodies[2], 135f);
        Place(prefab, definitions[2], bodies[3], 225f);
        if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
            Camera.main.gameObject.AddComponent<AudioListener>();

        var font = MakeFont();
        var hudTemplate = BuildPanel(font);
        var hudPrefab = PrefabUtility.SaveAsPrefabAsset(hudTemplate, Root + "/Prefabs/PianoCollectionPanel.prefab");
        UnityEngine.Object.DestroyImmediate(hudTemplate);
        var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
        var panel = hud.GetComponent<PianoCollectionPanel>();
        panel.collection = collection;
        PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("Piano UI Events", typeof(EventSystem));
            var input = events.AddComponent<InputSystemUIInputModule>();
            input.AssignDefaultActions();
            // Mouse replay must not consume Space / south-button jumps as UI submissions.
            input.submit = null;
            input.cancel = null;
        }
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Piano demo saved. Three example fragments, any number supported: " + ScenePath);
    }

    private static bool DetectPipeline()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        return pipeline != null && pipeline.GetType().FullName.Contains("Universal");
    }

    private static void EnsureFolder(string name)
    {
        if (!AssetDatabase.IsValidFolder(Root + "/" + name)) AssetDatabase.CreateFolder(Root, name);
    }

    private static PianoPieceDefinition MakeDefinition(string note)
    {
        var existing = AssetDatabase.LoadAssetAtPath<PianoPieceDefinition>(Root + "/Definitions/Piano" + note + ".asset");
        if (existing != null) return existing;
        var asset = ScriptableObject.CreateInstance<PianoPieceDefinition>();
        asset.pieceId = "piano.demo." + note.ToLowerInvariant();
        asset.displayName = "Piano " + note;
        asset.sound = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Piano" + note + ".wav");
        if (asset.sound == null) throw new InvalidOperationException("Missing demo audio for " + note);
        var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(asset.sound));
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        AssetDatabase.CreateAsset(asset, Root + "/Definitions/Piano" + note + ".asset");
        return asset;
    }

    private static void Place(GameObject prefab, PianoPieceDefinition definition, GravityBody moon, float degrees)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = definition.displayName + " Score";
        go.transform.SetParent(moon.transform, false);
        float angle = degrees * Mathf.Deg2Rad;
        var up = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        go.transform.localPosition = up * (moon.radius + 0.7f);
        go.transform.localRotation = Quaternion.Euler(0, 0, -degrees);
        go.GetComponent<PianoPickup>().piece = definition;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<PianoPickup>());
    }

    private static void AddRect(Transform parent, string name, Vector3 position, Vector2 size, Color color, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        var shape = go.AddComponent<PrototypeShape>();
        shape.shape = PrototypeShape.Shape.Rectangle;
        shape.size = size;
        shape.color = color;
        shape.material = material;
        shape.Rebuild();
    }

    private static Font MakeFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) throw new InvalidOperationException("Unity's built-in runtime font is unavailable.");
        return font;
    }

    private static GameObject BuildPanel(Font font)
    {
        var go = new GameObject("Piano Collection", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        scaler.matchWidthOrHeight = 0.5f;
        var panel = go.AddComponent<PianoCollectionPanel>();
        var box = Rect("Panel", go.transform);
        box.anchorMin = new Vector2(0, 0);
        box.anchorMax = new Vector2(1, 0);
        box.pivot = new Vector2(0.5f, 0);
        box.anchoredPosition = new Vector2(0, 16);
        box.sizeDelta = new Vector2(-40, 124);
        var background = box.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.035f, 0.07f, 0.12f, 0.96f);
        background.raycastTarget = false;
        panel.heading = Label("Heading", box, font, "PIANO / 0 SCORE FRAGMENTS", 18, new Color(0.5f, 1f, 0.85f));
        SetRect(panel.heading.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(-32, 26));
        panel.status = Label("Hint", box, font, "Touch a score fragment. Click a collected fragment to replay.", 15, new Color(0.8f, 0.86f, 0.94f));
        SetRect(panel.status.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(-32, 24));
        var row = Rect("Replay Row", box);
        SetRect(row, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-32, 40));
        var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;
        panel.previousButton = Button("Previous", row, font, "<", out _, 40);
        panel.replayButtons = new UnityEngine.UI.Button[4];
        panel.replayLabels = new UnityEngine.UI.Text[4];
        for (int i = 0; i < 4; i++)
            panel.replayButtons[i] = Button("Replay " + (i + 1), row, font, "---", out panel.replayLabels[i], 160);
        panel.nextButton = Button("Next", row, font, ">", out _, 40);
        return go;
    }

    private static UnityEngine.UI.Button Button(string name, Transform parent, Font font, string caption, out UnityEngine.UI.Text label, float width)
    {
        var rect = Rect(name, parent);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.15f, 0.25f, 0.34f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.6f, 1f, 0.85f);
        colors.pressedColor = new Color(0.3f, 0.8f, 0.65f);
        colors.disabledColor = new Color(0.38f, 0.42f, 0.5f, 0.6f);
        button.colors = colors;
        button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        var sizing = rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        sizing.preferredWidth = width;
        sizing.flexibleWidth = width > 40 ? 1 : 0;
        label = Label("Label", rect, font, caption, 17, Color.white);
        label.alignment = TextAnchor.MiddleCenter;
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        return button;
    }

    private static UnityEngine.UI.Text Label(string name, Transform parent, Font font, string text, int size, Color color)
    {
        var rect = Rect(name, parent);
        var label = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        label.font = font;
        label.fontSize = size;
        label.text = text;
        label.color = color;
        label.raycastTarget = false;
        label.alignment = TextAnchor.MiddleLeft;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
