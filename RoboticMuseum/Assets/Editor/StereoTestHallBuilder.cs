using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RoboticMuseum.Player;
using RoboticMuseum.Stereo;

namespace RoboticMuseum.EditorTools
{
    public static class StereoTestHallBuilder
    {
        const string ScenePath = "Assets/Scenes/StereoTestHall.unity";
        const string MaterialDir = "Assets/Materials";
        const int PresenterLayer = 8;

        [MenuItem("RoboticMuseum/Build Stereo Test Hall")]
        public static void Build()
        {
            ProjectSettings.Apply();
            EnsureLayer("StereoPresenter", PresenterLayer);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets/Scenes"));
            if (!AssetDatabase.IsValidFolder(MaterialDir))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateHall();
            CreateMarkers();
            CreateStations();
            CreateLight();
            CreatePlayer();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[StereoTestHall] stations=6 length_m=22 width_m=12 ceiling_m=4.00 player_start=(0.00,0.00,0.00) scene=Assets/Scenes/StereoTestHall.unity");
        }

        static void CreateHall()
        {
            var floorMat = SaveLit("Floor", new Color(0.35f, 0.36f, 0.38f));
            var wallMat = SaveLit("Wall", new Color(0.75f, 0.76f, 0.78f));
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Floor";
            box.transform.position = new Vector3(0f, -0.05f, 9f);
            box.transform.localScale = new Vector3(12f, 0.1f, 22f);
            box.GetComponent<Renderer>().sharedMaterial = floorMat;

            CreateWall("WallNorth", new Vector3(0f, 2f, 20.05f), new Vector3(12f, 4f, 0.1f), wallMat);
            CreateWall("WallSouth", new Vector3(0f, 2f, -2.05f), new Vector3(12f, 4f, 0.1f), wallMat);
            CreateWall("WallEast", new Vector3(6.05f, 2f, 9f), new Vector3(0.1f, 4f, 22f), wallMat);
            CreateWall("WallWest", new Vector3(-6.05f, 2f, 9f), new Vector3(0.1f, 4f, 22f), wallMat);
            // Underside is y=4, flush with the wall tops. The slab covers the 0.1 m wall thickness.
            // It does not cast shadows, so the directional light still lights the floor.
            var roof = CreateWall("Roof", new Vector3(0f, 4.05f, 9f), new Vector3(12.2f, 0.1f, 22.2f), wallMat);
            roof.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static GameObject CreateWall(string name, Vector3 position, Vector3 scale, Material material)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = material;
            return wall;
        }

        static void CreateMarkers()
        {
            CreateMarker("MarkerNear", new Vector3(0f, 1.2f, 1.5f), new Color(0.85f, 0.15f, 0.12f), "Near marker\ncloser than 3 m");
            CreateMarker("MarkerZeroParallax", new Vector3(0f, 1.2f, 3f), Color.white, "Zero parallax\nconvergence 3 m");
            CreateMarker("MarkerFar", new Vector3(0f, 1.2f, 8f), new Color(0.15f, 0.35f, 0.9f), "Far marker\nfarther than 3 m");
        }

        static void CreateMarker(string name, Vector3 position, Color color, string label)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * 0.28f;
            marker.GetComponent<Renderer>().sharedMaterial = SaveLit(name, color);
            CreatePlaque(marker.transform, label, new Vector3(0.45f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), 0.7f);
        }

        static void CreateStations()
        {
            var stations = new[]
            {
                new Station("FrankaArm", "Franka Arm", "7-DoF research arm", new Color(0.80f, 0.20f, 0.15f), PrimitiveType.Cylinder, new Vector3(0.35f, 0.7f, 0.35f)),
                new Station("Quadruped", "Quadruped", "Four-legged mobile robot", new Color(0.20f, 0.55f, 0.25f), PrimitiveType.Cube, new Vector3(1.1f, 0.35f, 0.55f)),
                new Station("MobileManipulator", "Mobile Manipulator", "Base plus arm", new Color(0.15f, 0.40f, 0.75f), PrimitiveType.Capsule, new Vector3(0.4f, 0.55f, 0.4f)),
                new Station("IndustrialArm", "Industrial Arm", "Factory robot arm", new Color(0.85f, 0.65f, 0.10f), PrimitiveType.Cylinder, new Vector3(0.45f, 0.9f, 0.45f)),
                new Station("BimanualRobot", "Bimanual Robot", "Two coordinated arms", new Color(0.55f, 0.20f, 0.65f), PrimitiveType.Sphere, new Vector3(0.35f, 0.35f, 0.35f)),
                new Station("Humanoid", "Humanoid", "Two-legged humanoid", new Color(0.85f, 0.85f, 0.88f), PrimitiveType.Capsule, new Vector3(0.45f, 0.85f, 0.45f))
            };

            for (int i = 0; i < stations.Length; i++)
            {
                float z = 2f + i * 3f;
                CreateStation(stations[i], new Vector3(-4f, 0f, z));
            }
        }

