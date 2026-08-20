using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.XR.Templates.AR
{
    public class TowerStability : MonoBehaviour
    {
        // ============================================================
        // PLATFORM
        // ============================================================

        [Header("Platform")]
        [Tooltip(
            "Platform is assigned automatically by ARTemplateMenuManager. " +
            "Do not assign it manually.")]
        GameObject m_Platform;

        // ============================================================
        // STABILITY
        // ============================================================

        [Header("Tower Stability")]

        [SerializeField]
        [Tooltip(
            "How far the center of mass is allowed to move " +
            "inside/outside the platform before the tower becomes unstable.")]
        float m_StabilityMargin = 0.02f;

        [SerializeField]
        [Tooltip(
            "How often the stability calculation is performed.")]
        float m_CheckInterval = 0.05f;

        [SerializeField]
        [Tooltip(
            "If enabled, an unstable tower wakes all frozen blocks.")]
        bool m_EnableToppling = true;

        [SerializeField]
        [Tooltip(
            "Delay before waking the tower after instability is detected.")]
        float m_UnstableDelay = 0.05f;

        [SerializeField]
        [Tooltip(
            "Extra distance used when returning from unstable to stable. " +
            "Prevents rapid stable/unstable flickering.")]
        float m_StableHysteresis = 0.01f;

        // ============================================================
        // DEBUG
        // ============================================================

        [Header("Debug")]

        [SerializeField]
        [Tooltip(
            "Show stability information in the Scene view.")]
        bool m_DebugMode = true;

        [SerializeField]
        [Tooltip(
            "Show the tower center of mass.")]
        bool m_ShowCenterOfMass = true;

        [SerializeField]
        [Tooltip(
            "Show the platform support area.")]
        bool m_ShowSupportArea = true;

        [SerializeField]
        [Tooltip(
            "Size of the center of mass debug sphere.")]
        float m_DebugSphereSize = 0.04f;

        [SerializeField]
        Color m_StableColor = Color.green;

        [SerializeField]
        Color m_UnstableColor = Color.red;

        [SerializeField]
        Color m_CenterOfMassColor = Color.yellow;

        // ============================================================
        // DATA
        // ============================================================

        readonly List<GameObject> m_Blocks =
            new List<GameObject>();

        readonly List<Rigidbody> m_Rigidbodies =
            new List<Rigidbody>();

        float m_CheckTimer;

        bool m_IsStable = true;
        bool m_TopplingTriggered;

        float m_UnstableTimer;

        Vector3 m_CenterOfMass;
        Vector3 m_LocalCenterOfMass;

        Bounds m_PlatformBounds;

        // ============================================================
        // PUBLIC PROPERTIES
        // ============================================================

        public GameObject platform
        {
            get => m_Platform;
        }

        public bool isStable
        {
            get => m_IsStable;
        }

        public Vector3 centerOfMass
        {
            get => m_CenterOfMass;
        }

        // ============================================================
        // START
        // ============================================================

        void Start()
        {
            ResetTower();
        }

        // ============================================================
        // FIXED UPDATE
        // ============================================================

        void FixedUpdate()
        {
            if (m_Platform == null)
                return;

            m_CheckTimer += Time.fixedDeltaTime;

            float interval =
                Mathf.Max(0.01f, m_CheckInterval);

            if (m_CheckTimer < interval)
                return;

            m_CheckTimer = 0f;

            UpdateStability();
        }

        // ============================================================
        // SET PLATFORM
        // ============================================================

        public void SetPlatform(GameObject platform)
        {
            if (platform == null)
                return;

            m_Platform = platform;

            m_TopplingTriggered = false;
            m_UnstableTimer = 0f;
            m_IsStable = true;

            RebuildRigidbodyList();

            Debug.Log(
                "TowerStability automatically registered platform: " +
                platform.name,
                platform);
        }

        // ============================================================
        // REGISTER BLOCK
        // ============================================================

        public void RegisterBlock(GameObject block)
        {
            if (block == null)
                return;

            if (block == m_Platform)
                return;

            if (!m_Blocks.Contains(block))
            {
                m_Blocks.Add(block);
            }

            RebuildRigidbodyList();

            m_TopplingTriggered = false;

            Debug.Log(
                "TowerStability registered block: " +
                block.name,
                block);
        }

        // ============================================================
        // REMOVE BLOCK
        // ============================================================

        public void RemoveBlock(GameObject block)
        {
            if (block == null)
                return;

            m_Blocks.Remove(block);

            RebuildRigidbodyList();

            m_TopplingTriggered = false;
        }

        // ============================================================
        // REFRESH
        // ============================================================

        public void RefreshTower()
        {
            CleanupDestroyedBlocks();

            RebuildRigidbodyList();

            m_CheckTimer = 0f;

            if (m_Platform != null)
                UpdateStability();
        }

        // ============================================================
        // RESET
        // ============================================================

        public void ResetTower()
        {
            m_Platform = null;

            m_Blocks.Clear();
            m_Rigidbodies.Clear();

            m_CenterOfMass = Vector3.zero;
            m_LocalCenterOfMass = Vector3.zero;

            m_IsStable = true;
            m_TopplingTriggered = false;

            m_UnstableTimer = 0f;
            m_CheckTimer = 0f;
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        void CleanupDestroyedBlocks()
        {
            for (int i = m_Blocks.Count - 1; i >= 0; --i)
            {
                if (m_Blocks[i] == null)
                {
                    m_Blocks.RemoveAt(i);
                }
            }
        }

        // ============================================================
        // RIGIDBODY LIST
        // ============================================================

        void RebuildRigidbodyList()
        {
            m_Rigidbodies.Clear();

            foreach (GameObject block in m_Blocks)
            {
                if (block == null)
                    continue;

                Rigidbody[] bodies =
                    block.GetComponentsInChildren<Rigidbody>(true);

                foreach (Rigidbody rb in bodies)
                {
                    if (rb == null)
                        continue;

                    if (!m_Rigidbodies.Contains(rb))
                    {
                        m_Rigidbodies.Add(rb);
                    }
                }
            }
        }

        // ============================================================
        // STABILITY UPDATE
        // ============================================================

        void UpdateStability()
        {
            CleanupDestroyedBlocks();

            if (m_Platform == null)
                return;

            if (m_Blocks.Count == 0)
            {
                m_IsStable = true;
                m_UnstableTimer = 0f;
                m_TopplingTriggered = false;
                return;
            }

            if (m_Rigidbodies.Count == 0)
            {
                RebuildRigidbodyList();

                if (m_Rigidbodies.Count == 0)
                    return;
            }

            CalculateCenterOfMass();

            bool stable;

            if (m_IsStable)
            {
                // Normal threshold.
                stable =
                    IsCenterOfMassInsideSupport(
                        Mathf.Max(
                            0f,
                            m_StabilityMargin));
            }
            else
            {
                // When recovering from instability we require
                // the center of mass to move further inside.
                stable =
                    IsCenterOfMassInsideSupport(
                        Mathf.Max(
                            0f,
                            m_StabilityMargin +
                            Mathf.Max(
                                0f,
                                m_StableHysteresis)));
            }

            if (stable)
            {
                m_IsStable = true;

                m_UnstableTimer = 0f;

                // A future instability event is allowed.
                m_TopplingTriggered = false;
            }
            else
            {
                m_IsStable = false;

                m_UnstableTimer +=
                    Time.fixedDeltaTime;

                if (m_EnableToppling &&
                    !m_TopplingTriggered &&
                    m_UnstableTimer >=
                    Mathf.Max(
                        0f,
                        m_UnstableDelay))
                {
                    TriggerToppling();

                    m_TopplingTriggered = true;
                }
            }
        }

        // ============================================================
        // CENTER OF MASS
        // ============================================================

        void CalculateCenterOfMass()
        {
            float totalMass = 0f;

            Vector3 weightedPosition =
                Vector3.zero;

            foreach (Rigidbody rb in m_Rigidbodies)
            {
                if (rb == null)
                    continue;

                float mass =
                    Mathf.Max(
                        0.0001f,
                        rb.mass);

                Vector3 bodyCenter =
                    rb.worldCenterOfMass;

                weightedPosition +=
                    bodyCenter * mass;

                totalMass += mass;
            }

            if (totalMass <= 0.0001f)
            {
                m_CenterOfMass =
                    Vector3.zero;

                m_LocalCenterOfMass =
                    Vector3.zero;

                return;
            }

            m_CenterOfMass =
                weightedPosition /
                totalMass;

            m_LocalCenterOfMass =
                m_Platform.transform.InverseTransformPoint(
                    m_CenterOfMass);
        }

        // ============================================================
        // SUPPORT AREA
        // ============================================================

        bool IsCenterOfMassInsideSupport(
            float margin)
        {
            if (m_Platform == null)
                return true;

            if (!TryGetPlatformSupportBounds(
                    out Bounds supportBounds))
            {
                return true;
            }

            m_PlatformBounds =
                supportBounds;

            float xMin =
                supportBounds.min.x +
                margin;

            float xMax =
                supportBounds.max.x -
                margin;

            float zMin =
                supportBounds.min.z +
                margin;

            float zMax =
                supportBounds.max.z -
                margin;

            // Prevent the margin from creating
            // an inverted support area.
            if (xMin > xMax)
            {
                float center =
                    (supportBounds.min.x +
                     supportBounds.max.x) *
                    0.5f;

                xMin = center;
                xMax = center;
            }

            if (zMin > zMax)
            {
                float center =
                    (supportBounds.min.z +
                     supportBounds.max.z) *
                    0.5f;

                zMin = center;
                zMax = center;
            }

            return
                m_LocalCenterOfMass.x >= xMin &&
                m_LocalCenterOfMass.x <= xMax &&
                m_LocalCenterOfMass.z >= zMin &&
                m_LocalCenterOfMass.z <= zMax;
        }

        // ============================================================
        // PLATFORM SUPPORT BOUNDS
        // ============================================================

        bool TryGetPlatformSupportBounds(
            out Bounds bounds)
        {
            bounds =
                new Bounds(
                    Vector3.zero,
                    Vector3.zero);

            if (m_Platform == null)
                return false;

            Collider[] colliders =
                m_Platform.GetComponentsInChildren<Collider>(
                    true);

            bool found = false;

            foreach (Collider collider in colliders)
            {
                if (collider == null ||
                    !collider.enabled ||
                    collider.isTrigger)
                    continue;

                Bounds worldBounds =
                    collider.bounds;

                Vector3[] corners =
                    GetBoundsCorners(
                        worldBounds);

                foreach (Vector3 worldCorner in corners)
                {
                    Vector3 local =
                        m_Platform.transform.InverseTransformPoint(
                            worldCorner);

                    // Stability is calculated horizontally.
                    local.y = 0f;

                    if (!found)
                    {
                        bounds =
                            new Bounds(
                                local,
                                Vector3.zero);

                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            return found;
        }

        static Vector3[] GetBoundsCorners(
            Bounds bounds)
        {
            Vector3 c =
                bounds.center;

            Vector3 e =
                bounds.extents;

            return new Vector3[]
            {
                c + new Vector3( e.x,  e.y,  e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x, -e.y, -e.z),

                c + new Vector3(-e.x,  e.y,  e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z),
                c + new Vector3(-e.x, -e.y, -e.z)
            };
        }

        // ============================================================
        // TOPPLE
        // ============================================================

        void TriggerToppling()
        {
            Debug.LogWarning(
                "TOWER UNSTABLE! Center of mass moved " +
                "outside the platform support area.",
                this);

            foreach (Rigidbody rb in m_Rigidbodies)
            {
                if (rb == null)
                    continue;

                rb.isKinematic = false;
                rb.useGravity = true;

                rb.WakeUp();
            }
        }

        // ============================================================
        // DEBUG GIZMOS
        // ============================================================

        void OnDrawGizmos()
        {
            if (!m_DebugMode)
                return;

            if (m_Platform == null)
                return;

            if (m_ShowSupportArea)
            {
                DrawSupportArea();
            }

            if (m_ShowCenterOfMass)
            {
                DrawCenterOfMass();
            }
        }

        // ============================================================
        // SUPPORT DEBUG
        // ============================================================

        void DrawSupportArea()
        {
            if (!TryGetPlatformSupportBounds(
                    out Bounds bounds))
                return;

            Color color =
                m_IsStable
                    ? m_StableColor
                    : m_UnstableColor;

            Gizmos.color =
                color;

            Vector3 localCenter =
                bounds.center;

            Vector3 localSize =
                bounds.size;

            Vector3 worldCenter =
                m_Platform.transform.TransformPoint(
                    new Vector3(
                        localCenter.x,
                        0f,
                        localCenter.z));

            Vector3 worldSize =
                Vector3.Scale(
                    localSize,
                    m_Platform.transform.lossyScale);

            worldSize.y =
                0.01f;

            Gizmos.DrawWireCube(
                worldCenter,
                worldSize);

            // --------------------------------------------------------
            // Stable support area
            // --------------------------------------------------------

            float margin =
                Mathf.Max(
                    0f,
                    m_StabilityMargin);

            float width =
                Mathf.Max(
                    0f,
                    bounds.size.x -
                    margin * 2f);

            float depth =
                Mathf.Max(
                    0f,
                    bounds.size.z -
                    margin * 2f);

            Vector3 stableLocal =
                new Vector3(
                    bounds.center.x,
                    0f,
                    bounds.center.z);

            Vector3 stableWorld =
                m_Platform.transform.TransformPoint(
                    stableLocal);

            Vector3 stableSize =
                Vector3.Scale(
                    new Vector3(
                        width,
                        0.01f,
                        depth),
                    m_Platform.transform.lossyScale);

            Gizmos.DrawWireCube(
                stableWorld,
                stableSize);
        }

        // ============================================================
        // CENTER OF MASS DEBUG
        // ============================================================

        void DrawCenterOfMass()
        {
            Gizmos.color =
                m_CenterOfMassColor;

            Gizmos.DrawSphere(
                m_CenterOfMass,
                Mathf.Max(
                    0.005f,
                    m_DebugSphereSize));

            if (m_Platform != null)
            {
                Vector3 platformCenter =
                    m_Platform.transform.position;

                Gizmos.DrawLine(
                    m_CenterOfMass,
                    new Vector3(
                        m_CenterOfMass.x,
                        platformCenter.y,
                        m_CenterOfMass.z));
            }
        }
    }
}