using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("Drag PauseMenuPanel ke sini")]
    public GameObject pauseMenuUI;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        Instance = this;
        isPaused = false;
        Time.timeScale = 1f;
        FindAndSetupPauseMenu();
        DeactivateAllPauseUIInActiveScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isPaused = false;
        Time.timeScale = 1f;
        FindAndSetupPauseMenu();
        ResumeGame();
    }

    private void Start()
    {
        FindAndSetupPauseMenu();
        ResumeGame();
    }

    public void FindAndSetupPauseMenu()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        pauseMenuUI = FindPauseMenu(currentScene);
        ResolvePauseMenuReference();

        if (pauseMenuUI != null)
        {
            NormalizePauseCanvasLayout(pauseMenuUI);

            // Pastikan binder terpasang agar tombol-tombol selalu terkoneksi
            if (pauseMenuUI.GetComponent<PauseMenuBinder>() == null)
            {
                pauseMenuUI.AddComponent<PauseMenuBinder>();
            }

            Transform parentCanvas = pauseMenuUI.transform.parent;
            if (parentCanvas != null && parentCanvas.GetComponent<PauseMenuBinder>() == null)
            {
                parentCanvas.gameObject.AddComponent<PauseMenuBinder>();
            }
        }
    }

    private GameObject FindPauseMenu(Scene scene)
    {
        if (!scene.isLoaded) return null;

        GameObject pauseCanvas = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in descendants)
            {
                if (child.name == "PauseMenuUI" || child.name == "PauseMenuPanel")
                    return child.gameObject;

                if (child.name == "Canvas_Pause")
                    pauseCanvas = child.gameObject;
            }
        }

        return pauseCanvas;
    }

    private void ResolvePauseMenuReference()
    {
        if (pauseMenuUI == null) return;

        if (pauseMenuUI.name == "Canvas" || pauseMenuUI.name == "Canvas_HUD")
        {
            pauseMenuUI = FindPauseMenu(SceneManager.GetActiveScene());
            if (pauseMenuUI == null) return;
        }

        if (pauseMenuUI.GetComponent<Canvas>() == null)
            return;

        Transform[] descendants = pauseMenuUI.GetComponentsInChildren<Transform>(true);
        Transform pausePanel = null;

        foreach (Transform child in descendants)
        {
            if (child.name == "PauseMenuUI" || child.name == "PauseMenuPanel")
            {
                pausePanel = child;
                break;
            }
        }

        if (pausePanel == null)
        {
            if (pauseMenuUI.name == "Canvas_Pause")
                return;

            pauseMenuUI = null;
            return;
        }

        pauseMenuUI = pausePanel.gameObject;
    }

    private void NormalizePauseCanvasLayout(GameObject target)
    {
        if (target == null) return;

        Canvas canvas = target.GetComponentInParent<Canvas>(true);
        if (canvas != null && canvas.name.Contains("Canvas_Pause"))
        {
            UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        Transform scope = canvas != null ? canvas.transform : target.transform;
        foreach (Transform t in scope.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("TitleBackground") || t.name == "ControlHints" || t.name == "ControlsPanel")
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // Jika GameManager ada, biarkan GameManager yang menangani input Escape untuk menghindari bentrok ganda
        if (GameManager.Instance != null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void SetPausedState(bool paused)
    {
        isPaused = paused;
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        DeactivateAllPauseUIInActiveScene();

        if (pauseMenuUI == null || pauseMenuUI.name == "Canvas" || pauseMenuUI.name == "Canvas_HUD")
        {
            FindAndSetupPauseMenu();
        }

        if (pauseMenuUI != null)
        {
            NormalizePauseCanvasLayout(pauseMenuUI);
            Transform parentCanvas = pauseMenuUI.transform.parent;
            if (parentCanvas != null && parentCanvas.GetComponent<Canvas>() != null)
            {
                parentCanvas.gameObject.SetActive(true);
            }
            pauseMenuUI.SetActive(true);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPausedState(true);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuUI != null && pauseMenuUI.name != "Canvas" && pauseMenuUI.name != "Canvas_HUD")
        {
            pauseMenuUI.SetActive(false);
            Transform parentCanvas = pauseMenuUI.transform.parent;
            if (parentCanvas != null && parentCanvas.name.Contains("Canvas_Pause"))
            {
                parentCanvas.gameObject.SetActive(false);
            }
        }

        // Pastikan seluruh elemen pause di scene saat ini dimatikan
        DeactivateAllPauseUIInActiveScene();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPausedState(false);
        }
    }

    private void DeactivateAllPauseUIInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.isLoaded)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in descendants)
                {
                    if (child.name == "PauseMenuUI" || child.name == "PauseMenuPanel")
                    {
                        child.gameObject.SetActive(false);
                    }
                    else if (child.name == "Canvas_Pause")
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Matikan Canvas_Pause nyasar di DontDestroyOnLoad agar tidak double
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c != null && c.name == "Canvas_Pause" && c.gameObject.scene != scene)
            {
                c.gameObject.SetActive(false);
            }
        }

        // Matikan prefab pause hijau lama (Assets/Prefabs_Reg/PauseMenu.prefab) jika ada di scene
        foreach (PauseMenu legacyPause in FindObjectsByType<PauseMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (legacyPause != null)
            {
                if (legacyPause.container != null) legacyPause.container.SetActive(false);
                legacyPause.gameObject.SetActive(false);
            }
        }
    }
}
