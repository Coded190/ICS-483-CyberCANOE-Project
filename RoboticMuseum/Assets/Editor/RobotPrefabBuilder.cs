using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoboticMuseum.EditorTools
{
    public static class RobotPrefabBuilder
    {
        static readonly string[] ExhibitIds =
        {
            "FrankaArm",
            "Quadruped",
            "MobileManipulator",
            "IndustrialArm",
            "BimanualRobot",
            "Humanoid"
        };

        [MenuItem("RoboticMuseum/Import Robot Exhibits")]
        public static void ImportAndBuildHall()
        {
            foreach (string id in ExhibitIds)
            {
                string meshPath = $"Assets/Robots/{id}/Meshes/{id}.obj";
                if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, meshPath)))
                {
                    throw new FileNotFoundException($"[RobotPrefab] missing mesh path={meshPath}");
                }

                ConfigureImporter(meshPath);
                BuildPrefab(id, meshPath);
            }

            AssetDatabase.SaveAssets();
            StereoTestHallBuilder.Build();
        }

        [MenuItem("RoboticMuseum/Face Exhibits Toward Aisle")]
        public static void FaceExhibitsTowardAisle()
        {
            foreach (string id in ExhibitIds)
            {
                string prefabPath = $"Assets/Robots/{id}/{id}.prefab";
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                root.transform.localRotation = Quaternion.Euler(-90f, 180f, 0f);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"[RobotPrefab] id={id} yaw_deg=180 axis=Y");
            }

            AssetDatabase.SaveAssets();
            StereoTestHallBuilder.Build();
        }

        public static void RebuildBimanual()
        {
            const string id = "BimanualRobot";
            string meshPath = $"Assets/Robots/{id}/Meshes/{id}.obj";
            if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, meshPath)))
            {
                throw new FileNotFoundException($"[RobotPrefab] missing mesh path={meshPath}");
            }

            ConfigureImporter(meshPath);
            BuildPrefab(id, meshPath);
            AssetDatabase.SaveAssets();
            StereoTestHallBuilder.Build();
        }

        static void ConfigureImporter(string meshPath)
        {
            var importer = AssetImporter.GetAtPath(meshPath) as ModelImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(meshPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(meshPath) as ModelImporter;
            }

            if (importer == null)
            {
                throw new InvalidDataException($"[RobotPrefab] OBJ importer missing path={meshPath}");
            }

            importer.globalScale = 1f;
            importer.useFileScale = false;
            importer.bakeAxisConversion = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.SaveAndReimport();
        }

        static void BuildPrefab(string id, string meshPath)
        {
            Mesh mesh = LoadMesh(meshPath);
            var root = new GameObject(id);
            // Rx(-90) stands the Z-up mesh up. Ry(180) turns it from the west wall toward the aisle.
            root.transform.localRotation = Quaternion.Euler(-90f, 180f, 0f);

            var child = new GameObject("Mesh");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SaveMaterial(id, ExhibitColor(id));
            var collider = child.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = mesh.bounds.size;

            string prefabPath = $"Assets/Robots/{id}/{id}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log(
                $"[RobotPrefab] id={id} vertices={mesh.vertexCount} height_m={mesh.bounds.size.z:F3} min_z_m={mesh.bounds.min.z:F3} path={prefabPath}");
        }

        static Mesh LoadMesh(string meshPath)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(meshPath))
            {
                if (asset is Mesh mesh && mesh.vertexCount > 0)
                {
                    return mesh;
                }
            }

            throw new InvalidDataException($"[RobotPrefab] imported mesh has no vertices path={meshPath}");
        }

        static Material SaveMaterial(string id, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new FileNotFoundException("[RobotPrefab] URP Lit shader was not found");
            }

            string path = $"Assets/Robots/{id}/{id}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", color);
            material.color = color;
            material.SetFloat("_Smoothness", 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Color ExhibitColor(string id)
        {
            switch (id)
            {
                case "FrankaArm":
                    return new Color(0.92f, 0.92f, 0.90f);
                case "Quadruped":
                    return new Color(0.16f, 0.17f, 0.16f);
                case "MobileManipulator":
                    return new Color(0.86f, 0.86f, 0.84f);
                case "IndustrialArm":
                    return new Color(0.45f, 0.55f, 0.68f);
                case "BimanualRobot":
                    return new Color(0.12f, 0.12f, 0.13f);
                case "Humanoid":
                    return new Color(0.78f, 0.78f, 0.80f);
                default:
                    return Color.white;
            }
        }
    }
}
