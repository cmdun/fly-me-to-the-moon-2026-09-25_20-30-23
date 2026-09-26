using System;
using FlyMeToTheMoon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PrototypeSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/FirstPlayablePrototype.unity";
    private const string AssetFolder = "Assets/Prototype";

    private static readonly (string id, float angle, float radius, float gap, Color outline, Color surface)[] Moons =
    {
        ("Moon", 0f, 3f, 6.5f, new Color(0.78f, 0.74f, 1f), new Color(0.23f, 0.19f, 0.38f)),
        ("Moon 2", 60f, 2.5f, 6f, new Color(1f, 0.72f, 0.55f), new Color(0.42f, 0.2f, 0.16f)),
        ("Moon 3", 135f, 3.5f, 7f, new Color(0.6f, 0.9f, 1f), new Color(0.13f, 0.25f, 0.42f)),
        ("Moon 4", 215f, 2f, 5.5f, new Color(1f, 0.9f, 0.5f), new Color(0.4f, 0.33f, 0.12f)),
        ("Moon 5", 290f, 2.75f, 6.5f, new Color(0.95f, 0.6f, 0.85f), new Color(0.35f, 0.15f, 0.33f)),
    };

    [MenuItem("Tools/Fly Me to the Moon/Play Prototype")]
    public static void Play()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Fly Me to the Moon/Create Prototype Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building the scene.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("The prototype already exists. Open it instead; this command never overwrites a scene.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (DetectPipeline() != "URP") throw new InvalidOperationException("The prototype requires the approved URP pipeline.");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var shader = Shader.Find("FlyMeToTheMoon/FlatColor");
        if (shader == null) throw new InvalidOperationException("FlatColor shader has not imported.");
        var material = new Material(shader);
        AssetDatabase.CreateAsset(material, AssetFolder + "/FlatColor.mat");

        var planetTemplate = new GameObject("Planet");
        var bodyTemplate = planetTemplate.AddComponent<GravityBody>();
        bodyTemplate.GetComponent<CircleCollider2D>().radius = bodyTemplate.radius;
        AddShape(planetTemplate.transform, "Outline", Vector3.zero, Vector2.one * 12f,
            PrototypeShape.Shape.Circle, new Color(0.35f, 0.8f, 0.75f), material);
        AddShape(planetTemplate.transform, "Surface", new Vector3(0f, 0f, -0.02f), Vector2.one * 11.8f,
            PrototypeShape.Shape.Circle, new Color(0.08f, 0.24f, 0.3f), material);
        var planetPrefab = PrefabUtility.SaveAsPrefabAsset(planetTemplate, AssetFolder + "/Planet.prefab");
        UnityEngine.Object.DestroyImmediate(planetTemplate);
        var home = MakePlanet(planetPrefab, "612-B", Vector2.zero, 12f, material, null);
        var bodies = new System.Collections.Generic.List<GravityBody> { home };
        foreach (var (id, angle, radius, gap, outline, surface) in Moons)
        {
            // Angle is clockwise from straight up; gap is the distance between the two surfaces.
            float distance = home.radius + gap + radius;
            var center = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * distance;
            bodies.Add(MakePlanet(planetPrefab, id, center, radius, material, (outline, surface)));
        }

        var systems = new GameObject("Prototype Systems");
        var gravity = systems.AddComponent<GravityManager>();
        gravity.bodies = bodies.ToArray();

        var playerTemplate = new GameObject("Player");
        playerTemplate.AddComponent<Rigidbody2D>().gravityScale = 0;
        playerTemplate.AddComponent<CircleCollider2D>().radius = 0.28f;
        var notes = playerTemplate.AddComponent<NoteBurst>();
        notes.material = material;
        var controller = playerTemplate.AddComponent<PlayerController>();
        controller.noteBurst = notes;
        controller.inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
        AddShape(playerTemplate.transform, "Outline", new Vector3(0, 0, -0.15f), new Vector2(0.5f, 0.55f),
            PrototypeShape.Shape.Rectangle, new Color(1f, 0.85f, 0.48f), material);
        AddShape(playerTemplate.transform, "Body", new Vector3(0, 0, -0.17f), new Vector2(0.36f, 0.41f),
            PrototypeShape.Shape.Rectangle, new Color(0.95f, 0.45f, 0.36f), material);
        AddShape(playerTemplate.transform, "Visor", new Vector3(0, 0.09f, -0.19f), new Vector2(0.27f, 0.09f),
            PrototypeShape.Shape.Rectangle, new Color(0.08f, 0.13f, 0.22f), material);
        AddShape(playerTemplate.transform, "Flute", new Vector3(0.3f, -0.05f, -0.21f), new Vector2(0.35f, 0.07f),
            PrototypeShape.Shape.Rectangle, new Color(0.4f, 1f, 0.85f), material);
        var playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerTemplate, AssetFolder + "/Player.prefab");
        UnityEngine.Object.DestroyImmediate(playerTemplate);
        var player = ((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab)).GetComponent<PlayerController>();
        player.gravityManager = gravity;
        player.transform.position = new Vector3(0f, 12.3f, 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);

        var cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.625f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.04f, 0.09f);
        camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        data.antialiasing = AntialiasingMode.None;
        var pixel = cameraObject.AddComponent<PixelPerfectCamera>();
        pixel.assetsPPU = 16;
        pixel.refResolutionX = 320;
        pixel.refResolutionY = 180;
        pixel.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        pixel.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        var follow = cameraObject.AddComponent<CameraController>();
        follow.player = player.transform;
        follow.SnapToPlayer();

        var planets = systems.AddComponent<PlanetManager>();
        planets.player = player;
        planets.gravityManager = gravity;
        planets.respawnPlanet = home;
        planets.cameraController = follow;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Prototype scene created: " + ScenePath);
    }

    private static string DetectPipeline()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        return pipeline != null && pipeline.GetType().FullName.Contains("Universal") ? "URP" : "Other";
    }

    private static GravityBody MakePlanet(GameObject prefab, string id, Vector2 center, float radius,
        Material material, (Color outline, Color surface)? colors)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = id;
        go.transform.position = center;
        var body = go.GetComponent<GravityBody>();
        body.planetId = id;
        body.radius = radius;
        go.GetComponent<CircleCollider2D>().radius = radius;
        foreach (var shape in go.GetComponentsInChildren<PrototypeShape>())
        {
            bool outline = shape.name == "Outline";
            shape.size = Vector2.one * (radius * 2f - (outline ? 0f : 0.2f));
            shape.material = material;
            if (colors.HasValue) shape.color = outline ? colors.Value.outline : colors.Value.surface;
            shape.Rebuild();
            PrefabUtility.RecordPrefabInstancePropertyModifications(shape);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<CircleCollider2D>());
        return body;
    }

    private static void AddShape(Transform parent, string name, Vector3 position, Vector2 size,
        PrototypeShape.Shape kind, Color color, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        var shape = go.AddComponent<PrototypeShape>();
        shape.shape = kind;
        shape.size = size;
        shape.color = color;
        shape.material = material;
        shape.Rebuild();
    }
}
