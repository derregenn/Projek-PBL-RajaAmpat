using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("Drag PauseMenuPanel ke sini")]
    public GameObject pauseMenuUI;

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResolvePauseMenuReference();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pauseMenuUI = FindPauseMenu(scene);
        ResolvePauseMenuReference();
        ResumeGame();
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

    private void Start()
    {
        ResolvePauseMenuReference();
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
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
                "[PauseManager] pauseMenuUI points to a Canvas, but no PauseMenuUI or PauseMenuPanel child was found. Assign the pause panel instead.",
                this);
            pauseMenuUI = null;
            return;
        }

        Debug.LogWarning(
            "[PauseManager] pauseMenuUI was assigned to a Canvas. Using its child '" + pausePanel.name + "' instead.",
            this);
        pauseMenuUI = pausePanel.gameObject;
    }

    private void Update()
    {
        if (GameManager.Instance != null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }
}