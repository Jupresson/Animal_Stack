using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace YourApp.AR
{
    /// <summary>
    /// Makes the player place ONE platform first, then switches the same
    /// ObjectSpawner over to spawning regular objects on every tap after that.
    /// Once placed, the platform is locked in place, and every new object
    /// is dropped in with physics so it lands/stacks on whatever is already there.
    ///
    /// Only needs ONE ObjectSpawner + ONE AR Interactor Spawn Trigger in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class TwoStageSpawnController : MonoBehaviour
    {
        public enum Stage
        {
            WaitingForPlatform,
            SpawningObjects
        }

        [Header("Spawner (drag the ONE ObjectSpawner in your scene here)")]
        [SerializeField]
        ObjectSpawner m_ObjectSpawner;

        [Header("Prefabs")]
        [SerializeField]
        [Tooltip("The platform prefab. This spawns first, on the very first tap.")]
        GameObject m_PlatformPrefab;

        [SerializeField]
        [Tooltip("The regular objects. These spawn (and drop/stack) on every tap after the platform exists.")]
        List<GameObject> m_ObjectPrefabs = new List<GameObject>();

        [Header("Locking")]
        [SerializeField]
        [Tooltip("If true, disables the platform's grab/move/scale components once it's placed, " +
            "so it can no longer be picked up or moved.")]
        bool m_LockPlatformAfterPlacement = true;

        [Header("Dropping / Stacking")]
        [SerializeField]
        [Tooltip("Layer(s) that count as something an object can land on: the platform AND the " +
            "objects themselves. Create a layer (e.g. 'Stackable'), put it on both prefabs' " +
            "colliders, and select ONLY that layer here. Keeps the drop raycast from hitting " +
            "AR planes or anything else.")]
        LayerMask m_StackableLayerMask = ~0;

        [SerializeField]
        [Tooltip("How far above the current highest surface a new object is dropped from, in meters. " +
            "Small values (e.g. 0.05-0.2) look like a gentle drop; larger values fall harder.")]
        float m_DropClearance = 0.1f;

        [SerializeField]
        [Tooltip("How far up/down to search for the current stack height, in meters. Should comfortably " +
            "cover the tallest stack you expect.")]
        float m_DropSearchHeight = 2f;

        public Stage currentStage { get; private set; } = Stage.WaitingForPlatform;

        /// <summary>The platform GameObject once placed, otherwise null.</summary>
        public GameObject placedPlatform { get; private set; }

        /// <summary>Invoked once, right after the platform is spawned and locked.</summary>
        public event Action<GameObject> platformPlaced;

        /// <summary>Invoked every time a regular object is spawned/dropped after that.</summary>
        public event Action<GameObject> objectPlaced;

        void Awake()
        {
            if (m_ObjectSpawner == null)
            {
                Debug.LogError("TwoStageSpawnController: Object Spawner is not assigned.", this);
                enabled = false;
                return;
            }

            m_ObjectSpawner.objectSpawned += OnObjectSpawned;
            SetSpawnerToPlatform();
        }

        void OnDestroy()
        {
            if (m_ObjectSpawner != null)
                m_ObjectSpawner.objectSpawned -= OnObjectSpawned;
        }

        void SetSpawnerToPlatform()
        {
            m_ObjectSpawner.objectPrefabs = new List<GameObject> { m_PlatformPrefab };
            m_ObjectSpawner.spawnOptionIndex = 0;
        }

        void SetSpawnerToObjects()
        {
            m_ObjectSpawner.objectPrefabs = m_ObjectPrefabs;
            m_ObjectSpawner.RandomizeSpawnOption();
        }

        void OnObjectSpawned(GameObject spawned)
        {
            if (currentStage == Stage.WaitingForPlatform)
            {
                placedPlatform = spawned;
                currentStage = Stage.SpawningObjects;
                SetSpawnerToObjects();

                if (m_LockPlatformAfterPlacement)
                    SetInteractionLocked(placedPlatform, true);

                platformPlaced?.Invoke(spawned);
            }
            else
            {
                DropOntoStack(spawned);
                objectPlaced?.Invoke(spawned);
            }
        }

        /// <summary>
        /// Moves a freshly spawned object straight up above whatever currently occupies that
        /// X/Z spot (the platform, or objects already stacked there), leaving a small gap so
        /// its Rigidbody + gravity carry it the rest of the way down and it settles naturally.
        /// </summary>
        void DropOntoStack(GameObject spawned)
        {
            var pos = spawned.transform.position;
            var rayOrigin = new Vector3(pos.x, pos.y + m_DropSearchHeight, pos.z);

            // Disable this object's own colliders first, so the raycast below can only hit
            // the platform or OTHER objects - not itself (which would give a wrong result).
            var ownColliders = spawned.GetComponentsInChildren<Collider>(true);
            foreach (var col in ownColliders)
                col.enabled = false;

            bool didHit = Physics.Raycast(rayOrigin, Vector3.down, out var hit,
                m_DropSearchHeight * 2f, m_StackableLayerMask);

            foreach (var col in ownColliders)
                col.enabled = true;

            spawned.transform.position = didHit
                ? hit.point + Vector3.up * m_DropClearance
                : pos + Vector3.up * m_DropClearance;

            // Make sure it actually falls: wake it up in case it spawned asleep.
            var rb = spawned.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.WakeUp();
            }
        }

        /// <summary>
        /// Disables (or re-enables) any component on the object whose type name contains
        /// "Interactable" or "Transformer" - covers XR Grab Interactable, AR Transformer,
        /// etc. without hard-depending on a specific XR Interaction Toolkit version/namespace.
        /// </summary>
        void SetInteractionLocked(GameObject target, bool locked)
        {
            foreach (var behaviour in target.GetComponentsInChildren<Behaviour>(true))
            {
                var typeName = behaviour.GetType().Name;
                if (typeName.Contains("Interactable") || typeName.Contains("Transformer"))
                    behaviour.enabled = !locked;
            }
        }

        /// <summary>
        /// Lets the player place a new platform again from scratch.
        /// Wire this to a "Reset" UI button if you want that.
        /// </summary>
        public void ResetPlacement()
        {
            if (placedPlatform != null)
                Destroy(placedPlatform);

            placedPlatform = null;
            currentStage = Stage.WaitingForPlatform;
            SetSpawnerToPlatform();
        }
    }
}