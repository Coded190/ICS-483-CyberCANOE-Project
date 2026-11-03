using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RoboticMuseum.EditorTools
{
    public static class MuseumBuild
    {
        public static void BuildAll()
        {
            ProjectSettings.Apply();
            BuildOne(BuildTarget.StandaloneOSX, "Builds/macOS/RoboticMuseum.app");
            BuildOne(BuildTarget.StandaloneWindows64, "Builds/Windows/RoboticMuseum.exe");
        }

        public static void BuildMac()
        {
            ProjectSettings.Apply();
            BuildOne(BuildTarget.StandaloneOSX, "Builds/macOS/RoboticMuseum.app");
        }

        public static void BuildWindows()
        {
            ProjectSettings.Apply();
            BuildOne(BuildTarget.StandaloneWindows64, "Builds/Windows/RoboticMuseum.exe");
        }

        [MenuItem("RoboticMuseum/Build macOS")]
        static void BuildMacMenu()
        {
            BuildMac();
        }

        [MenuItem("RoboticMuseum/Build Windows x64")]
        static void BuildWindowsMenu()
        {
            BuildWindows();
        }

        static void BuildOne(BuildTarget target, string relativePath)
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
                if (!switched)
                {
                    throw new System.InvalidOperationException($"[MuseumBuild] switch failed target={target}");
                }
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(projectRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/StereoTestHall.unity" },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException(
                    $"[MuseumBuild] failed target={target} result={report.summary.result} errors={report.summary.totalErrors}");
            }

            Debug.Log($"[MuseumBuild] succeeded target={target} path={path} size_bytes={report.summary.totalSize}");
        }
    }
}
