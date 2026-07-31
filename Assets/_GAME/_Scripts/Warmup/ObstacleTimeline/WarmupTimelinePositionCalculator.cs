using UnityEngine;

namespace GameYT.Warmup
{
    /// <summary>
    /// Immutable course-space data shared by runtime playback and Editor preview.
    /// </summary>
    public readonly struct WarmupTimelineCourseFrame
    {
        public WarmupTimelineCourseFrame(Transform courseTransform)
        {
            Origin = courseTransform.position;
            Rotation = courseTransform.rotation;
            Forward = courseTransform.forward.normalized;
            Right = courseTransform.right.normalized;
            LocalToWorldMatrix = courseTransform.localToWorldMatrix;
        }

        public Vector3 Origin { get; }
        public Quaternion Rotation { get; }
        public Vector3 Forward { get; }
        public Vector3 Right { get; }
        public Matrix4x4 LocalToWorldMatrix { get; }
    }

    /// <summary>
    /// Pure timeline positioning rules used by both gameplay and Scene scrub preview.
    /// </summary>
    public static class WarmupTimelinePositionCalculator
    {
        public static Vector3 CalculatePlayerPosition(
            WarmupTimelineCourseFrame courseFrame,
            float elapsedTime,
            float runSpeed)
        {
            float distance =
                Mathf.Max(0f, elapsedTime) * Mathf.Max(0f, runSpeed);
            return courseFrame.Origin + courseFrame.Forward * distance;
        }

        public static Vector3 CalculateObstaclePosition(
            WarmupTimelineCourseFrame courseFrame,
            float encounterTime,
            float runSpeed,
            float courseStartPadding,
            WarmupLane lane,
            float laneWidth,
            Vector3 positionOffset)
        {
            float forwardDistance = Mathf.Max(
                courseStartPadding,
                encounterTime * runSpeed);
            float lateralDistance = (int)lane * laneWidth;

            return courseFrame.Origin +
                   courseFrame.Forward * forwardDistance +
                   courseFrame.Right * lateralDistance +
                   courseFrame.LocalToWorldMatrix.MultiplyVector(
                       positionOffset);
        }

        public static Quaternion CalculateObstacleRotation(
            WarmupTimelineCourseFrame courseFrame,
            Vector3 rotationOffset)
        {
            return courseFrame.Rotation * Quaternion.Euler(rotationOffset);
        }

        public static bool IsObstacleVisible(
            float elapsedTime,
            float encounterTime,
            float visibilityLeadTime,
            float visibilityTailTime,
            bool keepVisible)
        {
            float visibleStart = encounterTime - visibilityLeadTime;
            float visibleEnd = encounterTime + visibilityTailTime;
            return elapsedTime >= visibleStart &&
                   (elapsedTime <= visibleEnd || keepVisible);
        }
    }
}
