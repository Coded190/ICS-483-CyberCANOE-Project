using UnityEngine;

namespace RoboticMuseum.Stereo
{
    /// <summary>
    /// Off-axis stereo for one head. Eyes stay parallel and look along head +Z.
    /// A point on the head forward axis at <see cref="convergence"/> projects to the
    /// same viewport x in both eyes. Disparity is leftViewportX minus rightViewportX:
    /// 0 on the convergence plane, positive (crossed) when the point is closer,
    /// negative (uncrossed) when it is farther.
    /// </summary>
    public static class StereoFrustum
    {
        public const float DefaultIpdMeters = 0.064f;
        public const float DefaultConvergenceMeters = 3f;
        public const float DefaultVerticalFovDegrees = 60f;
        public const float DefaultNearMeters = 0.05f;
        public const float DefaultFarMeters = 100f;
        public const int EyeWidthPixels = 1280;
        public const int EyeHeightPixels = 720;

        public static float EyeAspect => (float)EyeWidthPixels / EyeHeightPixels;

        public static float EyeOffsetX(bool leftEye, float ipdMeters)
        {
            return (leftEye ? -0.5f : 0.5f) * ipdMeters;
        }

        public static void NearPlaneBounds(
            float eyeOffsetX,
            float convergenceMeters,
            float verticalFovDegrees,
            float aspect,
            float nearMeters,
            out float left,
            out float right,
            out float bottom,
            out float top)
        {
            top = nearMeters * Mathf.Tan(verticalFovDegrees * 0.5f * Mathf.Deg2Rad);
            bottom = -top;
            float halfWidth = aspect * top;
            float center = -eyeOffsetX * nearMeters / convergenceMeters;
            left = -halfWidth + center;
            right = halfWidth + center;
        }

        public static Matrix4x4 ProjectionMatrix(
            float eyeOffsetX,
            float convergenceMeters,
            float verticalFovDegrees,
            float aspect,
            float nearMeters,
            float farMeters)
        {
            NearPlaneBounds(
                eyeOffsetX,
                convergenceMeters,
                verticalFovDegrees,
                aspect,
                nearMeters,
                out float left,
                out float right,
                out float bottom,
                out float top);
            return Matrix4x4.Frustum(left, right, bottom, top, nearMeters, farMeters);
        }

        /// <summary>
        /// Viewport x in pixels for a head-space point. x is 0 at the left edge of this eye.
        /// Head space is x-right, y-up, z-forward. The eye is at (eyeOffsetX, 0, 0).
        /// </summary>
        public static float ProjectViewportX(
            Vector3 headPoint,
            float eyeOffsetX,
            float convergenceMeters,
            float verticalFovDegrees,
            float aspect,
            float nearMeters,
            float farMeters,
            float pixelWidth)
        {
            NearPlaneBounds(
                eyeOffsetX,
                convergenceMeters,
                verticalFovDegrees,
                aspect,
                nearMeters,
                out float left,
                out float right,
                out _,
                out _);
            float xAtNear = (headPoint.x - eyeOffsetX) * nearMeters / headPoint.z;
            float ndcX = (2f * (xAtNear - left) / (right - left)) - 1f;
            return (ndcX * 0.5f + 0.5f) * pixelWidth;
        }

        public static float DisparityPixels(
            Vector3 headPoint,
            float ipdMeters,
            float convergenceMeters,
            float verticalFovDegrees,
            float aspect,
            float nearMeters,
            float farMeters,
            float pixelWidth)
        {
            float left = ProjectViewportX(
                headPoint,
                EyeOffsetX(true, ipdMeters),
                convergenceMeters,
                verticalFovDegrees,
                aspect,
                nearMeters,
                farMeters,
                pixelWidth);
            float right = ProjectViewportX(
                headPoint,
                EyeOffsetX(false, ipdMeters),
                convergenceMeters,
                verticalFovDegrees,
                aspect,
                nearMeters,
                farMeters,
                pixelWidth);
            return left - right;
        }
    }
}
