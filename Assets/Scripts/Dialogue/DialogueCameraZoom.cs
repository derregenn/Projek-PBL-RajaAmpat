using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class DialogueCameraZoom : MonoBehaviour
{
    public static DialogueCameraZoom Instance { get; private set; }

    [Header("Cinemachine Cameras")]
    [Tooltip("Kamera eksplorasi / biasa yang mengikuti Player (Priority default 10)")]
    [SerializeField] private CinemachineCamera gameplayCamera;

    [Tooltip("Kamera dialog saat berbicara dengan NPC (Priority default 0, saat dialog 20)")]
    [SerializeField] private CinemachineCamera dialogueCamera;

    [Tooltip("Kamera objektif / cutscene / pemandangan (Priority default 0, saat event 30)")]
    [SerializeField] private CinemachineCamera objectiveCamera;

    [Header("Priority Settings")]
    [SerializeField] private int gameplayPriority = 10;
    [SerializeField] private int dialoguePriority = 20;
    [SerializeField] private int objectivePriority = 0;

    [Header("Zoom Settings")]
    [SerializeField] private float normalSize = 3.0f;
    [SerializeField] private float zoomSize = 2.0f;
    [SerializeField] private float zoomDuration = 0.5f;
    private Coroutine zoomCoroutine;

    private void Awake()
    {
        Instance = this;
        EnsureCameraReferences();
    }

    private void Start()
    {
        EnsureCameraReferences();
        InitializeCameraPriorities();
    }

    public void EnsureCameraReferences()
    {
        // 1. Gameplay Camera (Kamera biasa yang mengikuti player)
        if (gameplayCamera == null)
        {
            GameObject go = GameObject.Find("CinemachineCamera");
            if (go != null) gameplayCamera = go.GetComponent<CinemachineCamera>();
        }

        // 2. Dialogue Camera (Kamera dialog zoom ke NPC)
        if (dialogueCamera == null)
        {
            GameObject go = GameObject.Find("CinemachineCameraDialogue");
            if (go != null) dialogueCamera = go.GetComponent<CinemachineCamera>();
        }

        // 3. Objective Camera (Kamera cutscene/objektif/pemandangan)
        // Perbaiki jika objectiveCamera salah tunjuk ke dialogueCamera atau masih kosong
        if (objectiveCamera == null || objectiveCamera == dialogueCamera)
        {
            objectiveCamera = null;
            string[] possibleObjectiveNames = new string[] {
                "ObjectiveCamera",
                "Cam_ViewTambang",
                "Cam_ViewPerahu",
                "CinemachineCameraPenyu"
            };

            foreach (string objName in possibleObjectiveNames)
            {
                GameObject go = GameObject.Find(objName);
                if (go != null)
                {
                    CinemachineCamera cam = go.GetComponent<CinemachineCamera>();
                    if (cam != null)
                    {
                        objectiveCamera = cam;
                        break;
                    }
                }
            }
        }

        // 4. Pastikan gameplayCamera selalu melacak Player (atau Boat di cutscene perahu) jika TrackingTarget masih kosong
        if (gameplayCamera != null && gameplayCamera.Target.TrackingTarget == null)
        {
            try
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                bool isBoatCutscene = sceneName.IndexOf("Cutscene", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      sceneName.IndexOf("Boat", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (isBoatCutscene)
                {
                    BoatBobbing boat = FindFirstObjectByType<BoatBobbing>();
                    if (boat != null) gameplayCamera.Target.TrackingTarget = boat.transform;
                }
                else
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player == null) player = GameObject.Find("Player");
                    if (player != null) gameplayCamera.Target.TrackingTarget = player.transform;
                }
            }
            catch { }
        }
    }

    public void InitializeCameraPriorities()
    {
        // Kamera gameplay aktif saat game dimulai (Priority 10)
        if (gameplayCamera != null)
        {
            gameplayCamera.Priority.Value = gameplayPriority;
        }

        // Kamera dialogue standby (Priority 0)
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = 0;
        }

        // 3. Kamera objective standby (Priority 0, hanya aktif saat event/cutscene)
        if (objectiveCamera != null && objectiveCamera != gameplayCamera)
        {
            objectiveCamera.Priority.Value = 0;
        }

        // Pastikan SEMUA kamera non-gameplay lain di scene standby pada prioritas 0
        CinemachineCamera[] allCams = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in allCams)
        {
            if (c == null) continue;
            if (c != gameplayCamera)
            {
                c.Priority.Value = 0;
            }
        }
    }

    // =========================================================
    // PUBLIC CONTROL METHODS (Untuk ganti-ganti kamera secara bersih)
    // =========================================================

    public void SwitchToGameplay()
    {
        EnsureCameraReferences();
        if (dialogueCamera != null) dialogueCamera.Priority.Value = 0;
        if (objectiveCamera != null) objectiveCamera.Priority.Value = 0;
        if (gameplayCamera != null) gameplayCamera.Priority.Value = gameplayPriority;
    }

    public void SwitchToDialogue(Transform targetNPC = null, float targetOrthoSize = -1f)
    {
        EnsureCameraReferences();
        if (dialogueCamera != null)
        {
            if (targetNPC != null)
            {
                dialogueCamera.Target.TrackingTarget = targetNPC;
            }
            if (targetOrthoSize > 0f)
            {
                var lens = dialogueCamera.Lens;
                lens.OrthographicSize = targetOrthoSize;
                dialogueCamera.Lens = lens;
            }
            dialogueCamera.Priority.Value = dialoguePriority;
        }
    }

    public void SwitchToObjective(CinemachineCamera customObjective = null)
    {
        EnsureCameraReferences();
        CinemachineCamera targetObj = customObjective != null ? customObjective : objectiveCamera;
        if (targetObj != null)
        {
            targetObj.Priority.Value = 30;
        }
    }

    public void ResetObjective(CinemachineCamera customObjective = null)
    {
        CinemachineCamera targetObj = customObjective != null ? customObjective : objectiveCamera;
        if (targetObj != null)
        {
            targetObj.Priority.Value = 0;
        }
        SwitchToGameplay();
    }

    public void ZoomIn()
    {
        EnsureCameraReferences();
        if (dialogueCamera == null || gameplayCamera == null)
            return;

        dialogueCamera.Priority.Value = dialoguePriority;

        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);

        zoomCoroutine = StartCoroutine(ChangeZoom(zoomSize));
    }

    public void ZoomOut()
    {
        EnsureCameraReferences();
        if (dialogueCamera == null || gameplayCamera == null)
            return;

        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);

        zoomCoroutine = StartCoroutine(ZoomOutCoroutine());
    }

    private IEnumerator ChangeZoom(float targetSize)
    {
        float startSize = dialogueCamera.Lens.OrthographicSize;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;

            float newSize = Mathf.Lerp(
                startSize,
                targetSize,
                elapsed / zoomDuration
            );

            var lens = dialogueCamera.Lens;
            lens.OrthographicSize = newSize;
            dialogueCamera.Lens = lens;

            yield return null;
        }

        var finalLens = dialogueCamera.Lens;
        finalLens.OrthographicSize = targetSize;
        dialogueCamera.Lens = finalLens;
    }

    private IEnumerator ZoomOutCoroutine()
    {
        float startSize = dialogueCamera.Lens.OrthographicSize;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;

            float newSize = Mathf.Lerp(
                startSize,
                normalSize,
                elapsed / zoomDuration
            );

            var lens = dialogueCamera.Lens;
            lens.OrthographicSize = newSize;
            dialogueCamera.Lens = lens;

            yield return null;
        }

        var finalLens = dialogueCamera.Lens;
        finalLens.OrthographicSize = normalSize;
        dialogueCamera.Lens = finalLens;

        dialogueCamera.Priority.Value = 0;
        if (gameplayCamera != null)
            gameplayCamera.Priority.Value = gameplayPriority;
    }
}
