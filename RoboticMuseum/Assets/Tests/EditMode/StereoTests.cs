using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoboticMuseum.Stereo;

namespace RoboticMuseum.Tests
{
    public class StereoFrustumTests
    {
        const float Aspect = 1280f / 720f;
        const float Fov = StereoFrustum.DefaultVerticalFovDegrees;
        const float Near = StereoFrustum.DefaultNearMeters;
        const float Far = StereoFrustum.DefaultFarMeters;
        const float Convergence = StereoFrustum.DefaultConvergenceMeters;
        const float Ipd = StereoFrustum.DefaultIpdMeters;
        const float Pixels = StereoFrustum.EyeWidthPixels;

        [Test]
        public void EyeSeparationEqualsIpd()
        {
            float left = StereoFrustum.EyeOffsetX(true, Ipd);
            float right = StereoFrustum.EyeOffsetX(false, Ipd);
            Assert.AreEqual(Ipd, Mathf.Abs(right - left), 1e-6f);
            Assert.Less(left, 0f);
            Assert.Greater(right, 0f);
        }

        [Test]
        public void ParallelEyesStayParallel()
        {
            var head = new GameObject("Head");
            var leftObject = new GameObject("LeftEye");
            var rightObject = new GameObject("RightEye");
            leftObject.transform.SetParent(head.transform, false);
            rightObject.transform.SetParent(head.transform, false);
            var left = leftObject.AddComponent<Camera>();
            var right = rightObject.AddComponent<Camera>();
            var rigObject = new GameObject("Rig");
            var rig = rigObject.AddComponent<StereoRig>();
            rig.leftEye = left;
            rig.rightEye = right;
            rig.ipdMeters = Ipd;
            rig.convergenceMeters = Convergence;
            head.transform.rotation = Quaternion.Euler(18f, -25f, 0f);

            try
            {
                rig.ApplyEyeLayout();
                float separation = Vector3.Distance(left.transform.position, right.transform.position);
                float alignment = Vector3.Dot(left.transform.forward, right.transform.forward);
                Assert.AreEqual(Ipd, separation, 1e-4f, "IPD is the distance between the eye cameras in meters.");
                Assert.Greater(alignment, 0.9999f, "Eye forward vectors stay parallel. Toe-in is not used.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rigObject);
                UnityEngine.Object.DestroyImmediate(head);
            }
        }

        [Test]
        public void ConvergencePointHasZeroDisparity()
        {
            float disparity = Disparity(new Vector3(0f, 0f, Convergence));
            Assert.AreEqual(0f, disparity, 0.5f);
        }

        [Test]
        public void NearPointHasPositiveCrossedDisparity()
        {
            float disparity = Disparity(new Vector3(0f, 0f, Convergence * 0.5f));
            Assert.Greater(disparity, 0f);
        }

        [Test]
        public void FarPointHasNegativeUncrossedDisparity()
        {
            float disparity = Disparity(new Vector3(0f, 0f, Convergence * 2f));
            Assert.Less(disparity, 0f);
        }

        [Test]
        public void ZeroIpdMakesBothProjectionsMatch()
        {
            Vector3[] points =
            {
                new Vector3(0f, 0f, 1.5f),
                new Vector3(0.4f, -0.2f, 3f),
                new Vector3(-1f, 0.5f, 8f)
            };
            foreach (Vector3 point in points)
            {
                float left = StereoFrustum.ProjectViewportX(
                    point, 0f, Convergence, Fov, Aspect, Near, Far, Pixels);
                float right = StereoFrustum.ProjectViewportX(
                    point, 0f, Convergence, Fov, Aspect, Near, Far, Pixels);
                Assert.AreEqual(left, right, 1e-4f);
                float disparity = StereoFrustum.DisparityPixels(
                    point, 0f, Convergence, Fov, Aspect, Near, Far, Pixels);
                Assert.AreEqual(0f, disparity, 1e-4f);
            }
        }

        [Test]
        public void UnityFrustumMatrixMatchesViewportMath()
        {
            Vector3 point = new Vector3(0.2f, -0.1f, 4f);
            foreach (bool leftEye in new[] { true, false })
            {
                float eye = StereoFrustum.EyeOffsetX(leftEye, Ipd);
                float expected = StereoFrustum.ProjectViewportX(
                    point, eye, Convergence, Fov, Aspect, Near, Far, Pixels);
                Matrix4x4 projection = StereoFrustum.ProjectionMatrix(
                    eye, Convergence, Fov, Aspect, Near, Far);
                Vector3 view = new Vector3(point.x - eye, point.y, -point.z);
                Vector4 clip = projection * new Vector4(view.x, view.y, view.z, 1f);
                float ndcX = clip.x / clip.w;
                float actual = (ndcX * 0.5f + 0.5f) * Pixels;
                Assert.AreEqual(expected, actual, 0.5f);
            }
        }

        static float Disparity(Vector3 headPoint)
        {
            return StereoFrustum.DisparityPixels(
                headPoint, Ipd, Convergence, Fov, Aspect, Near, Far, Pixels);
        }

        [Test]
        public void PresenterQuadMapsScreenRightAndUpToTextureRightAndUp()
        {
            var presenterObject = new GameObject("Presenter");
            var presenter = presenterObject.AddComponent<Camera>();
            presenter.enabled = true;
            presenter.fieldOfView = 60f;
            presenter.aspect = Aspect;
            var quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.transform.SetParent(presenterObject.transform, false);
            var rigObject = new GameObject("Rig");
            var rig = rigObject.AddComponent<StereoRig>();
            rig.presenter = presenter;
            rig.presenterQuad = quadObject.transform;
            rig.mode = StereoMode.SideBySide;

            try
            {
                rig.FitPresenterQuad();
                Vector3 planeCenter = presenter.transform.position + presenter.transform.forward * quadObject.transform.localPosition.z;
                float rightU = QuadUv(quadObject.transform, planeCenter + presenter.transform.right * 0.2f).x;
                float upV = QuadUv(quadObject.transform, planeCenter + presenter.transform.up * 0.2f).y;
                float centerU = QuadUv(quadObject.transform, planeCenter).x;
                Assert.AreEqual(0.5f, centerU, 0.02f, "The view center stays at texture U = 0.5.");
                Assert.Greater(rightU, 0.5f, "Screen right must sample the right side of the eye texture. A mirrored quad reverses look and strafe.");
                Assert.Greater(upV, 0.5f, "Screen up must sample the top of the eye texture.");
                Assert.Greater(quadObject.transform.localScale.x, 0f);
                Debug.Log($"[StereoProof] presenter_uv right_u={rightU:F3} up_v={upV:F3} center_u={centerU:F3}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rigObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        static Vector2 QuadUv(Transform quad, Vector3 worldPoint)
        {
            Vector3 meshLocal = quad.InverseTransformPoint(worldPoint);
            return new Vector2(meshLocal.x + 0.5f, meshLocal.y + 0.5f);
        }
    }

    public static class StereoProofCapture
    {
        const string ScenePath = "Assets/Scenes/StereoTestHall.unity";

        [MenuItem("RoboticMuseum/Capture Stereo Proof")]
        public static void CaptureFromMenu()
        {
            string dir = CaptureHall(ArtifactsDir());
            Debug.Log($"[StereoProof] wrote dir={dir}");
        }

        public static string ArtifactsDir()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, "Artifacts");
        }

        public static string CaptureHall(string directory)
        {
            Directory.CreateDirectory(directory);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StereoRig rig = UnityEngine.Object.FindFirstObjectByType<StereoRig>();
            Assert.IsNotNull(rig, "StereoTestHall is missing StereoRig.");
            rig.mode = StereoMode.SideBySide;
            rig.ipdMeters = StereoFrustum.DefaultIpdMeters;
            rig.convergenceMeters = StereoFrustum.DefaultConvergenceMeters;
            rig.ApplyMode(logChange: false);

            Texture2D left = RenderEye(rig.leftEye, rig.LeftTexture);
            Texture2D right = RenderEye(rig.rightEye, rig.RightTexture);
            Texture2D sideBySide = CompositeSideBySide(left, right);
            Texture2D anaglyph = CompositeAnaglyph(left, right);

            WritePng(left, Path.Combine(directory, "left-eye.png"));
            WritePng(right, Path.Combine(directory, "right-eye.png"));
            WritePng(sideBySide, Path.Combine(directory, "side-by-side.png"));
            WritePng(anaglyph, Path.Combine(directory, "anaglyph.png"));

            int difference = ChannelDifference(left, right);
            bool leftRed = ContainsRedMarker(left);
            bool rightRed = ContainsRedMarker(right);
            Debug.Log($"[StereoProof] hall eye_channel_difference={difference} left_red_marker={leftRed} right_red_marker={rightRed} dir={directory}");
            Assert.Greater(difference, 1000, "Left and right hall renders should differ.");
            Assert.IsTrue(leftRed, "Left eye should show the red near marker. A gold frame means the first URP pass was captured.");
            Assert.IsTrue(rightRed, "Right eye should show the red near marker.");

            UnityEngine.Object.DestroyImmediate(left);
            UnityEngine.Object.DestroyImmediate(right);
            UnityEngine.Object.DestroyImmediate(sideBySide);
            UnityEngine.Object.DestroyImmediate(anaglyph);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return directory;
        }

        public static void AssertSyntheticDisparity()
        {
            float zero = CentroidDisparity(StereoFrustum.DefaultConvergenceMeters, Color.white);
            float near = CentroidDisparity(StereoFrustum.DefaultConvergenceMeters * 0.5f, Color.white);
            Debug.Log($"[StereoProof] synthetic zero_disparity_px={zero:F2} near_disparity_px={near:F2}");
            Assert.AreEqual(0f, zero, 3f, "Centroid disparity at the convergence distance.");
            Assert.Greater(near, 2f, "A nearer quad has positive crossed disparity.");
        }

        static float CentroidDisparity(float distance, Color color)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StereoRig rig = UnityEngine.Object.FindFirstObjectByType<StereoRig>();
            Assert.IsNotNull(rig, "StereoTestHall is missing StereoRig.");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.IsNotNull(shader, "URP Lit shader was not found.");
            foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                renderer.enabled = false;
            }

            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Transform head = rig.leftEye.transform.parent;
            marker.transform.position = head.position + head.forward * distance;
            marker.transform.localScale = Vector3.one * 0.35f;
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            marker.GetComponent<Renderer>().sharedMaterial = material;

            rig.mode = StereoMode.SideBySide;
            rig.ipdMeters = StereoFrustum.DefaultIpdMeters;
            rig.convergenceMeters = StereoFrustum.DefaultConvergenceMeters;
            rig.leftEye.clearFlags = CameraClearFlags.SolidColor;
            rig.rightEye.clearFlags = CameraClearFlags.SolidColor;
            rig.leftEye.backgroundColor = Color.black;
            rig.rightEye.backgroundColor = Color.black;
            try
            {
                rig.ApplyMode(logChange: false);
                Texture2D leftImage = RenderEye(rig.leftEye, rig.LeftTexture);
                Texture2D rightImage = RenderEye(rig.rightEye, rig.RightTexture);
                float leftX = BrightCentroidX(leftImage);
                float rightX = BrightCentroidX(rightImage);
                Debug.Log($"[StereoProof] marker distance_m={distance:F3} left_x_px={leftX:F2} right_x_px={rightX:F2}");
                UnityEngine.Object.DestroyImmediate(leftImage);
                UnityEngine.Object.DestroyImmediate(rightImage);
                return leftX - rightX;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        static Texture2D RenderEye(Camera camera, RenderTexture texture)
        {
            camera.enabled = true;
            camera.targetTexture = texture;
            // The first URP Camera.Render in a batch session draws lit materials with a fallback color.
            camera.Render();
            camera.Render();
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            return image;
        }

        static bool ContainsRedMarker(Texture2D image)
        {
            Color32[] pixels = image.GetPixels32();
            for (int i = 0; i < pixels.Length; i += 4)
            {
                if (pixels[i].r > 180 && pixels[i].g < 100 && pixels[i].b < 100)
                {
                    return true;
                }
            }

            return false;
        }

        static float BrightCentroidX(Texture2D image)
        {
            Color32[] pixels = image.GetPixels32();
            double sumX = 0.0;
            double sumW = 0.0;
            int width = image.width;
            for (int i = 0; i < pixels.Length; i++)
            {
                float luma = (pixels[i].r + pixels[i].g + pixels[i].b) / (3f * 255f);
                if (luma < 0.5f)
                {
                    continue;
                }

                sumX += (i % width) * luma;
                sumW += luma;
            }

            Assert.Greater(sumW, 10.0, "The marker quad did not render.");
            return (float)(sumX / sumW);
        }

        static Texture2D CompositeSideBySide(Texture2D left, Texture2D right)
        {
            var image = new Texture2D(left.width * 2, left.height, TextureFormat.RGB24, false);
            image.SetPixels(0, 0, left.width, left.height, left.GetPixels());
            image.SetPixels(left.width, 0, right.width, right.height, right.GetPixels());
            image.Apply();
            return image;
        }

        static Texture2D CompositeAnaglyph(Texture2D left, Texture2D right)
        {
            Color[] leftPixels = left.GetPixels();
            Color[] rightPixels = right.GetPixels();
            var output = new Color[leftPixels.Length];
            for (int i = 0; i < leftPixels.Length; i++)
            {
                float leftLuma = leftPixels[i].r * 0.299f + leftPixels[i].g * 0.587f + leftPixels[i].b * 0.114f;
                float rightLuma = rightPixels[i].r * 0.299f + rightPixels[i].g * 0.587f + rightPixels[i].b * 0.114f;
                output[i] = new Color(leftLuma, rightLuma, rightLuma, 1f);
            }

            var image = new Texture2D(left.width, left.height, TextureFormat.RGB24, false);
            image.SetPixels(output);
            image.Apply();
            return image;
        }

        static int ChannelDifference(Texture2D left, Texture2D right)
        {
            Color32[] a = left.GetPixels32();
            Color32[] b = right.GetPixels32();
            int difference = 0;
            int step = Math.Max(1, a.Length / 20000);
            for (int i = 0; i < a.Length; i += step)
            {
                difference += Math.Abs(a[i].r - b[i].r);
                difference += Math.Abs(a[i].g - b[i].g);
                difference += Math.Abs(a[i].b - b[i].b);
            }

            return difference;
        }

        static void WritePng(Texture2D image, string path)
        {
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log($"[StereoProof] png path={path} bytes={new FileInfo(path).Length} width_px={image.width} height_px={image.height}");
        }
    }

    public class StereoImageProofTests
    {
        [Test]
        public void HallEyesDifferAndProofImagesExist()
        {
            string dir = StereoProofCapture.CaptureHall(StereoProofCapture.ArtifactsDir());
            Assert.IsTrue(File.Exists(Path.Combine(dir, "left-eye.png")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "right-eye.png")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "side-by-side.png")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "anaglyph.png")));
        }

        [Test]
        public void SyntheticMarkerDisparityMatchesDefinition()
        {
            StereoProofCapture.AssertSyntheticDisparity();
        }
    }
}
