using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Templates.AR
{
    public class ARTemplateMenuManager : MonoBehaviour
    {
        [Header("UI")]

        [SerializeField]
        Button m_CreateButton;

        public Button createButton
        {
            get => m_CreateButton;
            set => m_CreateButton = value;
        }

        [SerializeField]
        Button m_DeleteButton;

        public Button deleteButton
        {
            get => m_DeleteButton;
            set => m_DeleteButton = value;
        }

        [SerializeField]
        Button m_ConfirmButton;

        public Button confirmButton
        {
            get => m_ConfirmButton;
            set => m_ConfirmButton = value;
        }

        [SerializeField]
        GameObject m_ObjectMenu;

        public GameObject objectMenu
        {
            get => m_ObjectMenu;
            set => m_ObjectMenu = value;
        }

        [SerializeField]
        GameObject m_ModalMenu;

        public GameObject modalMenu
        {
            get => m_ModalMenu;
            set => m_ModalMenu = value;
        }

        [SerializeField]
        Animator m_ObjectMenuAnimator;

        public Animator objectMenuAnimator
        {
            get => m_ObjectMenuAnimator;
            set => m_ObjectMenuAnimator = value;
        }

        [SerializeField]
        ObjectSpawner m_ObjectSpawner;

        public ObjectSpawner objectSpawner
        {
            get => m_ObjectSpawner;
            set => m_ObjectSpawner = value;
        }

        [SerializeField]
        Button m_CancelButton;

        public Button cancelButton
        {
            get => m_CancelButton;
            set => m_CancelButton = value;
        }

        // ============================================================
        // PLACEMENT
        // ============================================================

        [Header("Placement")]

        [SerializeField]
        ARPlacementManipulator m_PlacementManipulator;

        public ARPlacementManipulator placementManipulator
        {
            get => m_PlacementManipulator;
            set => m_PlacementManipulator = value;
        }

        [SerializeField]
        [Tooltip("Distance between the platform and the first preview object.")]
        float m_PlatformTopGap = 0.02f;

        [SerializeField]
        [Tooltip("Delay before spawning next object.")]
        float m_NextObjectDelay = 0.5f;

        [Header("Vertical Object Stacking")]

        [SerializeField]
        [Tooltip("Fixed gap between the highest confirmed COLLISION point and the next object collision.")]
        float m_ObjectStackGap = 0.02f;

        [SerializeField]
        [Tooltip("When enabled, every new object is placed above the highest confirmed object.")]
        bool m_StackObjectsVertically = true;

        [Header("Object Settling")]

        [SerializeField]
        [Tooltip("Linear velocity magnitude below which a confirmed object is considered almost stopped.")]
        float m_SettleLinearVelocityThreshold = 0.03f;

        [SerializeField]
        [Tooltip("Angular velocity magnitude below which a confirmed object is considered almost stopped.")]
        float m_SettleAngularVelocityThreshold = 0.03f;

        [SerializeField]
        [Tooltip("After the object first reaches the near-zero velocity threshold, it must remain there for this many seconds before becoming static.")]
        float m_SettleTime = 3f;

        [Header("Stability Debug Visualization")]
        [SerializeField]
        [Tooltip("Shows a colored marker at the base of each confirmed object. Red = moving, Yellow = settling, Green = frozen/stable.")]
        bool m_ShowStabilityDebug = false;

        [SerializeField]
        [Tooltip("Diameter of each stability marker.")]
        float m_StabilityDebugSize = 0.12f;

        [SerializeField]
        [Tooltip("Thickness of each stability marker.")]
        float m_StabilityDebugHeight = 0.008f;

        [SerializeField]
        [Tooltip("Vertical offset above the lowest collider point.")]
        float m_StabilityDebugOffset = 0.005f;

        readonly Dictionary<GameObject, GameObject> m_StabilityDebugMarkers =
            new Dictionary<GameObject, GameObject>();

        Material m_DebugStableMaterial;
        Material m_DebugSettlingMaterial;
        Material m_DebugMovingMaterial;

        // Projection along the platform's up axis.
        float m_HighestConfirmedPoint =
            float.NegativeInfinity;

        bool m_HasConfirmedRandomObject;

        bool m_PlatformConfirmed;
        bool m_AwaitingConfirmation;

        GameObject m_PlatformInstance;
        GameObject m_CurrentPlacementObject;

        // Confirmed random objects are tracked explicitly so stacking does
        // not depend on whether ObjectSpawner uses parented objects.
        readonly List<GameObject> m_ConfirmedRandomObjects =
            new List<GameObject>();

        Vector3 m_PlacementScale;

        // ============================================================
        // PLATFORM LOCK
        // ============================================================

        sealed class BlockSelectFilter : IXRSelectFilter
        {
            public bool canProcess => true;

            public bool Process(
                IXRSelectInteractor interactor,
                IXRSelectInteractable interactable)
            {
                return false;
            }
        }

        readonly List<XRBaseInteractable>
            m_LockedPlatformInteractables =
                new List<XRBaseInteractable>();

        // ============================================================
        // XR INTERACTION
        // ============================================================

        [SerializeField]
        XRInteractionGroup m_InteractionGroup;

        public XRInteractionGroup interactionGroup
        {
            get => m_InteractionGroup;
            set => m_InteractionGroup = value;
        }

        // ============================================================
        // DEBUG
        // ============================================================

        [SerializeField]
        DebugSlider m_DebugPlaneSlider;

        public DebugSlider debugPlaneSlider
        {
            get => m_DebugPlaneSlider;
            set => m_DebugPlaneSlider = value;
        }

        [SerializeField]
        ARPlaneManager m_PlaneManager;

        public ARPlaneManager planeManager
        {
            get => m_PlaneManager;
            set => m_PlaneManager = value;
        }

        [SerializeField]
        bool m_UseARPlaneFading = true;

        public bool useARPlaneFading
        {
            get => m_UseARPlaneFading;
            set => m_UseARPlaneFading = value;
        }

        [SerializeField]
        ARDebugMenu m_ARDebugMenu;

        public ARDebugMenu arDebugMenu
        {
            get => m_ARDebugMenu;
            set => m_ARDebugMenu = value;
        }

        [SerializeField]
        DebugSlider m_DebugMenuSlider;

        public DebugSlider debugMenuSlider
        {
            get => m_DebugMenuSlider;
            set => m_DebugMenuSlider = value;
        }

        // ============================================================
        // INPUT
        // ============================================================

        [SerializeField]
        XRInputValueReader<Vector2>
            m_TapStartPositionInput =
            new XRInputValueReader<Vector2>(
                "Tap Start Position");

        public XRInputValueReader<Vector2>
            tapStartPositionInput
        {
            get => m_TapStartPositionInput;

            set =>
                XRInputReaderUtility.SetInputProperty(
                    ref m_TapStartPositionInput,
                    value,
                    this);
        }

        [SerializeField]
        XRInputValueReader<Vector2>
            m_DragCurrentPositionInput =
            new XRInputValueReader<Vector2>(
                "Drag Current Position");

        public XRInputValueReader<Vector2>
            dragCurrentPositionInput
        {
            get => m_DragCurrentPositionInput;

            set =>
                XRInputReaderUtility.SetInputProperty(
                    ref m_DragCurrentPositionInput,
                    value,
                    this);
        }

        // ============================================================
        // MENU STATE
        // ============================================================

        bool m_IsPointerOverUI;
        bool m_ShowObjectMenu;
        bool m_ShowOptionsModal;
        bool m_VisualizePlanes = true;
        bool m_ShowDebugMenu;
        bool m_InitializingDebugMenu;

        float m_DebugMenuPlanesButtonValue;

        Vector2 m_ObjectButtonOffset;
        Vector2 m_ObjectMenuOffset;

        readonly List<ARPlane>
            m_ARPlanes =
                new List<ARPlane>();

        readonly Dictionary<
            ARPlane,
            ARPlaneMeshVisualizer>
            m_ARPlaneMeshVisualizers =
                new Dictionary<
                    ARPlane,
                    ARPlaneMeshVisualizer>();

        readonly Dictionary<
            ARPlane,
            ARPlaneMeshVisualizerFader>
            m_ARPlaneMeshVisualizerFaders =
                new Dictionary<
                    ARPlane,
                    ARPlaneMeshVisualizerFader>();

        // ============================================================
        // ENABLE
        // ============================================================

        void OnEnable()
        {
            if (m_CreateButton != null)
                m_CreateButton.onClick.AddListener(
                    OnCreateButtonPressed);

            if (m_ConfirmButton != null)
                m_ConfirmButton.onClick.AddListener(
                    OnConfirmButtonPressed);

            if (m_CancelButton != null)
                m_CancelButton.onClick.AddListener(
                    HideMenu);

            if (m_DeleteButton != null)
                m_DeleteButton.onClick.AddListener(
                    DeleteFocusedObject);

            if (m_PlaneManager != null)
                m_PlaneManager.trackablesChanged.AddListener(
                    OnPlaneChanged);

            if (m_ObjectSpawner != null)
                m_ObjectSpawner.objectSpawned +=
                    OnObjectSpawned;
        }

        // ============================================================
        // DISABLE
        // ============================================================

        void OnDisable()
        {
            StopAllCoroutines();

            if (m_CreateButton != null)
                m_CreateButton.onClick.RemoveListener(
                    OnCreateButtonPressed);

            if (m_ConfirmButton != null)
                m_ConfirmButton.onClick.RemoveListener(
                    OnConfirmButtonPressed);

            if (m_CancelButton != null)
                m_CancelButton.onClick.RemoveListener(
                    HideMenu);

            if (m_DeleteButton != null)
                m_DeleteButton.onClick.RemoveListener(
                    DeleteFocusedObject);

            if (m_PlaneManager != null)
                m_PlaneManager.trackablesChanged.RemoveListener(
                    OnPlaneChanged);

            if (m_ObjectSpawner != null)
                m_ObjectSpawner.objectSpawned -=
                    OnObjectSpawned;

            if (m_PlacementManipulator != null)
                m_PlacementManipulator.StopPlacement();

            foreach (GameObject target in
                     new List<GameObject>(
                         m_StabilityDebugMarkers.Keys))
            {
                DestroyStabilityDebugMarker(target);
            }
        }

        // ============================================================
        // START
        // ============================================================

        void Start()
        {
            if (m_ARDebugMenu != null)
            {
                m_ARDebugMenu.gameObject.SetActive(true);

                m_InitializingDebugMenu = true;

                InitializeDebugMenuOffsets();
            }

            HideMenu();

            if (m_DebugMenuSlider != null)
                m_DebugMenuSlider.value =
                    m_ShowDebugMenu ? 1 : 0;

            if (m_DebugPlaneSlider != null)
                m_DebugPlaneSlider.value =
                    m_VisualizePlanes ? 1 : 0;

            if (m_ObjectSpawner != null)
            {
                if (m_ObjectSpawner.objectPrefabs.Count > 0)
                {
                    // Index 0 = platform.
                    m_ObjectSpawner.spawnOptionIndex = 0;
                    m_ObjectSpawner.spawnEnabled = true;
                }
                else
                {
                    Debug.LogWarning(
                        "ObjectSpawner needs at least one prefab.",
                        this);
                }
            }

            m_PlatformConfirmed = false;
            m_AwaitingConfirmation = false;

            ResetStackHeight();
        }

        // ============================================================
        // STABILITY DEBUG VISUALIZATION
        // ============================================================

        void EnsureDebugMaterials()
        {
            if (m_DebugStableMaterial != null)
                return;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default");

            if (shader == null)
                return;

            m_DebugStableMaterial = CreateDebugMaterial(shader, Color.green);
            m_DebugSettlingMaterial = CreateDebugMaterial(shader, Color.yellow);
            m_DebugMovingMaterial = CreateDebugMaterial(shader, Color.red);
        }

        static Material CreateDebugMaterial(Shader shader, Color color)
        {
            Material material = new Material(shader);
            material.name = "Tower Stability Debug Material";

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            return material;
        }

        void CreateStabilityDebugMarker(GameObject target)
        {
            if (!m_ShowStabilityDebug ||
                target == null ||
                m_StabilityDebugMarkers.ContainsKey(target))
                return;

            EnsureDebugMaterials();

            GameObject marker =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            marker.name = target.name + " [STABILITY]";
            marker.transform.SetParent(target.transform, true);

            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
                Destroy(markerCollider);

            marker.transform.localScale = new Vector3(
                Mathf.Max(0.001f, m_StabilityDebugSize),
                Mathf.Max(0.001f, m_StabilityDebugHeight),
                Mathf.Max(0.001f, m_StabilityDebugSize));

            m_StabilityDebugMarkers[target] = marker;
            SetDebugMarkerMaterial(marker, m_DebugMovingMaterial);
        }

        void DestroyStabilityDebugMarker(GameObject target)
        {
            if (target == null)
                return;

            if (m_StabilityDebugMarkers.TryGetValue(
                    target, out GameObject marker))
            {
                if (marker != null)
                    Destroy(marker);
            }

            m_StabilityDebugMarkers.Remove(target);
        }

        void UpdateStabilityDebugMarkers()
        {
            if (!m_ShowStabilityDebug)
            {
                if (m_StabilityDebugMarkers.Count > 0)
                {
                    List<GameObject> targets =
                        new List<GameObject>(m_StabilityDebugMarkers.Keys);

                    foreach (GameObject target in targets)
                        DestroyStabilityDebugMarker(target);
                }

                return;
            }

            EnsureDebugMaterials();

            foreach (GameObject target in m_ConfirmedRandomObjects)
            {
                if (target == null)
                    continue;

                CreateStabilityDebugMarker(target);

                if (!m_StabilityDebugMarkers.TryGetValue(
                        target, out GameObject marker) ||
                    marker == null)
                    continue;

                Rigidbody[] rigidbodies =
                    target.GetComponentsInChildren<Rigidbody>(true);

                bool hasDynamicBody = false;
                bool almostStopped = true;

                foreach (Rigidbody rb in rigidbodies)
                {
                    if (rb == null)
                        continue;

                    if (!rb.isKinematic)
                        hasDynamicBody = true;

                    if (rb.linearVelocity.sqrMagnitude >
                            m_SettleLinearVelocityThreshold *
                            m_SettleLinearVelocityThreshold ||
                        rb.angularVelocity.sqrMagnitude >
                            m_SettleAngularVelocityThreshold *
                            m_SettleAngularVelocityThreshold)
                    {
                        almostStopped = false;
                    }
                }

                if (!hasDynamicBody)
                    SetDebugMarkerMaterial(marker, m_DebugStableMaterial);
                else if (almostStopped)
                    SetDebugMarkerMaterial(marker, m_DebugSettlingMaterial);
                else
                    SetDebugMarkerMaterial(marker, m_DebugMovingMaterial);

                if (TryGetLowestColliderPoint(
                        target, out Vector3 lowestPoint))
                {
                    marker.transform.position =
                        lowestPoint +
                        Vector3.up * m_StabilityDebugOffset;
                }
            }
        }

        static void SetDebugMarkerMaterial(
            GameObject marker,
            Material material)
        {
            if (marker == null || material == null)
                return;

            Renderer renderer = marker.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        bool TryGetLowestColliderPoint(
            GameObject target,
            out Vector3 lowestPoint)
        {
            lowestPoint = Vector3.zero;

            if (target == null)
                return false;

            Collider[] colliders =
                target.GetComponentsInChildren<Collider>(true);

            float lowestY = float.PositiveInfinity;
            bool found = false;

            foreach (Collider collider in colliders)
            {
                if (collider == null ||
                    !collider.enabled ||
                    collider.isTrigger)
                    continue;

                Bounds bounds = collider.bounds;

                if (bounds.min.y < lowestY)
                {
                    lowestY = bounds.min.y;
                    lowestPoint = new Vector3(
                        bounds.center.x,
                        bounds.min.y,
                        bounds.center.z);
                    found = true;
                }
            }

            return found;
        }

        // ============================================================
        // UPDATE
        // ============================================================

        void Update()
        {
            if (m_InitializingDebugMenu)
            {
                if (m_ARDebugMenu != null)
                    m_ARDebugMenu.gameObject.SetActive(false);

                m_InitializingDebugMenu = false;
            }

            if (m_ShowObjectMenu ||
                m_ShowOptionsModal)
            {
                m_IsPointerOverUI =
                    EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(-1);
            }
            else
            {
                m_IsPointerOverUI = false;
            }

            UpdateStabilityDebugMarkers();

            if (!m_ShowObjectMenu &&
                !m_ShowOptionsModal)
            {
                if (m_CreateButton != null)
                {
                    m_CreateButton.gameObject.SetActive(
                        m_PlatformConfirmed &&
                        !m_AwaitingConfirmation);
                }

                if (m_ConfirmButton != null)
                {
                    m_ConfirmButton.gameObject.SetActive(
                        m_AwaitingConfirmation);
                }

                if (m_DeleteButton != null)
                {
                    m_DeleteButton.gameObject.SetActive(
                        m_InteractionGroup != null &&
                        m_InteractionGroup.focusInteractable != null);
                }
            }
        }

        // ============================================================
        // LATE UPDATE
        // ============================================================

        void LateUpdate()
        {
            if (!m_PlatformConfirmed ||
                !m_AwaitingConfirmation ||
                m_CurrentPlacementObject == null)
            {
                return;
            }

            if (m_CurrentPlacementObject !=
                m_PlatformInstance)
            {
                m_CurrentPlacementObject.transform.localScale =
                    m_PlacementScale;
            }
        }

        // ============================================================
        // CREATE BUTTON
        // ============================================================

        void OnCreateButtonPressed()
        {
            if (!m_PlatformConfirmed)
                return;

            if (m_AwaitingConfirmation)
                return;

            StartRandomObjectSpawn();
        }

        // ============================================================
        // RANDOM OBJECT SPAWN
        // ============================================================

        void StartRandomObjectSpawn()
        {
            if (m_ObjectSpawner == null)
                return;

            if (!m_PlatformConfirmed)
                return;

            if (m_PlatformInstance == null)
                return;

            if (m_AwaitingConfirmation)
                return;

            if (!SelectRandomObject())
                return;

            m_ObjectSpawner.spawnEnabled = true;

            Vector3 platformUp =
                m_PlatformInstance.transform.up.normalized;

            float targetBottomHeight;

            if (m_StackObjectsVertically &&
                m_HasConfirmedRandomObject)
            {
                // Next object's lowest mesh vertex will be placed
                // above the highest confirmed mesh vertex.
                targetBottomHeight =
                    m_HighestConfirmedPoint +
                    Mathf.Max(0f, m_ObjectStackGap);
            }
            else
            {
                // First random object sits above the platform.
                float platformTop =
                    GetHighestMeshPoint(
                        m_PlatformInstance,
                        platformUp);

                if (float.IsNegativeInfinity(platformTop))
                {
                    Debug.LogWarning(
                        "Could not determine platform mesh height.",
                        m_PlatformInstance);

                    m_ObjectSpawner.spawnEnabled = false;
                    m_ObjectSpawner.spawnOptionIndex = -1;
                    return;
                }

                targetBottomHeight =
                    platformTop +
                    Mathf.Max(0f, m_PlatformTopGap);
            }

            bool spawned =
                m_ObjectSpawner.TrySpawnObjectAtHeight(
                    m_PlatformInstance,
                    targetBottomHeight);

            if (!spawned)
            {
                m_ObjectSpawner.spawnEnabled = false;
                m_ObjectSpawner.spawnOptionIndex = -1;
            }
        }

        // ============================================================
        // RANDOM SELECTION
        // ============================================================

        bool SelectRandomObject()
        {
            if (m_ObjectSpawner == null)
                return false;

            if (m_ObjectSpawner.objectPrefabs.Count < 2)
            {
                Debug.LogWarning(
                    "You need:\n" +
                    "Prefab 0 = Platform\n" +
                    "Prefab 1+ = Objects",
                    this);

                return false;
            }

            m_ObjectSpawner.spawnOptionIndex =
                Random.Range(
                    1,
                    m_ObjectSpawner.objectPrefabs.Count);

            return true;
        }

        // ============================================================
        // CONFIRM
        // ============================================================

        void OnConfirmButtonPressed()
        {
            if (!m_AwaitingConfirmation)
                return;

            if (m_CurrentPlacementObject == null)
                return;

            bool confirmingPlatform =
                m_CurrentPlacementObject ==
                m_PlatformInstance;

            // ========================================================
            // PLATFORM
            // ========================================================

            if (confirmingPlatform)
            {
                m_PlatformConfirmed = true;
                m_AwaitingConfirmation = false;

                SetPhysicsState(
                    m_CurrentPlacementObject,
                    false);

                LockPlatform();

                if (m_ObjectSpawner != null)
                {
                    m_ObjectSpawner.spawnOptionIndex = -1;
                    m_ObjectSpawner.spawnEnabled = false;
                }

                m_CurrentPlacementObject = null;

                StartRandomObjectSpawn();

                return;
            }

            // ========================================================
            // RANDOM OBJECT
            // ========================================================

            // Calculate the highest point using the object's FINAL
            // position and rotation while it is still being previewed.
            if (!m_ConfirmedRandomObjects.Contains(
                    m_CurrentPlacementObject))
            {
                m_ConfirmedRandomObjects.Add(
                    m_CurrentPlacementObject);
            }

            RegisterConfirmedObject(
                m_CurrentPlacementObject);
            CreateStabilityDebugMarker(
                m_CurrentPlacementObject);

            if (m_PlacementManipulator != null)
                m_PlacementManipulator.StopPlacement();

            GameObject confirmedObject =
                m_CurrentPlacementObject;

            SetPhysicsState(
                confirmedObject,
                true);

            // Let the object fall normally. Once both linear and angular
            // velocity remain almost zero, freeze it in place.
            StartCoroutine(
                FreezeObjectWhenSettled(confirmedObject));

            m_AwaitingConfirmation = false;
            m_CurrentPlacementObject = null;

            if (m_ObjectSpawner != null)
            {
                m_ObjectSpawner.spawnOptionIndex = -1;
                m_ObjectSpawner.spawnEnabled = false;
            }

            StartCoroutine(
                SpawnNextObjectAfterDelay());
        }

        // ============================================================
        // NEXT OBJECT
        // ============================================================

        IEnumerator FreezeObjectWhenSettled(
            GameObject target)
        {
            if (target == null)
                yield break;

            Rigidbody[] rigidbodies =
                target.GetComponentsInChildren<Rigidbody>(true);

            if (rigidbodies.Length == 0)
                yield break;

            float stillTime = 0f;
            float linearThreshold =
                Mathf.Max(0.0001f, m_SettleLinearVelocityThreshold);
            float angularThreshold =
                Mathf.Max(0.0001f, m_SettleAngularVelocityThreshold);
            float requiredStillTime =
                Mathf.Max(0f, m_SettleTime);

            while (target != null)
            {
                bool almostStopped = true;

                foreach (Rigidbody rb in rigidbodies)
                {
                    if (rb == null)
                        continue;

                    if (rb.linearVelocity.sqrMagnitude >
                            linearThreshold * linearThreshold ||
                        rb.angularVelocity.sqrMagnitude >
                            angularThreshold * angularThreshold)
                    {
                        almostStopped = false;
                        break;
                    }
                }

                if (almostStopped)
                    stillTime += Time.fixedDeltaTime;
                else
                    stillTime = 0f;

                if (stillTime >= requiredStillTime)
                {
                    foreach (Rigidbody rb in rigidbodies)
                    {
                        if (rb == null)
                            continue;

                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.useGravity = false;
                        rb.isKinematic = true;
                    }

                    // Re-read the final collision position after the object
                    // has settled so the next object gets the fixed gap.
                    RecalculateHighestConfirmedPoint();
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }
        }

        IEnumerator SpawnNextObjectAfterDelay()
        {
            yield return new WaitForSeconds(
                m_NextObjectDelay);

            if (!m_PlatformConfirmed)
                yield break;

            if (m_PlatformInstance == null)
                yield break;

            if (m_AwaitingConfirmation)
                yield break;

            // The confirmed object has gravity enabled. Its collision height
            // can therefore change while it falls/settles. Recalculate here
            // immediately before spawning the next preview so the gap is
            // measured from the CURRENT highest collision, not the old
            // pre-drop position.
            RecalculateHighestConfirmedPoint();

            StartRandomObjectSpawn();
        }

        // ============================================================
        // OBJECT SPAWNED
        // ============================================================

        void OnObjectSpawned(
            GameObject spawnedObject)
        {
            if (spawnedObject == null)
                return;

            // ========================================================
            // PLATFORM
            // ========================================================

            if (m_PlatformInstance == null &&
                !m_PlatformConfirmed)
            {
                m_PlatformInstance =
                    spawnedObject;
            }

            m_CurrentPlacementObject =
                spawnedObject;

            m_AwaitingConfirmation = true;

            // ========================================================
            // PREVIEW OBJECT DOES NOT FALL
            // ========================================================

            SetPhysicsState(
                spawnedObject,
                false);

            if (spawnedObject !=
                m_PlatformInstance)
            {
                m_PlacementScale =
                    spawnedObject.transform.localScale;

                if (m_PlacementManipulator != null)
                {
                    Vector3 platformUp =
                        m_PlatformInstance.transform.up.normalized;

                    float targetBottomHeight;

                    if (m_StackObjectsVertically &&
                        m_HasConfirmedRandomObject)
                    {
                        targetBottomHeight =
                            m_HighestConfirmedPoint +
                            Mathf.Max(0f, m_ObjectStackGap);
                    }
                    else
                    {
                        float platformTop =
                            GetHighestMeshPoint(
                                m_PlatformInstance,
                                platformUp);

                        targetBottomHeight =
                            platformTop +
                            Mathf.Max(0f, m_PlatformTopGap);
                    }

                    m_PlacementManipulator.BeginPlacement(
                        spawnedObject,
                        m_PlatformInstance,
                        targetBottomHeight);
                }
            }

            // Prevent accidental plane spawning.
            if (m_ObjectSpawner != null)
            {
                m_ObjectSpawner.spawnEnabled = false;
                m_ObjectSpawner.spawnOptionIndex = -1;
            }
        }

        // ============================================================
        // HIGHEST CONFIRMED OBJECT
        // ============================================================

        void RegisterConfirmedObject(
            GameObject confirmedObject)
        {
            if (confirmedObject == null ||
                m_PlatformInstance == null)
            {
                return;
            }

            Vector3 platformUp =
                m_PlatformInstance.transform.up.normalized;

            float highest =
                GetHighestMeshPoint(
                    confirmedObject,
                    platformUp);

            if (float.IsNegativeInfinity(highest))
            {
                Debug.LogWarning(
                    "Could not find collision or mesh bounds on confirmed object.",
                    confirmedObject);

                return;
            }

            if (!m_HasConfirmedRandomObject)
            {
                m_HighestConfirmedPoint = highest;
                m_HasConfirmedRandomObject = true;
            }
            else
            {
                m_HighestConfirmedPoint =
                    Mathf.Max(
                        m_HighestConfirmedPoint,
                        highest);
            }

            Debug.Log(
                "Highest confirmed object point = " +
                m_HighestConfirmedPoint.ToString("F4") +
                " m",
                confirmedObject);
        }

        // Returns the highest point of the object's COLLISION volume along
        // the platform up axis. The method name is kept for compatibility
        // with the rest of this script, but collisions are intentionally
        // preferred over visual mesh vertices.
        float GetHighestMeshPoint(
            GameObject target,
            Vector3 axis)
        {
            if (target == null)
                return float.NegativeInfinity;

            axis = axis.sqrMagnitude > 0.0001f
                ? axis.normalized
                : Vector3.up;

            float highest = float.NegativeInfinity;
            bool found = false;

            Collider[] colliders =
                target.GetComponentsInChildren<Collider>(true);

            foreach (Collider collider in colliders)
            {
                if (collider == null ||
                    !collider.enabled ||
                    collider.isTrigger)
                {
                    continue;
                }

                CheckBounds(
                    collider.bounds,
                    axis,
                    ref highest,
                    true);

                found = true;
            }

            if (found)
                return highest;

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
                        meshTransform.TransformPoint(
                            localVertex);

                    highest = Mathf.Max(
                        highest,
                        Vector3.Dot(
                            worldVertex,
                            axis));

                    found = true;
                }
            }

            if (found)
                return highest;

            Renderer[] renderers =
                target.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    !renderer.enabled)
                {
                    continue;
                }

                CheckBounds(
                    renderer.bounds,
                    axis,
                    ref highest,
                    true);

                found = true;
            }

            return found
                ? highest
                : float.NegativeInfinity;
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
                    Vector3.Dot(
                        corner,
                        axis);

                if (maximum)
                    value = Mathf.Max(value, projection);
                else
                    value = Mathf.Min(value, projection);
            }
        }

        // ============================================================
        // PHYSICS
        // ============================================================

        void SetPhysicsState(
            GameObject target,
            bool enableGravity)
        {
            if (target == null)
                return;

            Rigidbody[] rigidbodies =
                target.GetComponentsInChildren<Rigidbody>(true);

            foreach (Rigidbody rb in rigidbodies)
            {
                rb.useGravity = enableGravity;
                rb.isKinematic = !enableGravity;
            }
        }

        // ============================================================
        // LOCK PLATFORM
        // ============================================================

        void LockPlatform()
        {
            if (m_PlatformInstance == null)
            {
                Debug.LogWarning(
                    "Could not lock platform: no platform instance.",
                    this);

                return;
            }

            XRBaseInteractable[] interactables =
                m_PlatformInstance.GetComponentsInChildren<
                    XRBaseInteractable>(true);

            foreach (XRBaseInteractable interactable in interactables)
            {
                if (m_LockedPlatformInteractables.Contains(
                        interactable))
                {
                    continue;
                }

                interactable.selectFilters.Add(
                    new BlockSelectFilter());

                m_LockedPlatformInteractables.Add(
                    interactable);

                if (interactable is IXRSelectInteractable selectInteractable &&
                    selectInteractable.isSelected &&
                    interactable.interactionManager != null)
                {
                    // Unity 6 / XRI 3 uses the interface-based interactor
                    // API. Cancel each active selection explicitly instead
                    // of calling the obsolete interactable overload.
                    for (int i = selectInteractable.interactorsSelecting.Count - 1; i >= 0; --i)
                    {
                        IXRSelectInteractor selectingInteractor =
                            selectInteractable.interactorsSelecting[i];

                        if (selectingInteractor != null)
                        {
                            interactable.interactionManager.SelectCancel(
                                selectingInteractor,
                                selectInteractable);
                        }
                    }
                }

                if (interactable is IXRHoverInteractable hoverInteractable &&
                    interactable.isHovered &&
                    interactable.interactionManager != null)
                {
                    interactable.interactionManager
                        .CancelInteractableHover(
                            hoverInteractable);
                }
            }
        }

        // ============================================================
        // OBJECT SELECTION
        // ============================================================

        public void SetObjectToSpawn(
            int objectIndex)
        {
            if (m_ObjectSpawner == null)
                return;

            if (objectIndex >= 0 &&
                objectIndex < m_ObjectSpawner.objectPrefabs.Count)
            {
                m_ObjectSpawner.spawnOptionIndex =
                    objectIndex;
            }
            else
            {
                Debug.LogWarning(
                    "Invalid object index.",
                    this);
            }

            HideMenu();
        }

        // ============================================================
        // DELETE
        // ============================================================

        void DeleteFocusedObject()
        {
            if (m_InteractionGroup == null)
                return;

            var focused =
                m_InteractionGroup.focusInteractable;

            if (focused == null)
                return;

            GameObject objectToDelete =
                focused.transform.gameObject;

            if (objectToDelete ==
                m_CurrentPlacementObject &&
                objectToDelete !=
                m_PlatformInstance)
            {
                if (m_PlacementManipulator != null)
                    m_PlacementManipulator.StopPlacement();

                m_CurrentPlacementObject = null;
                m_AwaitingConfirmation = false;
            }

            if (objectToDelete != m_PlatformInstance)
            {
                m_ConfirmedRandomObjects.Remove(
                    objectToDelete);
            }

            Destroy(objectToDelete);

            // Recalculate the stack on the next frame if a confirmed
            // object was deleted. This prevents stale height data.
            if (objectToDelete != m_PlatformInstance)
            {
                StartCoroutine(
                    RecalculateStackAfterDestroy());
            }
        }

        IEnumerator RecalculateStackAfterDestroy()
        {
            yield return null;

            RecalculateHighestConfirmedPoint();
        }

        void RecalculateHighestConfirmedPoint()
        {
            if (m_PlatformInstance == null)
            {
                ResetStackHeight();
                return;
            }

            Vector3 up =
                m_PlatformInstance.transform.up.sqrMagnitude > 0.0001f
                    ? m_PlatformInstance.transform.up.normalized
                    : Vector3.up;

            float highest = float.NegativeInfinity;
            bool foundRandomObject = false;

            // Only confirmed random objects participate in the stack.
            // Use collision bounds, not visual mesh bounds.
            for (int i = m_ConfirmedRandomObjects.Count - 1; i >= 0; --i)
            {
                GameObject candidate =
                    m_ConfirmedRandomObjects[i];

                if (candidate == null)
                {
                    m_ConfirmedRandomObjects.RemoveAt(i);
                    continue;
                }

                if (candidate == m_CurrentPlacementObject)
                    continue;

                float candidateHighest =
                    GetHighestMeshPoint(
                        candidate,
                        up);

                if (float.IsNegativeInfinity(candidateHighest))
                    continue;

                highest = Mathf.Max(
                    highest,
                    candidateHighest);

                foundRandomObject = true;
            }

            if (foundRandomObject)
            {
                m_HighestConfirmedPoint = highest;
                m_HasConfirmedRandomObject = true;
            }
            else
            {
                ResetStackHeight();
            }
        }

        // ============================================================
        // CLEAR
        // ============================================================

        public void ClearAllObjects()
        {
            StopAllCoroutines();

            if (m_PlacementManipulator != null)
                m_PlacementManipulator.StopPlacement();

            if (m_ObjectSpawner != null)
            {
                List<GameObject> children =
                    new List<GameObject>();

                foreach (Transform child in
                         m_ObjectSpawner.transform)
                {
                    children.Add(
                        child.gameObject);
                }

                foreach (GameObject child in children)
                {
                    Destroy(child);
                }
            }

            m_PlatformInstance = null;
            m_CurrentPlacementObject = null;
            m_PlatformConfirmed = false;
            m_AwaitingConfirmation = false;

            m_ConfirmedRandomObjects.Clear();

            ResetStackHeight();

            m_LockedPlatformInteractables.Clear();

            foreach (GameObject target in
                     new List<GameObject>(
                         m_StabilityDebugMarkers.Keys))
            {
                DestroyStabilityDebugMarker(target);
            }

            if (m_ObjectSpawner != null)
            {
                m_ObjectSpawner.spawnOptionIndex = 0;
                m_ObjectSpawner.spawnEnabled = true;
            }
        }

        void ResetStackHeight()
        {
            m_HighestConfirmedPoint =
                float.NegativeInfinity;

            m_HasConfirmedRandomObject = false;
        }

        // ============================================================
        // MENU
        // ============================================================

        public void HideMenu()
        {
            if (m_ObjectMenuAnimator != null)
            {
                m_ObjectMenuAnimator.SetBool(
                    "Show",
                    false);
            }

            m_ShowObjectMenu = false;

            AdjustARDebugMenuPosition();
        }

        // ============================================================
        // MODAL
        // ============================================================

        public void ShowHideModal()
        {
            if (m_ModalMenu == null)
                return;

            bool show =
                !m_ModalMenu.activeSelf;

            m_ShowOptionsModal = show;

            m_ModalMenu.SetActive(show);
        }

        // ============================================================
        // PLANES
        // ============================================================

        public void ShowHideDebugPlane()
        {
            m_VisualizePlanes =
                !m_VisualizePlanes;

            if (m_DebugPlaneSlider != null)
            {
                m_DebugPlaneSlider.value =
                    m_VisualizePlanes ? 1 : 0;
            }

            ChangePlaneVisibility(
                m_VisualizePlanes);
        }

        void ChangePlaneVisibility(
            bool visible)
        {
            foreach (ARPlane plane in m_ARPlanes)
            {
                if (m_ARPlaneMeshVisualizers.TryGetValue(
                        plane,
                        out var visualizer))
                {
                    visualizer.enabled =
                        m_UseARPlaneFading
                            ? true
                            : visible;
                }

                if (m_ARPlaneMeshVisualizerFaders.TryGetValue(
                        plane,
                        out var fader))
                {
                    if (m_UseARPlaneFading)
                    {
                        fader.visualizeSurfaces =
                            visible;
                    }
                }
            }
        }

        // ============================================================
        // DEBUG MENU
        // ============================================================

        public void ShowHideDebugMenu()
        {
            m_ShowDebugMenu =
                !m_ShowDebugMenu;

            if (m_DebugMenuSlider != null)
            {
                m_DebugMenuSlider.value =
                    m_ShowDebugMenu ? 1 : 0;
            }

            if (m_ARDebugMenu == null)
                return;

            m_ARDebugMenu.gameObject.SetActive(
                m_ShowDebugMenu);

            if (m_ShowDebugMenu)
                AdjustARDebugMenuPosition();
        }

        // ============================================================
        // DEBUG MENU POSITION
        // ============================================================

        void InitializeDebugMenuOffsets()
        {
            if (m_CreateButton != null &&
                m_CreateButton.TryGetComponent<RectTransform>(
                    out var buttonRect))
            {
                m_ObjectButtonOffset =
                    new Vector2(
                        0f,
                        buttonRect.anchoredPosition.y +
                        buttonRect.rect.height +
                        10f);
            }
            else
            {
                m_ObjectButtonOffset =
                    new Vector2(0f, 200f);
            }

            if (m_ObjectMenu != null &&
                m_ObjectMenu.TryGetComponent<RectTransform>(
                    out var menuRect))
            {
                m_ObjectMenuOffset =
                    new Vector2(
                        0f,
                        menuRect.anchoredPosition.y +
                        menuRect.rect.height +
                        10f);
            }
            else
            {
                m_ObjectMenuOffset =
                    new Vector2(0f, 345f);
            }
        }

        void AdjustARDebugMenuPosition()
        {
            if (m_ARDebugMenu == null)
                return;

            float screenWidthInInches =
                Screen.dpi > 0
                    ? Screen.width / Screen.dpi
                    : 10f;

            if (screenWidthInInches >= 5f)
                return;

            Vector2 menuOffset =
                m_ShowObjectMenu
                    ? m_ObjectMenuOffset
                    : m_ObjectButtonOffset;

            if (m_ARDebugMenu.toolbar.TryGetComponent<
                    RectTransform>(
                    out var toolbar))
            {
                toolbar.anchorMin =
                    new Vector2(0.5f, 0f);

                toolbar.anchorMax =
                    new Vector2(0.5f, 0f);

                toolbar.eulerAngles =
                    new Vector3(
                        toolbar.eulerAngles.x,
                        toolbar.eulerAngles.y,
                        90f);

                toolbar.anchoredPosition =
                    new Vector2(0f, 20f) +
                    menuOffset;
            }
        }

        // ============================================================
        // PLANE CHANGED
        // ============================================================

        void OnPlaneChanged(
            ARTrackablesChangedEventArgs<ARPlane>
                eventArgs)
        {
            foreach (ARPlane plane in eventArgs.added)
            {
                if (!m_ARPlanes.Contains(plane))
                    m_ARPlanes.Add(plane);

                if (plane.TryGetComponent<
                        ARPlaneMeshVisualizer>(
                        out var visualizer))
                {
                    if (!m_ARPlaneMeshVisualizers.ContainsKey(plane))
                    {
                        m_ARPlaneMeshVisualizers.Add(
                            plane,
                            visualizer);
                    }

                    if (!m_UseARPlaneFading)
                    {
                        visualizer.enabled =
                            m_VisualizePlanes;
                    }
                }

                if (!plane.TryGetComponent<
                        ARPlaneMeshVisualizerFader>(
                        out var fader))
                {
                    fader =
                        plane.gameObject.AddComponent<
                            ARPlaneMeshVisualizerFader>();
                }

                if (!m_ARPlaneMeshVisualizerFaders.ContainsKey(plane))
                {
                    m_ARPlaneMeshVisualizerFaders.Add(
                        plane,
                        fader);
                }

                fader.visualizeSurfaces =
                    m_VisualizePlanes;
            }

            foreach (var removed in eventArgs.removed)
            {
                ARPlane plane = removed.Value;

                if (plane == null)
                    continue;

                m_ARPlanes.Remove(plane);
                m_ARPlaneMeshVisualizers.Remove(plane);
                m_ARPlaneMeshVisualizerFaders.Remove(plane);
            }
        }
    }
}

