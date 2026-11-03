using UnityEngine;

namespace RoboticMuseum.Stereo
{
    /// <summary>
    /// Two parallel eye cameras plus a presenter that composites mono, side-by-side, or anaglyph.
    /// Keys: 1 mono, 2 side-by-side, 3 anaglyph, [ ] IPD by 1 mm, comma and period convergence by 0.25 m.
    /// </summary>
    public class StereoRig : MonoBehaviour
    {
        public const float IpdStepMeters = 0.001f;
        public const float ConvergenceStepMeters = 0.25f;
        const float MinIpdMeters = 0f;
        const float MaxIpdMeters = 0.100f;
        const float MinConvergenceMeters = 0.50f;
        const float MaxConvergenceMeters = 20f;

        public StereoMode mode = StereoMode.SideBySide;
        public float ipdMeters = StereoFrustum.DefaultIpdMeters;
        public float convergenceMeters = StereoFrustum.DefaultConvergenceMeters;
        public float verticalFovDegrees = StereoFrustum.DefaultVerticalFovDegrees;

        public Camera leftEye;
        public Camera rightEye;
        public Camera centerEye;
        public Camera presenter;
        public Transform presenterQuad;

        RenderTexture leftTexture;
        RenderTexture rightTexture;
        Material presenterMaterial;
        bool loggedStartup;

        public RenderTexture LeftTexture => leftTexture;
        public RenderTexture RightTexture => rightTexture;

        void Awake()
        {
            EnsureTargets();
            ApplyMode(logChange: false);
            LogState("startup");
            loggedStartup = true;
        }

        void LateUpdate()
        {
            ReadAdjustments();
            ApplyEyeLayout();
            FitPresenterQuad();
        }

        void OnDestroy()
        {
            ReleaseTargets();
        }

        public void SetMode(StereoMode next)
        {
            if (mode == next && loggedStartup)
            {
                return;
            }

            mode = next;
            ApplyMode(logChange: true);
        }

        public void ApplyEyeLayout()
        {
            if (leftEye == null || rightEye == null)
            {
                return;
            }

            float aspect = StereoFrustum.EyeAspect;
            LayoutEye(leftEye, leftTexture, true, aspect);
            LayoutEye(rightEye, rightTexture, false, aspect);

            if (centerEye != null)
            {
                centerEye.fieldOfView = verticalFovDegrees;
                centerEye.nearClipPlane = StereoFrustum.DefaultNearMeters;
                centerEye.farClipPlane = StereoFrustum.DefaultFarMeters;
                centerEye.ResetProjectionMatrix();
                centerEye.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        public void ApplyMode(bool logChange)
        {
            EnsureTargets();
            bool stereo = mode != StereoMode.Mono;

            if (leftEye != null)
            {
                leftEye.enabled = stereo;
                leftEye.targetTexture = stereo ? leftTexture : null;
            }

            if (rightEye != null)
            {
                rightEye.enabled = stereo;
                rightEye.targetTexture = stereo ? rightTexture : null;
            }

            if (centerEye != null)
            {
                centerEye.enabled = !stereo;
                centerEye.targetTexture = null;
            }

            if (presenter != null)
            {
                presenter.enabled = stereo;
            }

            if (presenterQuad != null)
            {
                presenterQuad.gameObject.SetActive(stereo);
            }

            if (presenterMaterial != null)
            {
                presenterMaterial.SetTexture("_LeftTex", leftTexture);
                presenterMaterial.SetTexture("_RightTex", rightTexture);
                presenterMaterial.SetFloat("_Mode", mode == StereoMode.Anaglyph ? 2f : 1f);
            }

            ApplyEyeLayout();
            ApplyResolution();
            FitPresenterQuad();

            if (logChange)
            {
                LogState("mode");
            }
        }

        void LayoutEye(Camera eye, RenderTexture target, bool left, float aspect)
        {
            float offset = StereoFrustum.EyeOffsetX(left, ipdMeters);
            eye.transform.localPosition = new Vector3(offset, 0f, 0f);
            eye.transform.localRotation = Quaternion.identity;
            eye.fieldOfView = verticalFovDegrees;
            eye.nearClipPlane = StereoFrustum.DefaultNearMeters;
            eye.farClipPlane = StereoFrustum.DefaultFarMeters;
            eye.aspect = aspect;
            eye.rect = new Rect(0f, 0f, 1f, 1f);
            eye.projectionMatrix = StereoFrustum.ProjectionMatrix(
                offset,
                convergenceMeters,
                verticalFovDegrees,
                aspect,
                eye.nearClipPlane,
                eye.farClipPlane);
            if (mode != StereoMode.Mono)
            {
                eye.targetTexture = target;
            }
        }

        void ReadAdjustments()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                SetMode(StereoMode.Mono);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                SetMode(StereoMode.SideBySide);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                SetMode(StereoMode.Anaglyph);
            }

            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                SetIpd(ipdMeters - IpdStepMeters);
            }
            else if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                SetIpd(ipdMeters + IpdStepMeters);
            }

