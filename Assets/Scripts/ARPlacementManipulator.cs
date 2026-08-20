using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace UnityEngine.XR.Templates.AR
{
    /// <summary>
    /// Handles movement and rotation of the currently previewed object.
    ///
    /// One finger:
    ///     Move the object horizontally.
    ///
    /// Two fingers:
    ///     Rotate the object.
    ///
    /// The object is kept at a specified absolute height while previewing.
    /// This allows stacked objects to stay above the highest confirmed object.
    /// </summary>
    public class ARPlacementManipulator : MonoBehaviour
    {
        [Header("Movement")]

        [SerializeField]
        [Tooltip("Movement sensitivity.")]
        float m_MoveSensitivity = 0.0015f;

        [Header("Rotation")]

        [SerializeField]
        [Tooltip("Two finger rotation multiplier.")]
        float m_RotationSensitivity = 1f;

        Camera m_Camera;
        GameObject m_Object;
        GameObject m_Platform;

        bool m_Manipulating;
        Vector2 m_LastTouchPosition;
        float m_CurrentYaw;

        // Absolute projection along platform up that the object's lowest
        // mesh/collider point must stay at.
        float m_TargetBottomHeight;

        public void BeginPlacement(
            GameObject objectToManipulate,
            GameObject platform,
            float targetBottomHeight)
        {
            m_Object = objectToManipulate;
            m_Platform = platform;
            m_TargetBottomHeight = targetBottomHeight;

            m_Camera = Camera.main;

            if (m_Object != null)
                m_CurrentYaw = m_Object.transform.eulerAngles.y;

            m_Manipulating = false;
        }

        public void StopPlacement()
        {
            m_Object = null;
            m_Platform = null;
            m_Manipulating = false;
        }

        void Awake()
        {
            m_Camera = Camera.main;
        }

        void Update()
        {
            if (m_Object == null || m_Platform == null)
                return;

            if (m_Camera == null)
                m_Camera = Camera.main;

            if (m_Camera == null)
                return;

            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen == null)
                return;

            int activeTouches = GetActiveTouchCount(touchscreen);

            if (activeTouches == 1)
            {
                HandleOneFingerMove(touchscreen);
            }
            else if (activeTouches >= 2)
            {
                HandleTwoFingerRotation(touchscreen);
            }
            else
            {
                m_Manipulating = false;
            }

            KeepObjectAtTargetHeight();
        }

        int GetActiveTouchCount(Touchscreen touchscreen)
        {
            int count = 0;

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (touch.press.isPressed)
                    count++;
            }

            return count;
        }

        TouchControl GetFirstActiveTouch(Touchscreen touchscreen)
        {
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (touch.press.isPressed)
                    return touch;
            }

            return null;
        }

        void HandleOneFingerMove(Touchscreen touchscreen)
        {
            TouchControl touch = GetFirstActiveTouch(touchscreen);

            if (touch == null)
            {
                m_Manipulating = false;
                return;
            }

            Vector2 currentPosition = touch.position.ReadValue();

            if (!m_Manipulating)
            {
                m_LastTouchPosition = currentPosition;
                m_Manipulating = true;
                return;
            }

            Vector2 delta =
                currentPosition - m_LastTouchPosition;

            m_LastTouchPosition = currentPosition;

            Vector3 platformUp =
                m_Platform.transform.up.normalized;

            Vector3 cameraForward =
                Vector3.ProjectOnPlane(
                    m_Camera.transform.forward,
                    platformUp);

            Vector3 cameraRight =
                Vector3.ProjectOnPlane(
                    m_Camera.transform.right,
                    platformUp);

            if (cameraForward.sqrMagnitude < 0.0001f)
                cameraForward = Vector3.forward;

            if (cameraRight.sqrMagnitude < 0.0001f)
                cameraRight = Vector3.right;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 movement =
                cameraRight * delta.x +
                cameraForward * delta.y;

            movement *= m_MoveSensitivity;

            movement =
                Vector3.ProjectOnPlane(
                    movement,
                    platformUp);

            m_Object.transform.position += movement;
        }

        void HandleTwoFingerRotation(Touchscreen touchscreen)
        {
            TouchControl first = null;
            TouchControl second = null;

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                    continue;

                if (first == null)
                    first = touch;
                else
                {
                    second = touch;
                    break;
                }
            }

            if (first == null || second == null)
                return;

            Vector2 firstPosition = first.position.ReadValue();
            Vector2 secondPosition = second.position.ReadValue();

            Vector2 firstDelta = first.delta.ReadValue();
            Vector2 secondDelta = second.delta.ReadValue();

            Vector2 previousFirst =
                firstPosition - firstDelta;

            Vector2 previousSecond =
                secondPosition - secondDelta;

            Vector2 previousVector =
                previousSecond - previousFirst;

            Vector2 currentVector =
                secondPosition - firstPosition;

            if (previousVector.sqrMagnitude < 0.001f ||
                currentVector.sqrMagnitude < 0.001f)
            {
                return;
            }

            float angle =
                Vector2.SignedAngle(
                    previousVector,
                    currentVector);

            m_CurrentYaw +=
                angle * m_RotationSensitivity;

            Vector3 platformUp =
                m_Platform.transform.up.normalized;

            Quaternion rotation =
                Quaternion.AngleAxis(
                    m_CurrentYaw,
                    platformUp);

            m_Object.transform.rotation = rotation;

            m_Manipulating = true;
        }

        void KeepObjectAtTargetHeight()
        {
            if (m_Object == null || m_Platform == null)
                return;

            Vector3 platformUp =
                m_Platform.transform.up.normalized;

            float objectBottom =
                GetLowestPoint(
                    m_Object,
                    platformUp);

            if (float.IsPositiveInfinity(objectBottom))
                return;

            float correction =
                m_TargetBottomHeight -
                objectBottom;

            m_Object.transform.position +=
                platformUp * correction;
        }

        float GetLowestPoint(
            GameObject target,
            Vector3 axis)
        {
            float lowest =
                float.PositiveInfinity;

            bool found = false;

            MeshFilter[] meshFilters =
                target.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter == null ||
                    meshFilter.sharedMesh == null)
                {
                    continue;
                }

                Vector3[] vertices =
                    meshFilter.sharedMesh.vertices;

                Transform meshTransform =
                    meshFilter.transform;

                foreach (Vector3 localVertex in vertices)
                {
                    Vector3 worldVertex =
                        meshTransform.TransformPoint(localVertex);

                    float projection =
                        Vector3.Dot(worldVertex, axis);

                    lowest =
                        Mathf.Min(
                            lowest,
                            projection);

                    found = true;
                }
            }

            // Fallback for objects that do not contain MeshFilters.
            if (!found)
            {
                Collider[] colliders =
                    target.GetComponentsInChildren<Collider>(true);

                foreach (Collider collider in colliders)
                {
                    if (collider == null || !collider.enabled)
                        continue;

                    CheckBounds(
                        collider.bounds,
                        axis,
                        ref lowest,
                        false);

                    found = true;
                }
            }

            if (!found)
            {
                Renderer[] renderers =
                    target.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled)
                        continue;

                    CheckBounds(
                        renderer.bounds,
                        axis,
                        ref lowest,
                        false);

                    found = true;
                }
            }

            return found
                ? lowest
                : float.PositiveInfinity;
        }

        static void CheckBounds(
            Bounds bounds,
            Vector3 axis,
            ref float value,
            bool maximum)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            Vector3[] corners =
            {
                center + new Vector3( extents.x,  extents.y,  extents.z),
                center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z),
                center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z),
                center + new Vector3(-extents.x, -extents.y, -extents.z)
            };

            foreach (Vector3 corner in corners)
            {
                float projection =
                    Vector3.Dot(corner, axis);

                if (maximum)
                    value = Mathf.Max(value, projection);
                else
                    value = Mathf.Min(value, projection);
            }
        }
    }
}