        static void CreateStation(Station station, Vector3 position)
        {
            var root = new GameObject(station.Id);
            root.transform.position = position;

            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(root.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            pedestal.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
            pedestal.GetComponent<Renderer>().sharedMaterial = SaveLit("Pedestal", new Color(0.25f, 0.25f, 0.27f));

            var exhibit = GameObject.CreatePrimitive(station.Shape);
            exhibit.name = "Exhibit";
            exhibit.transform.SetParent(root.transform, false);
            exhibit.transform.localScale = station.Scale;
            float top = 0.8f + station.Scale.y * 0.5f;
            exhibit.transform.localPosition = new Vector3(0f, top, 0f);
            exhibit.GetComponent<Renderer>().sharedMaterial = SaveLit(station.Id, station.Color);

            if (station.Id == "BimanualRobot")
            {
                var second = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                second.name = "ExhibitRight";
                second.transform.SetParent(root.transform, false);
                second.transform.localScale = station.Scale;
                second.transform.localPosition = new Vector3(0.45f, top, 0f);
                exhibit.transform.localPosition = new Vector3(-0.45f, top, 0f);
                second.GetComponent<Renderer>().sharedMaterial = exhibit.GetComponent<Renderer>().sharedMaterial;
            }

            CreatePlaque(
                root.transform,
                station.Title + "\n" + station.Blurb,
                new Vector3(1.35f, 1.35f, 0f),
                Quaternion.Euler(0f, -90f, 0f),
                1.4f);
        }

        static void CreatePlaque(Transform parent, string text, Vector3 localPosition, Quaternion localRotation, float widthMeters)
        {
            var canvasObject = new GameObject("Plaque");
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.localPosition = localPosition;
            rect.localRotation = localRotation;
            rect.sizeDelta = new Vector2(900f, 280f);
            rect.localScale = Vector3.one * (widthMeters / 900f);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var image = panel.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);

            var label = new GameObject("Label");
            label.transform.SetParent(panel.transform, false);
            var labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 16f);
            labelRect.offsetMax = new Vector2(-24f, -16f);
            var uiText = label.AddComponent<Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.text = text;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.black;
            uiText.fontSize = 56;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            uiText.resizeTextForBestFit = true;
            uiText.resizeTextMinSize = 24;
            uiText.resizeTextMaxSize = 56;
        }

        static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = Color.white;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static void CreatePlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.7f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.85f, 0f);
            controller.stepOffset = 0.3f;

            var head = new GameObject("Head");
            head.transform.SetParent(player.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            int presenterLayer = LayerMask.NameToLayer("StereoPresenter");
            int hallMask = ~(1 << presenterLayer);

            Camera left = CreateEyeCamera("LeftEye", head.transform, hallMask);
            Camera right = CreateEyeCamera("RightEye", head.transform, hallMask);
            Camera center = CreateEyeCamera("CenterEye", head.transform, hallMask);

            var presenterObject = new GameObject("Presenter");
            presenterObject.transform.SetParent(player.transform, false);
            var presenter = presenterObject.AddComponent<Camera>();
            presenter.clearFlags = CameraClearFlags.SolidColor;
            presenter.backgroundColor = Color.black;
            presenter.cullingMask = 1 << presenterLayer;
            presenter.fieldOfView = 60f;
            presenter.nearClipPlane = 0.01f;
            presenter.farClipPlane = 10f;
            presenter.depth = 10f;
            presenter.allowHDR = false;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PresenterQuad";
            quad.layer = presenterLayer;
            quad.transform.SetParent(presenterObject.transform, false);
            Object.DestroyImmediate(quad.GetComponent<Collider>());

            var motion = player.AddComponent<FirstPersonController>();
            motion.head = head.transform;
            motion.eyeHeightMeters = 1.6f;

            var rig = player.AddComponent<StereoRig>();
            rig.leftEye = left;
            rig.rightEye = right;
            rig.centerEye = center;
            rig.presenter = presenter;
            rig.presenterQuad = quad.transform;
            rig.mode = StereoMode.SideBySide;
            rig.ipdMeters = StereoFrustum.DefaultIpdMeters;
            rig.convergenceMeters = StereoFrustum.DefaultConvergenceMeters;
        }

        static Camera CreateEyeCamera(string name, Transform parent, int cullingMask)
        {
            var cameraObject = new GameObject(name);
            cameraObject.transform.SetParent(parent, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = StereoFrustum.DefaultVerticalFovDegrees;
            camera.nearClipPlane = StereoFrustum.DefaultNearMeters;
            camera.farClipPlane = StereoFrustum.DefaultFarMeters;
            camera.cullingMask = cullingMask;
            camera.allowHDR = false;
            camera.depth = 0f;
            return camera;
        }

        static Material SaveLit(string name, Color color)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader);
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void EnsureLayer(string name, int index)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                return;
            }

            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            var slot = layers.GetArrayElementAtIndex(index);
            if (slot.stringValue == name)
            {
                return;
            }

            if (!string.IsNullOrEmpty(slot.stringValue))
            {
                throw new System.InvalidOperationException($"[StereoTestHall] layer slot {index} is already '{slot.stringValue}'");
            }

            slot.stringValue = name;
            tagManager.ApplyModifiedProperties();
        }

        struct Station
        {
            public string Id;
            public string Title;
            public string Blurb;
            public Color Color;
            public PrimitiveType Shape;
            public Vector3 Scale;

            public Station(string id, string title, string blurb, Color color, PrimitiveType shape, Vector3 scale)
            {
                Id = id;
                Title = title;
                Blurb = blurb;
                Color = color;
                Shape = shape;
                Scale = scale;
            }
        }
    }

    public static class ProjectSettings
    {
        public static void Apply()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            PlayerSettings.companyName = "ICS685";
            PlayerSettings.productName = "RoboticMuseum";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = StereoFrustum.EyeWidthPixels;
            PlayerSettings.defaultScreenHeight = StereoFrustum.EyeHeightPixels;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Debug.Log("[ProjectSettings] serialization=ForceText vcs=VisibleMetaFiles backend=Mono color_space=Linear input=Both");
        }
    }
}