            if (Input.GetKeyDown(KeyCode.Comma))
            {
                SetConvergence(convergenceMeters - ConvergenceStepMeters);
            }
            else if (Input.GetKeyDown(KeyCode.Period))
            {
                SetConvergence(convergenceMeters + ConvergenceStepMeters);
            }
        }

        void SetIpd(float value)
        {
            float next = Mathf.Clamp(value, MinIpdMeters, MaxIpdMeters);
            if (Mathf.Approximately(next, ipdMeters))
            {
                return;
            }

            ipdMeters = next;
            ApplyEyeLayout();
            LogState("ipd");
        }

        void SetConvergence(float value)
        {
            float next = Mathf.Clamp(value, MinConvergenceMeters, MaxConvergenceMeters);
            if (Mathf.Approximately(next, convergenceMeters))
            {
                return;
            }

            convergenceMeters = next;
            ApplyEyeLayout();
            LogState("convergence");
        }

        void ApplyResolution()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            int width = mode == StereoMode.SideBySide
                ? StereoFrustum.EyeWidthPixels * 2
                : StereoFrustum.EyeWidthPixels;
            int height = StereoFrustum.EyeHeightPixels;
            if (Screen.width == width && Screen.height == height)
            {
                return;
            }

            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            Debug.Log($"[StereoRig] resolution_px={width}x{height} mode={mode}");
        }

        public void FitPresenterQuad()
        {
            if (presenter == null || presenterQuad == null || !presenter.enabled)
            {
                return;
            }

            const float distance = 1f;
            float aspect = presenter.aspect > 0.01f ? presenter.aspect : 16f / 9f;
            float halfHeight = Mathf.Tan(presenter.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            float halfWidth = halfHeight * aspect;
            // Unity's quad already faces the camera. A 180° yaw turns texture U around,
            // so screen-left shows the right side of each eye and look/strafe feel reversed.
            presenterQuad.localPosition = new Vector3(0f, 0f, distance);
            presenterQuad.localRotation = Quaternion.identity;
            presenterQuad.localScale = new Vector3(halfWidth * 2f, halfHeight * 2f, 1f);
        }

        void EnsureTargets()
        {
            if (leftTexture == null)
            {
                leftTexture = CreateEyeTexture("StereoLeftEye");
            }

            if (rightTexture == null)
            {
                rightTexture = CreateEyeTexture("StereoRightEye");
            }

            if (presenterMaterial == null)
            {
                Shader shader = Shader.Find("RoboticMuseum/StereoPresenter");
                if (shader != null)
                {
                    presenterMaterial = new Material(shader);
                    presenterMaterial.name = "StereoPresenter (Runtime)";
                    if (presenterQuad != null)
                    {
                        var renderer = presenterQuad.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.sharedMaterial = presenterMaterial;
                        }
                    }
                }
            }
        }

        static RenderTexture CreateEyeTexture(string textureName)
        {
            var texture = new RenderTexture(
                StereoFrustum.EyeWidthPixels,
                StereoFrustum.EyeHeightPixels,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            texture.name = textureName;
            texture.antiAliasing = 1;
            texture.Create();
            return texture;
        }

        void ReleaseTargets()
        {
            if (leftEye != null)
            {
                leftEye.targetTexture = null;
            }

            if (rightEye != null)
            {
                rightEye.targetTexture = null;
            }

            Release(leftTexture);
            Release(rightTexture);
            leftTexture = null;
            rightTexture = null;

            if (presenterMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(presenterMaterial);
                }
                else
                {
                    DestroyImmediate(presenterMaterial);
                }

                presenterMaterial = null;
            }
        }

        static void Release(RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }

            texture.Release();
            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

        void LogState(string reason)
        {
            Debug.Log(
                $"[StereoRig] {reason} mode={mode} ipd_m={ipdMeters:F4} convergence_m={convergenceMeters:F3} eye_px={StereoFrustum.EyeWidthPixels}x{StereoFrustum.EyeHeightPixels}");
        }
    }
}
