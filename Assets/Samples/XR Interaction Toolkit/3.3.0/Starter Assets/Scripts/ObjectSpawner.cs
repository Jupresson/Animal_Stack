using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        Camera m_CameraToFace;

        public Camera cameraToFace
        {
            get
            {
                EnsureFacingCamera();
                return m_CameraToFace;
            }
            set => m_CameraToFace = value;
        }

        [SerializeField]
        List<GameObject> m_ObjectPrefabs = new List<GameObject>();

        public List<GameObject> objectPrefabs
        {
            get => m_ObjectPrefabs;
            set => m_ObjectPrefabs = value;
        }

        [SerializeField]
        GameObject m_SpawnVisualizationPrefab;

        public GameObject spawnVisualizationPrefab
        {
            get => m_SpawnVisualizationPrefab;
            set => m_SpawnVisualizationPrefab = value;
        }

        [SerializeField]
        int m_SpawnOptionIndex = -1;

        public int spawnOptionIndex
        {
            get => m_SpawnOptionIndex;
            set => m_SpawnOptionIndex = value;
        }

        [SerializeField]
        bool m_SpawnEnabled = true;

        public bool spawnEnabled
        {
            get => m_SpawnEnabled;
            set => m_SpawnEnabled = value;
        }

        public bool isSpawnOptionRandomized =>
            m_SpawnOptionIndex < 0 ||
            m_SpawnOptionIndex >= m_ObjectPrefabs.Count;

        [SerializeField]
        bool m_OnlySpawnInView = true;

        public bool onlySpawnInView
        {
            get => m_OnlySpawnInView;
            set => m_OnlySpawnInView = value;
        }

        [SerializeField]
        float m_ViewportPeriphery = 0.15f;

        public float viewportPeriphery
        {
            get => m_ViewportPeriphery;
            set => m_ViewportPeriphery = value;
        }

        [SerializeField]
        bool m_ApplyRandomAngleAtSpawn = true;

        public bool applyRandomAngleAtSpawn
        {
            get => m_ApplyRandomAngleAtSpawn;
            set => m_ApplyRandomAngleAtSpawn = value;
        }

        [SerializeField]
        float m_SpawnAngleRange = 45f;

        public float spawnAngleRange
        {
            get => m_SpawnAngleRange;
            set => m_SpawnAngleRange = value;
        }

        [SerializeField]
        bool m_SpawnAsChildren;

        public bool spawnAsChildren
        {
            get => m_SpawnAsChildren;
            set => m_SpawnAsChildren = value;
        }

        public event Action<GameObject> objectSpawned;

        void Awake()
        {
            EnsureFacingCamera();
        }

        void EnsureFacingCamera()
        {
            if (m_CameraToFace == null)
                m_CameraToFace = Camera.main;
        }

        public void RandomizeSpawnOption()
        {
            m_SpawnOptionIndex = -1;
        }

        public void SetSpawnObjectIndex(int index)
        {
            if (index >= 0 && index < m_ObjectPrefabs.Count)
            {
                m_SpawnOptionIndex = index;
            }
            else
            {
                Debug.LogWarning(
                    "Object index is outside the prefab list.",
                    this);
            }
        }

        public bool TrySpawnObject(
            Vector3 spawnPoint,
            Vector3 spawnNormal)
        {
            if (!m_SpawnEnabled)
                return false;

            return TrySpawnObjectInternal(
                spawnPoint,
                spawnNormal,
                null,
                0f);
        }

        public bool TrySpawnObjectOnTopOf(
            GameObject surface,
            float gap = 0.05f)
        {
            if (!m_SpawnEnabled)
                return false;

            if (surface == null)
            {
                Debug.LogWarning(
                    "Cannot spawn on a null surface.",
                    this);

                return false;
            }

            Vector3 surfaceUp =
                surface.transform.up.sqrMagnitude > 0.0001f
                    ? surface.transform.up.normalized
                    : Vector3.up;

            return TrySpawnObjectInternal(
                surface.transform.position,
                surfaceUp,
                surface,
                Mathf.Max(0f, gap));
        }

        /// <summary>
        /// Spawns the selected prefab so its lowest actual mesh vertex
        /// is positioned at the supplied absolute height along surfaceUp.
        /// </summary>
        public bool TrySpawnObjectAtHeight(
            GameObject referenceSurface,
            float targetBottomHeight)
        {
            if (!m_SpawnEnabled)
                return false;

            if (referenceSurface == null)
            {
                Debug.LogWarning(
                    "Cannot spawn using a null reference surface.",
                    this);

                return false;
            }

            Vector3 surfaceUp =
                referenceSurface.transform.up.sqrMagnitude > 0.0001f
                    ? referenceSurface.transform.up.normalized
                    : Vector3.up;

            Vector3 spawnPoint =
                referenceSurface.transform.position;

            return TrySpawnObjectAtAbsoluteHeight(
                spawnPoint,
                surfaceUp,
                targetBottomHeight);
        }

        bool TrySpawnObjectAtAbsoluteHeight(
            Vector3 spawnPoint,
            Vector3 spawnNormal,
            float targetBottomHeight)
        {
            if (m_ObjectPrefabs == null ||
                m_ObjectPrefabs.Count == 0)
            {
                Debug.LogWarning(
                    "ObjectSpawner has no prefabs.",
                    this);

                return false;
            }

            EnsureFacingCamera();

            if (m_CameraToFace == null)
            {
                Debug.LogWarning(
                    "ObjectSpawner could not find a camera.",
                    this);

                return false;
            }

            if (m_OnlySpawnInView)
            {
                Vector3 viewport =
                    m_CameraToFace.WorldToViewportPoint(
                        spawnPoint);

                float min = m_ViewportPeriphery;
                float max = 1f - m_ViewportPeriphery;

                if (viewport.z < 0f ||
                    viewport.x < min ||
                    viewport.x > max ||
                    viewport.y < min ||
                    viewport.y > max)
                {
                    return false;
                }
            }

            int objectIndex =
                isSpawnOptionRandomized
                    ? UnityEngine.Random.Range(
                        0,
                        m_ObjectPrefabs.Count)
                    : m_SpawnOptionIndex;

            if (objectIndex < 0 ||
                objectIndex >= m_ObjectPrefabs.Count)
            {
                Debug.LogWarning(
                    "Invalid prefab index: " +
                    objectIndex,
                    this);

                return false;
            }

            GameObject prefab =
                m_ObjectPrefabs[objectIndex];

            if (prefab == null)
            {
                Debug.LogWarning(
                    "Selected prefab is null.",
                    this);

                return false;
            }

            GameObject newObject =
                Instantiate(prefab);

            if (m_SpawnAsChildren)
            {
                newObject.transform.SetParent(
                    transform,
                    true);
            }

            newObject.transform.position =
                spawnPoint;

            Vector3 forward =
                m_CameraToFace.transform.position -
                spawnPoint;

            BurstMathUtility.ProjectOnPlane(
                forward,
                spawnNormal,
                out Vector3 projectedForward);

            if (projectedForward.sqrMagnitude < 0.0001f)
            {
                projectedForward =
                    Vector3.Cross(
                        spawnNormal,
                        Vector3.right);
            }

            if (projectedForward.sqrMagnitude < 0.0001f)
            {
                projectedForward =
                    Vector3.Cross(
                        spawnNormal,
                        Vector3.forward);
            }

            newObject.transform.rotation =
                Quaternion.LookRotation(
                    projectedForward.normalized,
                    spawnNormal.normalized);

            if (m_ApplyRandomAngleAtSpawn)
            {
                float randomAngle =
                    UnityEngine.Random.Range(
                        -m_SpawnAngleRange,
                        m_SpawnAngleRange);

                newObject.transform.Rotate(
                    spawnNormal,
                    randomAngle,
                    Space.World);
            }

            // Position using actual mesh vertices after the final
            // initial rotation has been applied.
            if (TryGetMeshProjectedBounds(
                newObject,
                spawnNormal,
                out float objectMin,
                out _))
            {
                float correction =
                    targetBottomHeight -
                    objectMin;

                newObject.transform.position +=
                    spawnNormal * correction;
            }
            else
            {
                Debug.LogWarning(
                    "Could not determine mesh bounds for spawned object.",
                    newObject);
            }

            CreateSpawnVisualization(newObject);

            objectSpawned?.Invoke(newObject);

            return true;
        }

        bool TrySpawnObjectInternal(
            Vector3 spawnPoint,
            Vector3 spawnNormal,
            GameObject surface,
            float gap)
        {
            if (m_ObjectPrefabs == null ||
                m_ObjectPrefabs.Count == 0)
            {
                Debug.LogWarning(
                    "ObjectSpawner has no prefabs.",
                    this);

                return false;
            }

            EnsureFacingCamera();

            if (m_CameraToFace == null)
            {
                Debug.LogWarning(
                    "ObjectSpawner could not find a camera.",
                    this);

                return false;
            }

            if (m_OnlySpawnInView)
            {
                Vector3 viewport =
                    m_CameraToFace.WorldToViewportPoint(
                        spawnPoint);

                float min = m_ViewportPeriphery;
                float max = 1f - m_ViewportPeriphery;

                if (viewport.z < 0f ||
                    viewport.x < min ||
                    viewport.x > max ||
                    viewport.y < min ||
                    viewport.y > max)
                {
                    return false;
                }
            }

            int objectIndex =
                isSpawnOptionRandomized
                    ? UnityEngine.Random.Range(
                        0,
                        m_ObjectPrefabs.Count)
                    : m_SpawnOptionIndex;

            if (objectIndex < 0 ||
                objectIndex >= m_ObjectPrefabs.Count)
            {
                Debug.LogWarning(
                    "Invalid prefab index: " +
                    objectIndex,
                    this);

                return false;
            }

            GameObject prefab =
                m_ObjectPrefabs[objectIndex];

            if (prefab == null)
            {
                Debug.LogWarning(
                    "Selected prefab is null.",
                    this);

                return false;
            }

            GameObject newObject =
                Instantiate(prefab);

            if (m_SpawnAsChildren)
            {
                newObject.transform.SetParent(
                    transform,
                    true);
            }

            newObject.transform.position =
                spawnPoint;

            Vector3 forward =
                m_CameraToFace.transform.position -
                spawnPoint;

            BurstMathUtility.ProjectOnPlane(
                forward,
                spawnNormal,
                out Vector3 projectedForward);

            if (projectedForward.sqrMagnitude < 0.0001f)
            {
                projectedForward =
                    Vector3.Cross(
                        spawnNormal,
                        Vector3.right);
            }

            if (projectedForward.sqrMagnitude < 0.0001f)
            {
                projectedForward =
                    Vector3.Cross(
                        spawnNormal,
                        Vector3.forward);
            }

            newObject.transform.rotation =
                Quaternion.LookRotation(
                    projectedForward.normalized,
                    spawnNormal.normalized);

            if (m_ApplyRandomAngleAtSpawn)
            {
                float randomAngle =
                    UnityEngine.Random.Range(
                        -m_SpawnAngleRange,
                        m_SpawnAngleRange);

                newObject.transform.Rotate(
                    spawnNormal,
                    randomAngle,
                    Space.World);
            }

            if (surface != null)
            {
                PlaceObjectOnTop(
                    newObject,
                    surface,
                    spawnNormal,
                    gap);
            }

            CreateSpawnVisualization(newObject);

            objectSpawned?.Invoke(newObject);

            return true;
        }

        void PlaceObjectOnTop(
            GameObject objectToPlace,
            GameObject surface,
            Vector3 surfaceUp,
            float gap)
        {
            if (!TryGetProjectedBounds(
                surface,
                surfaceUp,
                out _,
                out float surfaceMax))
            {
                Debug.LogWarning(
                    "Could not determine platform bounds.",
                    this);

                return;
            }

            if (!TryGetProjectedBounds(
                objectToPlace,
                surfaceUp,
                out float objectMin,
                out _))
            {
                Debug.LogWarning(
                    "Could not determine object bounds.",
                    this);

                return;
            }

            float offset =
                surfaceMax +
                gap -
                objectMin;

            objectToPlace.transform.position +=
                surfaceUp * offset;
        }

        void CreateSpawnVisualization(
            GameObject spawnedObject)
        {
            if (m_SpawnVisualizationPrefab == null ||
                spawnedObject == null)
            {
                return;
            }

            Transform visualization =
                Instantiate(
                    m_SpawnVisualizationPrefab)
                    .transform;

            visualization.position =
                spawnedObject.transform.position;

            visualization.rotation =
                spawnedObject.transform.rotation;
        }

        bool TryGetMeshProjectedBounds(
            GameObject root,
            Vector3 axis,
            out float minProjection,
            out float maxProjection)
        {
            minProjection =
                float.PositiveInfinity;

            maxProjection =
                float.NegativeInfinity;

            if (root == null)
                return false;

            axis = axis.sqrMagnitude > 0.0001f
                ? axis.normalized
                : Vector3.up;

            bool found = false;

            // Stacking is based on the actual collision volume.
            // Always use colliders first so the visual mesh cannot change
            // the requested gap. Trigger colliders are ignored.
            Collider[] colliders =
                root.GetComponentsInChildren<Collider>(true);

            foreach (Collider collider in colliders)
            {
                if (collider == null ||
                    !collider.enabled ||
                    collider.isTrigger)
                {
                    continue;
                }

                ProjectBounds(
                    collider.bounds,
                    axis,
                    ref minProjection,
                    ref maxProjection);

                found = true;
            }

            // Fallback when the object has no usable colliders.
            if (!found)
            {
                MeshFilter[] meshFilters =
                    root.GetComponentsInChildren<MeshFilter>(true);

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
                            meshTransform.TransformPoint(
                                localVertex);

                        float projection =
                            Vector3.Dot(
                                worldVertex,
                                axis);

                        minProjection =
                            Mathf.Min(
                                minProjection,
                                projection);

                        maxProjection =
                            Mathf.Max(
                                maxProjection,
                                projection);

                        found = true;
                    }
                }
            }

            // Final fallback for objects without colliders or readable mesh.
            if (!found)
            {
                Renderer[] renderers =
                    root.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null ||
                        !renderer.enabled)
                    {
                        continue;
                    }

                    ProjectBounds(
                        renderer.bounds,
                        axis,
                        ref minProjection,
                        ref maxProjection);

                    found = true;
                }
            }

            return found;
        }

        bool TryGetProjectedBounds(
            GameObject root,
            Vector3 axis,
            out float minProjection,
            out float maxProjection)
        {
            return TryGetMeshProjectedBounds(
                root,
                axis,
                out minProjection,
                out maxProjection);
        }

        static void ProjectBounds(
            Bounds bounds,
            Vector3 axis,
            ref float min,
            ref float max)
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
                    Vector3.Dot(
                        corner,
                        axis);

                min = Mathf.Min(min, projection);
                max = Mathf.Max(max, projection);
            }
        }

        public void SpawnObject(
            Vector3 spawnPoint,
            Vector3 spawnNormal)
        {
            if (!TrySpawnObject(
                spawnPoint,
                spawnNormal))
            {
                Debug.LogWarning(
                    "Could not spawn object.",
                    this);
            }
        }
    }
}
