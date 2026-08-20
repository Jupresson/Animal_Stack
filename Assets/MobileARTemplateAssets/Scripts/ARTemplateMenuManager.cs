using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
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
        [Tooltip(
            "Distance between preview object and platform.")]
        float m_PlatformTopGap = 0.02f;


        [SerializeField]
        [Tooltip(
            "Delay before spawning next object.")]
        float m_NextObjectDelay = 0.5f;


        bool m_PlatformConfirmed;

        bool m_AwaitingConfirmation;

        GameObject m_PlatformInstance;

        GameObject m_CurrentPlacementObject;

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
                m_CurrentPlacementObject
                    .transform.localScale =
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


            bool spawned =
                m_ObjectSpawner.TrySpawnObjectOnTopOf(
                    m_PlatformInstance,
                    m_PlatformTopGap);


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

            if (m_PlacementManipulator != null)
            {
                m_PlacementManipulator.StopPlacement();
            }


            SetPhysicsState(
                m_CurrentPlacementObject,
                true);


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


            m_AwaitingConfirmation =
                true;


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
                    spawnedObject
                        .transform
                        .localScale;


                if (m_PlacementManipulator != null)
                {
                    m_PlacementManipulator.BeginPlacement(
                        spawnedObject,
                        m_PlatformInstance,
                        m_PlatformTopGap);
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
        // PHYSICS
        // ============================================================

        void SetPhysicsState(
            GameObject target,
            bool enableGravity)
        {
            if (target == null)
                return;


            Rigidbody[] rigidbodies =
                target.GetComponentsInChildren<
                    Rigidbody>(true);


            foreach (Rigidbody rb in rigidbodies)
            {
                rb.useGravity =
                    enableGravity;


                rb.isKinematic =
                    !enableGravity;
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
                    interactable.isSelected &&
                    interactable.interactionManager != null)
                {
                    interactable.interactionManager
                        .CancelInteractableSelection(
                            selectInteractable);
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
                objectIndex <
                m_ObjectSpawner.objectPrefabs.Count)
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


            Destroy(objectToDelete);
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


            m_LockedPlatformInteractables.Clear();


            if (m_ObjectSpawner != null)
            {
                m_ObjectSpawner.spawnOptionIndex = 0;

                m_ObjectSpawner.spawnEnabled = true;
            }
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


            m_ShowOptionsModal =
                show;


            m_ModalMenu.SetActive(
                show);
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
                if (m_ARPlaneMeshVisualizers
                    .TryGetValue(
                        plane,
                        out var visualizer))
                {
                    visualizer.enabled =
                        m_UseARPlaneFading
                            ? true
                            : visible;
                }


                if (m_ARPlaneMeshVisualizerFaders
                    .TryGetValue(
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
            {
                AdjustARDebugMenuPosition();
            }
        }


        // ============================================================
        // DEBUG MENU POSITION
        // ============================================================

        void InitializeDebugMenuOffsets()
        {
            if (m_CreateButton != null &&
                m_CreateButton.TryGetComponent<
                    RectTransform>(
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
                    new Vector2(
                        0f,
                        200f);
            }


            if (m_ObjectMenu != null &&
                m_ObjectMenu.TryGetComponent<
                    RectTransform>(
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
                    new Vector2(
                        0f,
                        345f);
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


            if (m_ARDebugMenu.toolbar
                .TryGetComponent<
                    RectTransform>(
                    out var toolbar))
            {
                toolbar.anchorMin =
                    new Vector2(
                        0.5f,
                        0f);

                toolbar.anchorMax =
                    new Vector2(
                        0.5f,
                        0f);

                toolbar.eulerAngles =
                    new Vector3(
                        toolbar.eulerAngles.x,
                        toolbar.eulerAngles.y,
                        90f);

                toolbar.anchoredPosition =
                    new Vector2(
                        0f,
                        20f) +
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
            foreach (ARPlane plane
                     in eventArgs.added)
            {
                if (!m_ARPlanes.Contains(plane))
                    m_ARPlanes.Add(plane);


                if (plane.TryGetComponent<
                        ARPlaneMeshVisualizer>(
                        out var visualizer))
                {
                    if (!m_ARPlaneMeshVisualizers
                        .ContainsKey(plane))
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


                if (!m_ARPlaneMeshVisualizerFaders
                    .ContainsKey(plane))
                {
                    m_ARPlaneMeshVisualizerFaders.Add(
                        plane,
                        fader);
                }


                fader.visualizeSurfaces =
                    m_VisualizePlanes;
            }


            foreach (var removed in
                     eventArgs.removed)
            {
                ARPlane plane =
                    removed.Value;


                if (plane == null)
                    continue;


                m_ARPlanes.Remove(
                    plane);


                m_ARPlaneMeshVisualizers.Remove(
                    plane);


                m_ARPlaneMeshVisualizerFaders.Remove(
                    plane);
            }
        }
    }
}