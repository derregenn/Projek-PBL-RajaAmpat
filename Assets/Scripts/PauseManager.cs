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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        FindAndSetupPauseMenu();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
        if (pauseMenuUI == null || pauseMenuUI.GetComponent<Canvas>() == null)
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

            Debug.LogError(
                "[PauseManager] pauseMenuUI points to a Canvas, but no PauseMenuUI or PauseMenuPanel child was found.",
                this);
            pauseMenuUI = null;
            return;
        }

        pauseMenuUI = pausePanel.gameObject;
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

        if (pauseMenuUI == null)
        {
            FindAndSetupPauseMenu();
        }

        if (pauseMenuUI != null)
        {
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

        if (pauseMenuUI != null)
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
        if (!scene.isLoaded) return;

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
}
